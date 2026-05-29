namespace MOBA.Engine.Networking;

/// <summary>
/// Server-side network transport. Distinct from <see cref="INetTransport"/>
/// because the server needs per-client routing: it must know which connection
/// sent a message, send messages to a single client, and observe connect /
/// disconnect events.
/// </summary>
public interface IServerNetTransport : IDisposable
{
    bool IsRunning { get; }

    event Action<NetClientId>? ClientConnected;

    event Action<NetClientId>? ClientDisconnected;

    /// <summary>Raised when a client sends a message. The sender is identified.</summary>
    event Action<NetClientId, ReadOnlyMemory<byte>>? MessageReceived;

    void SendToAll(NetChannel channel, ReadOnlySpan<byte> payload);

    void SendTo(NetClientId client, NetChannel channel, ReadOnlySpan<byte> payload);

    void Poll();
}
