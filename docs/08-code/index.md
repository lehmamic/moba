# Code

Code-level conventions and the repo layout. The principles behind these conventions live in [Principles](../06-principles/index.md); the architectural reasons for the project split live in [Software Architecture](../07-software-architecture/index.md).

---

## Repo layout

```
MOBA.Engine.Core/          – Sim infra: Game, Scene, Actor, Component, GameTime
MOBA.Engine.Graphics/      – IGraphicsBackend + abstractions + OpenGL/ backend
MOBA.Engine.Networking/    – INetTransport, NetChannel, NullTransport
MOBA.Game/                 – Sim: MobaWorld, Map, Actors, sim components
MOBA.Game.Client/          – Render components, camera controllers, mesh/texture factories
MOBA.Server/               – Headless entry point
MOBA.Client/               – Window + GL + game loop
tests/MOBA.Architecture.Tests/  – ArchUnitNET + xUnit v3, enforces dependency / naming / visibility invariants
assets/                    – shaders/, textures/ (copied into MOBA.Client output)
docs/                      – Software Guidebook (this folder), 14-decision-log/ holds ADRs
.editorconfig              – style + naming + analyzer severities
Directory.Build.props      – TargetFramework=net10.0, Nullable, WarningsAsErrors
```

`.plans/` exists in the working copy as a local-only plan history but is gitignored.

---

## Style conventions

Enforced project-wide via `.editorconfig` + `Directory.Build.props` (`TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`). Style violations break the build, not just the IDE.

- **File-scoped namespaces** (`namespace MOBA.X;`)
- **PascalCase** types/methods/properties/events
- **camelCase** parameters/locals
- **`_camelCase`** private/internal fields (Microsoft runtime convention)
- **`s_camelCase`** private static fields
- **`I`-prefix** interfaces, **`T`-prefix** type parameters
- **`nullable enable`** project-wide; no `!` suppression without a justification comment
- **`var`** when the right-hand-side type is obvious, otherwise explicit type
- **Allman braces**, 4-space indent
- **`async`/`await`** with `Async` method-name suffix; `ConfigureAwait` not needed in app code
- **`readonly`** wherever possible; `readonly record struct` for value semantics
- **Properties** instead of public fields
- **Pattern matching** instead of cast chains
- **Records** for DTOs / net messages
- **`using` directives** sorted: `System.*` first, then `Silk.NET.*`, then ours
- One `.cs` per type. **No** artificial `partial class` splits.

See [ADR-007](../14-decision-log/adr-007-dotnet-idiomatic-style.md) for the full rationale and substitution table.

---

## Globally suppressed analyzer rules

Configured in `.editorconfig`:

| Rule | Why suppressed |
|---|---|
| `CA1707` (underscores in identifiers) | We use `_camelCase` and `s_camelCase` |
| `CA1859` (use concrete type for perf) | Fights the `IGraphicsBackend` abstraction — see [ADR-003](../14-decision-log/adr-003-graphics-backend-abstraction.md) |
| `CA1812` (unused internal classes) | Some are instantiated via DI/reflection |
| `CS1591` (missing XML doc) | XML docs are optional, only where useful |

---

## Configuration files

- **`.editorconfig`** — style, naming rules, analyzer severities. Picked up automatically by Rider, Visual Studio, VS Code (with C# Dev Kit) and by the .NET SDK at build time.
- **`Directory.Build.props`** — applied to every project. Pins `TargetFramework=net10.0`, `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest-recommended`. Generates XML doc files so `IDE0005` (remove unnecessary using) can run at build.
- **`Directory.Packages.props`** — Central Package Management (CPM). All NuGet package versions live here as `<PackageVersion Include="..." Version="..." />` entries; `.csproj` files declare `<PackageReference Include="..." />` **without** an inline `Version` attribute. `CentralPackageTransitivePinningEnabled=true` makes the build fail if a project tries to re-declare a version. Bump versions in one place.
- **`dotnet-moba.slnx`** — solution file in the newer `.slnx` XML format, lists all seven production projects plus the test project.
