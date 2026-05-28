# AGENTS.md — Engine & Project Guidelines

Binding guidelines for every code change in the `dotnet-moba` repo. Read this document **before** any non-trivial change. Most sections link to an ADR with the full rationale — the ADRs are the source of truth, this document is the quick reference.

---

## 1. Language

All written artifacts in this repo are **English only**: source-code comments, XML docs, ADRs, AGENTS.md, plan files, console output strings, configuration-file comments. The user converses in German (and German replies in chat are fine) but anything that lives in the repo is English-only.

---

## 2. Plans

Plan-mode plans are initially written by the harness to `~/.claude/plans/`. **As soon as the plan is approved (or as the very first implementation action):** copy the plan content into the repo at `./.plans/YYYY-MM-DD-short-title.md`.

- Format: `YYYY-MM-DD-short-title.md`. The date is the day the plan was approved.
- Plans are **immutable** — a new iteration creates a new dated file alongside, the old one stays as history.
- `.plans/` is **gitignored** (see `.gitignore`) — local-only working-copy history, not shared via the repo.

---

## 3. Coordinate & Math Conventions

See [ADR-001](docs/14-decision-log/adr-001-coordinate-system.md) and [ADR-002](docs/14-decision-log/adr-002-math-library.md).

| Axis | Meaning |
|---|---|
| +X | right |
| +Y | up |
| −Z | forward (into the scene) |
| +Z | back (toward the default camera) |

- **Handedness:** right-handed.
- **Front-face winding:** CCW (set explicitly in `OpenGLBackend`).
- **Math library:** Silk.NET.Maths (`Vector3D<float>`, `Matrix4X4<float>`, `Quaternion<float>`).
- **Matrix convention:** row-major storage, row-vector convention, multiplication order `model * view * projection`.
- **GLSL upload:** `transpose=false`. The shader uses the column-vector form: `gl_Position = u_mvp * vec4(pos, 1.0)`. The storage flip and the convention flip cancel out — see `OpenGLShader.SetUniform` for the code proof.
- **DotRecast interop** (later): convert between `Silk.NET.Maths.Vector3D<float>` and `System.Numerics.Vector3` via operators. Axes match without permutation — we live in the same RH Y-up world as Recast.

**Forbidden:**
- Custom `Vector3`/`Matrix4` types — use Silk types directly.
- Axis permutations in game code "because of book example code" — Madhav is LH +X forward, we are RH −Z forward. Adapt, do not permute.

---

## 4. Sim ↔ Render Separation (hard!)

See [ADR-004](docs/14-decision-log/adr-004-server-authoritative.md) and [ADR-005](docs/14-decision-log/adr-005-project-structure.md).

**Dependency invariants** (compiler-enforced; on violation, refactor immediately):

- `MOBA.Game.csproj` must **never** reference `MOBA.Engine.Graphics` or any Silk graphics package.
- `MOBA.Server.csproj` must **never** reference `Silk.NET.Windowing`, `Silk.NET.OpenGL`, `Silk.NET.Input`, `MOBA.Engine.Graphics`, or `MOBA.Game.Client`.

**Component naming convention:**

- **Sim components** in `MOBA.Game` use the suffix `Component` (`TransformComponent`, `MoveComponent`, `HealthComponent`).
- **Render components** in `MOBA.Game.Client` use the suffix `RendererComponent` / `VisualComponent` (`MeshRendererComponent`).

Sim components live on server + client (replicated truth). Render components live only on the client (they react to replicated state).

**Server-authoritative invariants:**

- No game logic on the client. Client = input sender + renderer + interpolation.
- The local player's champion may later do client-side prediction (with server reconciliation). Other entities are interpolated (~100 ms behind server time).
- Fog-of-war is **filtered on the server**. The client never receives what it is not allowed to see.
- **No lockstep determinism.** State replication tolerates float indeterminism.

---

## 5. .NET / C# Idiom Discipline

See [ADR-007](docs/14-decision-log/adr-007-dotnet-idiomatic-style.md) for the full substitution table.

**Ground rule:** Madhav's *Game Programming in C++* is a **conceptual** learning reference, not a code template. C++ idioms are replaced with .NET idioms. Target style: like Microsoft writes in `dotnet/runtime`, `dotnet/aspnetcore`, MAUI.

**Style conventions** (enforced via `.editorconfig` + `Directory.Build.props` — `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`):

- File-scoped namespaces (`namespace MOBA.X;`)
- PascalCase types/methods/properties/events, camelCase parameters/locals, `_camelCase` private/internal fields, `s_camelCase` private static, `I`-prefix interfaces, `T`-prefix type parameters
- `nullable enable`
- `var` when the type is obvious from the right-hand side, otherwise explicit type
- Allman braces, 4-space indent
- `readonly` wherever possible; `readonly record struct` for value semantics
- Properties instead of public fields
- Pattern matching instead of cast chains
- Records for DTOs / net messages
- `async`/`await` with `Async` suffix
- `using` directives sorted, `System.*` first

**Key substitutions** (book → .NET):

- `new`/`delete` → GC; `IDisposable` + `using` only for unmanaged resources (GL handles)
- Raw pointers (`Actor*`) → reference passed via constructor
- Singleton (`Game::GetInstance()`) → constructor injection
- `std::vector<T>` → `List<T>` or `T[]`; `Span<T>` for hot paths
- `static_cast` → `(T)x`, `x as T`, or pattern matching
- Macros → generics, attributes, source generators
- Multiple inheritance → composition + interfaces

**Deviations only** on explicit user request. When the idiomatic path is not obvious → **ask**, do not guess.

**Globally suppressed analyzer rules** (in `.editorconfig`):

- `CA1707` (underscores in identifiers) — we use `_camelCase`
- `CA1859` (use concrete type for perf) — fights the `IGraphicsBackend` abstraction (see ADR-003)
- `CA1812` (unused internal classes) — some are instantiated via DI/reflection
- `CS1591` (missing XML doc) — XML docs are optional

---

## 6. Graphics Backend

See [ADR-003](docs/14-decision-log/adr-003-graphics-backend-abstraction.md).

- Game code only talks to abstractions: `IGraphicsBackend`, `IMesh`, `ITexture`, `IShader`, `Material`, `Camera`, `Vertex`.
- The OpenGL backend lives under `MOBA.Engine.Graphics/OpenGL/`. A Vulkan backend will arrive under `Vulkan/` (or as a sibling assembly if dependencies force it).
- **Vulkan patch (later):** Y-flip in clip space (negative viewport height or `M22 *= -1` in the projection) + Z-range remap [−1, +1] → [0, +1]. World/view matrices stay RH Y-up.
- GLSL shaders live under `assets/shaders/`. The same content is also embedded as string constants in `MOBA.Game.Client/ShaderSources.cs` for the first skeleton; hot-reload file loading will come later.

---

## 7. Networking

See [ADR-006](docs/14-decision-log/adr-006-networking-riptide.md).

- Game code only talks to `INetTransport` + `NetChannel`.
- Concrete library: **Riptide** (`RiptideNetworking.Riptide`), concrete implementation deferred.
- First skeleton: `NullTransport` (in-process loopback). Send = no-op.
- When the Riptide adapter is built: `NetChannel.Reliable` → Riptide reliable channel, `NetChannel.Unreliable` → Riptide unreliable; `MessageReceived` is adapted from Riptide's `MessageReceived`.

---

## 8. Repo Layout

```
MOBA.Engine.Core/          – Sim infra: Game, Scene, Actor, Component, GameTime
MOBA.Engine.Graphics/      – IGraphicsBackend + abstractions + OpenGL/ backend
MOBA.Engine.Networking/    – INetTransport, NetChannel, NullTransport
MOBA.Game/                 – Sim: MobaWorld, Map, Actors, sim components
MOBA.Game.Client/          – Render components, camera controllers, mesh/texture factories
MOBA.Server/               – Headless entry point
MOBA.Client/               – Window + GL + game loop
assets/                    – shaders/, textures/ (copied into MOBA.Client output)
docs/14-decision-log/      – ADRs (immutable once accepted)
.plans/                    – plan-mode plan history
.editorconfig              – style + naming + analyzer severities
Directory.Build.props      – TargetFramework=net10.0, Nullable, WarningsAsErrors
```

---

## 9. Adding ADRs

When an architectural decision is made or changed:

1. New file `docs/14-decision-log/adr-NNN-short-title.md` based on `adr-000-template.md`.
2. Status: `Accepted` (or `Proposed` if still being discussed).
3. Add an entry to `docs/14-decision-log/index.md`.
4. If an old ADR is superseded: change its Status to `Superseded by [ADR-NNN](adr-NNN-title.md)`. **Nothing else** is edited on the old ADR — Context/Decision/Consequences are immutable.
