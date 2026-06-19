using MOBA.Engine.Graphics.Abstractions;
using MOBA.Engine.Graphics.Rendering;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Meshes;

/// <summary>
/// A unit XY quad in [-0.5, 0.5]² with UVs in [0, 1] and +Z normal. Backs the
/// camera-facing billboard shader; the bar.vert vertex shader reads
/// <c>a_position.xy</c> as the offset (in units of <c>u_size</c>) from the
/// world centre carried by <c>u_model</c>'s translation.
/// </summary>
public static class QuadMesh
{
    public static IMesh Create(IGraphicsBackend backend)
    {
        Span<Vertex> vertices =
        [
            new(new Vector3D<float>(-0.5f, -0.5f, 0f), new Vector2D<float>(0f, 0f), new Vector3D<float>(0f, 0f, 1f)),
            new(new Vector3D<float>( 0.5f, -0.5f, 0f), new Vector2D<float>(1f, 0f), new Vector3D<float>(0f, 0f, 1f)),
            new(new Vector3D<float>( 0.5f,  0.5f, 0f), new Vector2D<float>(1f, 1f), new Vector3D<float>(0f, 0f, 1f)),
            new(new Vector3D<float>(-0.5f,  0.5f, 0f), new Vector2D<float>(0f, 1f), new Vector3D<float>(0f, 0f, 1f)),
        ];
        Span<uint> indices = [0, 1, 2, 0, 2, 3];
        return backend.CreateMesh(vertices, indices);
    }
}
