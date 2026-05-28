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
}

/// <summary>Distinguishes spawn-message payloads on the wire.</summary>
public enum ActorKind : byte
{
    Cube = 1,
    Marker = 2,
}
