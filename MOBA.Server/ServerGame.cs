using System.Diagnostics;
using MOBA.Engine.Core;
using MOBA.Engine.Networking.Riptide;
using MOBA.Game;
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
        // Single AssetManager for every server-side asset type. AddMapCache lives in
        // MOBA.Game as an extension method so both server and client deserialise the
        // same JSON the same way.
        var assets = new AssetManager();
        var mapsRoot = AbsolutePath.AppBaseDirectory / "assets" / "maps";
        assets.AddMapCache(mapsRoot);
        assets.AddNavMeshCache(mapsRoot);
        AddSystem(assets);

        var map = Map.FromDefinition(assets.LoadMap("dimension-rift.json"));
        // Generated out-of-game by tools/MOBA.Tools.NavMeshGen. Server is authoritative
        // for movement validation, so the navmesh load is mandatory.
        var navMesh = assets.LoadNavMesh(map.NavMesh);
        Console.WriteLine($"[MOBA.Server] navmesh polys = {navMesh.PolyCount}");

        var world = new MobaWorld(map, navMesh);
        world.Populate(Game.Scene);

        // Order matters: transport first so subsequent systems can subscribe
        // during their OnInitialize. PlayerConnectionSystem owns the
        // NetClientId → PlayerActor map; MovementSystem reads it to route
        // MoveCommands and broadcasts position updates.
        var transport = new RiptideServerTransport();
        AddSystem(transport);
        var connections = new PlayerConnectionSystem(Game.Scene, transport);
        AddSystem(connections);
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
