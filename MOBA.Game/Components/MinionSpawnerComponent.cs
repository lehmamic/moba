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
/// gets a <c>MoveTargetComponent</c> walking its lane's authored polyline to the
/// enemy nexus (where it gathers, fanned out) and a <c>ReplicatedSpawnComponent</c>
/// so the generic <c>ActorReplicationSystem</c> assigns its id and broadcasts it
/// to clients. Server-authoritative; no networking code lives here.
/// </summary>
public sealed class MinionSpawnerComponent : Component
{
    private const float MinionSpeed = 3.5f;
    private const float SpawnSpread = 1.2f;
    private const float GatherRadius = 4f;

    private readonly Scene _scene;
    private readonly NavMesh _navMesh;
    private readonly TeamActor _team;
    private Dictionary<string, IReadOnlyList<Vector3D<float>>>? _snappedLanes;
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
        EnsureSnappedLanes();
        var composition = MinionWaveSchedule.LaneComposition(waveNumber);
        foreach (var lane in _team.Lanes)
        {
            if (!_snappedLanes!.TryGetValue(lane.Lane, out var path) || path.Count == 0)
            {
                continue;
            }
            foreach (var type in composition)
            {
                SpawnMinion(type, path);
            }
        }
        Console.WriteLine(
            $"[MOBA.Server] {_team.Name} wave {waveNumber}: {composition.Count}/lane x {_team.Lanes.Count} lanes");
    }

    private void SpawnMinion(MinionType type, IReadOnlyList<Vector3D<float>> lanePath)
    {
        var index = _spawnCounter++;
        var spawnPos = Fan(_team.SpawnAreaCenter, index, SpawnSpread);
        if (_navMesh.TryFindNearestPoint(spawnPos, out var snapped))
        {
            spawnPos = snapped;
        }

        var minion = new MinionActor(spawnPos, _team.Name, type);
        var move = new MoveTargetComponent(minion, MinionSpeed);
        _ = new ReplicatedSpawnComponent(minion, ActorKind.Minion, TeamIds.FromName(_team.Name), (byte)type);

        // Walk the lane corridor, then fan out around the enemy nexus to gather.
        var points = new List<Vector3D<float>>(lanePath.Count);
        for (var k = 0; k < lanePath.Count - 1; k++)
        {
            points.Add(lanePath[k]);
        }
        points.Add(Fan(lanePath[^1], index, GatherRadius));
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

    private void EnsureSnappedLanes()
    {
        if (_snappedLanes is not null)
        {
            return;
        }

        _snappedLanes = new Dictionary<string, IReadOnlyList<Vector3D<float>>>(StringComparer.Ordinal);

        foreach (var lane in _team.Lanes)
        {
            var snapped = new List<Vector3D<float>>(lane.Waypoints.Count);
            foreach (var wp in lane.Waypoints)
            {
                snapped.Add(_navMesh.TryFindNearestPoint(wp, out var s) ? s : wp);
            }
            _snappedLanes[lane.Lane] = snapped;
        }
    }
}
