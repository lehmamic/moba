using System.Diagnostics;
using MOBA.Engine.Core.Hosting;
using Riptide;

namespace MOBA.Engine.Networking.Riptide;

/// <summary>
/// UDP client transport backed by <see cref="Client"/>. <see cref="OnInitialize"/>
/// dials the server and blocks (with a 5-second timeout) until the connection is
/// established; <see cref="OnUpdate"/> pumps Riptide's tick; <see cref="OnShutdown"/>
/// disconnects gracefully. The transport throws from <see cref="OnInitialize"/>
/// if the server is unreachable - there is no offline mode in this iteration.
/// </summary>
public sealed class RiptideClientTransport : INetTransport, IEngineSystem
{
    private const ushort BinaryPassThroughMessageId = 0;
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);

    private readonly Client _client = new();
    private readonly string _hostAddress;

    public RiptideClientTransport(string hostAddress = "127.0.0.1:7777") =>
        _hostAddress = hostAddress;

    public bool IsConnected => _client.IsConnected;

    public event Action<ReadOnlyMemory<byte>>? MessageReceived;

    public void OnInitialize()
    {
        _client.Connected += (_, _) =>
            Console.WriteLine($"[MOBA.Client] connected to {_hostAddress}");
        _client.Disconnected += (_, args) =>
            Console.WriteLine($"[MOBA.Client] disconnected ({args.Reason})");
        _client.ConnectionFailed += (_, args) =>
            Console.WriteLine($"[MOBA.Client] connection failed ({args.Reason})");
        _client.MessageReceived += OnClientMessageReceived;

        _client.Connect(_hostAddress, useMessageHandlers: false);

        var stopwatch = Stopwatch.StartNew();
        while (!_client.IsConnected && stopwatch.Elapsed < ConnectionTimeout)
        {
            _client.Update();
            Thread.Sleep(10);
        }
        if (!_client.IsConnected)
        {
            throw new InvalidOperationException(
                $"Could not connect to {_hostAddress} within {ConnectionTimeout.TotalSeconds:F0} seconds. " +
                "Is the server running?");
        }
    }

    public void OnUpdate(GameTime time) => _client.Update();

    public void OnShutdown()
    {
        if (_client.IsConnected)
        {
            _client.Disconnect();
        }
    }

    public void Poll() => _client.Update();

    public void Send(NetChannel channel, ReadOnlySpan<byte> payload)
    {
        var mode = channel == NetChannel.Reliable
            ? MessageSendMode.Reliable
            : MessageSendMode.Unreliable;
        var message = Message.Create(mode, BinaryPassThroughMessageId);
        message.AddBytes(payload.ToArray());
        _client.Send(message);
    }

    public void Dispose()
    {
        OnShutdown();
        GC.SuppressFinalize(this);
    }

    private void OnClientMessageReceived(object? sender, MessageReceivedEventArgs args)
    {
        if (args.MessageId != BinaryPassThroughMessageId)
        {
            return;
        }
        var bytes = args.Message.GetBytes();
        MessageReceived?.Invoke(bytes);
    }
}
