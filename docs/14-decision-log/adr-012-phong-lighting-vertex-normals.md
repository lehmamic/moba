# ADR-012: Classical Phong lighting with one directional light and Vertex-with-Normal layout

## Status

Accepted

## Context

The first renderer iteration used a single `unlit_textured` shader: vertices carried only Position and UV (20-byte layout), and every draw sampled the base-color texture with no lighting. This was deliberately the smallest path to "things on screen". With character models, a wider variety of meshes, and a Brawl-Stars-style art direction coming in, flat unlit shading no longer reads as 3D — there's no cue for surface orientation, no separation between sun-lit and shaded surfaces, no specular hint where the surface is glossy.

Before introducing skeletal meshes from external files (a separate, larger ADR), we need a working lighting baseline that all meshes — procedural primitives (cube, sphere, ground) and any future imported model — render against. The forces:

- The lighting model has to fit `IGraphicsBackend` without breaking the abstraction (ADR-003) and stay portable to the future Vulkan backend.
- The math conventions (RH Y-up, row-vector / row-major, `model * view * projection`) from ADR-001 / ADR-002 must remain intact.
- Per-fragment lighting requires per-vertex normals; that means every mesh must supply them, including procedural ones.
- The learning reference (Madhav, *Game Programming in C++*, see ADR-005-area constraints) teaches classical Phong with `reflect()` and a `pow()` specular term, which is the natural starting point for someone learning the rendering layer alongside the engine.

## Decision

We introduce **classical Phong fragment shading driven by a single scene-wide directional light**, computed in world space.

Three coupled changes:

1. **Vertex layout grows to Position + UV + Normal.** `MOBA.Engine.Graphics.Vertex` becomes an 8-float (32-byte) struct: `Vector3D<float> Position, Vector2D<float> Uv, Vector3D<float> Normal`. The OpenGL VAO binds a third attribute (location 2, vec3, offset 20, stride 32). All procedural mesh generators (CubeMesh, SphereMesh, GroundMesh) compute and emit normals — flat per face for the cube, smooth `normalize(localPosition)` for the sphere, `UnitY` for the ground.

2. **One scene-wide DirectionalLight.** A new `DirectionalLight` record struct in `MOBA.Engine.Graphics` carries `Direction`, `Color`, `AmbientColor`, `SpecularStrength`, `Shininess`. `Direction` is the unit vector *toward* the light source (e.g. roughly +Y for a sun overhead) — the convention that keeps the GLSL clean (no negation). The renderer owns no light state; it is passed in per frame: `Renderer.RenderFrame(Scene, Camera, DirectionalLight)`. `IGraphicsBackend.DrawMesh` is extended to `(IMesh, Material, Matrix4X4<float> model, Matrix4X4<float> viewProjection, Vector3D<float> viewPosition, DirectionalLight light)`. The OpenGL backend sets `u_mvp`, `u_model`, `u_viewPos`, `u_lightDir`, `u_lightColor`, `u_ambientColor`, `u_specularStrength`, `u_shininess` on every draw. Uniforms that the bound shader does not declare resolve to location -1 and are silently ignored, so legacy unlit shaders remain usable.

3. **A `phong_textured` shader pair.** New `assets/shaders/phong_textured.{vert,frag}` performs per-fragment classical Phong:

   ```
   N = normalize(world-space normal)
   L = normalize(u_lightDir)               // toward light
   V = normalize(u_viewPos - worldPos)
   R = reflect(-L, N)
   ambient  = u_ambientColor
   diffuse  = max(N·L, 0) * u_lightColor
   specular = u_specularStrength * pow(max(V·R, 0), u_shininess) * u_lightColor
   frag.rgb = (ambient + diffuse + specular) * texture.rgb
   ```

   The vertex stage uploads world-space position and `mat3(u_model) * a_normal` to the fragment stage. This assumes uniform (or no) scale on the model matrix — non-uniform scale would require the inverse-transpose of the upper-left 3×3, which we defer until a use case demands it.

   The client switches the cube, ground, and marker materials over to this shader. `unlit_textured` stays in the tree as a fallback for future debug overlays and HUD content.

We deliberately reject Blinn-Phong (cheaper half-vector specular) and toon shading for this iteration. Both remain valid future ADRs; classical Phong is the form taught in the learning reference and is the simplest readable baseline.

We deliberately reject multi-light support, shadow mapping, normal mapping, and any form of PBR for this iteration. They are all addressable behind the same `DirectionalLight`-or-successor parameter without breaking the abstraction.

## Consequences

- **Positive:**
  - All scene geometry now has volume cues. The cube reads as three differently-lit faces, the sphere has smooth shading, the ground darkens at grazing angles.
  - The shader API is ready for skinned meshes (ADR pending): only Position needs to be skinned in the vertex shader, the rest of the layout already serves the lighting pipeline.
  - Lighting state lives outside the renderer and the backend — passed per frame — which keeps both stateless w.r.t. scene content. Vulkan can implement the same `DrawMesh` signature unchanged.
  - The `u_lightDir = toward light` convention removes one source of off-by-sign bugs the learning reference's pseudo-code is prone to.
- **Negative:**
  - Every draw uploads eight Phong-related uniforms even on unlit material slots. The cost is a handful of `glUniform*` calls per draw — negligible at this scale. The cached uniform-location map already amortises the lookup.
  - The Vertex struct gained 12 bytes; VBO sizes for procedural meshes grow by 60 %. At our mesh sizes (hundreds of vertices) this is rounding noise.
  - Non-uniform scale in actor transforms will skew lit surfaces (normals shear). We accept this limitation for now; the fix (inverse-transpose mat3) is a one-line shader change when needed.
- **Breaking API change:** `IGraphicsBackend.DrawMesh` and `Renderer.RenderFrame` signatures changed. The only call sites are `MOBA.Client/ClientGame.cs` and the `OpenGLBackend`; both updated in the same change.
- **Future work this unblocks:** skeletal mesh import (new ADR), normal-mapped surfaces (extend Vertex with tangents), toon shading variant (sibling shader, same uniforms), multiple lights / point lights (extend DirectionalLight into a small light list).
