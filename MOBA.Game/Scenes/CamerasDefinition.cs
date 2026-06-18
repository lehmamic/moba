namespace MOBA.Game.Scenes;

/// <summary>
/// Bundle of camera configurations for a Game-Scene. <see cref="Default"/>
/// names the camera the scene starts on (matches the dictionary keys we
/// expect on the host: "FreeFly", "TopDown"); <see cref="FreeFly"/> and
/// <see cref="TopDown"/> carry the initial-state parameters for the two
/// controllers we have today. Both controller-specific blocks are optional
/// — missing values fall back to the controller's hard-coded defaults.
/// </summary>
public sealed record CamerasDefinition(
    string? Default = null,
    CameraDefinition? FreeFly = null,
    CameraDefinition? TopDown = null);