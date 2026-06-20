using MOBA.Engine.Graphics.Abstractions;
using MOBA.Engine.Graphics.Rendering;
using Silk.NET.Maths;

namespace MOBA.Editor.Passes;

/// <summary>
/// Editor-only render pass. Draws a unit-spaced grid on the Y = 0 ground
/// plane so the author can eyeball world distances while flying around.
/// The grid mesh is built once via <see cref="Build"/> and re-used every
/// frame — the grid is static, no allocations per tick. Colour is pushed
/// into the line shader's <c>u_color</c> uniform each frame so the same
/// shared <c>path</c> shader can drive grid + lane overlays without
/// stepping on each other's colour state.
/// </summary>
public sealed class GridOverlayPass : IRenderPass
{
    /// <summary>
    /// Neutral mid-grey that mirrors Unity's default Scene-View grid — bright
    /// enough to read against grass, dark enough not to fight the actual
    /// geometry for attention.
    /// </summary>
    public static readonly Vector3D<float> UnityGrey = new(0.35f, 0.35f, 0.35f);

    private readonly IMesh _mesh;
    private readonly Material _material;
    private readonly Vector3D<float> _color;

    public GridOverlayPass(IMesh mesh, IShader shader, Vector3D<float>? color = null)
    {
        _mesh = mesh;
        _material = new Material(shader);
        _color = color ?? UnityGrey;
    }

    public void Execute(RenderFrameContext context)
    {
        var backend = context.Backend;
        backend.BeginPass(
            _material.Shader,
            context.Camera.ViewProjection,
            context.Camera.Position,
            context.Light);
        _material.Shader.SetUniform("u_color", _color);
        backend.DrawMeshInPass(_mesh, _material, Matrix4X4<float>.Identity);
    }

    /// <summary>
    /// Builds a line mesh covering <c>[-extent, +extent]</c> on both X and Z
    /// with the given <paramref name="stepUnits"/> spacing. Drawn at Y =
    /// <paramref name="liftAboveGround"/> so it sits visibly above the terrain
    /// instead of z-fighting it.
    /// </summary>
    public static IMesh Build(
        IGraphicsBackend backend,
        float stepUnits = 4f,
        int halfCount = 20,
        float liftAboveGround = 0.05f)
    {
        var extent = stepUnits * halfCount;
        var lineCount = halfCount * 2 + 1;
        var vertices = new Vertex[lineCount * 4];
        var indices = new uint[lineCount * 4];
        var v = 0;
        for (var i = -halfCount; i <= halfCount; i++)
        {
            var coord = i * stepUnits;
            // Line parallel to X (varying Z = coord).
            vertices[v + 0] = new Vertex(new Vector3D<float>(-extent, liftAboveGround, coord), default, default);
            vertices[v + 1] = new Vertex(new Vector3D<float>(extent, liftAboveGround, coord), default, default);
            // Line parallel to Z (varying X = coord).
            vertices[v + 2] = new Vertex(new Vector3D<float>(coord, liftAboveGround, -extent), default, default);
            vertices[v + 3] = new Vertex(new Vector3D<float>(coord, liftAboveGround, extent), default, default);
            indices[v + 0] = (uint)(v + 0);
            indices[v + 1] = (uint)(v + 1);
            indices[v + 2] = (uint)(v + 2);
            indices[v + 3] = (uint)(v + 3);
            v += 4;
        }
        return backend.CreateLineMesh(vertices, indices);
    }
}
