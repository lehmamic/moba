using MOBA.Engine.Graphics.Rendering;
using MOBA.Game.Scenes;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Cameras;

/// <summary>
/// LoL-style top-down camera. The camera looks at a ground-level target;
/// height + back-offset are derived from the current zoom level so the
/// pitch stays constant while zooming. Pan is driven by keyboard (arrow
/// keys) and by holding the mouse against the viewport edge; mouse wheel
/// zooms in / out within hard-coded bounds.
/// </summary>
public sealed class TopDownCameraController
{
    private const float PanSpeedUnitsPerSecond = 35f;
    // ~2% of a 1080p frame — close to LoL's edge-pan trigger zone.
    private const float EdgePanThresholdPixels = 24f;
    private const float ZoomMinHeight = 18f;
    private const float ZoomMaxHeight = 45f;
    private const float ZoomStepPerNotch = 3f;

    /// <summary>
    /// Ratio between the back-offset (Z) and the height (Y) — sets the
    /// camera pitch. <c>0.7</c> ≈ a 55° pitch (height / hypotenuse), which
    /// matches LoL's classic angle reasonably well.
    /// </summary>
    private const float BackOffsetRatio = 0.7f;

    private readonly IKeyboard _keyboard;
    private readonly IMouse _mouse;
    private readonly Func<Vector2D<int>> _viewportSizePixels;

    private Vector3D<float> _target;
    private float _height;
    private Vector2D<float>? _panBoundsMin;
    private Vector2D<float>? _panBoundsMax;

    public TopDownCameraController(
        IInputContext input,
        float aspectRatio,
        Func<Vector2D<int>> viewportSizePixels,
        CameraDefinition? config = null)
    {
        _keyboard = input.Keyboards[0];
        _mouse = input.Mice[0];
        _viewportSizePixels = viewportSizePixels;
        _mouse.Scroll += OnScroll;

        var pos = config?.Position is { Length: 3 } p
            ? new Vector3D<float>(p[0], p[1], p[2])
            : new Vector3D<float>(0f, 25f, 17f);
        _height = Math.Clamp(pos.Y, ZoomMinHeight, ZoomMaxHeight);
        _target = config?.Target is { Length: 3 } t
            ? new Vector3D<float>(t[0], t[1], t[2])
            : Vector3D<float>.Zero;

        Camera = new Camera
        {
            Up = Vector3D<float>.UnitY,
            FieldOfViewRadians = config?.FieldOfViewRadians ?? MathF.PI / 4f,
            AspectRatio = aspectRatio,
        };
        ApplyTransform();
    }

    public Camera Camera { get; }

    /// <summary>Re-centres the camera on a world-space point (Y is ignored — the camera always looks at ground level).</summary>
    public void CenterOn(Vector3D<float> worldPosition)
    {
        _target = new Vector3D<float>(worldPosition.X, 0f, worldPosition.Z);
        ApplyTransform();
    }

    /// <summary>
    /// Constrains the pan target to a rectangle in the X/Z plane. Bound
    /// values are world-space coordinates; the camera target is clamped to
    /// stay inside so the camera never wanders off the playable field.
    /// Called by the host after the scene loads — before that the controller
    /// pans without limit.
    /// </summary>
    public void SetPanBounds(Vector2D<float> min, Vector2D<float> max)
    {
        _panBoundsMin = min;
        _panBoundsMax = max;
        ApplyTransform();
    }

    public void Update(float deltaSeconds)
    {
        var pan = ReadPanDirection();
        if (pan.LengthSquared > 0f)
        {
            _target += Vector3D.Normalize(pan) * (PanSpeedUnitsPerSecond * deltaSeconds);
            ApplyTransform();
        }
    }

    private Vector3D<float> ReadPanDirection()
    {
        var pan = Vector3D<float>.Zero;

        // Arrow-key pan. Up/Down move the target along -Z / +Z (into / out of
        // the screen in our RH Y-up convention); Left/Right map to -X / +X.
        if (_keyboard.IsKeyPressed(Key.Up))
        {
            pan += new Vector3D<float>(0f, 0f, -1f);
        }
        if (_keyboard.IsKeyPressed(Key.Down))
        {
            pan += new Vector3D<float>(0f, 0f, 1f);
        }
        if (_keyboard.IsKeyPressed(Key.Left))
        {
            pan += new Vector3D<float>(-1f, 0f, 0f);
        }
        if (_keyboard.IsKeyPressed(Key.Right))
        {
            pan += new Vector3D<float>(1f, 0f, 0f);
        }

        // Edge-pan: mouse held within EdgePanThresholdPixels of a viewport
        // edge nudges the target in that direction. Mouse Y grows downward
        // in window-space; that maps to +Z in world-space.
        var size = _viewportSizePixels();
        var pos = _mouse.Position;
        // Skip edge-pan entirely when the cursor is outside the window —
        // otherwise the stale OS-reported position (often clamped to the
        // edge) keeps panning the camera while the user is interacting with
        // some other app.
        var mouseInsideWindow = size.X > 0 && size.Y > 0
            && pos.X >= 0f && pos.X < size.X
            && pos.Y >= 0f && pos.Y < size.Y;
        if (mouseInsideWindow)
        {
            if (pos.X <= EdgePanThresholdPixels)
            {
                pan += new Vector3D<float>(-1f, 0f, 0f);
            }
            if (pos.X >= size.X - EdgePanThresholdPixels)
            {
                pan += new Vector3D<float>(1f, 0f, 0f);
            }
            if (pos.Y <= EdgePanThresholdPixels)
            {
                pan += new Vector3D<float>(0f, 0f, -1f);
            }
            if (pos.Y >= size.Y - EdgePanThresholdPixels)
            {
                pan += new Vector3D<float>(0f, 0f, 1f);
            }
        }
        return pan;
    }

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        // wheel.Y > 0 = scroll up = zoom in (decrease height); LoL convention.
        _height = Math.Clamp(_height - wheel.Y * ZoomStepPerNotch, ZoomMinHeight, ZoomMaxHeight);
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        if (_panBoundsMin is { } min && _panBoundsMax is { } max)
        {
            _target = new Vector3D<float>(
                Math.Clamp(_target.X, min.X, max.X),
                _target.Y,
                Math.Clamp(_target.Z, min.Y, max.Y));
        }
        var back = _height * BackOffsetRatio;
        Camera.Position = new Vector3D<float>(_target.X, _height, _target.Z + back);
        Camera.Target = _target;
    }
}
