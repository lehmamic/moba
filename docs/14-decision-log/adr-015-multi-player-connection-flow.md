# ADR-015: Multi-player connection + player-spawn flow

## Status

Accepted

## Context

Up to ADR-014 the prototype was effectively single-player: `MobaWorld.Populate` pre-spawned exactly one `TestCubeActor` (hardcoded id = 2) on both the server and the client process. Both `MoveCommandMessage`s ever sent landed on that same actor, so two clients running against one server would both control the same character. Real multi-player needs:

- one player actor per connected client, with its own network id;
- the connecting client must know which network id is theirs (so it attaches the local input component to its own player and not to other players' visuals);
- new clients must see the players that joined before them ("catch-up");
- when a client disconnects the corresponding actor must vanish for everyone;
- `MoveCommand`s must be routed to the *sender's* actor, never to some hardcoded id.

The transport so far (`INetTransport`) was symmetric and broadcast-only: `Send` always went "to the peer", `MessageReceived` carried only the payload. Server-side that meant every `Send` reached every client and no callback ever told the server who sent what. That contract is wrong for a server.

## Decision

We split the transport abstraction in two, codify a small two-message multi-player handshake, and let a dedicated server system own the connection → actor map.

### Transport split

- **`INetTransport`** stays the client/peer interface — `Send` (to the only peer), `MessageReceived(ReadOnlyMemory<byte>)`. The client doesn't care about who else is connected.
- **`IServerNetTransport`** is the new server-side interface: `SendToAll(channel, payload)`, `SendTo(NetClientId client, channel, payload)`, `MessageReceived(NetClientId sender, ReadOnlyMemory<byte>)`, plus `ClientConnected` / `ClientDisconnected` events.
- `NetClientId` wraps the underlying transport's connection id (Riptide gives us `ushort`) so it can't be confused with actor network ids.
- `RiptideServerTransport` implements `IServerNetTransport`; `RiptideClientTransport` stays on `INetTransport`. Both still implement `IEngineSystem` for lifecycle.

The client → server protocol is now strictly one-way per message direction; server code that needs sender info (everything that handles `MoveCommand` / `Join`) uses `IServerNetTransport.MessageReceived`'s sender argument.

### Wire-protocol additions

- `MessageType.Join = 5`. Client → Server, payload is just the framing byte. Sent reliably once, the moment the client's transport finishes connecting (`NetworkSyncSystem.OnInitialize`).
- `MessageType.AssignLocalActor = 6` carrying `uint NetworkId`. Server → Client (single recipient, reliable). Tells the joining client which network id is "theirs"; the client stashes it and, when the matching `ActorSpawn` arrives, attaches the `LocalPlayerInputComponent` to that one actor only.
- `ActorKind.Player = 3`. Distinguishes player spawns from marker spawns in the existing `ActorSpawnMessage`.

`MoveCommandMessage`, `ActorPositionUpdateMessage`, `ActorDespawnMessage`, and the marker variant of `ActorSpawnMessage` are unchanged.

### Server: `PlayerConnectionSystem`

A new sim-side system in `MOBA.Game` that owns the `NetClientId → PlayerActor` map. On `Join` from a connection it:

1. Allocates a new network id from a monotonically increasing counter (starting at 1000 so it stays clear of the marker id range).
2. Constructs a `PlayerActor` at the next free spawn slot (alternating ±X offsets along the map centre so two players don't overlap).
3. Attaches `NetworkIdentityComponent` + `MoveTargetComponent`, adds the actor to the scene.
4. Sends `AssignLocalActorMessage` reliably to the joiner only.
5. Broadcasts `ActorSpawnMessage(kind=Player)` reliably to *all* clients (including the joiner — the receive code is idempotent on duplicate ids).
6. **Catch-up:** for every existing player actor, sends a reliable `ActorSpawnMessage` to the joiner so they see the existing cast.

On `ClientDisconnected` from the transport it removes the actor from the scene, drops the map entry, and broadcasts `ActorDespawnMessage` so everyone else stops seeing the leaver.

`PlayerConnectionSystem.GetPlayerActor(NetClientId)` is the read-only lookup used by `MovementSystem` to route incoming `MoveCommand`s.

### Server: `MovementSystem` (refactored)

The system used to find the cube by hardcoded id; it now constructor-injects `PlayerConnectionSystem` and routes `MoveCommand`s through `GetPlayerActor(sender)`. Marker spawn/despawn and per-tick position broadcasts work the same — they read `MoveTargetComponent` state and use `SendToAll`.

`MobaWorld.Populate` no longer adds a player actor. Only `GroundPlaneActor` is pre-spawned (it's static map geometry, present in every snapshot).

### Client: `NetworkSyncSystem` (extended)

The client no longer pre-spawns its player. `NetworkSyncSystem.OnInitialize` sends `JoinMessage` (the blocking `RiptideClientTransport.OnInitialize` guarantees the connection is up by the time we get here), then reacts to the wire as it lands:

- `AssignLocalActor` → store the local network id.
- `ActorSpawn(kind=Player)` → instantiate `PlayerActor`, attach `NetworkIdentityComponent` + `SkeletalMeshRendererComponent` (sharing the one knight `Model` loaded at startup). If the id matches the stored local id, *also* attach `LocalPlayerInputComponent`. Idempotent on duplicate spawns.
- `ActorSpawn(kind=Marker)` → unchanged.
- `ActorPositionUpdate` → unchanged.
- `ActorDespawn` → remove from scene.

The same client process therefore renders its own player + everyone else's, with input wired up only to its own.

### Renaming

`TestCubeActor` → `PlayerActor` and `LocalCubeInputComponent` → `LocalPlayerInputComponent`. The class names now match what they actually represent.

## Consequences

- **Positive:**
  - Two clients launched against one server each get their own knight, see the other, and only steer their own. The MoveCommand path is sender-routed end to end.
  - The server-only abstraction (`IServerNetTransport`) keeps the client transport interface (`INetTransport`) small and symmetric; no overloaded broadcast/per-client overloads on the client side.
  - The spawn flow is reliable-channel only, ordering-safe (catch-up replays happen after the local AssignLocalActor lands), and idempotent on duplicate ids.
  - Late-joiners see the existing cast via the catch-up loop without needing a separate "state snapshot" message type.
- **Negative:**
  - Spawn position is currently a hardcoded alternating offset around the map centre. With more than ~6 slots it gets cramped. Real matchmaking needs proper spawn-point data per map.
  - Reconnection is not handled — a disconnect always drops the actor; the same client opening a new connection gets a fresh network id and a fresh `PlayerActor`. Persistent identity is a separate ADR.
  - `JoinMessage` carries no auth or version info. A real deployment needs at minimum a protocol-version byte; that's a deliberate follow-up.
  - Every player gets the same knight asset. Skin / colour / per-player variation is future work.
- **Build-time enforcement:** existing ArchUnitNET rules carry over. `PlayerConnectionSystem` and the new transport interface live in sim/abstraction projects only; `MOBA.Server` stays free of graphics deps; `MOBA.Game.Client` references `MOBA.Game` for `PlayerActor` but neither references the Riptide concrete transport (game code only ever sees the abstractions, per ADR-011).
