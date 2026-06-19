using MOBA.Engine.Networking;

namespace MOBA.Game.Tests;

/// <summary>
/// In-memory <see cref="IServerNetTransport"/> test double. Captures broadcasts
/// and per-client sends, and lets a test raise the connect / disconnect /
/// message events.
/// </summary>
internal sealed class FakeServerTransport : IServerNetTransport
{
    public List<byte[]> Broadcasts { get; } = [];

    public List<(NetClientId Client, byte[] Payload)> DirectSends { get; } = [];

    public bool IsRunning => true;

    public event Action<NetClientId>? ClientConnected;

    public event Action<NetClientId>? ClientDisconnected;

    public event Action<NetClientId, ReadOnlyMemory<byte>>? MessageReceived;

    public void SendToAll(NetChannel channel, ReadOnlySpan<byte> payload) => Broadcasts.Add(payload.ToArray());

    public void SendTo(NetClientId client, NetChannel channel, ReadOnlySpan<byte> payload) =>
        DirectSends.Add((client, payload.ToArray()));

    public void Poll()
    {
    }

    public void Dispose()
    {
    }

    public void RaiseMessage(NetClientId from, byte[] payload) => MessageReceived?.Invoke(from, payload);

    public void RaiseConnected(NetClientId client) => ClientConnected?.Invoke(client);

    public void RaiseDisconnected(NetClientId client) => ClientDisconnected?.Invoke(client);
}
