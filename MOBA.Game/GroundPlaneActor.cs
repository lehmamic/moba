using MOBA.Engine.Core;
using Silk.NET.Maths;

namespace MOBA.Game;

public sealed class GroundPlaneActor : Actor
{
    public GroundPlaneActor(Map map)
    {
        Map = map;
        Position = Vector3D<float>.Zero;
        // Scale stays 1: the mesh is generated directly in the map's dimensions (see GroundMesh).
        _ = new TransformComponent(this);
    }

    public Map Map { get; }
}
