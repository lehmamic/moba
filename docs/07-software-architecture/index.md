# Software Architecture

High-level structure of the engine and game. Detailed code-level concerns (naming, file layout, configuration) live in [Code](../08-code/index.md); the engineering rules behind these decisions live in [Principles](../06-principles/index.md).

---

## Project layout

Seven assemblies. Compile-time references encode the [sim/render separation principle](../06-principles/index.md#3-simulation--rendering-separation).

| Project | Responsibility | May reference |
|---|---|---|
| `MOBA.Engine.Core` | Game loop, Actor/Component, Time, Scene | Silk.NET.Maths |
| `MOBA.Engine.Graphics` | `IGraphicsBackend` + OpenGL backend, mesh/texture/shader abstractions | Engine.Core, Silk.NET.OpenGL, Silk.NET.Maths, StbImageSharp |
| `MOBA.Engine.Networking` | `INetTransport`, channels, `NullTransport` | (no external deps; Riptide arrives later) |
| `MOBA.Game` | Sim: MobaWorld, Map, actors, sim components | Engine.Core, Engine.Networking, Silk.NET.Maths |
| `MOBA.Game.Client` | Render components, camera controllers, mesh/texture factories | Engine.Core, Engine.Graphics, Game, Silk.NET.Input, Silk.NET.Maths |
| `MOBA.Server` | Headless entry point | Engine.Core, Engine.Networking, Game |
| `MOBA.Client` | Window + GL bootstrap + asset loading + game loop | Engine.Core, Engine.Graphics, Engine.Networking, Game, Game.Client, Silk.NET (Windowing/OpenGL/Input/Maths) |

**Hard invariants** (verified by `.csproj` inspection, CI sanity-check planned):

- `MOBA.Game.csproj` does **not** reference `MOBA.Engine.Graphics` and **no** Silk graphics package.
- `MOBA.Server.csproj` does **not** reference `Silk.NET.Windowing`, `Silk.NET.OpenGL`, `Silk.NET.Input`.

See [ADR-005](../14-decision-log/adr-005-project-structure.md) for the rationale.

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

- The OpenGL backend lives under `MOBA.Engine.Graphics/OpenGL/`.
- A Vulkan backend will arrive under `Vulkan/` (or as a sibling assembly if dependencies force it).
- **Vulkan patch (later):** Y-flip in clip space (negative viewport height or `M22 *= -1` in the projection) + Z-range remap [−1, +1] → [0, +1]. World/view matrices stay RH Y-up.
- GLSL shaders live under `assets/shaders/`. The same content is also embedded as string constants in `MOBA.Game.Client/ShaderSources.cs` for the first skeleton; hot-reload file loading is a later step.

See [ADR-003](../14-decision-log/adr-003-graphics-backend-abstraction.md).

---

## Networking transport abstraction

Game code only talks to `INetTransport` + `NetChannel`.

- Concrete library: **Riptide** (`RiptideNetworking.Riptide`), concrete implementation deferred to the netcode phase.
- First skeleton: `NullTransport` (in-process loopback). Send = no-op.
- When the Riptide adapter is built: `NetChannel.Reliable` → Riptide reliable channel, `NetChannel.Unreliable` → Riptide unreliable; `MessageReceived` is adapted from Riptide's `MessageReceived`.

See [ADR-006](../14-decision-log/adr-006-networking-riptide.md).
