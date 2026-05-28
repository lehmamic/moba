# ADR-011: Riptide concrete transport lives in a sibling project

## Status

Accepted

## Context

[ADR-006](adr-006-networking-riptide.md) chose Riptide as the netcode library and pinned `MOBA.Engine.Networking` as the home of the `INetTransport` abstraction, with the concrete implementation deferred to "the netcode phase". That phase has arrived: we need real UDP transport for click-to-move.

[ADR-008](adr-008-opengl-backend-sibling-project.md) established the pattern for backend implementations: keep the abstraction project type-pure (no concrete-backend package reference, no `unsafe`), and put each concrete backend in a sibling project (`MOBA.Engine.Graphics` + `MOBA.Engine.Graphics.OpenGL`).

The same pattern fits networking. Hosting `RiptideServerTransport` and `RiptideClientTransport` inside `MOBA.Engine.Networking` would force the abstraction project to take a `RiptideNetworking.Riptide` package reference; an architectural test could only check "MOBA.Game does not use Riptide types" rather than the stronger "the abstraction project cannot see Riptide types at all".

## Decision

Move the Riptide implementation to a new sibling project **`MOBA.Engine.Networking.Riptide`**:

- `MOBA.Engine.Networking.Riptide/MOBA.Engine.Networking.Riptide.csproj` — takes `RiptideNetworking.Riptide` package reference plus project references to `MOBA.Engine.Networking` (the abstraction) and `MOBA.Engine.Core` (for `IEngineSystem`).
- `RiptideServerTransport` and `RiptideClientTransport` implement both `INetTransport` (so game code sees them only through that abstraction) and `IEngineSystem` (so a `GameHost` drives them via `AddSystem`).
- `MOBA.Engine.Networking.csproj` stays as today — no package references, just `INetTransport`, `NetChannel`, `NullTransport`.
- `MOBA.Server.csproj` and `MOBA.Client.csproj` reference both projects (the abstraction for the interface, the Riptide sibling for instantiation).
- `tests/MOBA.Architecture.Tests/` adds invariants enforcing the boundary:
  - `MOBA.Engine.Networking` does not depend on `MOBA.Engine.Networking.Riptide`.
  - `MOBA.Engine.Networking` does not depend on the `RiptideNetworking` assembly.
  - `MOBA.Engine.Networking.Riptide` does not depend on higher layers (Game, Game.Client, Server, Client, Graphics).
  - `MOBA.Game` and `MOBA.Game.Client` do not depend on the Riptide assembly directly.

## Consequences

- **Positive:**
  - Mirrors the established graphics pattern; the engine has a uniform shape for "abstraction + N concrete backends".
  - The networking abstraction can be referenced (and tested) in isolation.
  - Swapping to a different transport library (LiteNetLib, ENet, custom UDP) is a sibling-project add, no game-code change.
  - Strong, type-level architectural guarantee that game code never reaches for a Riptide type by accident.
- **Negative:**
  - Another project in the solution. Build time goes up by milliseconds. Acceptable.
- **Naming:** namespace is `MOBA.Engine.Networking.Riptide`, matching the project name. Riptide types referenced via `using Riptide;` (the upstream namespace).
- **Not superseding ADR-006:** ADR-006 already implied this split when it said the concrete implementation "comes later"; this ADR makes the structural decision concrete.
