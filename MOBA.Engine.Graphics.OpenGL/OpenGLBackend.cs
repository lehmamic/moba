using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace MOBA.Engine.Graphics.OpenGL;

public sealed class OpenGLBackend : IGraphicsBackend
{
    private readonly GL _gl;

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

    public ITexture CreateTexture(ReadOnlySpan<byte> pixelsRgba, int width, int height) =>
        new OpenGLTexture(_gl, pixelsRgba, width, height);

    public IShader CreateShader(string vertexSource, string fragmentSource) =>
        new OpenGLShader(_gl, vertexSource, fragmentSource);

    public void Resize(int framebufferWidth, int framebufferHeight) =>
        _gl.Viewport(0, 0, (uint)framebufferWidth, (uint)framebufferHeight);

    public void BeginFrame(float clearR, float clearG, float clearB)
    {
        _gl.ClearColor(clearR, clearG, clearB, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void DrawMesh(
        IMesh mesh,
        Material material,
        Matrix4X4<float> model,
        Matrix4X4<float> viewProjection,
        Vector3D<float> viewPosition,
        DirectionalLight light)
    {
        var glMesh = (OpenGLMesh)mesh;
        var glShader = (OpenGLShader)material.Shader;

        glShader.Use();
        // Row-vector convention: clip = model * viewProjection * vertex^T (see ADR-002).
        glShader.SetUniform("u_mvp", model * viewProjection);
        glShader.SetUniform("u_model", model);
        glShader.SetUniform("u_viewPos", viewPosition);
        glShader.SetUniform("u_lightDir", light.Direction);
        glShader.SetUniform("u_lightColor", light.Color);
        glShader.SetUniform("u_ambientColor", light.AmbientColor);
        glShader.SetUniform("u_specularStrength", light.SpecularStrength);
        glShader.SetUniform("u_shininess", light.Shininess);
        // Uniforms absent from the bound shader resolve to -1 and are silently
        // ignored, so unlit shaders are safe to keep in the mix.

        if (material.Texture is OpenGLTexture glTexture)
        {
            glTexture.Bind(unit: 0);
            glShader.SetUniform("u_tex", 0);
        }

        glMesh.Draw();
    }

    public void EndFrame()
    {
        // Swap-chain is handled by the windowing layer (Silk.NET.Windowing).
        // For Vulkan this will be command-buffer submission.
    }

    public void Dispose()
    {
        // GL context lifetime belongs to the window; nothing backend-owned to dispose.
    }
}
