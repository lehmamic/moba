# Quality Attributes

The non-functional properties the engine and game are optimised for. Each attribute links to the principles that operationalise it and the ADRs that justify it.

## Cheat resistance

The server is the only source of truth. The client renders an interpolated view and sends inputs. Whole classes of cheats (speed hacks, stat hacks, map hacks for vision the player should not have) are physically impossible because the data never reaches the client.

- Operationalised by the [sim/render separation principle](../06-principles/index.md#3-simulation--rendering-separation).
- Architectural realisation: see [Software Architecture](../07-software-architecture/index.md).
- Rationale: [ADR-004](../14-decision-log/adr-004-server-authoritative.md).

## Backend portability

Both graphics and networking are accessed only through engine-internal abstractions (`IGraphicsBackend`, `INetTransport`). Switching from OpenGL to Vulkan, or from Riptide to LiteNetLib, is a backend swap — game code is not touched.

- Architectural realisation: see [Software Architecture](../07-software-architecture/index.md).
- Rationale: [ADR-003](../14-decision-log/adr-003-graphics-backend-abstraction.md) (graphics), [ADR-006](../14-decision-log/adr-006-networking-riptide.md) (networking).

## Build-time enforcement of invariants

Dependency invariants between projects (Game does not see Graphics; Server does not see Windowing/OpenGL/Input) are compiler-enforced via the [7-project layout](../07-software-architecture/index.md). Code-style invariants (file-scoped namespaces, naming, nullable annotations) are build-time enforced via `EnforceCodeStyleInBuild=true` + `TreatWarningsAsErrors=true` in `Directory.Build.props`. A violation does not compile.

- Rationale: [ADR-005](../14-decision-log/adr-005-project-structure.md), [ADR-007](../14-decision-log/adr-007-dotnet-idiomatic-style.md).

## What is explicitly **not** a quality attribute

- **Lockstep determinism.** State replication tolerates float indeterminism — the server is the truth. See [ADR-004](../14-decision-log/adr-004-server-authoritative.md).
- **AAA-engine performance.** The engine is built for learning + functional gameplay. Performance optimisations are deferred until profiling demonstrates a need.
