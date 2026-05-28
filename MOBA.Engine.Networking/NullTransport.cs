namespace MOBA.Engine.Networking;

/// <summary>
/// In-process loopback stub. Send = no-op, no inbound messages. Enough for the first skeleton
/// slice where sim and render run in the same process and there is no real netcode yet.
/// </summary>
public sealed class NullTransport : INetTransport
{
    public bool IsConnected => true;

    public event Action<ReadOnlyMemory<byte>>? MessageReceived;

    public void Send(NetChannel channel, ReadOnlySpan<byte> payload)
    {
        // Intentionally empty — single-process loopback.
    }

    public void Poll()
    {
        // No inbound traffic; the event stays unused.
        _ = MessageReceived;
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }
}
