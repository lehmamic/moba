using MOBA.Engine.Core;
using MOBA.Engine.Networking;
using MOBA.Game.Actors;
using MOBA.Game.Components;
using MOBA.Game.Messages;
using Silk.NET.Maths;

namespace MOBA.Game.Systems;

/// <summary>
/// Server-side handler for the multi-player lifecycle: client connect → join →
/// player-actor spawn → player-actor despawn on disconnect. Owns the
/// <see cref="NetClientId"/> → <see cref="PlayerActor"/> map, which the
/// <see cref="MovementSystem"/> queries when routing incoming
/// <see cref="MoveCommandMessage"/>s to the correct actor.
///
/// <para>
/// Spawn flow on <see cref="MessageType.Join"/>:
/// </para>
/// <list type="number">
///   <item>Allocate a network id for the new player (incrementing counter).</item>
///   <item>Construct a <see cref="PlayerActor"/> at the next free slot position
///   and attach <see cref="NetworkIdentityComponent"/> + <see cref="MoveTargetComponent"/>.</item>
///   <item>Add it to the scene.</item>
///   <item>Send <see cref="AssignLocalActorMessage"/> reliably to the joining
///   client only.</item>
///   <item>Broadcast <see cref="ActorSpawnMessage"/> with kind
///   <see cref="ActorKind.Player"/> to <em>all</em> clients so everyone sees
///   the new player.</item>
///   <item>Catch-up: send a <see cref="ActorSpawnMessage"/> for every
///   <em>existing</em> player back to the joiner so it sees them too.</item>
/// </list>
/// </summary>
public sealed class PlayerConnectionSystem : IEngineSystem
{
    private const uint FirstPlayerNetworkId = 1000;
    private const float SpawnSlotSpacing = 3f;

    private readonly Scene _scene;
    private readonly IServerNetTransport _transport;
    private readonly Dictionary<NetClientId, PlayerActor> _actorByClient = [];
    private uint _nextPlayerNetworkId = FirstPlayerNetworkId;
    private int _nextSpawnSlot;

    public PlayerConnectionSystem(Scene scene, IServerNetTransport transport)
    {
        _scene = scene;
        _transport = transport;
    }

    /// <summary>
    /// Map lookup for other server systems (notably <see cref="MovementSystem"/>)
    /// to route per-client messages to the correct actor.
    /// </summary>
    public PlayerActor? GetPlayerActor(NetClientId client) =>
        _actorByClient.GetValueOrDefault(client);

    public void OnInitialize()
    {
        _transport.MessageReceived += OnMessageReceived;
        _transport.ClientDisconnected += OnClientDisconnected;
    }

    public void OnUpdate(GameTime time)
    {
    }

    public void OnShutdown()
    {
        _transport.MessageReceived -= OnMessageReceived;
        _transport.ClientDisconnected -= OnClientDisconnected;
    }

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
        if (type == MessageType.Join)
        {
            HandleJoin(sender);
        }
    }

    private void HandleJoin(NetClientId sender)
    {
        if (_actorByClient.ContainsKey(sender))
        {
            // Duplicate Join — ignore so the player doesn't get cloned.
            return;
        }

        var networkId = _nextPlayerNetworkId++;
        var spawnPosition = NextSpawnPosition();
        var actor = new PlayerActor(spawnPosition);
        _ = new NetworkIdentityComponent(actor, networkId);
        _ = new MoveTargetComponent(actor);
        _scene.AddActor(actor);
        _actorByClient[sender] = actor;

        // Tell the joiner which network id is theirs (reliable, single recipient).
        var assign = new AssignLocalActorMessage(networkId);
        _transport.SendTo(sender, NetChannel.Reliable, assign.Serialize());

        // Tell everyone (including the joiner) about the new player.
        var spawn = new ActorSpawnMessage(
            networkId,
            ActorKind.Player,
            spawnPosition.X,
            spawnPosition.Y,
            spawnPosition.Z);
        _transport.SendToAll(NetChannel.Reliable, spawn.Serialize());

        // Catch-up: send the joiner a spawn message for every player already
        // in the scene (other than themselves) so they render the existing cast.
        foreach (var (otherClient, otherActor) in _actorByClient)
        {
            if (otherClient == sender)
            {
                continue;
            }
            var otherId = otherActor.GetComponent<NetworkIdentityComponent>()!.Id;
            var pos = otherActor.Transform.Position;
            var catchUp = new ActorSpawnMessage(otherId, ActorKind.Player, pos.X, pos.Y, pos.Z);
            _transport.SendTo(sender, NetChannel.Reliable, catchUp.Serialize());
        }

        Console.WriteLine($"[MOBA.Server] player {networkId} spawned for client {sender}");
    }

    private void OnClientDisconnected(NetClientId client)
    {
        if (!_actorByClient.TryGetValue(client, out var actor))
        {
            return;
        }
        var id = actor.GetComponent<NetworkIdentityComponent>()!.Id;
        _scene.RemoveActor(actor);
        _actorByClient.Remove(client);

        var despawn = new ActorDespawnMessage(id);
        _transport.SendToAll(NetChannel.Reliable, despawn.Serialize());
        Console.WriteLine($"[MOBA.Server] player {id} despawned (client {client} left)");
    }

    private Vector3D<float> NextSpawnPosition()
    {
        // Lay out players along the X axis: slot 0 at x=0, slot 1 at x=+3,
        // slot 2 at x=-3, slot 3 at x=+6, … Centred so the first players land
        // near the middle of the map.
        var slot = _nextSpawnSlot++;
        var direction = (slot % 2 == 0) ? 1 : -1;
        var offset = ((slot + 1) / 2) * SpawnSlotSpacing * direction;
        return new Vector3D<float>(offset, 1f, 0f);
    }
}
