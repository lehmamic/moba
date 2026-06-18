using MOBA.Engine.Core.World;

namespace MOBA.Game.Components;

/// <summary>
/// Stable identifier shared between server and client for one logical actor.
/// Server assigns IDs; client maintains the inverse map (<see cref="uint"/> →
/// local <see cref="Actor"/>) so incoming network messages can mutate the
/// right local copy. Pre-spawned actors use hardcoded IDs (cube = 2). The
/// server allocates marker IDs starting at 100.
/// </summary>
public sealed class NetworkIdentityComponent : Component
{
    public NetworkIdentityComponent(Actor owner, uint id) : base(owner) => Id = id;

    public uint Id { get; }
}
