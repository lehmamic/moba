using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Components;
using MOBA.Game.Models;
using Silk.NET.Maths;

namespace MOBA.Game.Actors;

/// <summary>
/// One of the two competing teams. Holds the team identity (Order / Chaos) and
/// the spawn-area centre that <c>PlayerConnectionSystem</c> uses to drop new
/// players into their base. Future team-level state (score, banned heroes,
/// objective timers) lives here too — either as properties or as components
/// dangled off the actor.
///
/// <para>
/// Buildings (nexus, towers) stay flat in <c>Scene.Actors</c> but carry a
/// matching <c>Team</c> property on their <c>BuildingDefinition</c>, so they
/// are queryable by team without a parent-child re-anchor pass at load time.
/// </para>
/// </summary>
public sealed class TeamActor : Actor
{
    public TeamActor(string name, Vector3D<float> spawnAreaCenter, IReadOnlyList<LaneRoute>? lanes = null)
    {
        Name = name;
        SpawnAreaCenter = spawnAreaCenter;
        Lanes = lanes ?? [];
        Transform.Position = spawnAreaCenter;
        _ = new TransformComponent(this);
    }

    public string Name { get; }

    public Vector3D<float> SpawnAreaCenter { get; }

    /// <summary>Fixed lane routes the team's minion waves follow to the enemy nexus.</summary>
    public IReadOnlyList<LaneRoute> Lanes { get; }
}
