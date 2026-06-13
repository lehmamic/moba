using MOBA.Engine.Core;
using MOBA.Engine.Networking;
using MOBA.Game.Messages;
using Silk.NET.Maths;

namespace MOBA.Game;

/// <summary>
/// Server-side movement handler. Two phases:
/// <list type="bullet">
///   <item><b>Pre-sim (event):</b> incoming <see cref="MoveCommandMessage"/>s
///     are routed via <see cref="PlayerConnectionSystem"/> to the sender's own
///     player actor, the <see cref="MoveTargetComponent.Target"/> is set, and a
///     destination marker is spawned + broadcast.</item>
///   <item><b>Post-sim (<see cref="IPostUpdateSystem.OnPostUpdate"/>):</b> walks
///     actors with a <see cref="MoveTargetComponent"/> + <see cref="NetworkIdentityComponent"/>
///     and broadcasts position updates. When the component cleared its target
///     this tick (i.e. just arrived), the matching marker actor is removed and
///     an <see cref="ActorDespawnMessage"/> is broadcast.</item>
/// </list>
/// The actual per-tick movement physics lives in <see cref="MoveTargetComponent.OnUpdate"/>;
/// this system never mutates <see cref="Actor.Transform"/> directly.
/// </summary>
public sealed class MovementSystem : IEngineSystem, IPostUpdateSystem
{
    private const uint FirstMarkerId = 100;

    private readonly Scene _scene;
    private readonly IServerNetTransport _transport;
    private readonly PlayerConnectionSystem _connections;
    private readonly NavMesh _navMesh;
    private uint _nextMarkerId = FirstMarkerId;

    public MovementSystem(Scene scene, IServerNetTransport transport, PlayerConnectionSystem connections, NavMesh navMesh)
    {
        _scene = scene;
        _transport = transport;
        _connections = connections;
        _navMesh = navMesh;
    }

    public void OnInitialize() => _transport.MessageReceived += OnMessageReceived;

    public void OnUpdate(GameTime time)
    {
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

            var hasTarget = moveTarget.Target is not null;
            var hasMarker = moveTarget.MarkerId is not null;
            if (!hasTarget && !hasMarker)
            {
                continue;
            }

            BroadcastPositionUpdate(actor, netId.Id);

            if (!hasTarget && hasMarker)
            {
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

    private void OnMessageReceived(NetClientId sender, ReadOnlyMemory<byte> payload)
    {
        using var stream = new MemoryStream(payload.ToArray());
        using var reader = new BinaryReader(stream);
        var type = (MessageType)reader.ReadByte();
        if (type != MessageType.MoveCommand)
        {
            return;
        }
        HandleMoveCommand(sender, MoveCommandMessage.ReadPayload(reader));
    }

    private void HandleMoveCommand(NetClientId sender, MoveCommandMessage command)
    {
        var actor = _connections.GetPlayerActor(sender);
        if (actor is null)
        {
            // Move command from a client that hasn't joined yet — ignore.
            return;
        }
        var moveTarget = actor.GetComponent<MoveTargetComponent>();
        if (moveTarget is null)
        {
            return;
        }

        // Snap the requested target to the navmesh — server is authoritative, so
        // even if the client already snapped before sending we re-validate here.
        // A click that lands off-mesh (deep skybox, hidden tower footprint outside
        // the search box) is rejected wholesale: the player stays put.
        var requested = new Vector3D<float>(command.TargetX, actor.Transform.Position.Y, command.TargetZ);
        if (!_navMesh.TryFindNearestPoint(requested, out var target))
        {
            return;
        }

        if (moveTarget.MarkerId is { } oldMarkerId)
        {
            DespawnMarker(oldMarkerId);
        }

        moveTarget.Target = target;

        var markerId = _nextMarkerId++;
        var markerPosition = new Vector3D<float>(target.X, 0.5f, target.Z);
        var marker = new MarkerActor(markerId, markerPosition);
        _scene.AddActor(marker);
        moveTarget.MarkerId = markerId;

        var spawn = new ActorSpawnMessage(markerId, ActorKind.Marker, markerPosition.X, markerPosition.Y, markerPosition.Z);
        _transport.SendToAll(NetChannel.Reliable, spawn.Serialize());
    }

    private void BroadcastPositionUpdate(Actor actor, uint id)
    {
        var position = actor.Transform.Position;
        var forward = actor.Transform.Forward;
        var message = new ActorPositionUpdateMessage(
            id,
            position.X,
            position.Y,
            position.Z,
            forward.X,
            forward.Z);
        _transport.SendToAll(NetChannel.Unreliable, message.Serialize());
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
        _transport.SendToAll(NetChannel.Reliable, despawn.Serialize());
    }
}
