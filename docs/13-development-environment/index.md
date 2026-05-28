# Development Environment

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` should report ≥ 10.0).
- A working OpenGL 3.3 Core driver for running the client. (The server is headless and runs anywhere.)
- An IDE that understands `.editorconfig` and `.slnx`: JetBrains Rider, Visual Studio 17.12+, or VS Code with the C# Dev Kit.

## Build

```sh
dotnet build dotnet-moba.slnx
```

All nine production projects (plus the test projects) must build with **zero warnings**. `TreatWarningsAsErrors=true` + `EnforceCodeStyleInBuild=true` (see [Code](../08-code/index.md)) means style violations also break the build.

## Test

```sh
dotnet test
```

Test projects under `tests/`:

- `tests/MOBA.Architecture.Tests/` (xUnit v3 + ArchUnitNET) — encodes the dependency, naming, and visibility invariants. See [Quality Attributes — Build-time enforcement of invariants](../04-quality-attributes/index.md#build-time-enforcement-of-invariants).
- `tests/MOBA.Utilities.Tests/` (xUnit v3) — behavioural tests for `MOBA.Utilities` (`AbsolutePath`, `RelativePath`, `string`-extension operators).

## Run

The client connects to the server over UDP on `127.0.0.1:7777`, so the server must be started first.

**Terminal 1** — start the server (no window, ticks at 30 Hz, Ctrl-C to stop):

```sh
dotnet run --project MOBA.Server
```

Expected output:

```
[MOBA.Server] match starting @ 30 Hz
[MOBA.Server] listening on UDP 7777
```

**Terminal 2** — start the client (opens a window, dials the server):

```sh
dotnet run --project MOBA.Client
```

Expected output:

```
[MOBA.Client] connected to 127.0.0.1:7777
[MOBA.Client] Loaded. LMB = move cube, F1 = camera toggle, …
```

The server logs `client <id> connected` once the client handshake completes. Left-clicking on the ground sends a `MoveCommand` to the server; the cube starts moving and a magenta marker appears at the click point. When the cube arrives, the marker disappears. Clicking again before arrival redirects the cube.

Both `Program.cs` files are intentionally two-liners — all behaviour lives in `ClientGame` / `ServerGame` (see [Software Architecture — Hosting & system lifecycle](../07-software-architecture/index.md#hosting--system-lifecycle)).

### Running multiple matches locally

One server process hosts exactly one match ([ADR-010](../14-decision-log/adr-010-one-match-per-process.md)). To test concurrent matches locally, open multiple terminals and run `dotnet run --project MOBA.Server` in each. Each instance is independent: separate sim time, separate console output, separate Ctrl-C.

## Client controls (skeleton)

| Key | Action |
|---|---|
| Left mouse | Send `MoveCommand` to server: cube heads to the click point, marker sphere appears there until arrival |
| W/A/S/D | Move free-fly camera along camera forward/right |
| Q/E | Move free-fly camera down/up along world +Y |
| Right mouse + drag | Mouse-look (yaw + pitch) |
| F1 | Toggle between free-fly and fixed MOBA top-down camera |
| Window close | Clean shutdown |

## IDE notes

- The `.editorconfig` at the repo root is the authoritative style source. Do not override style settings per-user.
- Rider / Visual Studio pick up `.slnx` natively. VS Code with C# Dev Kit also supports it.
- The `assets/` folder is copied into `MOBA.Client`'s output via a glob in `MOBA.Client.csproj`; no manual copy step is needed.
