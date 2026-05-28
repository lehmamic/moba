using System.Diagnostics;
using MOBA.Engine.Core;
using MOBA.Engine.Networking;
using MOBA.Game;

const float TickRate = 30f;
const float TickInterval = 1f / TickRate;

Console.WriteLine($"[MOBA.Server] Headless server starting @ {TickRate} Hz");

using var transport = new NullTransport();
var game = new Game();
var world = new MobaWorld(Map.LeagueSized());
world.Populate(game.Scene);

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    Console.WriteLine("[MOBA.Server] Shutdown requested…");
    shutdown.Cancel();
};

var tickCount = 0L;
var nextReportTick = (long)(TickRate * 5);
var stopwatch = Stopwatch.StartNew();

while (!shutdown.IsCancellationRequested)
{
    var tickStartSeconds = stopwatch.Elapsed.TotalSeconds;

    transport.Poll();
    game.Tick(TickInterval);
    tickCount++;

    if (tickCount >= nextReportTick)
    {
        Console.WriteLine($"[MOBA.Server] tick {tickCount}, sim time {game.TotalSeconds:F1}s, actors={game.Scene.Actors.Count}");
        nextReportTick += (long)(TickRate * 5);
    }

    var elapsedSeconds = stopwatch.Elapsed.TotalSeconds - tickStartSeconds;
    var remainingSeconds = TickInterval - elapsedSeconds;
    if (remainingSeconds > 0)
    {
        shutdown.Token.WaitHandle.WaitOne((int)(remainingSeconds * 1000));
    }
}

game.Shutdown();
Console.WriteLine($"[MOBA.Server] Stopped at tick {tickCount}.");
