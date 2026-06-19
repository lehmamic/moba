using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using MOBA.Game.Models;
using Silk.NET.Maths;

namespace MOBA.Game.Components;

/// <summary>
/// Coordination + execution of an actor's basic attack — the central
/// component the whole combat loop lives on. Holds the engaged enemy
/// (<see cref="CurrentTarget"/>), the static per-actor stats
/// (<see cref="Profile"/>), and the bookkeeping the minion aggro
/// priority list needs (<see cref="LastTarget"/> / <see cref="LastAttackAt"/>
/// — "did this champion basic-attack one of my allies recently?").
///
/// <para>
/// Each tick the component drives the entire combat episode:
/// </para>
/// <list type="bullet">
///   <item>Dead targets are cleared automatically (sticky aggro never sticks to a corpse).</item>
///   <item>On a null → set transition the owner's <see cref="MoveTargetComponent"/> (if any)
///         has its lane queue stashed via <see cref="MoveTargetComponent.StashPath"/>.</item>
///   <item>While out of range a navmesh-pathed chase corridor is pushed onto the mover,
///         re-computed when the target drifts past a small threshold so the actor stays
///         on walkable ground.</item>
///   <item>While in range the mover's live queue is cleared so the actor stands still,
///         faces the target, and fires on cadence.</item>
///   <item>Damage applies instantly for melee attackers, or is queued and drained when
///         <see cref="AttackProfile.ProjectileTravel"/> elapses for ranged ones —
///         a tiny "projectile in flight" model without a projectile actor.</item>
///   <item>On a set → null transition the lane queue is popped back via
///         <see cref="MoveTargetComponent.RestorePath"/>.</item>
/// </list>
///
/// <para>
/// <b>Component order matters.</b> Attach <c>AttackComponent</c> on the actor
/// <b>before</b> <see cref="MoveTargetComponent"/> so its decisions land on the same
/// tick the mover walks — otherwise the mover walks last tick's queue and the
/// chase / clear lags by one frame.
/// </para>
/// </summary>
public sealed class AttackComponent : Component
{
    /// <summary>Re-path the chase corridor when the target has drifted more than this many world units from the position we last pathed to.</summary>
    private const float ChaseRepathThreshold = 0.5f;

    private readonly NavMesh? _navMesh;
    private readonly List<Vector3D<float>> _scratch = new();
    private readonly Queue<ScheduledHit> _pendingHits = new();
    private Actor? _previousTarget;
    private Vector3D<float> _lastChasePathTarget;
    private double _nextAttackAt;

    public AttackComponent(Actor owner, AttackProfile profile, NavMesh? navMesh = null) : base(owner)
    {
        Profile = profile;
        _navMesh = navMesh;
    }

    public AttackProfile Profile { get; }

    /// <summary>The enemy the actor is currently engaging, or null while idle. Written by aggro logic / player input; read by the component itself.</summary>
    public Actor? CurrentTarget { get; set; }

    /// <summary>The most recent target this actor actually fired at. Used by minion aggro priority 2.</summary>
    public Actor? LastTarget { get; private set; }

    /// <summary>Server time of the last attack-tick that fired. Used by minion aggro priority 2 (within-window check).</summary>
    public double LastAttackAt { get; private set; }

    public override void OnUpdate(GameTime time)
    {
        // Land any projectile damage whose travel timer has expired — runs
        // regardless of current-target state so an in-flight hit still
        // connects even when the attacker has already moved on.
        DrainPendingHits(time.TotalSeconds);

        var target = CurrentTarget;
        if (target is not null && IsDead(target))
        {
            // A dead actor is never a sticky target. The aggro layer owns the
            // sticky / drop-range rules; this just guarantees corpse-locking
            // is impossible end-to-end.
            CurrentTarget = null;
            target = null;
        }

        var move = Owner.GetComponent<MoveTargetComponent>();
        if (target is null && _previousTarget is not null)
        {
            move?.RestorePath();
        }
        else if (target is not null && _previousTarget is null)
        {
            move?.StashPath();
        }
        _previousTarget = target;

        if (target is null)
        {
            return;
        }

        var delta = target.Transform.Position - Owner.Transform.Position;
        delta.Y = 0f;
        var distance = delta.Length;

        if (distance > Profile.Range)
        {
            // Out of range — make sure the chase corridor is current.
            // Stationary attackers (towers, nexus) have no mover; they simply
            // do nothing this tick and wait for the target to walk into range.
            if (move is not null && (!move.HasPath || ShouldRepath(target.Transform.Position)))
            {
                PushChasePath(move, target.Transform.Position);
            }
            return;
        }

        // In range — stand still, face the target, fire on cadence.
        move?.ClearPath();
        FaceTowards(target);
        if (time.TotalSeconds >= _nextAttackAt)
        {
            FireAttack(target, time.TotalSeconds);
        }
    }

    private static bool IsDead(Actor target) =>
        target.GetComponent<HealthComponent>() is { Current: <= 0f };

    private bool ShouldRepath(Vector3D<float> targetPos) =>
        (targetPos - _lastChasePathTarget).LengthSquared > ChaseRepathThreshold * ChaseRepathThreshold;

    private void PushChasePath(MoveTargetComponent move, Vector3D<float> targetPos)
    {
        if (_navMesh is not null && _navMesh.TryFindPath(Owner.Transform.Position, targetPos, _scratch))
        {
            // TryFindPath emits the snapped start as the first corner — skip it
            // so the live queue begins with the first real "go-to" point.
            var corridor = new List<Vector3D<float>>(Math.Max(_scratch.Count - 1, 0));
            for (var i = 1; i < _scratch.Count; i++)
            {
                corridor.Add(_scratch[i]);
            }
            if (corridor.Count > 0)
            {
                move.SetPath(corridor);
                _lastChasePathTarget = targetPos;
                return;
            }
        }

        // No navmesh, or pathing collapsed to just the snapped start (target shares
        // our poly). Walk a single straight-line waypoint and hope for the best —
        // this is the fallback for smoke scenes / tests without a navmesh.
        move.SetPath([targetPos]);
        _lastChasePathTarget = targetPos;
    }

    private void FireAttack(Actor target, double now)
    {
        LastTarget = target;
        LastAttackAt = now;
        _nextAttackAt = now + Profile.Cooldown;

        if (Profile.ProjectileTravel <= 0f)
        {
            ApplyDamageIfAlive(target, Profile.Damage);
            return;
        }

        // A single actor only ever has one Profile.ProjectileTravel, so the
        // queue is naturally ordered by ApplyAt — FIFO drain is correct.
        _pendingHits.Enqueue(new ScheduledHit(target, Profile.Damage, now + Profile.ProjectileTravel));
    }

    private void DrainPendingHits(double now)
    {
        while (_pendingHits.Count > 0 && _pendingHits.Peek().ApplyAt <= now)
        {
            var hit = _pendingHits.Dequeue();
            ApplyDamageIfAlive(hit.Target, hit.Amount);
        }
    }

    private static void ApplyDamageIfAlive(Actor target, float amount)
    {
        if (target.GetComponent<HealthComponent>() is { Current: > 0f } health)
        {
            health.ApplyDamage(amount);
        }
    }

    // The actor's bind-pose forward is +X (see Transform XML doc); we want a
    // Y-axis rotation that takes +X to (dx, 0, dz). Silk's Y-rotation in
    // row-vector form sends (1, 0, 0) → (cos yaw, 0, -sin yaw), so
    // yaw = atan2(-deltaZ, deltaX).
    private void FaceTowards(Actor target)
    {
        var look = target.Transform.Position - Owner.Transform.Position;
        look.Y = 0f;
        if (look.X == 0f && look.Z == 0f)
        {
            return;
        }

        var yaw = MathF.Atan2(-look.Z, look.X);
        Owner.Transform.Rotation = Quaternion<float>.CreateFromYawPitchRoll(yaw, 0f, 0f);
    }

    private readonly record struct ScheduledHit(Actor Target, float Amount, double ApplyAt);
}
