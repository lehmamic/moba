namespace MOBA.Game.Messages;

/// <summary>
/// Server → Client (single recipient): "your local player actor has this
/// network id." Sent reliably right after a <see cref="JoinMessage"/>, before
/// the corresponding <see cref="ActorSpawnMessage"/> broadcast. The client
/// remembers the id and, when the matching ActorSpawn arrives, attaches its
/// local input component to that actor; everyone else's player actor stays
/// remote (visual + position updates only).
/// </summary>
public readonly record struct AssignLocalActorMessage(uint NetworkId)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.AssignLocalActor);
        writer.Write(NetworkId);
        return stream.ToArray();
    }

    public static AssignLocalActorMessage ReadPayload(BinaryReader reader) =>
        new(reader.ReadUInt32());
}
