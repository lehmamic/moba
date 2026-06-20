using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MOBA.Engine.Core.Input;
using Silk.NET.Maths;
using AvaPoint = Avalonia.Point;

namespace MOBA.Editor.Input;

/// <summary>
/// Translates Avalonia pointer + key events into the engine's
/// <see cref="InputState"/> snapshot. The editor's <c>SceneViewport</c> owns
/// one of these, hands the per-frame snapshot to <c>Scene.ProcessInput</c>,
/// and engine-side components (<c>OrbitCameraComponent</c>) consume the
/// snapshot exactly the same way a Silk.NET-driven game client does. No
/// Avalonia type leaks past this wall.
///
/// <para>
/// Events are tapped at the host's <see cref="TopLevel"/> in the tunnelling
/// phase because <c>OpenGlControlBase</c> is hit-test transparent — direct
/// pointer subscriptions on it never fire. Pointer presses outside the host's
/// bounds are ignored so clicking on the inspector sidebar doesn't start a
/// camera drag.
/// </para>
/// </summary>
public sealed class AvaloniaInputSource
{
    private readonly InputElement _host;
    private AvaPoint _currentPos;
    private AvaPoint _lastSnapshotPos;
    private bool _left, _right, _middle;
    private bool _prevLeft, _prevRight, _prevMiddle;
    private Vector2D<float> _wheelAccum;
    private EngineKeys _keysDown;

    public AvaloniaInputSource(InputElement host)
    {
        _host = host;
        // AvaloniaInputSource is constructed from SceneViewport.OnOpenGlInit,
        // which only fires after the control is mounted — AttachedToVisualTree
        // has already fired by then. Hook directly when a TopLevel exists,
        // fall back to the event for the unlikely race.
        if (TopLevel.GetTopLevel(host) is not null)
        {
            Hook();
        }
        else
        {
            host.AttachedToVisualTree += (_, _) => Hook();
        }
    }

    /// <summary>
    /// Returns an <see cref="InputState"/> describing what happened since the
    /// last call: button state, pixel-space mouse delta, accumulated wheel
    /// notches, held-key bitmask. Per-frame deltas reset to zero.
    /// </summary>
    public InputState CaptureSnapshot(Vector2D<int> framebufferSize)
    {
        var delta = new Vector2D<float>(
            (float)(_currentPos.X - _lastSnapshotPos.X),
            (float)(_currentPos.Y - _lastSnapshotPos.Y));
        var wheel = _wheelAccum;
        var snapshot = new InputState(
            MousePosition: new Vector2D<float>((float)_currentPos.X, (float)_currentPos.Y),
            FramebufferSize: framebufferSize,
            LeftMouseDown: _left,
            LeftMouseJustPressed: _left && !_prevLeft,
            RightMouseDown: _right,
            RightMouseJustPressed: _right && !_prevRight,
            MiddleMouseDown: _middle,
            MiddleMouseJustPressed: _middle && !_prevMiddle,
            MouseDelta: delta,
            WheelDelta: wheel,
            KeysDown: _keysDown);
        _lastSnapshotPos = _currentPos;
        _wheelAccum = default;
        _prevLeft = _left;
        _prevRight = _right;
        _prevMiddle = _middle;
        return snapshot;
    }

    private void Hook()
    {
        var top = TopLevel.GetTopLevel(_host);
        if (top is null)
        {
            return;
        }
        _currentPos = default;
        _lastSnapshotPos = default;
        top.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private bool IsInsideViewport(PointerEventArgs e)
    {
        var pos = e.GetPosition(_host);
        var bounds = _host.Bounds;

        return pos.X >= 0 && pos.Y >= 0 && pos.X < bounds.Width && pos.Y < bounds.Height;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsInsideViewport(e))
        {
            return;
        }

        var props = e.GetCurrentPoint(_host).Properties;
        if (props.IsLeftButtonPressed)
        {
            _left = true;
        }

        if (props.IsRightButtonPressed)
        {
            _right = true;
        }

        if (props.IsMiddleButtonPressed)
        {
            _middle = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e) =>
        _currentPos = e.GetPosition(_host);

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        switch (e.InitialPressMouseButton)
        {
            case MouseButton.Left:
                _left = false;
                break;
            case MouseButton.Right:
                _right = false;
                break;
            case MouseButton.Middle:
                _middle = false;
                break;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!IsInsideViewport(e))
        {
            return;
        }

        _wheelAccum += new Vector2D<float>((float)e.Delta.X, (float)e.Delta.Y);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e) => _keysDown |= Map(e.Key);

    private void OnKeyUp(object? sender, KeyEventArgs e) => _keysDown &= ~Map(e.Key);

    private static EngineKeys Map(Key key) => key switch
    {
        Key.W => EngineKeys.W,
        Key.A => EngineKeys.A,
        Key.S => EngineKeys.S,
        Key.D => EngineKeys.D,
        Key.Q => EngineKeys.Q,
        Key.E => EngineKeys.E,
        _ => EngineKeys.None,
    };
}
