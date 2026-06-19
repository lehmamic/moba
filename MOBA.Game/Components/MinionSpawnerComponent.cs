using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using MOBA.Game.Actors;
using MOBA.Game.Messages;
using MOBA.Game.Models;
using Silk.NET.Maths;

namespace MOBA.Game.Components;

/// <summary>
/// Game behaviour (not a system): the minion wave spawner. Lives on a
/// <see cref="TeamActor"/> and, on the LoL cadence in <see cref="MinionWaveSchedule"/>,
/// spawns each wave split across the team's three fixed lane routes. Every minion
/// walks a <b>navmesh-followed</b> corridor (the authored lane anchors are joined
/// with <see cref="NavMesh.TryFindPath"/> so movement never cuts across walls or
/// obstacles) to the enemy nexus, where it gathers fanned out. Each minion carries
/// a <c>MoveTargetComponent</c> and a <c>ReplicatedSpawnComponent</c> so the generic
/// <c>ActorReplicationSystem</c> assigns its id and broadcasts it to clients.
/// Server-authoritative; no networking code lives here.
/// </summary>
public sealed class MinionSpawnerComponent : Component
{
    private const float MinionSpeed = 3.5f;
    private const float SpawnSpread = 1.2f;
    private const float GatherRadius = 4f;

    private readonly Scene _scene;
    private readonly NavMesh _navMesh;
    private readonly TeamActor _team;
    private Dictionary<string, IReadOnlyList<Vector3D<float>>>? _laneCorridors;
    private float _time;
    private int _wavesSpawned;
    private int _spawnCounter;

    public MinionSpawnerComponent(TeamActor owner, Scene scene, NavMesh navMesh)
        : base(owner)
    {
        _team = owner;
        _scene = scene;
        _navMesh = navMesh;
    }

    public override void OnUpdate(GameTime time)
    {
        _time += time.DeltaSeconds;
        while (_time >= MinionWaveSchedule.WaveTime(_wavesSpawned + 1))
        {
            _wavesSpawned++;
            SpawnWave(_wavesSpawned);
        }
    }

    private void SpawnWave(int waveNumber)
    {
        EnsureLaneCorridors();
        var composition = MinionWaveSchedule.LaneComposition(waveNumber);
        foreach (var lane in _team.Lanes)
        {
            if (!_laneCorridors!.TryGetValue(lane.Lane, out var corridor) || corridor.Count == 0)
            {
                continue;
            }

            foreach (var type in composition)
            {
                SpawnMinion(type, corridor);
            }
        }

        Console.WriteLine(
            $"[MOBA.Server] {_team.Name} wave {waveNumber}: {composition.Count}/lane x {_team.Lanes.Count} lanes");
    }

    private void SpawnMinion(MinionType type, IReadOnlyList<Vector3D<float>> corridor)
    {
        var index = _spawnCounter++;
        var spawnPos = Snap(Fan(_team.SpawnAreaCenter, index, SpawnSpread));

        var minion = new MinionActor(spawnPos, _team.Name, type);
        var move = new MoveTargetComponent(minion, MinionSpeed);
        _ = new ReplicatedSpawnComponent(minion, ActorKind.Minion, TeamIds.FromName(_team.Name), (byte)type);

        // Walk the navmesh corridor, then a navmesh-snapped fan point around the
        // enemy nexus so the minions gather in a ring instead of stacking.
        var points = new List<Vector3D<float>>(corridor.Count + 1);
        for (var k = 0; k < corridor.Count - 1; k++)
        {
            points.Add(corridor[k]);
        }

        points.Add(Snap(Fan(corridor[^1], index, GatherRadius)));
        move.SetPath(points);

        _scene.AddActor(minion);
    }

    /// <summary>Deterministic golden-angle ring offset so minions don't stack.</summary>
    private static Vector3D<float> Fan(Vector3D<float> center, int index, float radius)
    {
        const float goldenAngle = 2.39996323f;
        var angle = index * goldenAngle;
        return center + new Vector3D<float>(MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);
    }

    private Vector3D<float> Snap(Vector3D<float> point) =>
        _navMesh.TryFindNearestPoint(point, out var snapped) ? snapped : point;

    /// <summary>
    /// Resolves each lane's authored anchor polyline into a navmesh-followed
    /// corner list once (cached): consecutive anchors are joined with
    /// <see cref="NavMesh.TryFindPath"/> so the minions only ever traverse
    /// walkable navmesh, never straight lines across obstacles.
    /// </summary>
    private void EnsureLaneCorridors()
    {
        if (_laneCorridors is not null)
        {
            return;
        }

        _laneCorridors = new Dictionary<string, IReadOnlyList<Vector3D<float>>>(StringComparer.Ordinal);
        var segment = new List<Vector3D<float>>();
        foreach (var lane in _team.Lanes)
        {
            var corridor = new List<Vector3D<float>>();
            var prev = Snap(_team.SpawnAreaCenter);
            var first = true;
            foreach (var anchor in lane.Waypoints)
            {
                if (!_navMesh.TryFindPath(prev, anchor, segment) || segment.Count == 0)
                {
                    continue;
                }
                // TryFindPath returns the snapped start as the first corner; skip
                // it on later segments so the joints don't duplicate.
                for (var i = first ? 0 : 1; i < segment.Count; i++)
                {
                    corridor.Add(segment[i]);
                }
                prev = segment[^1];
                first = false;
            }
            _laneCorridors[lane.Lane] = corridor;
        }
    }
}
