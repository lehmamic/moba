using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Models;

namespace MOBA.Game.Components;

/// <summary>
/// Coordination + execution slot for an actor's basic attack: holds the
/// currently-engaged enemy (<see cref="CurrentTarget"/>), the static
/// per-actor stats (<see cref="Profile"/>), and the bookkeeping the
/// minion aggro priority list needs (<see cref="LastTarget"/> /
/// <see cref="LastAttackAt"/> — "did this champion basic-attack one of
/// my allies recently?"). The component itself only stores state in
/// this iteration; the attack-tick that turns it into damage lands in
/// the next commit. Reused unchanged by minions, towers, nexus and
/// champions — only the <see cref="Profile"/> values differ.
///
/// <para>
/// Movement is **not** this component's concern. <c>MoveTargetComponent</c>
/// reads <see cref="CurrentTarget"/> each tick and switches between lane
/// walking and chasing — the two channels (target slot here, waypoint
/// queue on the mover) flow into one mover that owns all position writes.
/// </para>
/// </summary>
public sealed class AttackComponent : Component
{
    public AttackComponent(Actor owner, AttackProfile profile) : base(owner) =>
        Profile = profile;

    public AttackProfile Profile { get; }

    /// <summary>The enemy the actor is currently engaging, or null while idle.</summary>
    public Actor? CurrentTarget { get; set; }

    /// <summary>The most recent target this actor actually landed a hit on. Used by minion aggro priority 2.</summary>
    public Actor? LastTarget { get; set; }

    /// <summary>Server-time of the last attack-tick that fired. Used by minion aggro priority 2 (within-window check).</summary>
    public double LastAttackAt { get; set; }
}
