using MOBA.Engine.Core;
using MOBA.Engine.Networking;
using MOBA.Game.Messages;

namespace MOBA.Game.Client;

/// <summary>
/// Local-player input. Attached only to the player actor that represents the
/// local client (id assigned via <see cref="AssignLocalActorMessage"/>); remote
/// players never receive this component. Reacts to per-frame
/// <see cref="InputState"/> via <see cref="OnProcessInput"/>: on left-mouse-
/// just-pressed it unprojects the cursor onto the ground plane with
/// <see cref="MousePicker"/> and sends a <see cref="MoveCommandMessage"/> to
/// the server. The server is authoritative; this component never mutates the
/// owner's transform.
/// </summary>
public sealed class LocalPlayerInputComponent : Component
{
    private readonly CameraSwitcher _cameras;
    private readonly INetTransport _transport;

    public LocalPlayerInputComponent(Actor owner, CameraSwitcher cameras, INetTransport transport)
        : base(owner)
    {
        _cameras = cameras;
        _transport = transport;
    }

    public override void OnProcessInput(InputState state)
    {
        if (!state.LeftMouseJustPressed)
        {
            return;
        }

        var hit = MousePicker.PickGround(state.MousePosition, state.FramebufferSize, _cameras.ActiveCamera);
        if (hit is { } target)
        {
            var command = new MoveCommandMessage(target.X, target.Z);
            _transport.Send(NetChannel.Reliable, command.Serialize());
        }
    }
}
