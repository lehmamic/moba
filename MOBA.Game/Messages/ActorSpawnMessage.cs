namespace MOBA.Game.Messages;

/// <summary>
/// Server → Client: "spawn a new networked actor with this id at this position."
/// <see cref="Team"/> and <see cref="Variant"/> are the compact type info the
/// client's factory needs to assemble + colour the actor (for a minion,
/// <see cref="Variant"/> is the <c>MinionType</c>). No mesh/asset path travels
/// on the wire — the client factory owns that mapping.
/// </summary>
public readonly record struct ActorSpawnMessage(
    uint Id,
    ActorKind Kind,
    float X,
    float Y,
    float Z,
    TeamId Team = TeamId.None,
    byte Variant = 0)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4 + 1 + 12 + 1 + 1);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.ActorSpawn);
        writer.Write(Id);
        writer.Write((byte)Kind);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write((byte)Team);
        writer.Write(Variant);
        return stream.ToArray();
    }

    public static ActorSpawnMessage ReadPayload(BinaryReader reader) =>
        new(
            reader.ReadUInt32(),
            (ActorKind)reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            (TeamId)reader.ReadByte(),
            reader.ReadByte());
}
