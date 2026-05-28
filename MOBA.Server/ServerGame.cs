using System.Diagnostics;
using MOBA.Engine.Core;
using MOBA.Game;

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
        // No client-bound systems yet — Networking arrives in a later phase.
        var world = new MobaWorld(Map.LeagueSized());
        world.Populate(Game.Scene);
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
