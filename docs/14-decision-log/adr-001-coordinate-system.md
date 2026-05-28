# ADR-001: Right-handed Y-up coordinate system

## Status

Accepted

## Context

We are building a custom game engine. Three common conventions are on the table:

- **Right-handed, Y-up, forward = −Z** (OpenGL default, DotRecast default, most online graphics tutorials).
- **Left-handed, Y-up, forward = +Z** (Unity, classic DirectX).
- **Left-handed, Z-up, forward = +X** (Unreal, Sanjay Madhav's *Game Programming in C++*).

Consequence: pathfinding for a MOBA will be built with DotRecast (a port of Recast/Detour) — Recast lives in the RH Y-up world. Any deviation would impose a permanent conversion wrapper (axis permutation + triangle winding flip) at a critical interface.

Vulkan differs from OpenGL only in clip-space Y and Z range. That difference lives **backend-locally** in the projection matrix and viewport setup — world space, view space, pathfinding, and game logic stay untouched.

System.Numerics and Silk.NET.Maths have RH defaults; explicit LH variants exist in Silk.NET.Maths if ever needed.

## Decision

We use a **right-handed coordinate system with Y-up**:

- +X = right
- +Y = up
- −Z = forward (into the scene)
- +Z = back (toward the default camera)

Front-face winding: **CCW** (OpenGL default; set explicitly in Engine.Graphics.OpenGL.OpenGLBackend).

Madhav's book stays as a learning reference, but at any axis-explicit point (look-at, projection, forward vector, rotation direction) we adapt to RH Y-up. The math concepts are handedness-independent — only those few spots are rewritten.

## Consequences

- **Positive:** DotRecast meshes (navmesh build, path queries) need no conversion layer. Online OpenGL tutorials and shader snippets work 1:1. The System.Numerics/Silk.NET.Maths RH helpers (`CreateLookAt`, `CreatePerspectiveFieldOfView`) are usable directly.
- **Negative:** Book code examples from Madhav (LH) must be adapted at axis-explicit points — extra effort during translation.
- **Vulkan:** when the backend drop-in arrives, the Vulkan backend patches clip-space Y (Y-flip via negative viewport height or `M22 *= -1` in the projection) and Z range ([−1, +1] → [0, +1]). The engine API and world space stay unchanged.
