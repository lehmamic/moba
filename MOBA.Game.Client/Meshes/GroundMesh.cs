using MOBA.Engine.Graphics;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Meshes;

/// <summary>
/// Flat grass plane in the XZ plane at <c>y</c>. UVs are scaled so the texture
/// tiles every <c>worldUnitsPerTile</c> world units. <c>y</c> defaults to 0; a
/// small negative offset is useful as a backdrop under a holey terrain mesh so
/// the holes show grass instead of the void.
/// </summary>
public static class GroundMesh
{
    public static IMesh CreatePlane(IGraphicsBackend backend, float width, float length, float worldUnitsPerTile = 2f, float y = 0f)
    {
        var hw = width * 0.5f;
        var hl = length * 0.5f;
        var uMax = width / worldUnitsPerTile;
        var vMax = length / worldUnitsPerTile;

        var up = Vector3D<float>.UnitY;
        var vertices = new Vertex[]
        {
            new(new Vector3D<float>(-hw, y, -hl), new Vector2D<float>(0f,   0f),   up),
            new(new Vector3D<float>(-hw, y, +hl), new Vector2D<float>(0f,   vMax), up),
            new(new Vector3D<float>(+hw, y, +hl), new Vector2D<float>(uMax, vMax), up),
            new(new Vector3D<float>(+hw, y, -hl), new Vector2D<float>(uMax, 0f),   up),
        };

        var indices = new uint[] { 0, 1, 2, 0, 2, 3 };

        return backend.CreateMesh(vertices, indices);
    }
}
