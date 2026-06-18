namespace MOBA.Game.Scenes;

/// <summary>
/// Serialisable form of the scene's primary directional light. Maps 1:1 to
/// the runtime <c>MOBA.Engine.Graphics.DirectionalLight</c> struct; lives in
/// MOBA.Game so the schema can be read without a graphics reference, while
/// the actual light type stays in the graphics layer.
/// </summary>
public sealed record DirectionalLightDefinition(
    float[] Direction,
    float[] Color,
    float[] Ambient,
    float SpecularStrength,
    float Shininess);