using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Graphics.Abstractions;
using Silk.NET.Maths;

namespace MOBA.Engine.Graphics.Rendering;

/// <summary>
/// Per-frame scene renderer. Walks the actors of a <see cref="Scene"/> in two
/// passes — static meshes first, skinned meshes second — and submits draws via
/// the abstract <see cref="IGraphicsBackend"/>. Inside each pass the shader is
/// bound once and the frame-level uniforms (viewProjection, view position,
/// directional light) are uploaded once; per-draw work shrinks to model
/// matrix + texture + (for skinned) bone palette. Modelled on Madhav
/// <i>Game Programming in C++</i> ch.6 / ch.12 (`mMeshComps` vs
/// `mSkeletalMeshComps` loops).
///
/// <para>
/// Not an <see cref="IEngineSystem"/>: the renderer's cadence is the window's
/// render callback, distinct from the sim/tick cadence the host drives.
/// </para>
/// </summary>
public sealed class Renderer
{
    private readonly IGraphicsBackend _backend;

    public Renderer(IGraphicsBackend backend) => _backend = backend;

    public (float R, float G, float B) ClearColor { get; set; } = (0.1f, 0.15f, 0.2f);

    public void RenderFrame(Scene scene, Camera camera, DirectionalLight light)
    {
        _backend.BeginFrame(ClearColor.R, ClearColor.G, ClearColor.B);
        var viewProjection = camera.ViewProjection;
        var viewPosition = camera.Position;

        // Pass 1: every static part of every renderable. A skinned-renderable
        // can still contribute static parts here (e.g. a weapon not bound to
        // bones) — the per-part `ISkinnedMesh` check defers skinned parts to
        // pass 2. Shader rebinds only when the next part actually uses a
        // different one; for the current asset set every static uses
        // phong_textured, so BeginPass typically fires exactly once.
        IShader? currentShader = null;
        foreach (var actor in scene.Actors)
        {
            foreach (var component in actor.Components)
            {
                if (component is not IRenderable renderable || !renderable.IsVisible)
                {
                    continue;
                }
                foreach (var part in renderable.Parts)
                {
                    if (part.Mesh is ISkinnedMesh)
                    {
                        continue;
                    }
                    if (part.Material.Shader != currentShader)
                    {
                        _backend.BeginPass(part.Material.Shader, viewProjection, viewPosition, light);
                        currentShader = part.Material.Shader;
                    }
                    _backend.DrawMeshInPass(part.Mesh, part.Material, part.LocalTransform * actor.Transform.World);
                }
            }
        }

        // Pass 2: skinned parts of skinned renderables, sharing the component's
        // matrix palette across every skinned part of the same character.
        currentShader = null;
        foreach (var actor in scene.Actors)
        {
            foreach (var component in actor.Components)
            {
                if (component is not ISkinnedRenderable skinned || !skinned.IsVisible)
                {
                    continue;
                }
                foreach (var part in skinned.Parts)
                {
                    if (part.Mesh is not ISkinnedMesh skinnedMesh)
                    {
                        continue;
                    }
                    if (part.Material.Shader != currentShader)
                    {
                        _backend.BeginPass(part.Material.Shader, viewProjection, viewPosition, light);
                        currentShader = part.Material.Shader;
                    }
                    _backend.DrawSkinnedMeshInPass(
                        skinnedMesh,
                        part.Material,
                        part.LocalTransform * actor.Transform.World,
                        skinned.Palette);
                }
            }
        }

        // Pass 3: billboards (camera-facing quads — HP bars today). The shader
        // builds the camera-aligned quad from a unit XY mesh + the camera right
        // / up uploaded once for the pass. Depth writes are disabled by the
        // backend so the bars draw on top of the world.
        var forward = Vector3D.Normalize(camera.Target - camera.Position);
        var cameraRight = Vector3D.Normalize(Vector3D.Cross(forward, camera.Up));
        var cameraUp = Vector3D.Cross(cameraRight, forward);
        currentShader = null;
        foreach (var actor in scene.Actors)
        {
            foreach (var component in actor.Components)
            {
                if (component is not IBillboardRenderable billboard || !billboard.IsVisible)
                {
                    continue;
                }
                if (billboard.Shader != currentShader)
                {
                    _backend.BeginBillboardPass(billboard.Shader, viewProjection, cameraRight, cameraUp);
                    currentShader = billboard.Shader;
                }
                var worldTranslation = ExtractTranslation(actor.Transform.World);
                var model = Matrix4X4.CreateTranslation(worldTranslation + billboard.WorldOffset);
                _backend.DrawBillboardInPass(
                    billboard.Mesh,
                    model,
                    billboard.SizeWorldUnits,
                    billboard.FillRatio,
                    billboard.FillColor,
                    billboard.BackgroundColor,
                    billboard.OutlineColor,
                    billboard.OutlineWidthFraction);
            }
        }

        _backend.EndFrame();
    }

    /// <summary>
    /// Row-vector convention: translation lives in the bottom row (M41..M43).
    /// </summary>
    private static Vector3D<float> ExtractTranslation(Matrix4X4<float> m) => new(m.M41, m.M42, m.M43);
}
