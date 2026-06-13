namespace MOBA.Game.Messages;

/// <summary>
/// First byte of every framed wire payload. The transport carries opaque bytes
/// (see <c>MOBA.Engine.Networking.INetTransport</c>); the framing belongs to the
/// game layer.
/// </summary>
public enum MessageType : byte
{
    MoveCommand = 1,
    ActorSpawn = 2,
    ActorPositionUpdate = 3,
    ActorDespawn = 4,
    /// <summary>Client → Server: "I want to play."</summary>
    Join = 5,
    /// <summary>Server → Client: "your local player actor is this network id."</summary>
    AssignLocalActor = 6,
    /// <summary>
    /// Server → Client: "this networked actor's currently-active path is this
    /// waypoint sequence." Sent once per accepted MoveCommand. Authoritative
    /// snapshot for client-side debug rendering (and, later, minimap path
    /// display) — clients do not pathfind themselves.
    /// </summary>
    MovePath = 7,
}

/// <summary>Distinguishes spawn-message payloads on the wire.</summary>
public enum ActorKind : byte
{
    Cube = 1,
    Marker = 2,
    Player = 3,
}
