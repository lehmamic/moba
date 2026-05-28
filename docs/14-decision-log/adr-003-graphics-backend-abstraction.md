# ADR-003: Graphics-backend abstraction with OpenGL as first backend

## Status

Accepted

## Context

OpenGL runs on every desktop platform out of the box (even though deprecated on macOS, it still works). It fully covers the skeleton's needs and is cleanly available in C# via Silk.NET. Vulkan is the better long-term performance path (explicit synchronization, multi-queue) and the right future backend for portable mobile/embedded (Android via Vulkan, MoltenVK on macOS).

Hard-coding against Silk.NET.OpenGL would cost us a complete renderer rewrite when we move to Vulkan.

## Decision

We define a backend abstraction in `MOBA.Engine.Graphics`:

- `IGraphicsBackend` — frame lifecycle (`BeginFrame`, `DrawMesh`, `EndFrame`), resource factories (`CreateMesh`, `CreateTexture`, `CreateShader`), `Resize`.
- `IMesh`, `ITexture`, `IShader` — opaque handles. No GL type in the public API.
- `Material` — binds shader + textures + uniforms in a backend-agnostic way.
- `Camera` — RH Y-up, RH projection (Silk.NET.Maths defaults).

Implementation backends live in subfolders of the same assembly (today `OpenGL/`, later `Vulkan/`). Game code references only the interfaces.

## Consequences

- **Positive:** Vulkan is slot-in-able without game-code changes. The OpenGL backend stays small (~150 LoC) and renderer patterns are explicitly documented.
- **Negative:** an interface indirection on every draw call. Negligible with reasonable batching; CA1859 (the "use concrete type for perf" hint) is therefore globally disabled — the abstraction is the whole point.
- **Vulkan drop-in plan:** the Vulkan backend lives under `MOBA.Engine.Graphics/Vulkan/` (or a sibling assembly `MOBA.Engine.Graphics.Vulkan` if dependencies require it). It patches clip-space Y (Y-flip via negative viewport height or `M22 *= -1`) and Z range ([−1, +1] → [0, +1]) internally. World/view matrices remain RH Y-up as in ADR-001.
