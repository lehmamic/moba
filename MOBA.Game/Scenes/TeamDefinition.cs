namespace MOBA.Game.Scenes;

/// <summary>
/// JSON shape of a <c>{ "Type": "Team" }</c> actor entry. Carries the team
/// name and the world-space centre of the team's spawn area (typically the
/// nexus position).
/// </summary>
public sealed record TeamDefinition(string Name, float[] SpawnAreaCenter);
