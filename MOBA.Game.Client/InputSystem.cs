using MOBA.Engine.Core;
using Silk.NET.Input;

namespace MOBA.Game.Client;

/// <summary>
/// Engine-system facade over Silk.NET's <see cref="IInputContext"/>. Exposes the
/// primary keyboard and mouse plus the raw context (for components like
/// <see cref="CameraSwitcher"/> that subscribe to per-key events). Owns the
/// <see cref="IInputContext"/> lifetime and disposes it in <see cref="OnShutdown"/>.
/// </summary>
public sealed class InputSystem : IEngineSystem
{
    public InputSystem(IInputContext context) => Context = context;

    public IInputContext Context { get; }

    public IKeyboard Keyboard => Context.Keyboards[0];

    public IMouse Mouse => Context.Mice[0];

    public void OnInitialize() { }

    public void OnUpdate(GameTime time) { }

    public void OnShutdown() => Context.Dispose();

    public void Dispose() => OnShutdown();
}
