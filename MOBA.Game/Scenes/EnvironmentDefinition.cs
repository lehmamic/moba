namespace MOBA.Game.Scenes;

/// <summary>
/// Per-scene render environment (clear colour + scene-wide lighting). Picked
/// up post-load by the client when it sets <c>Renderer.ClearColor</c> and the
/// scene's <c>DirectionalLight</c>. Server scenes have no use for it; the
/// JSON block is simply ignored on that side.
/// </summary>
public sealed record EnvironmentDefinition(
    float[] ClearColor,
    DirectionalLightDefinition? Light = null);