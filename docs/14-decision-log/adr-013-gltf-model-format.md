# ADR-013: glTF 2.0 (.glb) as the model format, via SharpGLTF, with a project-owned shader registry

## Status

Accepted

## Context

ADR-012 introduced classical Phong lighting and grew the `Vertex` layout to Position + UV + Normal, so the renderer is ready for arbitrary meshes beyond the procedural cube/sphere/ground. The next force is "render an actual character" — produced externally (AI tools like Tripo3D, Meshy; asset libraries like Kenney, Quaternius; future hand-modelled work in Blender) — without rewriting the renderer for each source.

Forces:

- **Authoring ecosystem.** Every relevant authoring/export path on the desktop in 2026 emits either glTF 2.0 or FBX. FBX is an Autodesk-controlled binary authoring format with no stable C# reader. glTF 2.0 is a Khronos open standard explicitly designed as a *runtime/transmission* format with a small, well-tested C# library (SharpGLTF, MIT).
- **Coverage.** A single glTF binary (`.glb`) packs mesh + skeleton + skin weights + animation channels + materials + embedded textures into one file. We only need mesh + materials for M2; skeleton + animation can join in M3 without changing the file format.
- **The learning reference (Madhav).** The book uses a custom JSON mesh format for teaching. In shipping engines, custom binary formats are a *ship-time optimisation* (Unreal `.uasset`, Unity `.asset`). Adopting a custom format now would be premature, would block every external authoring path, and would not teach us anything we don't get from working with glTF directly.
- **Shader control.** The minute a model file can dictate which shader it draws with — via embedded GLSL, a file-path reference, or arbitrary code — we lose the ability to (a) review every shader the project ships, (b) port to Vulkan without reauthoring shaders, (c) trust that a downloaded asset doesn't ship malicious or broken GPU code. Yet model materials still need *some* way to ask for "this should be lit as toon, not Phong" once the shader library grows.
- **Sim/render separation (ADR-004).** The headless server must never see a `.glb` file. Model loading is client-only.

## Decision

We adopt **glTF 2.0 (`.glb` binary form), read via SharpGLTF.Core 1.0.6, with a project-curated shader registry that asset materials reference by name**.

Concretely:

1. **Format & library.** `.glb` (binary, embedded textures) is the model format for the project. Read via `SharpGLTF.Core` referenced from `MOBA.Engine.Graphics` only (verified at build time by ArchUnitNET: `MOBA.Server` must not reference `SharpGLTF.Core`). Models live under `assets/models/`.

2. **Engine-side API: `Model` + `LoadModel(name)`.** The asset surface the game sees is `MOBA.Engine.Graphics.Model` — one named asset, internally a `IReadOnlyList<ModelPart>` where each `ModelPart` is an `(IMesh, Material)` pair, plus convenience `Mesh`/`Material` accessors for the common single-part case. Loading is keyed by short name through the standard cache pattern: `assets.AddModelCache(backend, modelsRoot, shadersRoot)` registers the cache, `assets.LoadModel("knight-garen")` returns the model. The cache resolves the file path internally (`{modelsRoot}/{name}.glb`). This mirrors the existing `LoadCubeMesh` / `LoadShader` / `LoadTexture` style — game code stays unaware that glTF is the on-disk format.

3. **Loader internals.** `GltfModelLoader` is `internal` to `MOBA.Engine.Graphics` (no client code calls it directly). It reads the `.glb` through `SharpGLTF.Schema2.ModelRoot.Load`, walks each glTF mesh primitive, extracts `POSITION` + `TEXCOORD_0` + `NORMAL` accessors into the engine `Vertex` struct and `GetIndices()` into the index buffer. Missing UV → `(0, 0)`; missing normal → `+Y` (so unnormaled meshes render as flat-lit but at least don't crash). glTF node hierarchies, skeletal joints, skin weights, and animation channels are deliberately ignored for now — they land in later iterations against the same `Model` type (new properties: `Skeleton`, `Animations`, …).

4. **Embedded textures.** `material.FindChannel("BaseColor")?.Texture?.PrimaryImage.Content` is decoded via `TextureLoader.LoadRgba(ReadOnlySpan<byte>)` (new overload) and uploaded through `IGraphicsBackend.CreateTexture`. **Flip-vertically is off for glTF textures** — glTF spec puts UV origin at the upper-left of the source image, the same orientation PNG bytes already have in memory. The procedural-mesh pipeline keeps its flip-on default because those PNGs are loaded against bottom-left UVs.

5. **Shader registry — `StandardShaders`.** `MOBA.Engine.Graphics.StandardShaders` holds the canonical set of shader keys (`unlit_textured`, `phong_textured`; future `skinned_phong`, `toon`, …). Each key resolves to `{shadersRoot}/{key}.vert` + `{key}.frag` under the project's own `assets/shaders/`. The glTF loader reads the model material's `extras.shader` JSON field — if present, the string is looked up in `StandardShaders`; **unknown keys throw at load time** (fail fast — an asset may not request a shader the project does not own). If `extras.shader` is absent, the loader falls back to `phong_textured`. Asset files never carry GLSL source and never reference shader file paths.

6. **Wiring.** The client (`MOBA.Client/ClientGame.cs`) loads the player character via `_assets.LoadModel("knight-garen")` and attaches `knight.Mesh` + `knight.Material` to the local `TestCubeActor` in place of the procedural cube. Ground and marker stay procedural.

7. **Out of scope (separate ADRs / iterations):**
   - Skeletal joints + skin weights + GPU skinning.
   - Animation playback (idle/walk/attack from glTF animation channels).
   - glTF node hierarchy baking (TRS composition, multi-node assets).
   - Full PBR materials (we read base-color only; metallic/roughness/normal maps ignored).
   - Asset compression (`KHR_draco_mesh_compression` etc.).

## Consequences

- **Positive:**
  - Every external authoring tool (Blender, Tripo3D, Meshy Pro, Mixamo via FBX→glb in Blender, Kenney/Quaternius packs, …) lands in our pipeline with no per-tool integration code.
  - The shader set stays project-owned. Adding a toon shader is a `StandardShaders.Toon` key plus the GLSL pair — no asset-file change required to *new* assets; existing assets opt in via `extras.shader`.
  - Unknown shader keys fail at load time, not at draw time — visual surprises are caught earlier.
  - The loader's primitive abstraction maps cleanly to skeletal meshes later: each primitive can grow joint indices + weights when the `Vertex` layout splits or `SkinnedVertex` arrives.
- **Negative:**
  - glTF binaries with high-resolution baked textures (Tripo's free-tier output for one character is ~56 MB) bloat the repo if checked into git. The first knight ships at that size; future work may need Git LFS for `assets/models/**.glb` or a build-time texture-resize step.
  - The loader reads only the first scene's vertex data and ignores node transforms. If an authoring tool exports a character whose mesh is offset by a non-identity node TRS, the import will be misplaced. We'll address this when it bites.
  - SharpGLTF adds ~1 MB of managed code to the client build. Negligible.
- **Build-time enforcement.** ArchUnitNET in `tests/MOBA.Architecture.Tests/DependencyTests.cs` asserts that `MOBA.Server` does not depend on `SharpGLTF.Core`. A new test runs alongside the existing dependency invariants.
- **Future work this unblocks:** the `StandardShaders.Toon` shader and a `material.extras.shader = "toon"` material are the smallest possible next step toward the Brawl-Stars-look without touching anything but `assets/shaders/` and one model's `extras`. Skeletal mesh support (M3) extends the loader to read joints/weights and `LogicalAnimations` without rewriting the format-decision side.