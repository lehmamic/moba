namespace MOBA.Game.Scenes;

/// <summary>
/// Per-instance transform as serialised in the scene JSON. Position is the
/// world-space spawn point (game Y-up). Rotation is Euler angles in degrees
/// applied as yaw (Y), pitch (X), roll (Z). Scale is uniform per-axis.
/// </summary>
public sealed record TransformDefinition(float[] Position, float[] Rotation, float[] Scale);