using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using Silk.NET.Maths;

namespace MOBA.Game.Components;

/// <summary>
/// Movement intent on an actor: the navmesh-following waypoint queue, walking
/// speed, and the destination marker the server has linked to this move. The
/// per-tick movement physics lives here in <see cref="OnUpdate"/> — the actor's
/// <c>UpdateComponents</c> ticks this component and walks the owner's
/// <see cref="Actor.Transform"/> along the queue. Networking (broadcasting
/// position updates, despawning the marker on arrival, sending the path
/// snapshot to all clients) is the concern of <c>MovementSystem</c>,
/// which inspects this component's state in its
/// <c>IPostUpdateSystem.OnPostUpdate</c> after the sim has run.
///
/// <para>
/// <b>Chase mode.</b> When the owner has an <see cref="AttackComponent"/>
/// with a <c>CurrentTarget</c> set, the mover switches modes: it stashes the
/// current lane queue (<see cref="StashPath"/>), walks a straight line toward
/// the target's live world position, and stops once it is within
/// <c>AttackComponent.Profile.Range</c> so the attack-tick can fire. When the
/// target clears the lane queue is popped back (<see cref="RestorePath"/>) and
/// the actor resumes from exactly the waypoint that was next when combat
/// started. Single-slot push/pop is enough for our use case — one lane goal
/// interrupted by one combat episode at a time.
/// </para>
/// </summary>
public sealed class MoveTargetComponent : Component
{
    public const float ArrivalThreshold = 0.1f;

    private readonly Queue<Vector3D<float>> _path = new();
    private Queue<Vector3D<float>>? _stashedPath;
    private Actor? _previousTarget;

    public MoveTargetComponent(Actor owner, float speed = 10f) : base(owner) =>
        Speed = speed;

    public uint? MarkerId { get; set; }

    public float Speed { get; }

    /// <summary>True while the queue has at least one waypoint left to walk to.</summary>
    public bool HasPath => _path.Count > 0;

    /// <summary>True while the live queue is empty and a stashed lane queue is waiting to be resumed.</summary>
    public bool HasStashedPath => _stashedPath is not null;

    /// <summary>
    /// Replaces the current waypoint queue with <paramref name="waypoints"/>.
    /// Used by <c>MovementSystem.HandleMoveCommand</c> after a successful
    /// <c>NavMesh.TryFindPath</c>.
    /// </summary>
    public void SetPath(IReadOnlyList<Vector3D<float>> waypoints)
    {
        _path.Clear();
        foreach (var w in waypoints)
        {
            _path.Enqueue(w);
        }
    }

    /// <summary>
    /// Copies the live waypoint queue into a single-slot stash and clears the
    /// live queue. Called automatically on a null → set <see cref="AttackComponent.CurrentTarget"/>
    /// transition so the lane goal survives the combat episode. A second
    /// stash before the first restore is a programmer error and overwrites
    /// silently — single-slot by design.
    /// </summary>
    public void StashPath()
    {
        _stashedPath = new Queue<Vector3D<float>>(_path);
        _path.Clear();
    }

    /// <summary>
    /// Pops the stashed lane queue back into the live queue, picking up
    /// lane-walking from exactly the waypoint that was next when combat
    /// started. No-op if nothing was stashed.
    /// </summary>
    public void RestorePath()
    {
        if (_stashedPath is null)
        {
            return;
        }
        _path.Clear();
        foreach (var w in _stashedPath)
        {
            _path.Enqueue(w);
        }
        _stashedPath = null;
    }

    public override void OnUpdate(GameTime time)
    {
        // Combat target acts as a higher-priority destination — when it
        // appears we stash the lane queue and chase; when it disappears we
        // pop the stash and resume the lane.
        var attack = Owner.GetComponent<AttackComponent>();
        var target = attack?.CurrentTarget;
        if (target is null && _previousTarget is not null)
        {
            RestorePath();
        }
        else if (target is not null && _previousTarget is null)
        {
            StashPath();
        }
        _previousTarget = target;

        if (target is not null)
        {
            ChaseTarget(target, attack!.Profile.Range, time.DeltaSeconds);
            return;
        }

        if (_path.Count == 0)
        {
            return;
        }
        WalkLane(time.DeltaSeconds);
    }

    private void ChaseTarget(Actor target, float stopRange, float deltaSeconds)
    {
        var position = Owner.Transform.Position;
        var delta = target.Transform.Position - position;
        delta.Y = 0f;
        var distance = delta.Length;

        // Already in attack range — stand still and face the target so the
        // attack-tick can fire on cadence.
        if (distance <= stopRange)
        {
            FaceDirection(delta);
            return;
        }

        var step = Speed * deltaSeconds;
        var advance = MathF.Min(step, distance - stopRange);
        position += Vector3D.Normalize(delta) * advance;
        Owner.Transform.Position = position;
        FaceDirection(delta);
    }

    private void WalkLane(float deltaSeconds)
    {
        // Walk multiple waypoints per tick if speed * dt overshoots the next
        // corner — otherwise tight corner sequences would visibly stutter as
        // each waypoint takes a full tick to consume.
        var remaining = Speed * deltaSeconds;
        var position = Owner.Transform.Position;
        while (_path.Count > 0 && remaining > 0f)
        {
            var next = _path.Peek();
            var delta = next - position;
            var distance = delta.Length;
            if (distance <= ArrivalThreshold || remaining >= distance)
            {
                position = next;
                _path.Dequeue();
                remaining -= distance;
            }
            else
            {
                position += Vector3D.Normalize(delta) * remaining;
                remaining = 0f;
            }
        }
        Owner.Transform.Position = position;

        if (_path.Count > 0)
        {
            FaceDirection(_path.Peek() - position);
        }
    }

    // The actor's bind-pose forward is +X (see Transform XML doc); we want a
    // Y-axis rotation that takes +X to (dx, 0, dz). Silk's Y-rotation in
    // row-vector form sends (1, 0, 0) → (cos yaw, 0, -sin yaw), so
    // yaw = atan2(-deltaZ, deltaX).
    private void FaceDirection(Vector3D<float> look)
    {
        if (look.X == 0f && look.Z == 0f)
        {
            return;
        }
        var yaw = MathF.Atan2(-look.Z, look.X);
        Owner.Transform.Rotation = Quaternion<float>.CreateFromYawPitchRoll(yaw, 0f, 0f);
    }
}
