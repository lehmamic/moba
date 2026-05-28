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

    /// <summary>Built from a deserialised <see cref="MapDefinition"/> (JSON).</summary>
    public static Map FromDefinition(MapDefinition definition) =>
        new(definition.Width, definition.Length);

    /// <summary>
    /// Skeleton fallback: 150 × 150 world units (≈ LoL-scale playing field in our unit).
    /// Use only where loading from <c>assets/maps/*.json</c> is impractical
    /// (e.g. unit tests, scripted simulations).
    /// </summary>
    public static Map LeagueSized() => new(150f, 150f);
}
