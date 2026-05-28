# Software Architecture

High-level structure of the engine and game. Detailed code-level concerns (naming, file layout, configuration) live in [Code](../08-code/index.md); the engineering rules behind these decisions live in [Principles](../06-principles/index.md).

---

## Project layout

Eight production assemblies + one test assembly. Compile-time references encode the [sim/render separation principle](../06-principles/index.md#3-simulation--rendering-separation).

| Project | Responsibility | May reference |
|---|---|---|
| `MOBA.Utilities` | Cross-cutting BCL helpers (`AbsolutePath`, `RelativePath`, …) | (no external deps) |
| `MOBA.Engine.Core` | Game loop, Actor/Component, Time, Scene | Utilities, Silk.NET.Maths |
| `MOBA.Engine.Graphics` | Backend-agnostic abstractions: `IGraphicsBackend`, `IMesh`, `ITexture`, `IShader`, `Material`, `Camera`, `Vertex`, `TextureLoader` | Engine.Core, Silk.NET.Maths, StbImageSharp |
| `MOBA.Engine.Graphics.OpenGL` | Concrete OpenGL backend (`OpenGLBackend`, `OpenGLMesh`, `OpenGLTexture`, `OpenGLShader`) | Engine.Graphics, Silk.NET.OpenGL, Silk.NET.Maths |
| `MOBA.Engine.Networking` | Backend-agnostic transport: `INetTransport`, `NetChannel`, `NullTransport` | (no external deps) |
| `MOBA.Engine.Networking.Riptide` | Concrete UDP transport (`RiptideServerTransport`, `RiptideClientTransport`) | Engine.Core, Engine.Networking, RiptideNetworking.Riptide |
| `MOBA.Game` | Sim: MobaWorld, Map, actors, sim components | Engine.Core, Engine.Networking, Silk.NET.Maths |
| `MOBA.Game.Client` | Render components, camera controllers, mesh/texture factories | Engine.Core, Engine.Graphics, Game, Silk.NET.Input, Silk.NET.Maths |
| `MOBA.Server` | Headless entry point | Engine.Core, Engine.Networking, Game |
| `MOBA.Client` | Window + GL bootstrap + asset loading + game loop | Engine.Core, Engine.Graphics, Engine.Graphics.OpenGL, Engine.Networking, Game, Game.Client, Silk.NET (Windowing/OpenGL/Input/Maths) |

**Hard invariants** (verified by `.csproj` inspection + `tests/MOBA.Architecture.Tests/`):

- `MOBA.Engine.Graphics.csproj` references neither its OpenGL backend nor `Silk.NET.OpenGL` (the abstraction does not know its implementation).
- `MOBA.Game.csproj` references neither `MOBA.Engine.Graphics` nor `MOBA.Engine.Graphics.OpenGL` nor any Silk graphics package.
- `MOBA.Server.csproj` references neither `Silk.NET.Windowing` / `Silk.NET.OpenGL` / `Silk.NET.Input`, nor the OpenGL backend.

See [ADR-005](../14-decision-log/adr-005-project-structure.md) and [ADR-008](../14-decision-log/adr-008-opengl-backend-sibling-project.md).

---

## Server-authoritative model

```
+-----------+   inputs (UDP, click/cast)   +-----------+
|  Client   | ---------------------------> |  Server   |
|           |                              | (truth)   |
|           | <--------------------------- |           |
+-----------+   snapshots (UDP, ~30 Hz)    +-----------+
       |                                          |
   renders                                   simulates
   interpolated                              (movement,
   state ~100 ms                              abilities,
   behind server                              pathfinding,
                                              vision/fog)
```

- Server runs headless at a fixed tick rate (skeleton default: 30 Hz).
- Client sends inputs; never owns gameplay state.
- Fog-of-war is filtered on the server — the client never receives what it should not see.
- See [ADR-004](../14-decision-log/adr-004-server-authoritative.md).

---

## Graphics backend abstraction

Game code only talks to abstractions: `IGraphicsBackend`, `IMesh`, `ITexture`, `IShader`, `Material`, `Camera`, `Vertex`.

- The OpenGL backend lives in the sibling project `MOBA.Engine.Graphics.OpenGL`; the abstraction project does not reference `Silk.NET.OpenGL` ([ADR-008](../14-decision-log/adr-008-opengl-backend-sibling-project.md)).
- A Vulkan backend will arrive as `MOBA.Engine.Graphics.Vulkan` alongside it.
- **Vulkan patch (later):** Y-flip in clip space (negative viewport height or `M22 *= -1` in the projection) + Z-range remap [−1, +1] → [0, +1]. World/view matrices stay RH Y-up.
- GLSL shaders live under `assets/shaders/` and are loaded from file (`File.ReadAllText`) at startup. Textures live under `assets/textures/` and are loaded via `MOBA.Engine.Graphics.TextureLoader` (StbImageSharp; PNG/JPG/TGA/BMP, RGBA8, vertical flip on load to match OpenGL's bottom-left texture origin). Hot-reload (re-read on file change) is a later step.

See [ADR-003](../14-decision-log/adr-003-graphics-backend-abstraction.md).

---

## Hosting & system lifecycle

Both entry points share an abstract `GameHost` in `MOBA.Engine.Core`. It owns the sim `Game` and a list of `IEngineSystem` instances, and propagates the per-tick lifecycle.

- `IEngineSystem` (in `MOBA.Engine.Core`) — `OnInitialize` / `OnUpdate(GameTime)` / `OnShutdown` plus `IDisposable`.
- `GameHost.Initialize()` runs `OnInitialize` on each system in registration order; `Update(dt)` runs `OnUpdate` on each system then `Game.Update(dt)`; `Shutdown()` runs `OnShutdown` in **LIFO** order and is idempotent. `Run()` is *not* on the base — the cadence differs.
- **`ClientGame : GameHost`** (in `MOBA.Client`) — owns the Silk.NET `IWindow` and the OpenGL backend. `Run()` delegates to `_window.Run()`; window callbacks route into the base lifecycle. Hosts these systems: `InputSystem`, `AssetManager`, `CameraSwitcher`. The `Renderer` is invoked from the window's render callback (not as a system, because per-frame ≠ per-tick).
- **`ServerGame : GameHost`** (in `MOBA.Server`) — owns the fixed-step loop. `Run()` calls `Initialize()`, loops `Update(TickInterval)` with a sleep, and calls `Shutdown()` on Ctrl-C. Hosts an `AssetCache<string, MapDefinition>` system for data assets; future Networking + NavMesh systems land beside it.

Each `Program.cs` is intentionally a two-liner:

```csharp
using var game = new ClientGame();  // or new ServerGame()
game.Run();
```

**Multi-match scaling** is out-of-process: one server process hosts one match; multi-match means multiple processes. See [ADR-010](../14-decision-log/adr-010-one-match-per-process.md).

Component-side: `MeshRendererComponent` (in `MOBA.Game.Client`) implements `IRenderable` (in `MOBA.Engine.Graphics`). The `Renderer` discovers what to draw by walking `Scene.Actors` and picking components that implement `IRenderable`, so it never sees client-side types.

### Asset caching

Both server and client use the **same** `AssetManager` (in `MOBA.Engine.Core`) — a hub that owns a list of typed `AssetCache<TKey, TAsset>` instances and propagates the host lifecycle to each. `AssetManager` itself stays free of graphics or game dependencies.

The typed caches and convenience load methods come from extension methods that live with the matching layer:

- `MOBA.Engine.Graphics.AssetManagerExtensions` — `AddShaderCache(backend)` + `LoadShader(vert, frag)`, `AddTextureCache(backend)` + `LoadTexture(path)`. Used by the client only.
- `MOBA.Game.AssetManagerExtensions` — `AddMapCache()` + `LoadMap(path)` (System.Text.Json over `MapDefinition`). Used by **both** server and client.
- `MOBA.Game.Client.AssetManagerExtensions` — `AddMeshCache(backend)` + `LoadCubeMesh()` / `LoadGroundMesh(w, l, t)`. One `AssetCache<string, IMesh>` covers every procedural mesh; the construction parameters are flattened into the key string (`cube`, `ground/150/150/2`, …). New mesh types extend the same key scheme. Disposal flows through the host lifecycle, replacing the ad-hoc mesh tracking list.

`AssetCache<TKey, TAsset>` is the underlying primitive (lazy load, cache by key, dispose any `IDisposable` entries on shutdown). Future asset types (ability tables, champion stats, navmesh blobs) ship as new extension methods that add the cache and expose a typed `Load*` helper — no changes to `AssetManager` itself.

Both server and client load the map dimensions from `assets/maps/default.json` through this pattern, then call `Map.FromDefinition(...)` to build the runtime `Map`. The server's `.csproj` only copies `assets/maps/**` to its output; shaders and textures stay in the client's bin.

See [ADR-009](../14-decision-log/adr-009-gamehost-shared-abstraction.md) for the GameHost rationale.

---

## Networking transport abstraction

Game code only talks to `INetTransport` + `NetChannel`.

- Concrete library: **Riptide** (`RiptideNetworking.Riptide`), implemented in the sibling project `MOBA.Engine.Networking.Riptide` ([ADR-011](../14-decision-log/adr-011-riptide-transport-sibling-project.md)).
- `RiptideServerTransport` listens on UDP/7777 and `RiptideClientTransport` connects to `127.0.0.1:7777`. Both implement `INetTransport` plus `IEngineSystem` so a `GameHost` drives them via `AddSystem(...)`: `OnInitialize` opens the socket / dials the server, `OnUpdate` pumps Riptide's required tick, `OnShutdown` closes.
- `NetChannel.Reliable` → `MessageSendMode.Reliable`, `NetChannel.Unreliable` → `MessageSendMode.Unreliable`. Payload bytes pass through opaquely — the game-side message protocol is hand-rolled binary on top.
- `NullTransport` stays as the no-op stub for tests and disconnected scenarios.

See [ADR-006](../14-decision-log/adr-006-networking-riptide.md).
