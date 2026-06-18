using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Core.Input;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Input;

/// <summary>
/// Engine-system facade over Silk.NET's <see cref="IInputContext"/>. Exposes the
/// primary keyboard and mouse plus the raw context (for systems like
/// <c>CameraSwitcher</c> that subscribe to per-key events). Owns the
/// <see cref="IInputContext"/> lifetime and disposes it in <see cref="OnShutdown"/>.
/// <see cref="CaptureSnapshot"/> produces the per-frame <see cref="InputState"/>
/// that the host threads through <c>ProcessInput</c>; the rising-edge flags
/// are derived against the previous captured frame.
/// </summary>
public sealed class InputSystem : IEngineSystem
{
    private bool _previousLeftDown;
    private bool _previousRightDown;

    public InputSystem(IInputContext context) => Context = context;

    public IInputContext Context { get; }

    public IKeyboard Keyboard => Context.Keyboards[0];

    public IMouse Mouse => Context.Mice[0];

    public InputState CaptureSnapshot(Vector2D<int> framebufferSize)
    {
        var rawPosition = Mouse.Position;
        var leftDown = Mouse.IsButtonPressed(MouseButton.Left);
        var rightDown = Mouse.IsButtonPressed(MouseButton.Right);

        var snapshot = new InputState(
            new Vector2D<float>(rawPosition.X, rawPosition.Y),
            framebufferSize,
            LeftMouseDown: leftDown,
            LeftMouseJustPressed: leftDown && !_previousLeftDown,
            RightMouseDown: rightDown,
            RightMouseJustPressed: rightDown && !_previousRightDown);

        _previousLeftDown = leftDown;
        _previousRightDown = rightDown;

        return snapshot;
    }

    public void OnInitialize() { }

    public void OnUpdate(GameTime time) { }

    public void OnShutdown() => Context.Dispose();

    public void Dispose() => OnShutdown();
}
