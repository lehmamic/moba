using Silk.NET.OpenGL;

namespace MOBA.Engine.Graphics.OpenGL;

internal sealed class OpenGLSkinnedMesh : ISkinnedMesh
{
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;

    public int IndexCount { get; }

    public OpenGLSkinnedMesh(GL gl, ReadOnlySpan<SkinnedVertex> vertices, ReadOnlySpan<uint> indices)
    {
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

        const uint stride = SkinnedVertex.SizeInBytes;
        const nint positionOffset = 0;
        const nint normalOffset = 3 * sizeof(float);
        const nint boneIndicesOffset = 6 * sizeof(float);
        const nint boneWeightsOffset = boneIndicesOffset + sizeof(uint);
        const nint uvOffset = boneWeightsOffset + (4 * sizeof(float));

        // location 0: position (vec3 float)
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, positionOffset);

        // location 1: normal (vec3 float)
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, normalOffset);

        // location 2: bone indices (uvec4 from 4 unsigned bytes) — pure-integer attribute
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribIPointer(2, 4, VertexAttribIType.UnsignedByte, stride, boneIndicesOffset);

        // location 3: bone weights (vec4 float)
        _gl.EnableVertexAttribArray(3);
        _gl.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, stride, boneWeightsOffset);

        // location 4: UV (vec2 float)
        _gl.EnableVertexAttribArray(4);
        _gl.VertexAttribPointer(4, 2, VertexAttribPointerType.Float, false, stride, uvOffset);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }

    public unsafe void Draw()
    {
        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)IndexCount, DrawElementsType.UnsignedInt, (void*)0);
        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(_vao);
    }
}
