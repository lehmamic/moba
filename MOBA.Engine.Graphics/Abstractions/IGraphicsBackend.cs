using MOBA.Engine.Graphics.Animations;
using MOBA.Engine.Graphics.Rendering;
using Silk.NET.Maths;

namespace MOBA.Engine.Graphics.Abstractions;

public interface IGraphicsBackend : IDisposable
{
    /// <summary>
    /// Pixel size of the currently bound framebuffer, last value passed to
    /// <see cref="Resize"/>. Passes that need to position screen-space overlays
    /// (corner gizmos, on-screen text) read this instead of threading the
    /// window size through every <c>RenderFrameContext</c>.
    /// </summary>
    Vector2D<int> FramebufferSize { get; }

    IMesh CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices);

    /// <summary>
    /// Variant of <see cref="CreateMesh"/> that draws each index pair as a line
    /// segment instead of triangles. Used by the F2 navmesh wireframe overlay —
    /// per-vertex UV and Normal are ignored by the unlit wireframe shader, so
    /// callers can leave them zeroed.
    /// </summary>
    IMesh CreateLineMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices);

    ISkinnedMesh CreateSkinnedMesh(ReadOnlySpan<SkinnedVertex> vertices, ReadOnlySpan<uint> indices);

    ITexture CreateTexture(ReadOnlySpan<byte> pixelsRgba, int width, int height);

    IShader CreateShader(string vertexSource, string fragmentSource);

    void Resize(int framebufferWidth, int framebufferHeight);

    void BeginFrame(float clearR, float clearG, float clearB);

    /// <summary>
    /// Begin a render pass under a given shader. Binds the shader, uploads
    /// frame-level uniforms (viewProjection, view position, directional light)
    /// once for the whole pass. Subsequent <see cref="DrawMeshInPass"/> /
    /// <see cref="DrawSkinnedMeshInPass"/> calls reuse those uniforms and only
    /// touch per-draw state (model matrix, texture, bone palette). Modelled on
    /// the static vs skinned passes in Madhav's <i>Game Programming in C++</i>
    /// ch.6 / ch.12 renderer.
    /// </summary>
    void BeginPass(
        IShader shader,
        Matrix4X4<float> viewProjection,
        Vector3D<float> viewPosition,
        DirectionalLight light);

    /// <summary>
    /// Draw a static (non-skinned) mesh inside the currently-bound pass.
    /// </summary>
    void DrawMeshInPass(IMesh mesh, Material material, Matrix4X4<float> model);

    /// <summary>
    /// Draw a skinned mesh inside the currently-bound pass. The pass's shader
    /// must accept the skinning uniforms (palette + skinned vertex layout).
    /// </summary>
    void DrawSkinnedMeshInPass(
        ISkinnedMesh mesh,
        Material material,
        Matrix4X4<float> model,
        MatrixPalette palette);

    /// <summary>
    /// Begin a screen-space overlay pass. Confines drawing to the given
    /// viewport rectangle (pixel coordinates, origin top-left from the
    /// caller's perspective) and disables depth testing so the overlay always
    /// paints on top of whatever the main scene rendered into that region.
    /// Typical use: a corner camera gizmo, a minimap, or a debug HUD. The
    /// usual <see cref="DrawMeshInPass"/> path applies. Viewport + depth state
    /// are restored at <see cref="EndFrame"/>.
    /// </summary>
    void BeginOverlayPass(
        IShader shader,
        Matrix4X4<float> viewProjection,
        int x,
        int y,
        int width,
        int height);

    /// <summary>
    /// Begin a billboard pass. Binds the shader, uploads viewProjection plus
    /// the world-space camera right / up vectors the vertex shader needs to
    /// rotate the unit quad into the camera plane. Depth writes are disabled
    /// for the duration of the pass so HP bars draw on top of geometry.
    /// </summary>
    void BeginBillboardPass(
        IShader shader,
        Matrix4X4<float> viewProjection,
        Vector3D<float> cameraRight,
        Vector3D<float> cameraUp);

    /// <summary>
    /// Draw one billboard inside the currently-bound billboard pass. The mesh
    /// is the shared unit quad; size, fill colours and outline are uploaded
    /// as per-draw uniforms.
    /// </summary>
    void DrawBillboardInPass(
        IMesh quad,
        Matrix4X4<float> model,
        Vector2D<float> sizeWorldUnits,
        float fillRatio,
        Vector3D<float> fillColor,
        Vector3D<float> backgroundColor,
        Vector3D<float> outlineColor,
        float outlineWidthFraction);

    void EndFrame();
}
