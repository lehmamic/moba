using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

public interface IGraphicsBackend : IDisposable
{
    IMesh CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices);

    ITexture CreateTexture(ReadOnlySpan<byte> pixelsRgba, int width, int height);

    IShader CreateShader(string vertexSource, string fragmentSource);

    void Resize(int framebufferWidth, int framebufferHeight);

    void BeginFrame(float clearR, float clearG, float clearB);

    void DrawMesh(
        IMesh mesh,
        Material material,
        Matrix4X4<float> model,
        Matrix4X4<float> viewProjection,
        Vector3D<float> viewPosition,
        DirectionalLight light);

    void EndFrame();
}
