using Silk.NET.OpenGL;

namespace MOBA.Engine.Graphics.OpenGL;

internal sealed class OpenGLMesh : IMesh
{
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private readonly PrimitiveType _primitive;

    public int IndexCount { get; }

    public OpenGLMesh(
        GL gl,
        ReadOnlySpan<Vertex> vertices,
        ReadOnlySpan<uint> indices,
        PrimitiveType primitive = PrimitiveType.Triangles)
    {
        _primitive = primitive;
        _gl = gl;
        IndexCount = indices.Length;

        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

        _ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);

        const uint stride = Vertex.SizeInBytes;
        const nint positionOffset = 0;
        const nint uvOffset = 3 * sizeof(float);
        const nint normalOffset = 5 * sizeof(float);

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, positionOffset);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, uvOffset);

        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, normalOffset);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }

    public unsafe void Draw()
    {
        _gl.BindVertexArray(_vao);
        // The void* is an offset into the bound element-array buffer, not a client pointer.
        _gl.DrawElements(_primitive, (uint)IndexCount, DrawElementsType.UnsignedInt, (void*)0);
        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(_vao);
    }
}
