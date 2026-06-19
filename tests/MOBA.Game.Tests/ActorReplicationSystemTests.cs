using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Networking;
using MOBA.Game.Components;
using MOBA.Game.Messages;
using MOBA.Game.Systems;
using Xunit;

namespace MOBA.Game.Tests;

public class ActorReplicationSystemTests
{
    [Fact]
    public void Announces_descriptor_actor_with_identity_and_spawn_broadcast()
    {
        var (scene, transport, system) = NewSystem();
        var actor = NewMinion(TeamId.Order, variant: 2);
        scene.AddActor(actor);

        system.OnPostUpdate(default);

        var identity = actor.GetComponent<NetworkIdentityComponent>();
        Assert.NotNull(identity);
        var spawn = ReadSpawn(Assert.Single(transport.Broadcasts));
        Assert.Equal(ActorKind.Minion, spawn.Kind);
        Assert.Equal(TeamId.Order, spawn.Team);
        Assert.Equal((byte)2, spawn.Variant);
        Assert.Equal(identity!.Id, spawn.Id);
    }

    [Fact]
    public void Announces_each_actor_only_once()
    {
        var (scene, transport, system) = NewSystem();
        scene.AddActor(NewMinion(TeamId.Chaos));

        system.OnPostUpdate(default);
        system.OnPostUpdate(default);

        Assert.Single(transport.Broadcasts);
    }

    [Fact]
    public void Despawns_when_actor_leaves_the_scene()
    {
        var (scene, transport, system) = NewSystem();
        var actor = NewMinion(TeamId.Order);
        scene.AddActor(actor);
        system.OnPostUpdate(default);
        var id = actor.GetComponent<NetworkIdentityComponent>()!.Id;

        scene.RemoveActor(actor);
        system.OnPostUpdate(default);

        Assert.Equal(2, transport.Broadcasts.Count);
        Assert.Equal(id, ReadDespawn(transport.Broadcasts[1]).Id);
    }

    [Fact]
    public void Replays_live_actors_to_a_joining_client()
    {
        var (scene, transport, system) = NewSystem();
        scene.AddActor(NewMinion(TeamId.Order));
        system.OnPostUpdate(default);

        transport.RaiseMessage(new NetClientId(7), new JoinMessage().Serialize());

        var send = Assert.Single(transport.DirectSends);
        Assert.Equal(new NetClientId(7), send.Client);
        Assert.Equal(ActorKind.Minion, ReadSpawn(send.Payload).Kind);
    }

    private static (Scene Scene, FakeServerTransport Transport, ActorReplicationSystem System) NewSystem()
    {
        var scene = new Scene();
        var transport = new FakeServerTransport();
        var system = new ActorReplicationSystem(scene, transport);
        system.OnInitialize();
        return (scene, transport, system);
    }

    private static Actor NewMinion(TeamId team, byte variant = 0)
    {
        var actor = new Actor();
        _ = new ReplicatedSpawnComponent(actor, ActorKind.Minion, team, variant);
        return actor;
    }

    private static ActorSpawnMessage ReadSpawn(byte[] payload)
    {
        using var reader = new BinaryReader(new MemoryStream(payload));
        Assert.Equal(MessageType.ActorSpawn, (MessageType)reader.ReadByte());
        return ActorSpawnMessage.ReadPayload(reader);
    }

    private static ActorDespawnMessage ReadDespawn(byte[] payload)
    {
        using var reader = new BinaryReader(new MemoryStream(payload));
        Assert.Equal(MessageType.ActorDespawn, (MessageType)reader.ReadByte());
        return ActorDespawnMessage.ReadPayload(reader);
    }
}
