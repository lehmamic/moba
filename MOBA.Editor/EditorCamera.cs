using Avalonia.Input;
using Avalonia.Interactivity;
using MOBA.Engine.Graphics.Rendering;
using Silk.NET.Maths;
using AvaPoint = Avalonia.Point;

namespace MOBA.Editor;

/// <summary>
/// Stripped-down free-fly camera for the editor viewport. WASD pans along
/// the camera's forward / right, QE moves vertically along world up, hold
/// right-mouse-button + drag to look, mouse wheel zooms (moves along
/// forward). Drives an internal <see cref="Camera"/> the
/// <see cref="SceneViewport"/> hands to the renderer each frame.
///
/// <para>
/// Wires its own input through Avalonia's pointer + key events on the host
/// control — no Silk.NET.Input stack is pulled in for the editor, the two
/// input systems would only fight over the same OS callbacks.
/// </para>
/// </summary>
public sealed class EditorCamera
{
    private const float MoveSpeed = 18f;
    private const float LookSensitivity = 0.005f;
    private const float ZoomStepPerNotch = 2.5f;
    private const float PitchLimit = (MathF.PI / 2f) - 0.01f;

    private readonly HashSet<Key> _keysHeld = [];
    private float _yaw = -MathF.PI / 2f;
    private float _pitch = -0.46f;
    private AvaPoint? _lastMousePos;
    private bool _isLooking;

    public EditorCamera(InputElement host)
    {
        Camera = new Camera
        {
            Position = new Vector3D<float>(0f, 25f, 40f),
            AspectRatio = 16f / 9f,
            FieldOfViewRadians = MathF.PI / 4f,
        };

        ApplyOrientation();

        host.Focusable = true;
        host.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        host.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        host.PointerPressed += OnPointerPressed;
        host.PointerMoved += OnPointerMoved;
        host.PointerReleased += OnPointerReleased;
        host.PointerWheelChanged += OnPointerWheelChanged;
    }

    public Camera Camera { get; }

    public void Tick(float deltaSeconds)
    {
        var forward = ComputeForward();
        var right = Vector3D.Normalize(Vector3D.Cross(forward, Vector3D<float>.UnitY));

        var move = Vector3D<float>.Zero;
        var hasInput = false;
        if (_keysHeld.Contains(Key.W))
        {
            move += forward; hasInput = true;
        }

        if (_keysHeld.Contains(Key.S))
        {
            move -= forward; hasInput = true;
        }

        if (_keysHeld.Contains(Key.D))
        {
            move += right; hasInput = true;
        }

        if (_keysHeld.Contains(Key.A))
        {
            move -= right; hasInput = true;
        }

        if (_keysHeld.Contains(Key.E))
        {
            move += Vector3D<float>.UnitY; hasInput = true;
        }

        if (_keysHeld.Contains(Key.Q))
        {
            move -= Vector3D<float>.UnitY; hasInput = true;
        }

        if (hasInput)
        {
            Camera.Position += Vector3D.Normalize(move) * (MoveSpeed * deltaSeconds);
        }

        ApplyOrientation();
    }

    private void ApplyOrientation() => Camera.Target = Camera.Position + ComputeForward();

    private Vector3D<float> ComputeForward()
    {
        var cosPitch = MathF.Cos(_pitch);
        return new Vector3D<float>(
            cosPitch * MathF.Cos(_yaw),
            MathF.Sin(_pitch),
            cosPitch * MathF.Sin(_yaw));
    }

    private void OnKeyDown(object? sender, KeyEventArgs e) => _keysHeld.Add(e.Key);

    private void OnKeyUp(object? sender, KeyEventArgs e) => _keysHeld.Remove(e.Key);

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not InputElement host)
        {
            return;
        }
        host.Focus();
        if (e.GetCurrentPoint(host).Properties.IsRightButtonPressed)
        {
            _isLooking = true;
            _lastMousePos = e.GetPosition(host);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isLooking || _lastMousePos is null || sender is not InputElement host)
        {
            return;
        }
        var pos = e.GetPosition(host);
        var dx = (float)(pos.X - _lastMousePos.Value.X);
        var dy = (float)(pos.Y - _lastMousePos.Value.Y);
        _lastMousePos = pos;
        _yaw -= dx * LookSensitivity;
        _pitch -= dy * LookSensitivity;
        _pitch = Math.Clamp(_pitch, -PitchLimit, PitchLimit);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            _isLooking = false;
            _lastMousePos = null;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var forward = ComputeForward();
        Camera.Position += forward * ((float)e.Delta.Y * ZoomStepPerNotch);
        ApplyOrientation();
    }
}
