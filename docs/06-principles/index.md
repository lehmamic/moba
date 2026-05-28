# Principles

The engineering principles every change has to honour. Each principle has a rationale ADR; the ADR is the immutable source of truth.

---

## 1. Coordinate & math conventions

See [ADR-001](../14-decision-log/adr-001-coordinate-system.md) and [ADR-002](../14-decision-log/adr-002-math-library.md).

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

## 2. Idiomatic .NET / C# style

See [ADR-007](../14-decision-log/adr-007-dotnet-idiomatic-style.md) for the full substitution table.

**Ground rule:** Madhav's *Game Programming in C++* is a **conceptual** learning reference, not a code template. C++ idioms are replaced with .NET idioms. Target style: like Microsoft writes in `dotnet/runtime`, `dotnet/aspnetcore`, MAUI.

**Key substitutions** (book → .NET):

- `new`/`delete` → GC; `IDisposable` + `using` only for unmanaged resources (GL handles)
- Raw pointers (`Actor*`) → reference passed via constructor
- Singleton (`Game::GetInstance()`) → constructor injection
- `std::vector<T>` → `List<T>` or `T[]`; `Span<T>` for hot paths
- `static_cast` → `(T)x`, `x as T`, or pattern matching
- Macros → generics, attributes, source generators
- Multiple inheritance → composition + interfaces

The day-to-day style rules (naming, namespaces, brace style, suppressed analyzers) live in [Code](../08-code/index.md) — they are enforced by `.editorconfig` + `Directory.Build.props` so they bind at build time, not just in the IDE.

**Deviations only** on explicit user request. When the idiomatic path is not obvious → ask, do not guess.

---

## 3. Simulation ↔ Rendering separation

See [ADR-004](../14-decision-log/adr-004-server-authoritative.md) and [ADR-005](../14-decision-log/adr-005-project-structure.md).

**Hard invariants** (compiler-enforced; on violation, refactor immediately):

- `MOBA.Game.csproj` must **never** reference `MOBA.Engine.Graphics` or any Silk graphics package.
- `MOBA.Server.csproj` must **never** reference `Silk.NET.Windowing`, `Silk.NET.OpenGL`, `Silk.NET.Input`, `MOBA.Engine.Graphics`, or `MOBA.Game.Client`.

**Component naming convention:**

- **Sim components** in `MOBA.Game` use the suffix `Component` (`TransformComponent`, `MoveComponent`, `HealthComponent`).
- **Client-only components** in `MOBA.Game.Client` use a distinguishing suffix that makes the concern obvious at the call site: `RendererComponent` / `VisualComponent` for render-side (`MeshRendererComponent`), `InputComponent` for local-player input (`LocalCubeInputComponent`).

Sim components live on server + client (replicated truth). Client-only components live only on the client — render components react to replicated state, input components turn local input into server-bound commands.

**Server-authoritative invariants:**

- No game logic on the client. Client = input sender + renderer + interpolation.
- The local player's champion may later do client-side prediction (with server reconciliation). Other entities are interpolated (~100 ms behind server time).
- Fog-of-war is **filtered on the server**. The client never receives what it is not allowed to see.
- **No lockstep determinism.** State replication tolerates float indeterminism.
