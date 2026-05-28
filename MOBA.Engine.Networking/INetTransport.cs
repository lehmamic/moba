namespace MOBA.Engine.Networking;

/// <summary>
/// Engine-internal transport abstraction. The concrete implementation (e.g. <c>RiptideTransport</c>)
/// arrives in the netcode phase. This keeps game code independent of Riptide types.
/// </summary>
public interface INetTransport : IDisposable
{
    bool IsConnected { get; }

    void Send(NetChannel channel, ReadOnlySpan<byte> payload);

    event Action<ReadOnlyMemory<byte>>? MessageReceived;

    void Poll();
}
