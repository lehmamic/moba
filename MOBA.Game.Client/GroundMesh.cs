using MOBA.Engine.Graphics;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// Flat grass plane in the XZ plane (Y = 0). UVs are scaled so the texture tiles
/// every <c>worldUnitsPerTile</c> world units.
/// </summary>
public static class GroundMesh
{
    public static IMesh CreatePlane(IGraphicsBackend backend, float width, float length, float worldUnitsPerTile = 2f)
    {
        var hw = width * 0.5f;
        var hl = length * 0.5f;
        var uMax = width / worldUnitsPerTile;
        var vMax = length / worldUnitsPerTile;

        var vertices = new Vertex[]
        {
            new(new Vector3D<float>(-hw, 0f, -hl), new Vector2D<float>(0f,   0f)),
            new(new Vector3D<float>(-hw, 0f, +hl), new Vector2D<float>(0f,   vMax)),
            new(new Vector3D<float>(+hw, 0f, +hl), new Vector2D<float>(uMax, vMax)),
            new(new Vector3D<float>(+hw, 0f, -hl), new Vector2D<float>(uMax, 0f)),
        };

        var indices = new uint[] { 0, 1, 2, 0, 2, 3 };

        return backend.CreateMesh(vertices, indices);
    }
}
