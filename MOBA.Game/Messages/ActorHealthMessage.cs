namespace MOBA.Game.Messages;

/// <summary>
/// Server → Client: "this networked actor's HP is now (Current, Max)." Broadcast
/// only when <c>HealthComponent.Version</c> ticks on the server, so an idle
/// match generates zero HP traffic. The client side calls
/// <c>HealthComponent.SetCurrent</c> on the matching local actor, which feeds
/// straight through to the HP-bar billboard.
/// </summary>
public readonly record struct ActorHealthMessage(uint Id, float Current, float Max)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4 + 4 + 4);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.ActorHealth);
        writer.Write(Id);
        writer.Write(Current);
        writer.Write(Max);

        return stream.ToArray();
    }

    public static ActorHealthMessage ReadPayload(BinaryReader reader) =>
        new(
            reader.ReadUInt32(),
            reader.ReadSingle(),
            reader.ReadSingle());
}
