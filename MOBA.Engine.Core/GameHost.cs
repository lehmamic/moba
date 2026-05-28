namespace MOBA.Engine.Core;

/// <summary>
/// Shared base for the client- and server-side entry hosts. Owns the sim
/// <see cref="Core.Game"/> and an <see cref="IEngineSystem"/> list, and propagates
/// the per-tick lifecycle. Subclasses (<c>ClientGame</c>, <c>ServerGame</c>) provide
/// their own <c>Run()</c> because the cadence differs (window vs fixed-step loop).
///
/// Shutdown order is LIFO of registration so later systems (which may depend on
/// earlier ones) tear down first.
/// </summary>
public abstract class GameHost : IDisposable
{
    private readonly List<IEngineSystem> _systems = [];
    private bool _shutDown;

    public Game Game { get; } = new();

    public IReadOnlyList<IEngineSystem> Systems => _systems;

    protected void AddSystem(IEngineSystem system) => _systems.Add(system);

    public virtual void Initialize()
    {
        foreach (var system in _systems)
        {
            system.OnInitialize();
        }
    }

    public virtual void Update(float deltaSeconds)
    {
        var time = new GameTime(deltaSeconds, Game.TotalSeconds + deltaSeconds);
        foreach (var system in _systems)
        {
            system.OnUpdate(time);
        }
        Game.Update(deltaSeconds);
    }

    public virtual void Shutdown()
    {
        if (_shutDown)
        {
            return;
        }
        for (var i = _systems.Count - 1; i >= 0; i--)
        {
            _systems[i].OnShutdown();
        }
        Game.Shutdown();
        _shutDown = true;
    }

    public virtual void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }
}
