using Silk.NET.Maths;

namespace MOBA.Engine.Core;

/// <summary>
/// Per-frame input snapshot captured by the client and threaded through
/// <see cref="GameHost.ProcessInput"/> → <see cref="Scene.ProcessInput"/> →
/// <see cref="Actor.ProcessInput"/> → <see cref="Component.OnProcessInput"/>.
/// Components own the decision of what to do with the input (Madhav,
/// <i>Game Programming in C++</i>, Ch. 2: <c>Game::ProcessInput</c>). The
/// server's <see cref="GameHost"/> never calls <c>ProcessInput</c>.
/// </summary>
/// <param name="MousePosition">Mouse position in screen-pixel coordinates.</param>
/// <param name="FramebufferSize">Framebuffer size in pixels; needed for ray-cast unprojection.</param>
/// <param name="LeftMouseDown">True while the left button is held.</param>
/// <param name="LeftMouseJustPressed">True only on the frame the left button went from up to down.</param>
/// <param name="RightMouseDown">True while the right button is held.</param>
/// <param name="RightMouseJustPressed">True only on the frame the right button went from up to down.</param>
public readonly record struct InputState(
    Vector2D<float> MousePosition,
    Vector2D<int> FramebufferSize,
    bool LeftMouseDown,
    bool LeftMouseJustPressed,
    bool RightMouseDown,
    bool RightMouseJustPressed);
