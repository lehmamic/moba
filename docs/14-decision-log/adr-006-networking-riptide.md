# ADR-006: Riptide as networking library

## Status

Accepted (concrete implementation deferred; only abstraction + NullTransport in the first skeleton)

## Context

For server-authoritative MOBA netcode we need reliable UDP with channels (snapshots unreliable, game events reliable). Candidates in the .NET ecosystem:

| | LiteNetLib | Riptide | ENet-CSharp |
|---|---|---|---|
| License | MIT | MIT | MIT |
| Pure managed | yes | yes | no (native) |
| Maturity | ~10 yrs, Mirror used it | ~5 yrs, active (Tom Weiland) | Quake-era, very old |
| API style | event + polling | message handlers (attribute-based) | classic ENet |
| Community | large | medium, growing | C-lang heritage |

## Decision

We choose **Riptide** (`RiptideNetworking.Riptide` NuGet). Clean message-handler API, MIT, pure managed, actively maintained.

`MOBA.Engine.Networking` defines an **internal transport abstraction** (`INetTransport`, `NetChannel` enum) that hides Riptide's constructs — game code never writes against Riptide types directly. This lets us swap to LiteNetLib (or raw `System.Net.Sockets`) later as an isolated backend change if needed.

**In the first skeleton slice** Riptide is not yet added as a NuGet reference. We have:

- `INetTransport`
- `NetChannel` (Reliable / Unreliable)
- `NullTransport` (in-process loopback, Send = no-op)

The concrete `RiptideTransport : INetTransport` arrives with the netcode phase.

## Consequences

- **Positive:** game code only knows `INetTransport`, decoupled from networking-library changes. The skeleton runs single-process without netcode complexity. Riptide is documented as the decision, so we will not re-evaluate next time.
- **Negative:** one layer of indirection between game and transport. For an extreme hot path this could matter — there is always the fallback of writing one specific send site directly against Riptide (as an explicit ADR violation).
- **Alternatives** documented: LiteNetLib (more battle-tested, our pick if Riptide stagnates); ENet-CSharp (native dep, an option if specific NAT-punching needs come up).
