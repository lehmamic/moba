using MOBA.Engine.Core.Hosting;
using Riptide;

namespace MOBA.Engine.Networking.Riptide;

/// <summary>
/// UDP server transport backed by <see cref="Server"/>. Implements
/// <see cref="IServerNetTransport"/> for the engine (per-client send + sender-
/// identified receive + connect/disconnect events) and <see cref="IEngineSystem"/>
/// so a <see cref="GameHost"/> can drive its lifecycle: <see cref="OnInitialize"/>
/// binds the socket and starts listening, <see cref="OnUpdate"/> pumps Riptide's
/// internal tick (incoming messages fan out via <see cref="MessageReceived"/>),
/// <see cref="OnShutdown"/> stops the server.
/// </summary>
public sealed class RiptideServerTransport : IServerNetTransport, IEngineSystem
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

    public bool IsRunning => _server.IsRunning;

    public event Action<NetClientId>? ClientConnected;

    public event Action<NetClientId>? ClientDisconnected;

    public event Action<NetClientId, ReadOnlyMemory<byte>>? MessageReceived;

    public void OnInitialize()
    {
        _server.ClientConnected += (_, args) =>
        {
            Console.WriteLine($"[MOBA.Server] client {args.Client.Id} connected");
            ClientConnected?.Invoke(new NetClientId(args.Client.Id));
        };
        _server.ClientDisconnected += (_, args) =>
        {
            Console.WriteLine($"[MOBA.Server] client {args.Client.Id} disconnected ({args.Reason})");
            ClientDisconnected?.Invoke(new NetClientId(args.Client.Id));
        };
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

    public void SendToAll(NetChannel channel, ReadOnlySpan<byte> payload)
    {
        var message = Message.Create(ToSendMode(channel), BinaryPassThroughMessageId);
        message.AddBytes(payload.ToArray());
        _server.SendToAll(message);
    }

    public void SendTo(NetClientId client, NetChannel channel, ReadOnlySpan<byte> payload)
    {
        var message = Message.Create(ToSendMode(channel), BinaryPassThroughMessageId);
        message.AddBytes(payload.ToArray());
        _server.Send(message, client.Value);
    }

    public void Dispose()
    {
        OnShutdown();
        GC.SuppressFinalize(this);
    }

    private static MessageSendMode ToSendMode(NetChannel channel) =>
        channel == NetChannel.Reliable ? MessageSendMode.Reliable : MessageSendMode.Unreliable;

    private void OnServerMessageReceived(object? sender, MessageReceivedEventArgs args)
    {
        if (args.MessageId != BinaryPassThroughMessageId)
        {
            return;
        }
        var bytes = args.Message.GetBytes();
        MessageReceived?.Invoke(new NetClientId(args.FromConnection.Id), bytes);
    }
}
