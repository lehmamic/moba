using Silk.NET.Maths;

namespace MOBA.Game.Models;

/// <summary>
/// Runtime form of a team's lane route: the lane label plus the ordered
/// world-space waypoints a minion of that lane walks through to reach the enemy
/// nexus. Parsed from <c>LaneRouteDefinition</c> at scene load (Y defaults to
/// ground level; the spawner snaps each point onto the navmesh).
/// </summary>
public sealed record LaneRoute(string Lane, IReadOnlyList<Vector3D<float>> Waypoints);
