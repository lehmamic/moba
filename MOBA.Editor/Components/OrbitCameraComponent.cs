using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Core.Input;
using MOBA.Engine.Graphics.Rendering;
using Silk.NET.Maths;

namespace MOBA.Editor.Components;

/// <summary>
/// Maya / Blender / Unity-orbit-mode camera controller, attached to a
/// <see cref="CameraActor"/>. State (pivot, yaw, pitch, distance) lives on
/// the component; the actor's <see cref="CameraActor.Camera"/> is rebuilt
/// every <see cref="OnUpdate"/> from that state.
///
/// <para>
/// Input arrives engine-side through <see cref="OnProcessInput"/> as an
/// <see cref="InputState"/> snapshot — no Avalonia / Silk types ever touch
/// this component. The Tick splits along the obvious axes:
/// </para>
/// <list type="bullet">
///   <item><b>Left-mouse-drag</b> — pan: pivot + camera move together along
///         the view-aligned right / up axes, scaled by distance.</item>
///   <item><b>Right-mouse-drag</b> — orbit around the pivot (yaw / pitch).</item>
///   <item><b>Mouse wheel</b> — multiplicative dolly: change camera-to-pivot
///         distance by a fixed fraction per notch (zoom feels symmetric in
///         and out).</item>
///   <item><b>WASD / QE</b> — fly the pivot along forward / right / world-up;
///         camera follows.</item>
/// </list>
/// </summary>
public sealed class OrbitCameraComponent : Component
{
    private const float KeySpeed = 18f;
    private const float OrbitSensitivity = 0.005f;
    private const float ZoomStepPerNotch = 0.1f;
    private const float PanSensitivity = 0.0025f;
    private const float PitchLimit = (MathF.PI / 2f) - 0.01f;
    private const float MinDistance = 1f;

    private readonly CameraActor _camera;
    private Vector3D<float> _pivot = Vector3D<float>.Zero;
    private float _distance = 50f;
    private float _yaw = -MathF.PI / 2f;
    private float _pitch = -0.5f;
    private InputState _lastInput;

    public OrbitCameraComponent(CameraActor owner) : base(owner)
    {
        _camera = owner;
        _camera.Camera.FieldOfViewRadians = MathF.PI / 4f;
        RebuildCameraTransform();
    }

    public override void OnProcessInput(InputState state) => _lastInput = state;

    public override void OnUpdate(GameTime time)
    {
        var input = _lastInput;
        var dirty = false;

        if (input.RightMouseDown && input.MouseDelta != default)
        {
            _yaw -= input.MouseDelta.X * OrbitSensitivity;
            _pitch += input.MouseDelta.Y * OrbitSensitivity;
            _pitch = Math.Clamp(_pitch, -PitchLimit, PitchLimit);
            dirty = true;
        }

        if (input.LeftMouseDown && input.MouseDelta != default)
        {
            var forward = ComputeForward();
            var right = Vector3D.Normalize(Vector3D.Cross(forward, Vector3D<float>.UnitY));
            var up = Vector3D.Normalize(Vector3D.Cross(right, forward));
            var scale = PanSensitivity * _distance;
            _pivot += (-right * (input.MouseDelta.X * scale)) + (up * (input.MouseDelta.Y * scale));
            dirty = true;
        }

        if (input.WheelDelta.Y != 0f)
        {
            // Multiplicative dolly so zoom feels symmetric in/out.
            var factor = MathF.Pow(1f - ZoomStepPerNotch, input.WheelDelta.Y);
            _distance = MathF.Max(MinDistance, _distance * factor);
            dirty = true;
        }

        var keys = input.KeysDown;
        if (keys != EngineKeys.None)
        {
            var forward = ComputeForward();
            var right = Vector3D.Normalize(Vector3D.Cross(forward, Vector3D<float>.UnitY));
            var move = Vector3D<float>.Zero;
            var hasInput = false;
            if ((keys & EngineKeys.W) != 0)
            {
                move += forward; hasInput = true;
            }

            if ((keys & EngineKeys.S) != 0)
            {
                move -= forward; hasInput = true;
            }

            if ((keys & EngineKeys.D) != 0)
            {
                move += right; hasInput = true;
            }

            if ((keys & EngineKeys.A) != 0)
            {
                move -= right; hasInput = true;
            }

            if ((keys & EngineKeys.E) != 0)
            {
                move += Vector3D<float>.UnitY; hasInput = true;
            }

            if ((keys & EngineKeys.Q) != 0)
            {
                move -= Vector3D<float>.UnitY; hasInput = true;
            }

            if (hasInput)
            {
                _pivot += Vector3D.Normalize(move) * (KeySpeed * time.DeltaSeconds);
                dirty = true;
            }
        }

        if (dirty)
        {
            RebuildCameraTransform();
        }
    }

    /// <summary>Updates the actor's Camera transform from pivot / yaw / pitch / distance.</summary>
    private void RebuildCameraTransform()
    {
        var forward = ComputeForward();
        _camera.Camera.Position = _pivot - (forward * _distance);
        _camera.Camera.Target = _pivot;
    }

    /// <summary>
    /// Unit vector from camera toward pivot, derived from the spherical
    /// (yaw, pitch) angles.
    /// </summary>
    private Vector3D<float> ComputeForward()
    {
        var cosPitch = MathF.Cos(_pitch);

        return new Vector3D<float>(
            cosPitch * MathF.Cos(_yaw),
            MathF.Sin(_pitch),
            cosPitch * MathF.Sin(_yaw));
    }
}
