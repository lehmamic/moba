using MOBA.Engine.Core;
using MOBA.Engine.Graphics;
using MOBA.Engine.Networking;
using MOBA.Game.Messages;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// Applies server messages to the local scene: spawn / position / despawn. Holds
/// the authoritative <see cref="uint"/> → local <see cref="Actor"/> map; the
/// entry point registers pre-spawned actors (cube = 2) up front, server-spawned
/// markers register here as <see cref="ActorSpawnMessage"/> arrives.
/// </summary>
public sealed class NetworkSyncSystem : IEngineSystem
{
    private readonly Scene _scene;
    private readonly INetTransport _transport;
    private readonly AssetManager _assets;
    private readonly Material _markerMaterial;
    private readonly Dictionary<uint, Actor> _actorsById = [];

    public NetworkSyncSystem(
        Scene scene,
        INetTransport transport,
        AssetManager assets,
        Material markerMaterial)
    {
        _scene = scene;
        _transport = transport;
        _assets = assets;
        _markerMaterial = markerMaterial;
    }

    public void Register(uint id, Actor actor) => _actorsById[id] = actor;

    public void OnInitialize() => _transport.MessageReceived += OnMessageReceived;

    public void OnUpdate(GameTime time) { }

    public void OnShutdown() => _transport.MessageReceived -= OnMessageReceived;

    public void Dispose() { }

    private void OnMessageReceived(ReadOnlyMemory<byte> payload)
    {
        using var stream = new MemoryStream(payload.ToArray());
        using var reader = new BinaryReader(stream);
        var type = (MessageType)reader.ReadByte();
        switch (type)
        {
            case MessageType.ActorSpawn:
                HandleSpawn(ActorSpawnMessage.ReadPayload(reader));
                break;
            case MessageType.ActorPositionUpdate:
                HandlePositionUpdate(ActorPositionUpdateMessage.ReadPayload(reader));
                break;
            case MessageType.ActorDespawn:
                HandleDespawn(ActorDespawnMessage.ReadPayload(reader));
                break;
            case MessageType.MoveCommand:
                // Client → Server message; ignore if echoed back.
                break;
            default:
                break;
        }
    }

    private void HandleSpawn(ActorSpawnMessage message)
    {
        if (message.Kind != ActorKind.Marker)
        {
            return;
        }
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
