namespace MOBA.Game.Messages;

/// <summary>
/// Client → Server: "I'm ready, please spawn a player actor for me." Sent
/// once after the connection is up. Payload is just the framing byte —
/// the server identifies the connection via the transport, not the message.
/// </summary>
public readonly record struct JoinMessage
{
    public byte[] Serialize()
    {
        var buffer = new byte[1];
        buffer[0] = (byte)MessageType.Join;
        return buffer;
    }
}
