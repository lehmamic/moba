using MOBA.Engine.Core;
using Silk.NET.Input;

namespace MOBA.Game.Client;

/// <summary>
/// Debug-key dispatcher (client-only). Listens for keyboard events on the same
/// input context the rest of the client uses, and toggles debug overlays in
/// response. F2 flips the navmesh wireframe, F3 the per-player path overlay.
/// Both overlays are regular scene actors with an <c>IsVisible</c> passthrough
/// — this system never touches their internal renderer state directly.
/// </summary>
public sealed class DebugOverlaySystem : IEngineSystem
{
    private readonly IKeyboard _keyboard;
    private readonly NavMeshOverlayActor _navMeshOverlay;
    private readonly PathOverlayActor _pathOverlay;

    public DebugOverlaySystem(IInputContext input, NavMeshOverlayActor navMeshOverlay, PathOverlayActor pathOverlay)
    {
        _keyboard = input.Keyboards[0];
        _navMeshOverlay = navMeshOverlay;
        _pathOverlay = pathOverlay;
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
                _navMeshOverlay.IsVisible = !_navMeshOverlay.IsVisible;
                break;
            case Key.F3:
                _pathOverlay.IsVisible = !_pathOverlay.IsVisible;
                break;
        }
    }
}
