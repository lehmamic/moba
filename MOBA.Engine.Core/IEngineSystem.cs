namespace MOBA.Engine.Core;

/// <summary>
/// Lifecycle contract for things that the <see cref="GameHost"/> drives every tick:
/// the cache layer, the input facade, per-frame controllers, the (future) network
/// system. Rendering is intentionally *not* part of this contract because per-frame
/// cadence differs from per-tick cadence.
/// </summary>
public interface IEngineSystem : IDisposable
{
    void OnInitialize();

    void OnUpdate(GameTime time);

    void OnShutdown();
}
