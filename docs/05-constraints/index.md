# Constraints

Fixed inputs to the design — things we accept rather than choose. Each constraint shapes one or more architectural decisions.

## Runtime

- **.NET 10** as target framework, pinned in `Directory.Build.props`.
- Cross-platform desktop initially (macOS, Linux, Windows). Mobile is not on the immediate roadmap.

## Graphics

- **OpenGL** as the first backend (Silk.NET). Picks: available everywhere out of the box, sufficient for the skeleton, well-supported in C# via Silk.NET. macOS deprecation notwithstanding, it still works.
- **Vulkan** is the planned future backend (better performance path, mobile/Android-friendly via MoltenVK on macOS). The graphics-backend abstraction is designed to make Vulkan a drop-in. See [ADR-003](../14-decision-log/adr-003-graphics-backend-abstraction.md).

## Networking

- **UDP-based reliable transport** with channels is required (snapshots unreliable, game events reliable).
- **Riptide** (`RiptideNetworking.Riptide`) is the chosen library; the concrete implementation is deferred to the netcode phase. See [ADR-006](../14-decision-log/adr-006-networking-riptide.md).

## Learning reference

- Sanjay Madhav's *Game Programming in C++* is the **conceptual** learning reference (game loop, Actor/Component pattern, renderer layering, math). It is not a code template — see [ADR-007](../14-decision-log/adr-007-dotnet-idiomatic-style.md).

## Things we explicitly do not constrain

- **No lockstep determinism.** State replication tolerates float indeterminism — see [ADR-004](../14-decision-log/adr-004-server-authoritative.md).
- **No custom math library.** Silk.NET.Maths covers everything; rolling our own would be busywork. See [ADR-002](../14-decision-log/adr-002-math-library.md).
