using MOBA.Engine.Core;

namespace MOBA.Game.Components;

/// <summary>
/// Marker component for the replicated transform of an actor. The fields themselves
/// (Position/Rotation/Scale) still live on <see cref="Actor"/>. Once state replication arrives,
/// this component will dirty-track the fields and trigger snapshot updates.
/// </summary>
public sealed class TransformComponent : Component
{
    public TransformComponent(Actor owner) : base(owner)
    {
    }
}
