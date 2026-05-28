namespace MOBA.Game.Messages;

/// <summary>Server → Client: "spawn a new networked actor with this id at this position."</summary>
public readonly record struct ActorSpawnMessage(uint Id, ActorKind Kind, float X, float Y, float Z)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4 + 1 + 12);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.ActorSpawn);
        writer.Write(Id);
        writer.Write((byte)Kind);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        return stream.ToArray();
    }

    public static ActorSpawnMessage ReadPayload(BinaryReader reader) =>
        new(
            reader.ReadUInt32(),
            (ActorKind)reader.ReadByte(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
}
