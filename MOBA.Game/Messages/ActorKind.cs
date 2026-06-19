namespace MOBA.Game.Messages;

/// <summary>Distinguishes spawn-message payloads on the wire.</summary>
public enum ActorKind : byte
{
    Cube = 1,
    Marker = 2,
    Player = 3,
    Minion = 4,
}
