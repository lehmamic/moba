namespace MOBA.Game.Scenes;

/// <summary>
/// Destructible building instance (Nexus, Tower, Inhibitor). The
/// <see cref="File"/> is a repo-relative path to the prefab GLB; the runtime
/// <c>Building</c> derives the short asset name from it.
/// </summary>
public sealed record BuildingDefinition(
    string Id,
    string Type,
    TransformDefinition Transform,
    string File,
    string? Team = null);
