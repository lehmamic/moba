using MOBA.Engine.Core;

namespace MOBA.Engine.Graphics;

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
                if (component is not IRenderable renderable)
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
                if (component is not ISkinnedRenderable skinned)
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

        _backend.EndFrame();
    }
}
