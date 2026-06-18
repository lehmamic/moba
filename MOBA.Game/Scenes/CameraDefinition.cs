namespace MOBA.Game.Scenes;

/// <summary>
/// Generic camera parameters. Not every field is meaningful to every
/// controller (FreeFly reads <see cref="Yaw"/>/<see cref="Pitch"/>; TopDown
/// reads <see cref="Target"/>) — each controller picks what it needs and
/// ignores the rest. Optional fields fall back to the controller default.
/// </summary>
public sealed record CameraDefinition(
    float[]? Position = null,
    float[]? Target = null,
    float? Yaw = null,
    float? Pitch = null,
    float? FieldOfViewRadians = null);
