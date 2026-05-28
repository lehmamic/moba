using MOBA.Engine.Graphics;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// Unit cube (extent ±0.5, side length 1). 24 vertices (4 per face for clean UVs),
/// 36 indices. CCW winding when viewed from outside — matches RH Y-up convention with
/// front face = CCW.
/// </summary>
public static class CubeMesh
{
    public static IMesh CreateUnitCube(IGraphicsBackend backend)
    {
        Vertex V(float x, float y, float z, float u, float v) =>
            new(new Vector3D<float>(x, y, z), new Vector2D<float>(u, v));

        var vertices = new Vertex[]
        {
            // +X face (outward normal +X)
            V(+0.5f, -0.5f, -0.5f, 0f, 0f),
            V(+0.5f, +0.5f, -0.5f, 1f, 0f),
            V(+0.5f, +0.5f, +0.5f, 1f, 1f),
            V(+0.5f, -0.5f, +0.5f, 0f, 1f),

            // -X face (outward normal -X)
            V(-0.5f, -0.5f, +0.5f, 0f, 0f),
            V(-0.5f, +0.5f, +0.5f, 1f, 0f),
            V(-0.5f, +0.5f, -0.5f, 1f, 1f),
            V(-0.5f, -0.5f, -0.5f, 0f, 1f),

            // +Y face (top, outward normal +Y)
            V(-0.5f, +0.5f, -0.5f, 0f, 0f),
            V(-0.5f, +0.5f, +0.5f, 1f, 0f),
            V(+0.5f, +0.5f, +0.5f, 1f, 1f),
            V(+0.5f, +0.5f, -0.5f, 0f, 1f),

            // -Y face (bottom, outward normal -Y)
            V(+0.5f, -0.5f, -0.5f, 0f, 0f),
            V(+0.5f, -0.5f, +0.5f, 1f, 0f),
            V(-0.5f, -0.5f, +0.5f, 1f, 1f),
            V(-0.5f, -0.5f, -0.5f, 0f, 1f),

            // +Z face (outward normal +Z)
            V(-0.5f, -0.5f, +0.5f, 0f, 0f),
            V(+0.5f, -0.5f, +0.5f, 1f, 0f),
            V(+0.5f, +0.5f, +0.5f, 1f, 1f),
            V(-0.5f, +0.5f, +0.5f, 0f, 1f),

            // -Z face (outward normal -Z)
            V(-0.5f, +0.5f, -0.5f, 0f, 0f),
            V(+0.5f, +0.5f, -0.5f, 1f, 0f),
            V(+0.5f, -0.5f, -0.5f, 1f, 1f),
            V(-0.5f, -0.5f, -0.5f, 0f, 1f),
        };

        var indices = new uint[6 * 6];
        for (uint face = 0; face < 6; face++)
        {
            var b = face * 4;
            var i = face * 6;
            indices[i + 0] = b + 0;
            indices[i + 1] = b + 1;
            indices[i + 2] = b + 2;
            indices[i + 3] = b + 0;
            indices[i + 4] = b + 2;
            indices[i + 5] = b + 3;
        }

        return backend.CreateMesh(vertices, indices);
    }
}
