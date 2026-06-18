namespace MOBA.Game.Scenes;

/// <summary>
/// Mob / monster spawn entry. Same shape as <see cref="BuildingDefinition"/>;
/// kept as a distinct type so the scene can pivot on the role of an entity.
/// </summary>
public sealed record MonsterDefinition(
    string Id,
    string Type,
    TransformDefinition Transform,
    string File,
    string? Team = null);
