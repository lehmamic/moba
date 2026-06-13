using MOBA.Engine.Core;
using Silk.NET.Input;

namespace MOBA.Game.Client;

/// <summary>
/// Debug-key dispatcher (client-only). Listens for keyboard events on the same
/// input context the rest of the client uses, and toggles debug overlays in
/// response. Today it owns F2 ↔ navmesh wireframe; more flags can add their own
/// key bindings here. The overlay is a regular scene actor — this system flips
/// its <see cref="NavMeshOverlayActor.IsVisible"/> passthrough.
/// </summary>
public sealed class DebugOverlaySystem : IEngineSystem
{
    private readonly IKeyboard _keyboard;
    private readonly NavMeshOverlayActor _navMeshOverlay;

    public DebugOverlaySystem(IInputContext input, NavMeshOverlayActor navMeshOverlay)
    {
        _keyboard = input.Keyboards[0];
        _navMeshOverlay = navMeshOverlay;
        _keyboard.KeyDown += OnKeyDown;
    }

    public void OnInitialize() { }

    public void OnUpdate(GameTime time) { }

    public void OnShutdown() => _keyboard.KeyDown -= OnKeyDown;

    public void Dispose() { }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.F2)
        {
            _navMeshOverlay.IsVisible = !_navMeshOverlay.IsVisible;
        }
    }
}
