namespace MOBA.Game.Messages;

/// <summary>Client → Server: "move my unit toward this point on the ground."</summary>
public readonly record struct MoveCommandMessage(float TargetX, float TargetZ)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4 + 4);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.MoveCommand);
        writer.Write(TargetX);
        writer.Write(TargetZ);
        return stream.ToArray();
    }

    /// <summary>Reads the payload after the <see cref="MessageType"/> byte has already been consumed.</summary>
    public static MoveCommandMessage ReadPayload(BinaryReader reader) =>
        new(reader.ReadSingle(), reader.ReadSingle());
}
