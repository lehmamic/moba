using Silk.NET.Maths;

namespace MOBA.Engine.Core.Input;

/// <summary>
/// Per-frame input snapshot captured by the client and threaded through
/// <c>GameHost.ProcessInput</c> → <c>Scene.ProcessInput</c> →
/// <c>Actor.ProcessInput</c> → <c>Component.OnProcessInput</c>.
/// Components own the decision of what to do with the input (Madhav,
/// <i>Game Programming in C++</i>, Ch. 2: <c>Game::ProcessInput</c>). The
/// server's <c>GameHost</c> never calls <c>ProcessInput</c>.
/// </summary>
/// <param name="MousePosition">Mouse position in screen-pixel coordinates.</param>
/// <param name="FramebufferSize">Framebuffer size in pixels; needed for ray-cast unprojection.</param>
/// <param name="LeftMouseDown">True while the left button is held.</param>
/// <param name="LeftMouseJustPressed">True only on the frame the left button went from up to down.</param>
/// <param name="RightMouseDown">True while the right button is held.</param>
/// <param name="RightMouseJustPressed">True only on the frame the right button went from up to down.</param>
/// <param name="MiddleMouseDown">True while the middle button is held.</param>
/// <param name="MiddleMouseJustPressed">True only on the frame the middle button went from up to down.</param>
/// <param name="MouseDelta">Cumulative mouse pixel delta since the previous snapshot — handy for cameras
/// that want a per-frame drag amount without tracking the previous position themselves.</param>
/// <param name="WheelDelta">Cumulative wheel notches since the previous snapshot (X horizontal, Y vertical).</param>
/// <param name="KeysDown">Bitmask of keys held this frame. Adapters map their native keys to <see cref="EngineKeys"/>.</param>
public readonly record struct InputState(
    Vector2D<float> MousePosition,
    Vector2D<int> FramebufferSize,
    bool LeftMouseDown,
    bool LeftMouseJustPressed,
    bool RightMouseDown,
    bool RightMouseJustPressed,
    bool MiddleMouseDown = false,
    bool MiddleMouseJustPressed = false,
    Vector2D<float> MouseDelta = default,
    Vector2D<float> WheelDelta = default,
    EngineKeys KeysDown = EngineKeys.None);
