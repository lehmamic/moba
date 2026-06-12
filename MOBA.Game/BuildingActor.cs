using MOBA.Engine.Core;

namespace MOBA.Game;

/// <summary>
/// Static destructible-building actor (Tower, Nexus, Inhibitor) spawned from
/// the map's <see cref="Building"/> list. Carries the per-instance transform
/// + identity metadata; the client side attaches the renderer separately.
/// </summary>
public sealed class BuildingActor : Actor
{
    public BuildingActor(Building definition)
    {
        Definition = definition;
        Transform.Position = definition.Position;
        Transform.Rotation = definition.Rotation;
        Transform.Scale = definition.Scale;
        _ = new TransformComponent(this);
    }

    public Building Definition { get; }
}
