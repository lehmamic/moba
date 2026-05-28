# ADR-005: 7-project solution layout

## Status

Accepted

## Context

The repo initially had a single `MOBA.Runner` console project. The boundaries we pinned in ADR-003 (backend abstraction) and ADR-004 (sim/render separation) cannot be enforced through folder convention inside a single project — we need assembly boundaries so the compiler enforces the invariants.

## Decision

We split into seven projects:

| Project | Responsibility | May reference |
|---|---|---|
| `MOBA.Engine.Core` | Game loop, Actor/Component, Time, Scene | Silk.NET.Maths |
| `MOBA.Engine.Graphics` | `IGraphicsBackend` + OpenGL backend, mesh/texture/shader abstractions | Engine.Core, Silk.NET.OpenGL, Silk.NET.Maths, StbImageSharp |
| `MOBA.Engine.Networking` | `INetTransport`, channels, `NullTransport` | (no external deps; Riptide arrives later) |
| `MOBA.Game` | Sim: MobaWorld, Map, actors, sim components | Engine.Core, Engine.Networking, Silk.NET.Maths |
| `MOBA.Game.Client` | Render components, camera controllers, mesh/texture factories | Engine.Core, Engine.Graphics, Game, Silk.NET.Input, Silk.NET.Maths |
| `MOBA.Server` | Headless entry point | Engine.Core, Engine.Networking, Game |
| `MOBA.Client` | Window + GL bootstrap + asset loading + game loop | Engine.Core, Engine.Graphics, Engine.Networking, Game, Game.Client, Silk.NET (Windowing/OpenGL/Input/Maths) |

`MOBA.Runner` is deleted — replaced by `MOBA.Server` and `MOBA.Client`.

**Hard invariants** (verified by `.csproj` inspection, later CI):

- `MOBA.Game.csproj` does **not** reference `MOBA.Engine.Graphics` and **no** Silk graphics package.
- `MOBA.Server.csproj` does **not** reference `Silk.NET.Windowing`, `Silk.NET.OpenGL`, `Silk.NET.Input`.

## Consequences

- **Positive:** the compiler enforces sim/render and server/client separation. A headless server build is physically possible (testable with `dotnet run --project MOBA.Server`). The Vulkan drop-in path is clear (`MOBA.Engine.Graphics/Vulkan/` or a sibling assembly).
- **Negative:** seven instead of one project — more ceremony when navigating, longer cold-start build. In practice only changed assemblies recompile per iteration, so it's negligible.
- **Assets** live under repo-root `assets/` (shared resources). `MOBA.Client.csproj` contains a glob that copies `assets/**` into the output directory (`CopyToOutputDirectory=PreserveNewest`). The server needs no assets.
