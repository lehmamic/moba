using MOBA.Engine.Graphics.Abstractions;
using MOBA.Engine.Graphics.Rendering;
using Silk.NET.Maths;

namespace MOBA.Editor.Passes;

/// <summary>
/// Editor-only render pass. Draws Unity's Scene-View axis gizmo: a small
/// top-right overlay with three coloured lines from the origin along +X
/// (red), +Y (green), +Z (blue). The gizmo shares the main camera's
/// rotation — moving / orbiting the scene tilts the gizmo with it — but
/// uses its own orthographic projection in a corner viewport, so it stays
/// fixed-size on screen and never gets occluded by world geometry.
/// </summary>
public sealed class AxisGizmoPass : IRenderPass
{
    private const int GizmoPixels = 96;
    private const int MarginPixels = 12;
    private const float AxisLength = 1.0f;
    private const float OrthoExtent = 1.3f;
    private const float CameraDistance = 3f;

    private static readonly Vector3D<float> XColor = new(1.0f, 0.25f, 0.25f);
    private static readonly Vector3D<float> YColor = new(0.25f, 1.0f, 0.25f);
    private static readonly Vector3D<float> ZColor = new(0.3f, 0.5f, 1.0f);

    private readonly Material _material;
    private IMesh? _xAxis;
    private IMesh? _yAxis;
    private IMesh? _zAxis;

    public AxisGizmoPass(IShader shader) => _material = new Material(shader);

    public void Execute(RenderFrameContext context)
    {
        var backend = context.Backend;
        _xAxis ??= BuildAxis(backend, new Vector3D<float>(AxisLength, 0f, 0f));
        _yAxis ??= BuildAxis(backend, new Vector3D<float>(0f, AxisLength, 0f));
        _zAxis ??= BuildAxis(backend, new Vector3D<float>(0f, 0f, AxisLength));

        var fb = backend.FramebufferSize;
        var cornerX = fb.X - GizmoPixels - MarginPixels;
        var cornerY = MarginPixels;

        backend.BeginOverlayPass(
            _material.Shader,
            BuildViewProjection(context.Camera),
            cornerX,
            cornerY,
            GizmoPixels,
            GizmoPixels);

        _material.Shader.SetUniform("u_color", XColor);
        backend.DrawMeshInPass(_xAxis, _material, Matrix4X4<float>.Identity);
        _material.Shader.SetUniform("u_color", YColor);
        backend.DrawMeshInPass(_yAxis, _material, Matrix4X4<float>.Identity);
        _material.Shader.SetUniform("u_color", ZColor);
        backend.DrawMeshInPass(_zAxis, _material, Matrix4X4<float>.Identity);
    }

    /// <summary>
    /// View-projection that mirrors the main camera's orientation but pulls
    /// the viewpoint to a fixed distance along the camera-forward direction
    /// and uses an orthographic projection sized to fit the axes plus a bit
    /// of padding.
    /// </summary>
    private static Matrix4X4<float> BuildViewProjection(Camera camera)
    {
        var forward = Vector3D.Normalize(camera.Target - camera.Position);
        var viewPos = -forward * CameraDistance;
        var view = Matrix4X4.CreateLookAt(viewPos, Vector3D<float>.Zero, camera.Up);
        var projection = Matrix4X4.CreateOrthographic(
            OrthoExtent * 2f,
            OrthoExtent * 2f,
            0.01f,
            CameraDistance * 4f);
        return view * projection;
    }

    private static IMesh BuildAxis(IGraphicsBackend backend, Vector3D<float> tip)
    {
        Span<Vertex> vertices =
        [
            new Vertex(Vector3D<float>.Zero, default, default),
            new Vertex(tip, default, default),
        ];
        Span<uint> indices = [0, 1];
        return backend.CreateLineMesh(vertices, indices);
    }
}
