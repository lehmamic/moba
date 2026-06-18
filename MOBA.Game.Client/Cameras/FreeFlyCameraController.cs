using MOBA.Engine.Graphics.Rendering;
using MOBA.Game.Scenes;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Cameras;

/// <summary>
/// Debug camera. WASD moves along the camera's forward/right vectors, Q/E moves vertically
/// along world up (+Y). Hold right mouse button + drag for mouse-look (yaw around +Y,
/// pitch around camera-right). RH Y-up; forward vector is (cosP·cosY, sinP, cosP·sinY).
/// </summary>
public sealed class FreeFlyCameraController
{
    private const float MoveSpeed = 15f;
    private const float LookSensitivity = 0.003f;
    private const float PitchLimit = (MathF.PI / 2f) - 0.01f;

    private readonly IKeyboard _keyboard;
    private readonly IMouse _mouse;

    private float _yaw = -MathF.PI / 2f;
    private float _pitch = -0.46f;
    private System.Numerics.Vector2 _lastMousePos;
    private bool _isLooking;

    public FreeFlyCameraController(IInputContext input, float aspectRatio, CameraDefinition? config = null)
    {
        _keyboard = input.Keyboards[0];
        _mouse = input.Mice[0];

        var pos = config?.Position is { Length: 3 } p
            ? new Vector3D<float>(p[0], p[1], p[2])
            : new Vector3D<float>(0f, 10f, 20f);
        _yaw = config?.Yaw ?? -MathF.PI / 2f;
        _pitch = Math.Clamp(config?.Pitch ?? -0.46f, -PitchLimit, PitchLimit);

        Camera = new Camera
        {
            Position = pos,
            AspectRatio = aspectRatio,
        };
        if (config?.FieldOfViewRadians is { } fov)
        {
            Camera.FieldOfViewRadians = fov;
        }
        ApplyOrientation();
    }

    public Camera Camera { get; }

    public void Update(float deltaSeconds)
    {
        HandleMouseLook();
        HandleMovement(deltaSeconds);
        ApplyOrientation();
    }

    private void HandleMouseLook()
    {
        var pos = _mouse.Position;
        if (_mouse.IsButtonPressed(MouseButton.Right))
        {
            if (!_isLooking)
            {
                _lastMousePos = pos;
                _isLooking = true;
                return;
            }
            var delta = pos - _lastMousePos;
            _lastMousePos = pos;
            _yaw -= delta.X * LookSensitivity;
            _pitch -= delta.Y * LookSensitivity;
            _pitch = Math.Clamp(_pitch, -PitchLimit, PitchLimit);
        }
        else
        {
            _isLooking = false;
        }
    }

    private void HandleMovement(float deltaSeconds)
    {
        var forward = ComputeForward();
        var right = Vector3D.Normalize(Vector3D.Cross(forward, Vector3D<float>.UnitY));

        var move = Vector3D<float>.Zero;
        var hasInput = false;

        if (_keyboard.IsKeyPressed(Key.W))
        {
            move += forward;
            hasInput = true;
        }
        if (_keyboard.IsKeyPressed(Key.S))
        {
            move -= forward;
            hasInput = true;
        }
        if (_keyboard.IsKeyPressed(Key.D))
        {
            move += right;
            hasInput = true;
        }
        if (_keyboard.IsKeyPressed(Key.A))
        {
            move -= right;
            hasInput = true;
        }
        if (_keyboard.IsKeyPressed(Key.E))
        {
            move += Vector3D<float>.UnitY;
            hasInput = true;
        }
        if (_keyboard.IsKeyPressed(Key.Q))
        {
            move -= Vector3D<float>.UnitY;
            hasInput = true;
        }

        if (hasInput)
        {
            Camera.Position += Vector3D.Normalize(move) * (MoveSpeed * deltaSeconds);
        }
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
}
