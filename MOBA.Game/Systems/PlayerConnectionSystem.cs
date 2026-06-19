using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Networking;
using MOBA.Game.Actors;
using MOBA.Game.Components;
using MOBA.Game.Messages;
using MOBA.Game.Models;
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
    private readonly NavMesh _navMesh;
    private readonly Dictionary<NetClientId, PlayerActor> _actorByClient = [];
    private uint _nextPlayerNetworkId = FirstPlayerNetworkId;
    private int _nextSpawnSlot;

    public PlayerConnectionSystem(Scene scene, IServerNetTransport transport, NavMesh navMesh)
    {
        _scene = scene;
        _transport = transport;
        _navMesh = navMesh;
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

        var team = AssignTeam();
        var networkId = _nextPlayerNetworkId++;
        var spawnPosition = SpawnPositionFor(team);
        var actor = new PlayerActor(spawnPosition, team?.Name ?? "Unassigned");
        _ = new NetworkIdentityComponent(actor, networkId);
        // Combat precedes the mover so its decisions land on the same tick the
        // mover walks. Champion has no AggroComponent — the human picks targets.
        _ = new AttackComponent(actor, AttackProfiles.ForChampion(), _navMesh);
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
            spawnPosition.Z,
            TeamIds.FromName(actor.Team));
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
            var catchUp = new ActorSpawnMessage(otherId, ActorKind.Player, pos.X, pos.Y, pos.Z, TeamIds.FromName(otherActor.Team));
            _transport.SendTo(sender, NetChannel.Reliable, catchUp.Serialize());
        }

        Console.WriteLine($"[MOBA.Server] player {networkId} spawned for client {sender} on team {actor.Team}");
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

    /// <summary>
    /// Picks the underpopulated team for the new player; ties broken by a
    /// random coin flip. If no <see cref="TeamActor"/> lives in the scene
    /// (smoke-test scenes), returns null and the caller falls back to a
    /// neutral spawn.
    /// </summary>
    private TeamActor? AssignTeam()
    {
        var teams = _scene.Actors.OfType<TeamActor>().ToList();
        if (teams.Count == 0)
        {
            return null;
        }

        var counts = teams.ToDictionary(t => t, _ => 0);
        foreach (var p in _actorByClient.Values)
        {
            var match = teams.FirstOrDefault(t => string.Equals(t.Name, p.Team, StringComparison.Ordinal));
            if (match is not null)
            {
                counts[match]++;
            }
        }

        var min = counts.Values.Min();
        var candidates = counts.Where(kv => kv.Value == min).Select(kv => kv.Key).ToList();
        return candidates[Random.Shared.Next(candidates.Count)];
    }

    /// <summary>
    /// Drops the player near their team's spawn-area centre; multiple
    /// players on the same team fan out along the X axis so they don't
    /// stack. When <paramref name="team"/> is null, falls back to the old
    /// origin-relative slotting (smoke-test scenes with no teams).
    /// </summary>
    private Vector3D<float> SpawnPositionFor(TeamActor? team)
    {
        if (team is null)
        {
            return NeutralSpawnSlot();
        }

        var teamCount = _actorByClient.Values.Count(p =>
            string.Equals(p.Team, team.Name, StringComparison.Ordinal));
        var direction = (teamCount % 2 == 0) ? 1 : -1;
        var offset = ((teamCount + 1) / 2) * SpawnSlotSpacing * direction;

        return new Vector3D<float>(
            team.SpawnAreaCenter.X + offset,
            team.SpawnAreaCenter.Y,
            team.SpawnAreaCenter.Z);
    }

    private Vector3D<float> NeutralSpawnSlot()
    {
        var slot = _nextSpawnSlot++;
        var direction = (slot % 2 == 0) ? 1 : -1;
        var offset = ((slot + 1) / 2) * SpawnSlotSpacing * direction;

        return new Vector3D<float>(offset, 1f, 0f);
    }
}
