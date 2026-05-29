using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

public interface IGraphicsBackend : IDisposable
{
    IMesh CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices);

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

    void EndFrame();
}
