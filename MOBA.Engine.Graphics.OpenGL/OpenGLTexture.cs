using Silk.NET.OpenGL;

namespace MOBA.Engine.Graphics.OpenGL;

internal sealed class OpenGLTexture : ITexture
{
    private readonly GL _gl;
    private readonly uint _handle;

    public int Width { get; }

    public int Height { get; }

    public OpenGLTexture(GL gl, ReadOnlySpan<byte> pixelsRgba, int width, int height)
    {
        _gl = gl;
        Width = width;
        Height = height;

        _handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _handle);

        _gl.TexImage2D(
            TextureTarget.Texture2D,
            level: 0,
            InternalFormat.Rgba8,
            (uint)width,
            (uint)height,
            border: 0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            pixelsRgba);

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        _gl.GenerateMipmap(TextureTarget.Texture2D);

        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind(uint unit)
    {
        _gl.ActiveTexture(TextureUnit.Texture0 + (int)unit);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose() => _gl.DeleteTexture(_handle);
}
