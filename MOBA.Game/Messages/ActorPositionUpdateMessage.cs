namespace MOBA.Game.Messages;

/// <summary>Server → Client: "this networked actor is now at this position."</summary>
public readonly record struct ActorPositionUpdateMessage(uint Id, float X, float Y, float Z)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4 + 12);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.ActorPositionUpdate);
        writer.Write(Id);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        return stream.ToArray();
    }

    public static ActorPositionUpdateMessage ReadPayload(BinaryReader reader) =>
        new(
            reader.ReadUInt32(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
}
