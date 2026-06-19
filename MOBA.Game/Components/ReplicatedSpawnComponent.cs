using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Messages;

namespace MOBA.Game.Components;

/// <summary>
/// Marks an actor for generic network replication. The server-side
/// <c>ActorReplicationSystem</c> assigns it a <see cref="NetworkIdentityComponent"/>,
/// broadcasts an <c>ActorSpawnMessage</c> carrying <see cref="Kind"/>,
/// <see cref="Team"/> and <see cref="Variant"/> — never a mesh/asset path — and
/// broadcasts a despawn when the actor leaves the scene. The client's factory
/// turns <c>(Kind, Team, Variant)</c> back into a fully assembled, rendered
/// actor. This is pure data: no rendering or asset knowledge lives here.
/// </summary>
public sealed class ReplicatedSpawnComponent : Component
{
    public ReplicatedSpawnComponent(Actor owner, ActorKind kind, TeamId team = TeamId.None, byte variant = 0)
        : base(owner)
    {
        Kind = kind;
        Team = team;
        Variant = variant;
    }

    public ActorKind Kind { get; }

    public TeamId Team { get; }

    public byte Variant { get; }
}
