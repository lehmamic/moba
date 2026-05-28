namespace MOBA.Engine.Networking;

/// <summary>
/// Logical channels above the concrete transport. Mapping to the concrete Riptide channel ID
/// happens inside <c>RiptideTransport</c> (arrives in the netcode phase).
/// </summary>
public enum NetChannel
{
    /// <summary>State snapshots, movement inputs — droppable, newest wins.</summary>
    Unreliable = 0,

    /// <summary>Ability casts, game events, connection handshakes — guaranteed delivery.</summary>
    Reliable = 1,
}
