using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Graphics.Rendering;
using MOBA.Game.Scenes;
using Silk.NET.Input;

namespace MOBA.Game.Client.Cameras;

/// <summary>
/// Holds the free-fly + top-down camera controllers; F1 toggles the active camera.
/// Implements <see cref="IEngineSystem"/> so its per-frame update flows through the
/// <see cref="GameHost"/> instead of being called manually from the entry point.
/// </summary>
public sealed class CameraSwitcher : IEngineSystem
{
    private readonly IKeyboard _keyboard;

    public CameraSwitcher(
        IInputContext input,
        float aspectRatio,
        Func<Silk.NET.Maths.Vector2D<int>> viewportSizePixels,
        CamerasDefinition? config = null)
    {
        FreeFly = new FreeFlyCameraController(input, aspectRatio, config?.FreeFly);
        TopDown = new TopDownCameraController(input, aspectRatio, viewportSizePixels, config?.TopDown);
        ActiveCamera = string.Equals(config?.Default, "FreeFly", StringComparison.OrdinalIgnoreCase)
            ? FreeFly.Camera
            : TopDown.Camera;

        _keyboard = input.Keyboards[0];
        _keyboard.KeyDown += OnKeyDown;
    }

    public FreeFlyCameraController FreeFly { get; }

    public TopDownCameraController TopDown { get; }

    public Camera ActiveCamera { get; private set; }

    public void Update(float deltaSeconds)
    {
        // Only the active controller gets to tick — otherwise the inactive
        // camera silently drifts (WASD moves the FreeFly camera while the
        // player is looking through TopDown, etc.).
        if (ReferenceEquals(ActiveCamera, FreeFly.Camera))
        {
            FreeFly.Update(deltaSeconds);
        }
        else
        {
            TopDown.Update(deltaSeconds);
        }
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
