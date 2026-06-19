namespace MOBA.Game.Messages;

/// <summary>
/// Client → Server: "engage this network-identified actor." Resolved by
/// <c>MovementSystem</c> into a write to the issuing player's
/// <c>AttackComponent.CurrentTarget</c>; the attack component then drives
/// chase + cadence on the server. Targets that can't be resolved (stale id,
/// already despawned) are silently dropped — the player simply keeps doing
/// whatever they were doing.
/// </summary>
public readonly record struct AttackCommandMessage(uint TargetNetworkId)
{
    public byte[] Serialize()
    {
        using var stream = new MemoryStream(1 + 4);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)MessageType.AttackCommand);
        writer.Write(TargetNetworkId);

        return stream.ToArray();
    }

    public static AttackCommandMessage ReadPayload(BinaryReader reader) =>
        new(reader.ReadUInt32());
}
