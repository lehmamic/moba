using System.Diagnostics;
using MOBA.Engine.Core.Assets;
using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Networking.Riptide;
using MOBA.Game;
using MOBA.Game.Actors;
using MOBA.Game.Components;
using MOBA.Game.Factories;
using MOBA.Game.Scenes;
using MOBA.Game.Systems;
using MOBA.Utilities;

namespace MOBA.Server;

/// <summary>
/// Server-side <see cref="GameHost"/>. Each server process hosts exactly one match:
/// crash isolation, per-process resource accounting, hot-restart, and the
/// industry-standard MOBA deployment model (see ADR-010). Scale to N matches by
/// launching N processes (Docker / k8s / systemd / multiple `dotnet run`).
/// </summary>
public sealed class ServerGame : GameHost
{
    private const float TickRate = 30f;
    private const float TickInterval = 1f / TickRate;

    public ServerGame()
    {
        var assets = new AssetManager();
        var scenesRoot = AbsolutePath.AppBaseDirectory / "assets" / "scenes";
        var mapsRoot = AbsolutePath.AppBaseDirectory / "assets" / "maps";
        assets.AddSceneCache(scenesRoot);
        assets.AddNavMeshCache(mapsRoot);
        AddSystem(assets);

        // Sim-only factory list — server has no rendering concerns. Adding a new
        // actor type means appending one new factory class and one entry here.
        var registry = new ActorFactoryRegistry([
            new MapActorFactory(assets),
            new BuildingActorFactory(Game.Scene),
            new MonsterActorFactory(),
            new TeamActorFactory(),
        ]);
        var sceneManager = new SceneManager(Game.Scene, definition => new GameSceneActor(definition, registry, assets));
        AddSystem(sceneManager);

        sceneManager.LoadScene(assets.LoadScene("dimension-rift.json"));
        var navMesh = Game.Scene.GetActor<MapActor>()!.NavMesh;
        Console.WriteLine($"[MOBA.Server] navmesh polys = {navMesh.PolyCount}");

        // Minion waves are game behaviour: one spawner component per team,
        // ticked by its TeamActor. Replication of the spawned minions is the
        // ActorReplicationSystem's job (below).
        foreach (var team in Game.Scene.Actors.OfType<TeamActor>())
        {
            _ = new MinionSpawnerComponent(team, Game.Scene, navMesh);
        }

        // Order matters: transport first so subsequent systems can subscribe
        // during their OnInitialize. PlayerConnectionSystem owns the
        // NetClientId → PlayerActor map; MovementSystem reads it to route
        // MoveCommands and broadcasts position updates.
        var transport = new RiptideServerTransport();
        AddSystem(transport);
        var connections = new PlayerConnectionSystem(Game.Scene, transport, navMesh);
        AddSystem(connections);
        // Generic replication runs before movement broadcasting so a newly
        // spawned descriptor actor is announced (and gets its network id) in the
        // same post-update tick its position first goes out.
        AddSystem(new ActorReplicationSystem(Game.Scene, transport));
        AddSystem(new MovementSystem(Game.Scene, transport, connections, navMesh));
    }

    public void Run()
    {
        Console.WriteLine($"[MOBA.Server] match starting @ {TickRate} Hz");
        Initialize();

        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Console.WriteLine("[MOBA.Server] Shutdown requested…");
            shutdown.Cancel();
        };

        var stopwatch = Stopwatch.StartNew();
        var tickCount = 0L;
        var nextReportTick = (long)(TickRate * 5);

        while (!shutdown.IsCancellationRequested)
        {
            var tickStart = stopwatch.Elapsed.TotalSeconds;
            Update(TickInterval);
            tickCount++;

            if (tickCount >= nextReportTick)
            {
                Console.WriteLine($"[MOBA.Server] tick {tickCount}, sim time {Game.TotalSeconds:F1}s, actors={Game.Scene.Actors.Count}");
                nextReportTick += (long)(TickRate * 5);
            }

            var elapsed = stopwatch.Elapsed.TotalSeconds - tickStart;
            var remaining = TickInterval - elapsed;
            if (remaining > 0)
            {
                shutdown.Token.WaitHandle.WaitOne((int)(remaining * 1000));
            }
        }

        Shutdown();
        Console.WriteLine($"[MOBA.Server] match stopped at tick {tickCount}.");
    }
}
