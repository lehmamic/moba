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
        AddSystem(assets);

        var map = Map.FromDefinition(assets.LoadMap("default.json"));

        var world = new MobaWorld(map);
        world.Populate(Game.Scene);

        // Attach networking + movement components to the cube (id = 2 by convention).
        foreach (var actor in Game.Scene.Actors)
        {
            if (actor is TestCubeActor cube)
            {
                _ = new NetworkIdentityComponent(cube, 2);
                _ = new MoveTargetComponent(cube);
            }
        }

        // Order matters: transport first (so MovementSystem.OnInitialize sees a live
        // MessageReceived event source), then the systems that subscribe.
        var transport = new RiptideServerTransport();
        AddSystem(transport);
        AddSystem(new MovementSystem(Game.Scene, transport));
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
