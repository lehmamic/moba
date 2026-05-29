using MOBA.Utilities;
using StbImageSharp;

namespace MOBA.Engine.Graphics;

/// <summary>
/// File-based texture loading via StbImageSharp. Decodes any format stb_image understands
/// (PNG, JPG, TGA, BMP, …) and returns 8-bit RGBA pixels ready to hand to
/// <see cref="IGraphicsBackend.CreateTexture(ReadOnlySpan{byte}, int, int)"/>.
/// </summary>
public static class TextureLoader
{
    public readonly record struct TextureData(byte[] Pixels, int Width, int Height);

    /// <summary>
    /// Loads an image file and returns its RGBA8 pixel data.
    /// </summary>
    /// <param name="filePath">Absolute path to the image file.</param>
    /// <param name="flipVertically">
    /// When true (default), flips the pixel rows so the image displays in OpenGL the same way
    /// as in a viewer. PNG/JPG have a top-left origin; OpenGL textures have a bottom-left
    /// origin, so without flipping the texture appears mirrored.
    /// </param>
    public static TextureData LoadRgba(AbsolutePath filePath, bool flipVertically = true)
    {
        using var stream = File.OpenRead(filePath);
        return DecodeStream(stream, flipVertically);
    }

    /// <summary>
    /// Decodes an in-memory image (PNG, JPG, …) such as one embedded in a glTF binary,
    /// returning RGBA8 pixel data.
    /// </summary>
    public static TextureData LoadRgba(ReadOnlySpan<byte> encoded, bool flipVertically = true)
    {
        using var stream = new MemoryStream(encoded.ToArray(), writable: false);
        return DecodeStream(stream, flipVertically);
    }

    private static TextureData DecodeStream(Stream stream, bool flipVertically)
    {
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        var pixels = image.Data;
        if (flipVertically)
        {
            FlipRowsInPlace(pixels, image.Width, image.Height);
        }

        return new TextureData(pixels, image.Width, image.Height);
    }

    private static void FlipRowsInPlace(byte[] pixels, int width, int height)
    {
        var rowBytes = width * 4;
        Span<byte> swap = stackalloc byte[rowBytes];
        for (var y = 0; y < height / 2; y++)
        {
            var top = pixels.AsSpan(y * rowBytes, rowBytes);
            var bottom = pixels.AsSpan((height - 1 - y) * rowBytes, rowBytes);
            top.CopyTo(swap);
            bottom.CopyTo(top);
            swap.CopyTo(bottom);
        }
    }
}
