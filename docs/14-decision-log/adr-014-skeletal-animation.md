# ADR-014: Skeletal animation via matrix-palette skinning, ported from Madhav ch.12

## Status

Accepted

## Context

ADR-013 brought glTF models into the engine, but read them in bind pose only — the knight stood in T-pose regardless of state. Tripo3D's free-tier auto-rig output (and Meshy/Mixamo equivalents) ships a full skeleton (41 joints for the knight) and at least one canned animation clip per character. We want idle + walk transitions on the character with minimal authoring work: drop a `.glb` in, get a walking knight.

The learning reference is Sanjay Madhav's *Game Programming in C++*, chapter 12 — "Skeletal Animation". It teaches matrix-palette skinning with a textbook-clean architecture: `BoneTransform`, `Skeleton`, `Animation`, `MatrixPalette`, `SkeletalMeshComponent`. The user explicitly asked us to stay as close to the book as the language and rendering API permit.

Forces:

- The renderer abstraction (ADR-003 / ADR-008) must stay backend-agnostic. The skinning paths cannot leak `Silk.NET.OpenGL` into `MOBA.Engine.Graphics`.
- The static vertex pipeline from ADR-012 (`Vertex` = Position + UV + Normal, 32 bytes) is in use across all procedural meshes (cube, sphere, ground). We must not break it.
- The sim/render separation from ADR-004 means animation state cannot live on the sim Actor — the server runs headless and doesn't know what a "walk clip" is. Animation must be a client-side rendering concern, derived from observable sim state.
- The vertex shader needs the bone palette as a uniform array. GL 3.3 caps total uniform components but 96 `mat4` slots (Madhav's `MAX_SKELETON_BONES`) fits comfortably.
- glTF's animation representation is per-channel with channel-specific keyframe times; Madhav's is per-bone with uniform frame intervals. The two formats are reconcilable via load-time resampling.

## Decision

We adopt the **matrix-palette skinning pipeline from Madhav ch.12**, ported into our C# / Silk.NET stack with the smallest possible deviations.

### Data structures (mirror the book 1:1)

- **`BoneTransform`** — `record struct(Quaternion<float> Rotation, Vector3D<float> Translation)`. `ToMatrix()` returns the row-vector composed matrix. `Interpolate(a, b, t)` does slerp on rotation, lerp on translation. Scale is omitted (matches Madhav; Mixamo / Tripo rigs don't bake per-bone scale).
- **`Skeleton`** — list of `Bone(Name, ParentIndex, LocalBindPose)` + parallel list of `GlobalInverseBindPoses`. Unlike Madhav, we read inverse bind poses directly from the glTF skin's `inverseBindMatrices` accessor rather than computing them — the data is already there.
- **`Animation`** — per-bone tracks of `BoneTransform[NumFrames]` at uniform `FrameDuration`. `GetGlobalPoseAtTime(skeleton, time, outPoses)` walks the bone hierarchy and composes the global pose at the sampled time, exactly as in the book.
- **`MatrixPalette`** — `Matrix4X4<float>[96]` (`MaxBones = 96`, same constant as `MAX_SKELETON_BONES`).

### Vertex layout

A new `SkinnedVertex` struct (Position + Normal + 4 packed bone indices + 4 bone weights + UV) lives alongside the existing `Vertex`. Skinned meshes use a 5-attribute VAO (locations 0–4: position vec3, normal vec3, bone indices `uvec4` from `UNSIGNED_BYTE`, bone weights vec4, UV vec2). Static meshes keep the 3-attribute VAO from ADR-012. The split mirrors Madhav's `VertexLayout::PosNormTex` vs `PosNormSkinTex`.

### Backend abstraction

`IGraphicsBackend` gains:

- `CreateSkinnedMesh(ReadOnlySpan<SkinnedVertex>, ReadOnlySpan<uint>) → ISkinnedMesh` (where `ISkinnedMesh : IMesh`).
- `DrawSkinnedMesh(ISkinnedMesh, Material, model, viewProjection, viewPosition, light, MatrixPalette)`.

`IShader` gains `SetUniform(string, ReadOnlySpan<Matrix4X4<float>>)` for the palette upload. OpenGL implementation flattens the spans into floats and uploads via `glUniformMatrix4fv` with the same row-major-without-transpose trick the single-matrix overload uses (see ADR-002).

### Shader

A new `assets/shaders/phong_skinned.{vert,frag}` shader. The vertex stage skins position and normal:

```glsl
mat4 skin = u_palette[a_boneIndices.x] * a_boneWeights.x
          + u_palette[a_boneIndices.y] * a_boneWeights.y
          + u_palette[a_boneIndices.z] * a_boneWeights.z
          + u_palette[a_boneIndices.w] * a_boneWeights.w;
vec4 skinnedPos = skin * vec4(a_position, 1.0);
vec4 worldPos = u_model * skinnedPos;
```

The fragment stage is identical to `phong_textured.frag` — skinning doesn't affect lighting math.

### Loader

`GltfModelLoader` detects skinned primitives (mesh node has a skin + primitive has `JOINTS_0`/`WEIGHTS_0` accessors) and dispatches:

- Static path: existing `Vertex` extraction + `backend.CreateMesh`.
- Skinned path: `SkinnedVertex` extraction + `backend.CreateSkinnedMesh`. Builds a `Skeleton` from the skin's joints + inverse bind matrices. Resamples every `LogicalAnimation` at **30 Hz** (`TargetFps`) into Madhav-style uniform-frame `Animation` objects, using SharpGLTF's `ICurveSampler<T>` for per-channel interpolation (linear / step / cubic — whatever the source specifies). Translation + rotation only; scale and morph weights are ignored.

`Model` is extended with `Skeleton? Skeleton` and `IReadOnlyDictionary<string, Animation> Animations`. Default shader for skinned materials is `"phong_skinned"`; for static it stays `"phong_textured"`.

### Render-time component

A new `MOBA.Game.Client/SkeletalMeshRendererComponent` (client-only) implements a new `ISkinnedRenderable : IRenderable` (which adds `MatrixPalette Palette` to the renderable interface). It mirrors Madhav's `SkeletalMeshComponent` plus the `FollowActor.mMoving` state machine, with one MOBA-specific twist: **the "moving vs idle" decision is derived from observed position deltas**, not from input, because input is owned by the server in our setup.

Each `OnUpdate`:

1. Compares `Owner.Transform.Position` against `_lastPosition`. If the move exceeds a movement threshold → snap to the walk clip; if not → snap back to idle. No crossfade in this iteration.
2. Advances `_animTime` by `dt × PlayRate`, wraps at the clip's `Duration`.
3. Calls `Animation.GetGlobalPoseAtTime` to fill a `globalPoses` span, then composes `palette[i] = skeleton.GlobalInverseBindPoses[i] * globalPoses[i]` exactly as `SkeletalMeshComponent::ComputeMatrixPalette` does in the book.

### Clip naming heuristic

Tripo / Mixamo export clips with uninformative names (`NlaTrack`, `NlaTrack.001`, …). The component picks **idle = longest clip, walk = shortest**: idle loops are consistently many seconds, walk cycles are well under three. This is a knowingly fragile heuristic — it gets the right answer for Tripo's free-tier output today and will be replaced by explicit clip-name → role mapping (a side-car JSON or per-model config) the first time it bites.

### Renderer dispatch

`Renderer.RenderFrame` checks for `ISkinnedRenderable` first, falling back to plain `IRenderable`. Static and skinned meshes coexist in the same actor walk with no behaviour change for non-skinned components.

## Consequences

- **Positive:**
  - One-line client-side switch (`new SkeletalMeshRendererComponent(actor, knight)`) gives any glTF character with a skin + ≥2 clips an idle/walk loop driven by movement state.
  - The architecture maps faithfully to ch.12 of the learning reference, so the book itself is the natural place to look for the next refinement (crossfade, animation graph, IK, …).
  - The Vulkan backend (future) implements `CreateSkinnedMesh` + `DrawSkinnedMesh` + the matrix-array uniform; no game-code change.
  - Tripo / Meshy / Mixamo output drops in unchanged — the loader detects the skin automatically.
- **Negative:**
  - Snap transitions between idle and walk look slightly mechanical. Crossfade is a follow-up (one new state in the component, lerp the palette between two clips for a few frames).
  - The clip-naming heuristic (longest = idle, shortest = walk) is brittle for characters with three or more clips (run, attack, dance, …). The fix is a named-clip mapping, deferred until needed.
  - The `MatrixPalette` array (96 × 64 = 6 KB) gets uploaded every draw call. For one player character this is fine; for hundreds of skinned actors we'd want UBOs / instancing.
  - Translation + rotation only — no per-bone scale animation. Reintroducing scale is a 3-field extension of `BoneTransform` and one more sampler in the loader, the cost is uniform.
- **Wire format:** unchanged. Animation state is purely client-side, derived from the already-replicated position. The position-update message stays X/Y/Z/ForwardX/ForwardZ.
- **Build-time enforcement:** the existing ArchUnitNET dependency rules (`MOBA.Server` doesn't depend on `Engine.Graphics` / `SharpGLTF`) cover the new types automatically — animation lives in `Engine.Graphics`, the skeletal component in `Game.Client`, neither reachable from the server build.

Future iterations on the same `Model` type can carry blend trees, IK targets, ragdoll data, etc. without rewriting the loader or the backend contract.