namespace MOBA.Game.Scenes;

/// <summary>
/// JSON shape of a <c>{ "Type": "Team" }</c> actor entry. Carries the team
/// name, the world-space centre of the team's spawn area (typically the nexus
/// position), and the fixed lane routes its minion waves follow.
/// </summary>
public sealed record TeamDefinition(string Name, float[] SpawnAreaCenter, LaneRouteDefinition[]? Lanes = null);

/// <summary>
/// JSON shape of one lane route on a team: a lane label (<c>Top</c> / <c>Mid</c>
/// / <c>Bottom</c>) and an ordered list of <c>[x, z]</c> waypoints the lane's
/// minions walk through, ending at the enemy nexus (the gather point).
/// </summary>
public sealed record LaneRouteDefinition(string Lane, float[][] Waypoints);
