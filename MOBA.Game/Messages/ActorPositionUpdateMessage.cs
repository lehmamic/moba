namespace MOBA.Game.Messages;

/// <summary>
/// Server → Client: this networked actor is now at this position and facing this
/// horizontal direction. The forward vector is the actor's gameplay-meaningful
/// look direction (the rotated +Z bind-pose axis projected onto the XZ plane);
/// the client derives the render rotation from it. Y is omitted because MOBA
/// characters do not pitch.
/// </summary>
public readonly record struct ActorPositionUpdateMessage(
    uint Id,
    float X,
    float Y,
    float Z,
    float ForwardX,
    float ForwardZ)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4 + 20);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.ActorPositionUpdate);
        writer.Write(Id);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(ForwardX);
        writer.Write(ForwardZ);
        return stream.ToArray();
    }

    public static ActorPositionUpdateMessage ReadPayload(BinaryReader reader) =>
        new(
            reader.ReadUInt32(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
}