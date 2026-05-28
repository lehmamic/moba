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

## Crash + resource isolation per match

The server runs one match per OS process. A crash, OOM, hang, or slow tick in one match cannot affect any other match — operating-system isolation is the boundary. Per-process metrics (CPU, RSS, file descriptors, GC pauses) are observable through standard tools without custom per-match instrumentation. Hot-restart of a single match leaves the rest of the cluster untouched.

Scaling to N matches is an operations concern: launch N processes (Docker container, k8s pod, systemd unit, or `dotnet run` in N terminals locally). There is deliberately no in-process orchestrator.

- Architectural realisation: see [Software Architecture](../07-software-architecture/index.md).
- Rationale: [ADR-010](../14-decision-log/adr-010-one-match-per-process.md).

## Build-time enforcement of invariants

Dependency invariants between projects (Game does not see Graphics; Server does not see Windowing/OpenGL/Input; the graphics abstraction does not see its OpenGL backend) are compiler-enforced via the [project layout](../07-software-architecture/index.md). Code-style invariants (file-scoped namespaces, naming, nullable annotations) are build-time enforced via `EnforceCodeStyleInBuild=true` + `TreatWarningsAsErrors=true` in `Directory.Build.props`. A violation does not compile.

Beyond what the compiler can catch, `tests/MOBA.Architecture.Tests/` uses [ArchUnitNET](https://github.com/TNG/ArchUnitNET) over xUnit v3 to assert:

- type-level dependency invariants (sim assemblies never reach into graphics; the server entry point pulls no Silk graphics/windowing/input packages);
- naming conventions (`I`-prefix on interfaces, `Component` suffix on sim components, `RendererComponent` / `VisualComponent` suffix on render-side components);
- visibility (OpenGL resource wrappers must not leak as part of the public API — only `OpenGLBackend` is public).

Run them with `dotnet test`. A failing test points at a load-bearing rule that has been broken.

- Rationale: [ADR-005](../14-decision-log/adr-005-project-structure.md), [ADR-007](../14-decision-log/adr-007-dotnet-idiomatic-style.md).

## What is explicitly **not** a quality attribute

- **Lockstep determinism.** State replication tolerates float indeterminism — the server is the truth. See [ADR-004](../14-decision-log/adr-004-server-authoritative.md).
- **AAA-engine performance.** The engine is built for learning + functional gameplay. Performance optimisations are deferred until profiling demonstrates a need.
