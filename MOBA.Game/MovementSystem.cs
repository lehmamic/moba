using MOBA.Engine.Core;
using MOBA.Engine.Networking;
using MOBA.Game.Messages;
using Silk.NET.Maths;

namespace MOBA.Game;

/// <summary>
/// Server-side network handler for movement. Two phases:
/// <list type="bullet">
///   <item><b>Pre-sim (event):</b> incoming <see cref="MoveCommandMessage"/> via the
///     transport. Sets <see cref="MoveTargetComponent.Target"/> on the cube and
///     spawns the destination marker; broadcasts an <see cref="ActorSpawnMessage"/>.</item>
///   <item><b>Post-sim (<see cref="IPostUpdateSystem.OnPostUpdate"/>):</b> walks
///     actors with a <see cref="MoveTargetComponent"/> + <see cref="NetworkIdentityComponent"/>
///     and broadcasts position updates. When it sees <c>Target == null</c> but
///     <c>MarkerId != null</c> (the component cleared the target this tick by
///     arriving), the marker actor is removed from the scene and an
///     <see cref="ActorDespawnMessage"/> is broadcast.</item>
/// </list>
/// The actual per-tick movement physics belongs to <see cref="MoveTargetComponent.OnUpdate"/>;
/// this system never mutates <see cref="Actor.Transform"/> directly.
/// </summary>
public sealed class MovementSystem : IEngineSystem, IPostUpdateSystem
{
    private const uint FirstMarkerId = 100;

    private readonly Scene _scene;
    private readonly INetTransport _transport;
    private uint _nextMarkerId = FirstMarkerId;

    public MovementSystem(Scene scene, INetTransport transport)
    {
        _scene = scene;
        _transport = transport;
    }

    public void OnInitialize() => _transport.MessageReceived += OnMessageReceived;

    public void OnUpdate(GameTime time)
    {
        // Per-tick movement is handled by MoveTargetComponent itself; this system's
        // work happens pre-sim via the event subscription (inbound commands) and
        // post-sim via OnPostUpdate (outbound state broadcasts).
    }

    public void OnPostUpdate(GameTime time)
    {
        foreach (var actor in _scene.Actors.ToArray())
        {
            var moveTarget = actor.GetComponent<MoveTargetComponent>();
            var netId = actor.GetComponent<NetworkIdentityComponent>();
            if (moveTarget is null || netId is null)
            {
                continue;
            }

            // Skip idle actors entirely.
            var hasTarget = moveTarget.Target is not null;
            var hasMarker = moveTarget.MarkerId is not null;
            if (!hasTarget && !hasMarker)
            {
                continue;
            }

            BroadcastPositionUpdate(actor, netId.Id);

            if (!hasTarget && hasMarker)
            {
                // The component cleared its Target this tick - that means it just
                // arrived. Tear down the marker.
                DespawnMarker(moveTarget.MarkerId!.Value);
                moveTarget.MarkerId = null;
            }
        }
    }

    public void OnShutdown() => _transport.MessageReceived -= OnMessageReceived;

    public void Dispose()
    {
        OnShutdown();
        GC.SuppressFinalize(this);
    }

    private void OnMessageReceived(ReadOnlyMemory<byte> payload)
    {
        using var stream = new MemoryStream(payload.ToArray());
        using var reader = new BinaryReader(stream);
        var type = (MessageType)reader.ReadByte();
        if (type != MessageType.MoveCommand)
        {
            return;
        }
        HandleMoveCommand(MoveCommandMessage.ReadPayload(reader));
    }

    private void HandleMoveCommand(MoveCommandMessage command)
    {
        // Find the cube (id = 2 by hardcoded convention for the skeleton).
        Actor? movable = null;
        MoveTargetComponent? moveTarget = null;
        foreach (var actor in _scene.Actors)
        {
            var netId = actor.GetComponent<NetworkIdentityComponent>();
            var mt = actor.GetComponent<MoveTargetComponent>();
            if (netId?.Id == 2 && mt is not null)
            {
                movable = actor;
                moveTarget = mt;
                break;
            }
        }
        if (movable is null || moveTarget is null)
        {
            return;
        }

        if (moveTarget.MarkerId is { } oldMarkerId)
        {
            DespawnMarker(oldMarkerId);
        }

        var target = new Vector3D<float>(command.TargetX, movable.Transform.Position.Y, command.TargetZ);
        moveTarget.Target = target;

        var markerId = _nextMarkerId++;
        // Marker sits just above the ground, beneath the cube's centre, so it stays
        // visible until the cube arrives at the click point.
        var markerPosition = new Vector3D<float>(command.TargetX, 0.5f, command.TargetZ);
        var marker = new MarkerActor(markerId, markerPosition);
        _scene.AddActor(marker);
        moveTarget.MarkerId = markerId;

        var spawn = new ActorSpawnMessage(markerId, ActorKind.Marker, markerPosition.X, markerPosition.Y, markerPosition.Z);
        _transport.Send(NetChannel.Reliable, spawn.Serialize());
    }

    private void BroadcastPositionUpdate(Actor actor, uint id)
    {
        var position = actor.Transform.Position;
        // Send the gameplay-meaningful facing direction rather than a raw rotation
        // angle. The XZ projection is enough because MOBA characters do not pitch.
        var forward = actor.Transform.Forward;
        var message = new ActorPositionUpdateMessage(
            id,
            position.X,
            position.Y,
            position.Z,
            forward.X,
            forward.Z);
        _transport.Send(NetChannel.Unreliable, message.Serialize());
    }

    private void DespawnMarker(uint markerId)
    {
        Actor? marker = null;
        foreach (var actor in _scene.Actors)
        {
            if (actor.GetComponent<NetworkIdentityComponent>()?.Id == markerId)
            {
                marker = actor;
                break;
            }
        }
        if (marker is null)
        {
            return;
        }
        _scene.RemoveActor(marker);
        var despawn = new ActorDespawnMessage(markerId);
        _transport.Send(NetChannel.Reliable, despawn.Serialize());
    }
}
