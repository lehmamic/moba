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
    /// <summary>
    /// Server → Client: "this networked actor's HP is now (Current, Max)."
    /// Broadcast only on change (HealthComponent.Version dirty) by
    /// <c>ActorReplicationSystem.BroadcastHealthChanges</c>.
    /// </summary>
    ActorHealth = 8,
    /// <summary>
    /// Client → Server: "engage this network-identified actor as my
    /// basic-attack target." Resolved by <c>MovementSystem</c> into a write to
    /// the player's <c>AttackComponent.CurrentTarget</c>; the attack component
    /// drives chase + cadence from there.
    /// </summary>
    AttackCommand = 9,
}
