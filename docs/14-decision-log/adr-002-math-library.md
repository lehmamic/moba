# ADR-002: Silk.NET.Maths as math library

## Status

Accepted

## Context

Two pragmatic options for `Vector3`, `Matrix4x4`, `Quaternion`, etc.:

- **System.Numerics** (BCL, SIMD-accelerated, RH-only by default, full interop with DotRecast and everything in the .NET ecosystem).
- **Silk.NET.Maths** (generic over `T`: `Vector3D<float>`/`Vector3D<double>`/...; explicitly provides **both** handedness variants for helpers like `CreateLookAt` / `CreateLookAtLeftHanded`; conversion operators to/from System.Numerics).

Both are row-major / row-vector / `model * view * projection` — identical convention.

## Decision

We use **Silk.NET.Maths** throughout the engine (Engine.Core, Engine.Graphics, Game, Game.Client).

Rationale:
- Genericity over `T` lets us later move sim-critical state to `double` without changing libraries.
- Explicit RH+LH variants of the projection/view helpers make the Vulkan backend patch (different clip space) more readable.
- Seamless with the rest of the Silk.NET ecosystem (Windowing, Input, OpenGL).

DotRecast interop: conversion operators between `System.Numerics.Vector3` and `Silk.NET.Maths.Vector3D<float>` exist — when the pathfinding integration arrives we convert at the boundary.

## Consequences

- **Positive:** one math library for the whole engine, consistent API, RH+LH explicitly available.
- **Negative:** one conversion line per vector at every DotRecast call later. Slightly more verbose than System.Numerics (generic type parameters visible in code).
- **Matrix convention** (critical, pinned in AGENTS.md): row-major storage, row-vector convention, multiplication order `model * view * projection`. GLSL upload uses `transpose=false` (GLSL reads as column-major and therefore sees the transpose, which matches the column-vector shader convention — see `OpenGLShader.SetUniform`).
