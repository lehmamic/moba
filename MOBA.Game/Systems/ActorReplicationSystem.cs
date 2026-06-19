using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Networking;
using MOBA.Game.Components;
using MOBA.Game.Messages;

namespace MOBA.Game.Systems;

/// <summary>
/// Networking capability (not game behaviour): generically replicates any actor
/// carrying a <see cref="ReplicatedSpawnComponent"/>. On the post-update pass it
/// assigns a network id to each freshly-spawned descriptor actor and broadcasts
/// an <see cref="ActorSpawnMessage"/>, and broadcasts an
/// <see cref="ActorDespawnMessage"/> when a previously-announced actor leaves
/// the scene. Position updates are handled generically by
/// <see cref="MovementSystem"/> for any actor that also has a
/// <c>MoveTargetComponent</c>. New clients get a catch-up replay on
/// <see cref="MessageType.Join"/>.
///
/// <para>
/// Players and markers have their own bespoke spawn path and carry no
/// descriptor, so they are untouched here. Ids are allocated from a range above
/// the player (1000+) and marker (100+) ranges to avoid collisions.
/// </para>
/// </summary>
public sealed class ActorReplicationSystem : IEngineSystem, IPostUpdateSystem
{
    private const uint FirstReplicatedId = 10000;

    private readonly Scene _scene;
    private readonly IServerNetTransport _transport;
    private readonly Dictionary<uint, Actor> _live = [];
    private readonly Dictionary<uint, uint> _lastSentHealthVersion = [];
    private uint _nextId = FirstReplicatedId;

    public ActorReplicationSystem(Scene scene, IServerNetTransport transport)
    {
        _scene = scene;
        _transport = transport;
    }

    public void OnInitialize() => _transport.MessageReceived += OnMessageReceived;

    public void OnUpdate(GameTime time)
    {
    }

    public void OnPostUpdate(GameTime time)
    {
        AnnounceNewActors();
        DespawnRemovedActors();
        BroadcastHealthChanges();
    }

    public void OnShutdown() => _transport.MessageReceived -= OnMessageReceived;

    public void Dispose()
    {
        OnShutdown();
        GC.SuppressFinalize(this);
    }

    private void AnnounceNewActors()
    {
        foreach (var actor in _scene.Actors)
        {
            if (actor.GetComponent<ReplicatedSpawnComponent>() is not { } descriptor
                || actor.GetComponent<NetworkIdentityComponent>() is not null)
            {
                continue;
            }
            var id = _nextId++;
            _ = new NetworkIdentityComponent(actor, id);
            _live[id] = actor;
            _transport.SendToAll(NetChannel.Reliable, SpawnFor(id, actor, descriptor).Serialize());
        }
    }

    private void DespawnRemovedActors()
    {
        if (_live.Count == 0)
        {
            return;
        }
        var present = new HashSet<Actor>(_scene.Actors);
        var gone = _live.Where(kv => !present.Contains(kv.Value)).Select(kv => kv.Key).ToList();
        foreach (var id in gone)
        {
            _live.Remove(id);
            _lastSentHealthVersion.Remove(id);
            _transport.SendToAll(NetChannel.Reliable, new ActorDespawnMessage(id).Serialize());
        }
    }

    /// <summary>
    /// Walks every networked actor with a <see cref="HealthComponent"/> and
    /// emits an <see cref="ActorHealthMessage"/> when its monotonic Version
    /// counter has advanced since the last broadcast. Idle matches generate
    /// zero traffic on this channel — only actual mutations move the
    /// counter.
    /// </summary>
    private void BroadcastHealthChanges()
    {
        foreach (var actor in _scene.Actors)
        {
            if (actor.GetComponent<NetworkIdentityComponent>() is not { } netId
                || actor.GetComponent<HealthComponent>() is not { } health)
            {
                continue;
            }

            if (_lastSentHealthVersion.GetValueOrDefault(netId.Id) == health.Version)
            {
                continue;
            }

            _transport.SendToAll(
                NetChannel.Reliable,
                new ActorHealthMessage(netId.Id, health.Current, health.Max).Serialize());
            _lastSentHealthVersion[netId.Id] = health.Version;
        }
    }

    private void OnMessageReceived(NetClientId sender, ReadOnlyMemory<byte> payload)
    {
        using var stream = new MemoryStream(payload.ToArray());
        using var reader = new BinaryReader(stream);

        if ((MessageType)reader.ReadByte() != MessageType.Join)
        {
            return;
        }

        // Catch-up: replay every live descriptor actor to the joiner so a client
        // joining mid-match sees the minions already on the field, plus its
        // current HP if any damage already landed.
        foreach (var (id, actor) in _live)
        {
            if (actor.GetComponent<ReplicatedSpawnComponent>() is { } descriptor)
            {
                _transport.SendTo(sender, NetChannel.Reliable, SpawnFor(id, actor, descriptor).Serialize());
            }

            if (actor.GetComponent<HealthComponent>() is { Version: > 0 } health)
            {
                _transport.SendTo(
                    sender,
                    NetChannel.Reliable,
                    new ActorHealthMessage(id, health.Current, health.Max).Serialize());
            }
        }
    }

    private static ActorSpawnMessage SpawnFor(uint id, Actor actor, ReplicatedSpawnComponent descriptor)
    {
        var p = actor.Transform.Position;
        return new ActorSpawnMessage(id, descriptor.Kind, p.X, p.Y, p.Z, descriptor.Team, descriptor.Variant);
    }
}
