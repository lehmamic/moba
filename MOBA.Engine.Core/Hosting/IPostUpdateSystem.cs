namespace MOBA.Engine.Core.Hosting;

/// <summary>
/// Opt-in hook for a system that needs to run <b>after</b> the per-tick simulation
/// (<c>Scene.Update</c>) has advanced actors and their components. Typical
/// users are server-side state broadcasters: the sim has produced the new
/// positions, OnPostUpdate observes them and pushes the deltas over the network.
/// The <see cref="GameHost"/> ticks <c>OnUpdate</c> on every system first, then
/// <c>Game.Update</c>, then <c>OnPostUpdate</c> on every system that implements
/// this interface.
/// </summary>
public interface IPostUpdateSystem
{
    void OnPostUpdate(GameTime time);
}
