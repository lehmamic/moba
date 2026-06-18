using MOBA.Engine.Core;
using MOBA.Engine.Graphics;
using Silk.NET.Input;

using MOBA.Game.Scenes;

namespace MOBA.Game.Client.Cameras;

/// <summary>
/// Holds the free-fly + top-down camera controllers; F1 toggles the active camera.
/// Implements <see cref="IEngineSystem"/> so its per-frame update flows through the
/// <see cref="GameHost"/> instead of being called manually from the entry point.
/// </summary>
public sealed class CameraSwitcher : IEngineSystem
{
    private readonly IKeyboard _keyboard;

    public CameraSwitcher(IInputContext input, float aspectRatio, CamerasDefinition? config = null)
    {
        FreeFly = new FreeFlyCameraController(input, aspectRatio, config?.FreeFly);
        TopDown = new TopDownCameraController(aspectRatio, config?.TopDown);
        ActiveCamera = string.Equals(config?.Default, "TopDown", StringComparison.OrdinalIgnoreCase)
            ? TopDown.Camera
            : FreeFly.Camera;

        _keyboard = input.Keyboards[0];
        _keyboard.KeyDown += OnKeyDown;
    }

    public FreeFlyCameraController FreeFly { get; }

    public TopDownCameraController TopDown { get; }

    public Camera ActiveCamera { get; private set; }

    public void Update(float deltaSeconds)
    {
        FreeFly.Update(deltaSeconds);
        TopDown.Update(deltaSeconds);
    }

    public void UpdateAspect(float aspectRatio)
    {
        FreeFly.Camera.AspectRatio = aspectRatio;
        TopDown.Camera.AspectRatio = aspectRatio;
    }

    public void OnInitialize() { }

    public void OnUpdate(GameTime time) => Update(time.DeltaSeconds);

    public void OnShutdown() => _keyboard.KeyDown -= OnKeyDown;

    public void Dispose() { }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.F1)
        {
            ActiveCamera = ReferenceEquals(ActiveCamera, FreeFly.Camera)
                ? TopDown.Camera
                : FreeFly.Camera;
        }
    }
}
