namespace MOBA.Game;

public sealed class Map
{
    public Map(float width, float length)
    {
        Width = width;
        Length = length;
    }

    public float Width { get; }

    public float Length { get; }

    /// <summary>Skeleton default: 150 × 150 world units (≈ LoL-scale playing field in our unit).</summary>
    public static Map LeagueSized() => new(150f, 150f);
}
