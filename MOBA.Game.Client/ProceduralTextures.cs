namespace MOBA.Game.Client;

/// <summary>
/// Stub textures for the skeleton — deliberately small and code-only so the first slice
/// has no external asset-file dependency. Real PNGs will later be loaded via StbImageSharp
/// (the package reference is already in MOBA.Engine.Graphics).
/// </summary>
public static class ProceduralTextures
{
    public const int DefaultSize = 64;

    public static byte[] CreateGrass(int size = DefaultSize, int seed = 42)
    {
        var rng = new Random(seed);
        var pixels = new byte[size * size * 4];
        for (var i = 0; i < size * size; i++)
        {
            var jitter = rng.Next(-15, 15);
            pixels[(i * 4) + 0] = (byte)Math.Clamp(35 + jitter, 0, 255);
            pixels[(i * 4) + 1] = (byte)Math.Clamp(110 + (jitter * 2), 0, 255);
            pixels[(i * 4) + 2] = (byte)Math.Clamp(40 + jitter, 0, 255);
            pixels[(i * 4) + 3] = 255;
        }
        return pixels;
    }

    public static byte[] CreateChecker(int size = DefaultSize, int cells = 8)
    {
        var pixels = new byte[size * size * 4];
        var cellSize = size / cells;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var cx = x / cellSize;
                var cy = y / cellSize;
                var bright = ((cx + cy) % 2) == 0;
                var c = bright ? (byte)220 : (byte)60;
                var i = ((y * size) + x) * 4;
                pixels[i + 0] = c;
                pixels[i + 1] = c;
                pixels[i + 2] = c;
                pixels[i + 3] = 255;
            }
        }
        return pixels;
    }
}
