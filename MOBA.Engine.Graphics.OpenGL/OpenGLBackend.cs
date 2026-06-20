using MOBA.Engine.Graphics.Abstractions;
using MOBA.Engine.Graphics.Animations;
using MOBA.Engine.Graphics.Rendering;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace MOBA.Engine.Graphics.OpenGL;

public sealed class OpenGLBackend : IGraphicsBackend
{
    private readonly GL _gl;

    // Pass state — set by BeginPass, consumed by *InPass draw calls.
    private OpenGLShader? _passShader;
    private Matrix4X4<float> _passViewProjection;

    public Vector2D<int> FramebufferSize { get; private set; } = new(1, 1);

    public OpenGLBackend(GL gl)
    {
        _gl = gl;
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(TriangleFace.Back);
        // RH Y-up: CCW = front face (OpenGL default, set explicitly for documentation).
        _gl.FrontFace(FrontFaceDirection.Ccw);
    }

    public IMesh CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices) =>
        new OpenGLMesh(_gl, vertices, indices);

    public IMesh CreateLineMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices) =>
        new OpenGLMesh(_gl, vertices, indices, PrimitiveType.Lines);

    public ISkinnedMesh CreateSkinnedMesh(ReadOnlySpan<SkinnedVertex> vertices, ReadOnlySpan<uint> indices) =>
        new OpenGLSkinnedMesh(_gl, vertices, indices);

    public ITexture CreateTexture(ReadOnlySpan<byte> pixelsRgba, int width, int height) =>
        new OpenGLTexture(_gl, pixelsRgba, width, height);

    public IShader CreateShader(string vertexSource, string fragmentSource) =>
        new OpenGLShader(_gl, vertexSource, fragmentSource);

    public void Resize(int framebufferWidth, int framebufferHeight)
    {
        FramebufferSize = new Vector2D<int>(framebufferWidth, framebufferHeight);
        _gl.Viewport(0, 0, (uint)framebufferWidth, (uint)framebufferHeight);
    }

    public void BeginFrame(float clearR, float clearG, float clearB)
    {
        _gl.ClearColor(clearR, clearG, clearB, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _passShader = null;
    }

    public void BeginPass(
        IShader shader,
        Matrix4X4<float> viewProjection,
        Vector3D<float> viewPosition,
        DirectionalLight light)
    {
        var glShader = (OpenGLShader)shader;
        glShader.Use();
        // Frame-level uniforms — set once per pass instead of once per draw.
        // Uniforms absent from the bound shader resolve to location -1 and are
        // silently ignored, so swapping shaders within the same pass is safe.
        glShader.SetUniform("u_viewPos", viewPosition);
        glShader.SetUniform("u_lightDir", light.Direction);
        glShader.SetUniform("u_lightColor", light.Color);
        glShader.SetUniform("u_ambientColor", light.AmbientColor);
        glShader.SetUniform("u_specularStrength", light.SpecularStrength);
        glShader.SetUniform("u_shininess", light.Shininess);

        _passShader = glShader;
        _passViewProjection = viewProjection;
    }

    public void DrawMeshInPass(IMesh mesh, Material material, Matrix4X4<float> model)
    {
        var shader = RequirePassShader();
        // Row-vector convention: clip = model * viewProjection * vertex^T (see ADR-002).
        shader.SetUniform("u_mvp", model * _passViewProjection);
        shader.SetUniform("u_model", model);
        BindMaterialTexture(material, shader);
        ((OpenGLMesh)mesh).Draw();
    }

    public void DrawSkinnedMeshInPass(
        ISkinnedMesh mesh,
        Material material,
        Matrix4X4<float> model,
        MatrixPalette palette)
    {
        var shader = RequirePassShader();
        shader.SetUniform("u_mvp", model * _passViewProjection);
        shader.SetUniform("u_model", model);
        shader.SetUniform("u_palette", new ReadOnlySpan<Matrix4X4<float>>(palette.Entries));
        BindMaterialTexture(material, shader);
        ((OpenGLSkinnedMesh)mesh).Draw();
    }

    public void BeginOverlayPass(
        IShader shader,
        Matrix4X4<float> viewProjection,
        int x,
        int y,
        int width,
        int height)
    {
        var glShader = (OpenGLShader)shader;
        glShader.Use();
        // OpenGL viewport origin is bottom-left; callers think top-left. Flip
        // y so the same (x, y) input lands in the screen corner the caller
        // expects.
        var glY = FramebufferSize.Y - y - height;
        _gl.Viewport(x, glY, (uint)width, (uint)height);
        // Overlay always paints on top — depth state restored at EndFrame.
        _gl.Disable(EnableCap.DepthTest);

        _passShader = glShader;
        _passViewProjection = viewProjection;
    }

    public void BeginBillboardPass(
        IShader shader,
        Matrix4X4<float> viewProjection,
        Vector3D<float> cameraRight,
        Vector3D<float> cameraUp)
    {
        var glShader = (OpenGLShader)shader;
        glShader.Use();
        glShader.SetUniform("u_viewProjection", viewProjection);
        glShader.SetUniform("u_cameraRight", cameraRight);
        glShader.SetUniform("u_cameraUp", cameraUp);
        // HP bars should not be hidden by terrain or buildings between actor
        // and camera. Disable depth test for the pass; restored in EndFrame.
        _gl.Disable(EnableCap.DepthTest);

        _passShader = glShader;
        _passViewProjection = viewProjection;
    }

    public void DrawBillboardInPass(
        IMesh quad,
        Matrix4X4<float> model,
        Vector2D<float> sizeWorldUnits,
        float fillRatio,
        Vector3D<float> fillColor,
        Vector3D<float> backgroundColor,
        Vector3D<float> outlineColor,
        float outlineWidthFraction)
    {
        var shader = RequirePassShader();
        shader.SetUniform("u_model", model);
        shader.SetUniform("u_size", sizeWorldUnits);
        shader.SetUniform("u_fillRatio", fillRatio);
        shader.SetUniform("u_fillColor", fillColor);
        shader.SetUniform("u_bgColor", backgroundColor);
        shader.SetUniform("u_outlineColor", outlineColor);
        shader.SetUniform("u_outlineWidth", outlineWidthFraction);
        ((OpenGLMesh)quad).Draw();
    }

    public void EndFrame()
    {
        // Restore depth test in case a billboard / overlay pass was the last thing drawn.
        _gl.Enable(EnableCap.DepthTest);
        // Restore the full-frame viewport so the next frame's main passes
        // aren't confined to the last overlay's corner rectangle.
        _gl.Viewport(0, 0, (uint)FramebufferSize.X, (uint)FramebufferSize.Y);
        // Swap-chain is handled by the windowing layer (Silk.NET.Windowing).
        // For Vulkan this will be command-buffer submission.
    }

    public void Dispose()
    {
        // GL context lifetime belongs to the window; nothing backend-owned to dispose.
    }

    private OpenGLShader RequirePassShader() =>
        _passShader ?? throw new InvalidOperationException(
            "No render pass active. Call BeginPass before DrawMeshInPass / DrawSkinnedMeshInPass.");

    private static void BindMaterialTexture(Material material, OpenGLShader shader)
    {
        if (material.Texture is OpenGLTexture glTexture)
        {
            glTexture.Bind(unit: 0);
            shader.SetUniform("u_tex", 0);
        }
    }
}
