# ADR-009: GameHost shared between client and server

## Status

Accepted

## Context

Both entry points (`MOBA.Client/Program.cs` and `MOBA.Server/Program.cs`) had grown an ad-hoc "everything in main" shape: each independently wired up a sim `Game`, a loop driver, lifecycle cleanup, and per-frame concerns. They share more concept than code:

- both own a `MOBA.Engine.Core.Game` (sim Scene + tick),
- both have lifecycle systems that need ordered Initialize / Update / Shutdown,
- both differ only in the loop driver (Silk.NET window callbacks vs. fixed-step sleep loop).

Duplicating the lifecycle propagation invites drift. A new system added on the client (e.g. `AssetManager`) should be addable on the server without re-implementing the same lifecycle plumbing.

## Decision

Introduce two abstractions in `MOBA.Engine.Core`:

- **`IEngineSystem`** — `OnInitialize` / `OnUpdate(GameTime)` / `OnShutdown` plus `IDisposable`. Implementers participate in the host's per-tick lifecycle. Rendering is intentionally *not* part of the contract because per-frame cadence differs.
- **`GameHost`** (abstract class) — owns a `Game` and a `List<IEngineSystem>`. Exposes `Initialize()`, `Update(float)`, and `Shutdown()` that propagate to the systems (registration order for `Initialize`/`Update`, LIFO for `Shutdown`). `Game.Update(dt)` runs after `OnUpdate` on every system. Idempotent: a second `Shutdown` is a no-op.

Subclasses:

- **`ClientGame : GameHost`** in `MOBA.Client` — owns the Silk.NET `IWindow` and the `OpenGLBackend`; `Run()` delegates to `_window.Run()` and the window callbacks route into the base lifecycle.
- **`ServerGame : GameHost`** in `MOBA.Server` — owns the fixed-step loop; `Run()` calls `Initialize()`, then loops `Update(TickInterval)` with a sleep, then `Shutdown()` on Ctrl-C.

`Run()` is intentionally **not** on the base class because the cadence is fundamentally different. The shared part is the lifecycle plumbing, not the loop.

Three concrete systems land in the same commit:

- `AssetManager` in `MOBA.Engine.Graphics` — caches shaders + textures by file path.
- `InputSystem` in `MOBA.Game.Client` — thin facade over `IInputContext`.
- `CameraSwitcher` in `MOBA.Game.Client` — gains `IEngineSystem` so its per-frame update flows through the host instead of being called manually.

`Renderer` in `MOBA.Engine.Graphics` stays a plain class (not `IEngineSystem`) and is invoked from `ClientGame.OnRender`. `MeshRendererComponent` picks up a new `IRenderable` interface so the renderer iterates `Scene.Actors` without knowing about `MOBA.Game.Client` types.

## Consequences

- **Positive:**
  - Both `Program.cs` files shrink to two lines (`using var game = new XGame(); game.Run();`).
  - Adding a new system (e.g. a future `NetworkSystem` on the server) is one `AddSystem(…)` call inside the subclass constructor.
  - Lifecycle ordering is uniform: forward `Initialize`/`Tick`, LIFO `Shutdown`. No duplicated try/finally bookkeeping in the entry points.
  - `Engine.Core.Game` is unchanged; tests and scripted simulations can keep using it without a host.
- **Negative:**
  - One extra abstract class + interface in `MOBA.Engine.Core`. Modest weight for the lifecycle uniformity gained.
- **Non-consequence:** the server still uses one process per match (see [ADR-010](adr-010-one-match-per-process.md)). `GameHost` represents one session, not the process.
