# Software Architecture

High-level structure of the engine and game. Detailed code-level concerns (naming, file layout, configuration) live in [Code](../08-code/index.md); the engineering rules behind these decisions live in [Principles](../06-principles/index.md).

---

## Project layout

Eight production assemblies + one test assembly. Compile-time references encode the [sim/render separation principle](../06-principles/index.md#3-simulation--rendering-separation).

| Project | Responsibility | May reference |
|---|---|---|
| `MOBA.Engine.Core` | Game loop, Actor/Component, Time, Scene | Silk.NET.Maths |
| `MOBA.Engine.Graphics` | Backend-agnostic abstractions: `IGraphicsBackend`, `IMesh`, `ITexture`, `IShader`, `Material`, `Camera`, `Vertex`, `TextureLoader` | Engine.Core, Silk.NET.Maths, StbImageSharp |
| `MOBA.Engine.Graphics.OpenGL` | Concrete OpenGL backend (`OpenGLBackend`, `OpenGLMesh`, `OpenGLTexture`, `OpenGLShader`) | Engine.Graphics, Silk.NET.OpenGL, Silk.NET.Maths |
| `MOBA.Engine.Networking` | `INetTransport`, channels, `NullTransport` | (no external deps; Riptide arrives later) |
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

## Networking transport abstraction

Game code only talks to `INetTransport` + `NetChannel`.

- Concrete library: **Riptide** (`RiptideNetworking.Riptide`), concrete implementation deferred to the netcode phase.
- First skeleton: `NullTransport` (in-process loopback). Send = no-op.
- When the Riptide adapter is built: `NetChannel.Reliable` → Riptide reliable channel, `NetChannel.Unreliable` → Riptide unreliable; `MessageReceived` is adapted from Riptide's `MessageReceived`.

See [ADR-006](../14-decision-log/adr-006-networking-riptide.md).
