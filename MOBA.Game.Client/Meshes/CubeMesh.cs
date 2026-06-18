using MOBA.Engine.Graphics.Abstractions;
using MOBA.Engine.Graphics.Rendering;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Meshes;

/// <summary>
/// Unit cube (extent ±0.5, side length 1). 24 vertices (4 per face for clean UVs
/// and per-face flat normals), 36 indices. CCW winding when viewed from outside —
/// matches RH Y-up convention with front face = CCW.
/// </summary>
public static class CubeMesh
{
    public static IMesh CreateUnitCube(IGraphicsBackend backend)
    {
        Vertex V(float x, float y, float z, float u, float v, Vector3D<float> n) =>
            new(new Vector3D<float>(x, y, z), new Vector2D<float>(u, v), n);

        var posX = new Vector3D<float>(+1f, 0f, 0f);
        var negX = new Vector3D<float>(-1f, 0f, 0f);
        var posY = new Vector3D<float>(0f, +1f, 0f);
        var negY = new Vector3D<float>(0f, -1f, 0f);
        var posZ = new Vector3D<float>(0f, 0f, +1f);
        var negZ = new Vector3D<float>(0f, 0f, -1f);

        var vertices = new Vertex[]
        {
            // +X face (outward normal +X)
            V(+0.5f, -0.5f, -0.5f, 0f, 0f, posX),
            V(+0.5f, +0.5f, -0.5f, 1f, 0f, posX),
            V(+0.5f, +0.5f, +0.5f, 1f, 1f, posX),
            V(+0.5f, -0.5f, +0.5f, 0f, 1f, posX),

            // -X face (outward normal -X)
            V(-0.5f, -0.5f, +0.5f, 0f, 0f, negX),
            V(-0.5f, +0.5f, +0.5f, 1f, 0f, negX),
            V(-0.5f, +0.5f, -0.5f, 1f, 1f, negX),
            V(-0.5f, -0.5f, -0.5f, 0f, 1f, negX),

            // +Y face (top, outward normal +Y)
            V(-0.5f, +0.5f, -0.5f, 0f, 0f, posY),
            V(-0.5f, +0.5f, +0.5f, 1f, 0f, posY),
            V(+0.5f, +0.5f, +0.5f, 1f, 1f, posY),
            V(+0.5f, +0.5f, -0.5f, 0f, 1f, posY),

            // -Y face (bottom, outward normal -Y)
            V(+0.5f, -0.5f, -0.5f, 0f, 0f, negY),
            V(+0.5f, -0.5f, +0.5f, 1f, 0f, negY),
            V(-0.5f, -0.5f, +0.5f, 1f, 1f, negY),
            V(-0.5f, -0.5f, -0.5f, 0f, 1f, negY),

            // +Z face (outward normal +Z)
            V(-0.5f, -0.5f, +0.5f, 0f, 0f, posZ),
            V(+0.5f, -0.5f, +0.5f, 1f, 0f, posZ),
            V(+0.5f, +0.5f, +0.5f, 1f, 1f, posZ),
            V(-0.5f, +0.5f, +0.5f, 0f, 1f, posZ),

            // -Z face (outward normal -Z)
            V(-0.5f, +0.5f, -0.5f, 0f, 0f, negZ),
            V(+0.5f, +0.5f, -0.5f, 1f, 0f, negZ),
            V(+0.5f, -0.5f, -0.5f, 1f, 1f, negZ),
            V(-0.5f, -0.5f, -0.5f, 0f, 1f, negZ),
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
