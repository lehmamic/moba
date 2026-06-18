using MOBA.Engine.Core.Input;
using MOBA.Engine.Core.World;
using MOBA.Engine.Networking;
using MOBA.Game.Client.Cameras;
using MOBA.Game.Client.Input;
using MOBA.Game.Messages;
using MOBA.Game.Models;

namespace MOBA.Game.Client.Components;

/// <summary>
/// Local-player input. Attached only to the player actor that represents the
/// local client (id assigned via <see cref="AssignLocalActorMessage"/>); remote
/// players never receive this component. Reacts to per-frame
/// <see cref="InputState"/> via <see cref="OnProcessInput"/>: on left-mouse-
/// just-pressed it unprojects the cursor onto the ground plane with
/// <see cref="MousePicker"/>, snaps the hit to the nearest navmesh point, and
/// sends a <see cref="MoveCommandMessage"/> to the server. The server re-validates
/// the snap (it is authoritative); the client-side snap exists for instant UI
/// feedback. This component never mutates the owner's transform.
/// </summary>
public sealed class LocalPlayerInputComponent : Component
{
    private readonly CameraSwitcher _cameras;
    private readonly INetTransport _transport;
    private readonly NavMesh _navMesh;

    public LocalPlayerInputComponent(Actor owner, CameraSwitcher cameras, INetTransport transport, NavMesh navMesh)
        : base(owner)
    {
        _cameras = cameras;
        _transport = transport;
        _navMesh = navMesh;
    }

    public override void OnProcessInput(InputState state)
    {
        if (!state.LeftMouseJustPressed)
        {
            return;
        }

        var hit = MousePicker.PickGround(state.MousePosition, state.FramebufferSize, _cameras.ActiveCamera);
        if (hit is not { } target)
        {
            return;
        }

        // Snap to the nearest navmesh poly so the click "feels" valid even if it
        // landed slightly into a tower / off-map / over a cliff. The server runs
        // the same snap on its copy of the navmesh, so both ends converge.
        if (!_navMesh.TryFindNearestPoint(target, out var snapped))
        {
            return;
        }

        var command = new MoveCommandMessage(snapped.X, snapped.Z);
        _transport.Send(NetChannel.Reliable, command.Serialize());
    }
}
