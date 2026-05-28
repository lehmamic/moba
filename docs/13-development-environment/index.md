# Development Environment

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` should report ≥ 10.0).
- A working OpenGL 3.3 Core driver for running the client. (The server is headless and runs anywhere.)
- An IDE that understands `.editorconfig` and `.slnx`: JetBrains Rider, Visual Studio 17.12+, or VS Code with the C# Dev Kit.

## Build

```sh
dotnet build dotnet-moba.slnx
```

All eight production projects (plus the architecture-tests project) must build with **zero warnings**. `TreatWarningsAsErrors=true` + `EnforceCodeStyleInBuild=true` (see [Code](../08-code/index.md)) means style violations also break the build.

## Test

```sh
dotnet test
```

The only test project today is `tests/MOBA.Architecture.Tests/` (xUnit v3 + ArchUnitNET). It encodes the dependency, naming, and visibility invariants — see [Quality Attributes — Build-time enforcement of invariants](../04-quality-attributes/index.md#build-time-enforcement-of-invariants).

## Run

**Headless server** (no window, ticks at 30 Hz, Ctrl-C to stop):

```sh
dotnet run --project MOBA.Server
```

**Client** (opens a window, renders the skeleton scene):

```sh
dotnet run --project MOBA.Client
```

Both `Program.cs` files are intentionally two-liners — all behaviour lives in `ClientGame` / `ServerGame` (see [Software Architecture — Hosting & system lifecycle](../07-software-architecture/index.md#hosting--system-lifecycle)).

### Running multiple matches locally

One server process hosts exactly one match ([ADR-010](../14-decision-log/adr-010-one-match-per-process.md)). To test concurrent matches locally, open multiple terminals and run `dotnet run --project MOBA.Server` in each. Each instance is independent: separate sim time, separate console output, separate Ctrl-C.

## Client controls (skeleton)

| Key | Action |
|---|---|
| W/A/S/D | Move free-fly camera along camera forward/right |
| Q/E | Move free-fly camera down/up along world +Y |
| Right mouse + drag | Mouse-look (yaw + pitch) |
| F1 | Toggle between free-fly and fixed MOBA top-down camera |
| Window close | Clean shutdown |

## IDE notes

- The `.editorconfig` at the repo root is the authoritative style source. Do not override style settings per-user.
- Rider / Visual Studio pick up `.slnx` natively. VS Code with C# Dev Kit also supports it.
- The `assets/` folder is copied into `MOBA.Client`'s output via a glob in `MOBA.Client.csproj`; no manual copy step is needed.
