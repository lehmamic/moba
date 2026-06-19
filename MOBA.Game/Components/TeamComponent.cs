using MOBA.Engine.Core.Abstractions;

namespace MOBA.Game.Components;

/// <summary>
/// Team allegiance of an actor — attached to players, towers, nexus, future
/// minions / heroes. The team value is the canonical name ("Blue" / "Red");
/// the scene's <c>TeamActor</c>s own the spawn-area centre and other team-
/// level state. Absence of this component means the actor has no faction
/// (jungle camp, neutral mob, world geometry).
/// </summary>
public sealed class TeamComponent : Component
{
    public TeamComponent(Actor owner, string team) : base(owner) => Team = team;

    public string Team { get; }
}
