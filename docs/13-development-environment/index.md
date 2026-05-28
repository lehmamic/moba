# Development Environment

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` should report ≥ 10.0).
- A working OpenGL 3.3 Core driver for running the client. (The server is headless and runs anywhere.)
- An IDE that understands `.editorconfig` and `.slnx`: JetBrains Rider, Visual Studio 17.12+, or VS Code with the C# Dev Kit.

## Build

```sh
dotnet build dotnet-moba.slnx
```

All seven projects must build with **zero warnings**. `TreatWarningsAsErrors=true` + `EnforceCodeStyleInBuild=true` (see [Code](../08-code/index.md)) means style violations also break the build.

## Run

**Headless server** (no window, ticks at 30 Hz, Ctrl-C to stop):

```sh
dotnet run --project MOBA.Server
```

**Client** (opens a window, renders the skeleton scene):

```sh
dotnet run --project MOBA.Client
```

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
