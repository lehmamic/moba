using MOBA.Engine.Core;
using Riptide;

namespace MOBA.Engine.Networking.Riptide;

/// <summary>
/// UDP server transport backed by <see cref="Server"/>. Implements
/// <see cref="INetTransport"/> for the engine and <see cref="IEngineSystem"/> so a
/// <see cref="GameHost"/> can drive it as a system: <see cref="OnInitialize"/>
/// binds the socket and starts listening, <see cref="OnUpdate"/> pumps Riptide's
/// internal tick (incoming messages fan out via <see cref="MessageReceived"/>),
/// <see cref="OnShutdown"/> stops the server.
/// </summary>
public sealed class RiptideServerTransport : INetTransport, IEngineSystem
{
    private const ushort BinaryPassThroughMessageId = 0;

    private readonly Server _server = new();
    private readonly ushort _port;
    private readonly ushort _maxClientCount;

    public RiptideServerTransport(ushort port = 7777, ushort maxClientCount = 4)
    {
        _port = port;
        _maxClientCount = maxClientCount;
    }

    public bool IsConnected => _server.IsRunning;

    public event Action<ReadOnlyMemory<byte>>? MessageReceived;

    public void OnInitialize()
    {
        _server.ClientConnected += (_, args) =>
            Console.WriteLine($"[MOBA.Server] client {args.Client.Id} connected");
        _server.ClientDisconnected += (_, args) =>
            Console.WriteLine($"[MOBA.Server] client {args.Client.Id} disconnected ({args.Reason})");
        _server.MessageReceived += OnServerMessageReceived;

        _server.Start(_port, _maxClientCount, useMessageHandlers: false);
        Console.WriteLine($"[MOBA.Server] listening on UDP {_port}");
    }

    public void OnUpdate(GameTime time) => _server.Update();

    public void OnShutdown()
    {
        if (_server.IsRunning)
        {
            _server.Stop();
        }
    }

    public void Poll() => _server.Update();

    public void Send(NetChannel channel, ReadOnlySpan<byte> payload)
    {
        var mode = channel == NetChannel.Reliable
            ? MessageSendMode.Reliable
            : MessageSendMode.Unreliable;
        var message = Message.Create(mode, BinaryPassThroughMessageId);
        message.AddBytes(payload.ToArray());
        _server.SendToAll(message);
    }

    public void Dispose()
    {
        OnShutdown();
        GC.SuppressFinalize(this);
    }

    private void OnServerMessageReceived(object? sender, MessageReceivedEventArgs args)
    {
        if (args.MessageId != BinaryPassThroughMessageId)
        {
            return;
        }
        var bytes = args.Message.GetBytes();
        MessageReceived?.Invoke(bytes);
    }
}
