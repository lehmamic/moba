namespace MOBA.Engine.Core;

/// <summary>
/// Sim host. Owns the Scene and advances the simulation one step at a time.
/// The actual loop lives in the entry point:
/// <list type="bullet">
///   <item>Server: fixed-step loop calls <see cref="Update"/> at a constant rate.</item>
///   <item>Client: the Silk.NET window's Update callback calls <see cref="Update"/> with a variable dt.</item>
/// </list>
/// </summary>
public class Game
{
    public Scene Scene { get; } = new();

    public double TotalSeconds { get; private set; }

    public void Update(float deltaSeconds)
    {
        TotalSeconds += deltaSeconds;
        Scene.Update(new GameTime(deltaSeconds, TotalSeconds));
    }

    public void Shutdown() => Scene.Shutdown();
}
