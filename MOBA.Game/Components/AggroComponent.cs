using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using MOBA.Game.Actors;
using MOBA.Game.Models;

namespace MOBA.Game.Components;

/// <summary>
/// Picks the actor's basic-attack target by walking the scene each tick and
/// running LoL's 5-step minion-aggro priority list. The picked target is
/// written into <c>Owner.GetComponent&lt;AttackComponent&gt;().CurrentTarget</c>;
/// the attack component does the rest (chase, fire, damage).
///
/// <para><b>Priority list</b> (lowest number = highest priority):</para>
/// <list type="number">
///   <item>Enemy minion currently engaging one of my allied champions in range.</item>
///   <item>Enemy champion who basic-attacked an allied champion within
///         <see cref="AggroProfile.DefendAllyWindow"/> seconds.</item>
///   <item>Closest enemy minion.</item>
///   <item>Closest enemy champion.</item>
///   <item>Closest enemy structure (minion aggro only — masked off on towers).</item>
/// </list>
///
/// <para>
/// <b>Sticky targeting</b>: once <see cref="AttackComponent.CurrentTarget"/>
/// is set, only priorities 1 and 2 preempt it. While the current target is
/// alive and inside <see cref="AggroProfile.DropRange"/>, candidates in
/// priority 3 / 4 / 5 are ignored — that's the LoL rule that stops minions
/// from oscillating between equally-close enemies.
/// </para>
///
/// <para>
/// <b>Component order matters.</b> Attach <c>AggroComponent</c> on the actor
/// <b>before</b> <see cref="AttackComponent"/> so the target slot is written
/// for the same tick the attack component reads it.
/// </para>
/// </summary>
public sealed class AggroComponent : Component
{
    private readonly Scene _scene;

    public AggroComponent(Actor owner, AggroProfile profile, Scene scene) : base(owner)
    {
        Profile = profile;
        _scene = scene;
    }

    public AggroProfile Profile { get; }

    public override void OnUpdate(GameTime time)
    {
        var attack = Owner.GetComponent<AttackComponent>();
        var myTeam = Owner.GetComponent<TeamComponent>()?.Team;
        if (attack is null || myTeam is null)
        {
            // Neutral actors never aggro — minions / towers / champions always
            // wear a TeamComponent, so a null myTeam is the smoke-test fallback.
            return;
        }

        var locked = IsStillEngaged(attack.CurrentTarget);
        var best = PickBestCandidate(myTeam, time.TotalSeconds, locked);

        if (locked)
        {
            // Sticky: only flip on a priority-1 / priority-2 trigger.
            if (best.Actor is not null && best.Priority <= 2)
            {
                attack.CurrentTarget = best.Actor;
            }

            return;
        }

        // Free pick (or clear if nothing's in range).
        attack.CurrentTarget = best.Actor;
    }

    private Candidate PickBestCandidate(string myTeam, double now, bool locked)
    {
        var acqSq = Profile.AcquisitionRange * Profile.AcquisitionRange;
        var best = Candidate.None;

        foreach (var actor in _scene.Actors)
        {
            if (!TryQualifyEnemy(actor, myTeam, acqSq, out var kind, out var distSq))
            {
                continue;
            }

            var priority = ResolvePriority(actor, kind, myTeam, now);

            // Sticky: skip everything that couldn't preempt a still-engaged target.
            if (locked && priority > 2)
            {
                continue;
            }

            if (priority < best.Priority || (priority == best.Priority && distSq < best.DistanceSq))
            {
                best = new Candidate(actor, priority, distSq);
            }
        }

        return best;
    }

    /// <summary>
    /// Bundles the guard clauses that filter the scene scan down to "alive
    /// enemy of an allowed kind, inside acquisition range." Yields the kind
    /// + squared distance so the caller can rank without recomputing.
    /// </summary>
    private bool TryQualifyEnemy(Actor actor, string myTeam, float acqSq, out TargetKind kind, out float distSq)
    {
        kind = TargetKind.None;
        distSq = 0f;

        if (ReferenceEquals(actor, Owner) || !IsAlive(actor) || !IsEnemy(actor, myTeam))
        {
            return false;
        }

        distSq = DistanceSqXZ(actor);
        if (distSq > acqSq)
        {
            return false;
        }

        kind = Classify(actor);

        return kind != TargetKind.None && (Profile.TargetMask & kind) != 0;
    }

    private int ResolvePriority(Actor actor, TargetKind kind, string myTeam, double now)
    {
        if (kind == TargetKind.Minion && IsDefendingAlly(actor, myTeam))
        {
            return 1;
        }

        if (kind == TargetKind.Champion && IsBasicAttackingAllyInWindow(actor, myTeam, now))
        {
            return 2;
        }

        return kind switch
        {
            TargetKind.Minion => 3,
            TargetKind.Champion => 4,
            TargetKind.Building => 5,
            _ => int.MaxValue,
        };
    }

    /// <summary>True iff <paramref name="enemyMinion"/> is currently engaged with one of my allied champions, in range.</summary>
    private bool IsDefendingAlly(Actor enemyMinion, string myTeam) =>
        enemyMinion.GetComponent<AttackComponent>()?.CurrentTarget is PlayerActor ally
        && IsAlly(ally, myTeam)
        && IsWithinAcquisition(ally);

    /// <summary>True iff <paramref name="enemyChamp"/> basic-attacked an allied champion within the defend-ally window.</summary>
    private bool IsBasicAttackingAllyInWindow(Actor enemyChamp, string myTeam, double now) =>
        enemyChamp.GetComponent<AttackComponent>() is { LastTarget: PlayerActor recent } enemyAttack
        && IsAlly(recent, myTeam)
        && now - enemyAttack.LastAttackAt <= Profile.DefendAllyWindow
        && IsWithinAcquisition(recent);

    private bool IsStillEngaged(Actor? target)
    {
        if (target is null || !IsAlive(target))
        {
            return false;
        }
        return DistanceSqXZ(target) <= Profile.DropRange * Profile.DropRange;
    }

    private bool IsWithinAcquisition(Actor actor) =>
        DistanceSqXZ(actor) <= Profile.AcquisitionRange * Profile.AcquisitionRange;

    private float DistanceSqXZ(Actor target)
    {
        var dx = target.Transform.Position.X - Owner.Transform.Position.X;
        var dz = target.Transform.Position.Z - Owner.Transform.Position.Z;
        return (dx * dx) + (dz * dz);
    }

    private static TargetKind Classify(Actor actor) => actor switch
    {
        MinionActor => TargetKind.Minion,
        PlayerActor => TargetKind.Champion,
        BuildingActor => TargetKind.Building,
        _ => TargetKind.None,
    };

    private static bool IsAlive(Actor actor) =>
        actor.GetComponent<HealthComponent>() is { Current: > 0f };

    private static bool IsEnemy(Actor actor, string myTeam) =>
        actor.GetComponent<TeamComponent>()?.Team is { } team
        && !string.Equals(team, myTeam, StringComparison.Ordinal);

    private static bool IsAlly(Actor actor, string myTeam) =>
        string.Equals(actor.GetComponent<TeamComponent>()?.Team, myTeam, StringComparison.Ordinal);

    private readonly record struct Candidate(Actor? Actor, int Priority, float DistanceSq)
    {
        public static readonly Candidate None = new(null, int.MaxValue, float.MaxValue);
    }
}
