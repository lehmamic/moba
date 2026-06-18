using MOBA.Engine.Core;
using MOBA.Game.Actors;
using MOBA.Game.Client.Cameras;
using MOBA.Game.Client.Components;
using MOBA.Game.Components;
using MOBA.Game.Models;
using MOBA.Engine.Graphics;
using MOBA.Engine.Networking;
using MOBA.Game.Messages;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Systems;

/// <summary>
/// Applies server messages to the local scene. Walks the wire format
/// (ActorSpawn / ActorPositionUpdate / ActorDespawn / AssignLocalActor) and
/// instantiates / updates / removes the corresponding client-side actors.
///
/// <para>
/// On <see cref="IEngineSystem.OnInitialize"/> the system also sends a
/// <see cref="JoinMessage"/> to the server, which kicks off the connection-
/// driven spawn flow: the server replies with an
/// <see cref="AssignLocalActorMessage"/> (which we remember) and a series of
/// <see cref="ActorSpawnMessage"/>s. When an ActorSpawn arrives for the
/// network id the server assigned us, this system attaches a
/// <see cref="LocalPlayerInputComponent"/>; remote players (ourselves seeing
/// other clients) get only the visual + position-update plumbing.
/// </para>
/// </summary>
public sealed class NetworkSyncSystem : IEngineSystem
{
    private readonly Scene _scene;
    private readonly INetTransport _transport;
    private readonly AssetManager _assets;
    private readonly Material _markerMaterial;
    private readonly Model _playerModel;
    private readonly CameraSwitcher _cameras;
    private readonly NavMesh _navMesh;
    private readonly Dictionary<uint, Actor> _actorsById = [];
    private uint? _localPlayerNetworkId;

    public NetworkSyncSystem(
        Scene scene,
        INetTransport transport,
        AssetManager assets,
        Material markerMaterial,
        Model playerModel,
        CameraSwitcher cameras,
        NavMesh navMesh)
    {
        _scene = scene;
        _transport = transport;
        _assets = assets;
        _markerMaterial = markerMaterial;
        _playerModel = playerModel;
        _cameras = cameras;
        _navMesh = navMesh;
    }

    public void OnInitialize()
    {
        _transport.MessageReceived += OnMessageReceived;
        // Kick off the multi-player handshake: ask the server to spawn a
        // player actor for us. The matching AssignLocalActor + ActorSpawn
        // come back as ordinary messages.
        _transport.Send(NetChannel.Reliable, new JoinMessage().Serialize());
    }

    public void OnUpdate(GameTime time)
    {
    }

    public void OnShutdown() => _transport.MessageReceived -= OnMessageReceived;

    public void Dispose()
    {
    }

    private void OnMessageReceived(ReadOnlyMemory<byte> payload)
    {
        using var stream = new MemoryStream(payload.ToArray());
        using var reader = new BinaryReader(stream);
        var type = (MessageType)reader.ReadByte();
        switch (type)
        {
            case MessageType.AssignLocalActor:
                HandleAssignLocalActor(AssignLocalActorMessage.ReadPayload(reader));
                break;
            case MessageType.ActorSpawn:
                HandleSpawn(ActorSpawnMessage.ReadPayload(reader));
                break;
            case MessageType.ActorPositionUpdate:
                HandlePositionUpdate(ActorPositionUpdateMessage.ReadPayload(reader));
                break;
            case MessageType.ActorDespawn:
                HandleDespawn(ActorDespawnMessage.ReadPayload(reader));
                break;
            case MessageType.MovePath:
                HandleMovePath(MovePathMessage.ReadPayload(reader));
                break;
            case MessageType.MoveCommand:
            case MessageType.Join:
                // Client → server messages — ignore if ever echoed back.
                break;
            default:
                break;
        }
    }

    private void HandleAssignLocalActor(AssignLocalActorMessage message)
    {
        _localPlayerNetworkId = message.NetworkId;
        // If the matching ActorSpawn happened to arrive first (out-of-order on
        // the same channel shouldn't happen with Riptide reliable, but be safe),
        // attach the input component now.
        if (_actorsById.TryGetValue(message.NetworkId, out var actor)
            && actor is PlayerActor player
            && player.GetComponent<LocalPlayerInputComponent>() is null)
        {
            _ = new LocalPlayerInputComponent(player, _cameras, _transport, _navMesh);
        }
    }

    private void HandleSpawn(ActorSpawnMessage message)
    {
        switch (message.Kind)
        {
            case ActorKind.Player:
                SpawnPlayer(message);
                break;
            case ActorKind.Marker:
                SpawnMarker(message);
                break;
        }
    }

    private void SpawnPlayer(ActorSpawnMessage message)
    {
        if (_actorsById.ContainsKey(message.Id))
        {
            // Idempotent — duplicate spawn from a catch-up replay.
            return;
        }
        var player = new PlayerActor(new Vector3D<float>(message.X, message.Y, message.Z));
        // Player visual scale relative to the dimension-rift map. The knight model
        // is ~2 units tall at source — boost so it reads at MOBA-champion proportions
        // against the textured Sketchfab terrain. Tune until camera/character feel right.
        player.Transform.Scale = Vector3D<float>.One * 3f;
        _ = new NetworkIdentityComponent(player, message.Id);
        _ = new SkeletalMeshRendererComponent(player, _playerModel);
        _ = new ReplicatedPathComponent(player);
        if (_localPlayerNetworkId == message.Id)
        {
            _ = new LocalPlayerInputComponent(player, _cameras, _transport, _navMesh);
        }
        _scene.AddActor(player);
        _actorsById[message.Id] = player;
    }

    private void HandleMovePath(MovePathMessage message)
    {
        if (_actorsById.TryGetValue(message.NetworkId, out var actor)
            && actor.GetComponent<ReplicatedPathComponent>() is { } path)
        {
            path.SetWaypoints(message.Waypoints);
        }
    }

    private void SpawnMarker(ActorSpawnMessage message)
    {
        var marker = new MarkerActor(message.Id, new Vector3D<float>(message.X, message.Y, message.Z));
        _ = new MeshRendererComponent(marker, _assets.LoadSphereMesh(), _markerMaterial);
        _scene.AddActor(marker);
        _actorsById[message.Id] = marker;
    }

    private void HandlePositionUpdate(ActorPositionUpdateMessage message)
    {
        if (_actorsById.TryGetValue(message.Id, out var actor))
        {
            actor.Transform.Position = new Vector3D<float>(message.X, message.Y, message.Z);
            // Derive yaw from the server's forward direction. Bind-pose forward is
            // +X (Madhav convention, matches Tripo/Mixamo character exports), and
            // Silk's Y-rotation sends (1,0,0) → (cos yaw, 0, -sin yaw) in row-vector
            // form, so yaw = atan2(-forward.Z, forward.X).
            var yaw = MathF.Atan2(-message.ForwardZ, message.ForwardX);
            actor.Transform.Rotation = Quaternion<float>.CreateFromYawPitchRoll(yaw, 0f, 0f);
        }
    }

    private void HandleDespawn(ActorDespawnMessage message)
    {
        if (_actorsById.TryGetValue(message.Id, out var actor))
        {
            _scene.RemoveActor(actor);
            _actorsById.Remove(message.Id);
        }
    }
}
