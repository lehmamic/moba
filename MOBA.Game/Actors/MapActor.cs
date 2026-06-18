using MOBA.Engine.Core.World;
using MOBA.Game.Components;
using MOBA.Game.Models;

namespace MOBA.Game.Actors;

/// <summary>
/// The world's geographic root: holds the runtime <see cref="Map"/>
/// definition and the precomputed <see cref="NavMesh"/>, and renders
/// the terrain itself (the client-side factory attaches the
/// <c>MeshRendererComponent</c> after construction). Replaces the earlier
/// MapComponent + GroundPlaneActor split — Map is a "thing in the scene"
/// with behaviour, not just a data bag attached to the scene root.
/// </summary>
public sealed class MapActor : Actor
{
    public MapActor(Map map, NavMesh navMesh)
    {
        Map = map;
        NavMesh = navMesh;
        _ = new TransformComponent(this);
    }

    public Map Map { get; }

    public NavMesh NavMesh { get; }
}
