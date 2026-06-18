using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Client.Actors;
using Silk.NET.Input;

namespace MOBA.Game.Client.Systems;

/// <summary>
/// Debug-key dispatcher (client-only). F2 flips the navmesh wireframe, F3 the
/// per-player path overlay, F4 cycles the registered debug scenes via the
/// host callback. Picks the overlay actors out of <see cref="Scene"/> on each
/// keypress instead of holding refs — survives scene switches automatically
/// because the new scene's overlays are visible the moment they're added to
/// the scene.
/// </summary>
public sealed class DebugOverlaySystem : IEngineSystem
{
    private readonly IKeyboard _keyboard;
    private readonly Scene _scene;
    private readonly Action? _onSceneSwitch;

    public DebugOverlaySystem(IInputContext input, Scene scene, Action? onSceneSwitch = null)
    {
        _keyboard = input.Keyboards[0];
        _scene = scene;
        _onSceneSwitch = onSceneSwitch;
        _keyboard.KeyDown += OnKeyDown;
    }

    public void OnInitialize() { }

    public void OnUpdate(GameTime time) { }

    public void OnShutdown() => _keyboard.KeyDown -= OnKeyDown;

    public void Dispose() { }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        switch (key)
        {
            case Key.F2:
                if (FindOverlay<NavMeshOverlayActor>() is { } nav)
                {
                    nav.IsVisible = !nav.IsVisible;
                }
                break;
            case Key.F3:
                if (FindOverlay<PathOverlayActor>() is { } path)
                {
                    path.IsVisible = !path.IsVisible;
                }
                break;
            case Key.F4:
                _onSceneSwitch?.Invoke();
                break;
        }
    }

    private T? FindOverlay<T>() where T : Actor
    {
        foreach (var a in _scene.Actors)
        {
            if (a is T t)
            {
                return t;
            }
        }
        return null;
    }
}
