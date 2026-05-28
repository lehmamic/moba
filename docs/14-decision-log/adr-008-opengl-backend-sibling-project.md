# ADR-008: OpenGL backend lives in a sibling project

## Status

Accepted

## Context

[ADR-003](adr-003-graphics-backend-abstraction.md) introduced the `IGraphicsBackend` abstraction with the OpenGL backend in a subfolder of `MOBA.Engine.Graphics`. The ADR explicitly left the door open to move the backend to a sibling assembly "if dependencies require it".

That moment has come. Keeping the backend in the same assembly means `MOBA.Engine.Graphics` carries a `Silk.NET.OpenGL` package reference and an `AllowUnsafeBlocks` property — both are concerns of the **backend**, not of the abstraction. A future `MOBA.Engine.Graphics.Vulkan` would create even more cross-pollution if it landed in the same assembly. And `tests/MOBA.Architecture.Tests/` cannot enforce "the abstraction does not see its backend" at type level when the two share an assembly.

## Decision

Move the OpenGL backend to a new sibling project **`MOBA.Engine.Graphics.OpenGL`**:

- The four backend files (`OpenGLBackend`, `OpenGLMesh`, `OpenGLTexture`, `OpenGLShader`) move from `MOBA.Engine.Graphics/OpenGL/` to `MOBA.Engine.Graphics.OpenGL/`. Namespace stays `MOBA.Engine.Graphics.OpenGL` — no callsite changes.
- The new project references `MOBA.Engine.Graphics` (for the abstractions) and takes the `Silk.NET.OpenGL` + `Silk.NET.Maths` package references, plus `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`.
- `MOBA.Engine.Graphics.csproj` loses its `Silk.NET.OpenGL` reference and `AllowUnsafeBlocks` property.
- `MOBA.Client.csproj` gains a `ProjectReference` to the new project (it instantiates `OpenGLBackend`).
- New `tests/MOBA.Architecture.Tests/DependencyTests.cs` invariants:
  - `Engine.Graphics` must not depend on `Engine.Graphics.OpenGL`.
  - `Engine.Graphics` must not depend on `Silk.NET.OpenGL`.
  - `Game.Client`, `Game`, `Server`, `Engine.Networking` must not depend on `Engine.Graphics.OpenGL` either.

## Consequences

- **Positive:**
  - The abstraction's package boundary is also a compile-time wall. `MOBA.Engine.Graphics` cannot accidentally use a GL type.
  - A future `MOBA.Engine.Graphics.Vulkan` sibling slots in without disturbing the abstraction or the OpenGL backend.
  - `MOBA.Server`, `MOBA.Game`, and `MOBA.Game.Client` continue to reference only `MOBA.Engine.Graphics` (they never needed the backend). The headless server build still pulls no Silk graphics packages.
- **Negative:**
  - Eighth production project. Build time grows by milliseconds; navigation gains one folder. Acceptable.
- **Naming:** the namespace stays `MOBA.Engine.Graphics.OpenGL`, so `using MOBA.Engine.Graphics.OpenGL;` works unchanged.
- **Not superseding ADR-003:** ADR-003 already allowed this; this ADR makes the choice concrete.
