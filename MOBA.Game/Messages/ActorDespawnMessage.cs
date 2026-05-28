namespace MOBA.Game.Messages;

/// <summary>Server → Client: "remove this networked actor from your scene."</summary>
public readonly record struct ActorDespawnMessage(uint Id)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.ActorDespawn);
        writer.Write(Id);
        return stream.ToArray();
    }

    public static ActorDespawnMessage ReadPayload(BinaryReader reader) =>
        new(reader.ReadUInt32());
}
