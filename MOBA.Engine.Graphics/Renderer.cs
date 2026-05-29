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

        // Pass 1: static meshes. Within the pass we only re-bind the shader
        // when the next renderable actually uses a different one — for the
        // current asset set every static uses phong_textured, so BeginPass
        // fires exactly once.
        IShader? currentShader = null;
        foreach (var actor in scene.Actors)
        {
            foreach (var component in actor.Components)
            {
                if (component is ISkinnedRenderable)
                {
                    continue;
                }
                if (component is not IRenderable renderable)
                {
                    continue;
                }
                if (renderable.Material.Shader != currentShader)
                {
                    _backend.BeginPass(renderable.Material.Shader, viewProjection, viewPosition, light);
                    currentShader = renderable.Material.Shader;
                }
                _backend.DrawMeshInPass(renderable.Mesh, renderable.Material, actor.Transform.World);
            }
        }

        // Pass 2: skinned meshes.
        currentShader = null;
        foreach (var actor in scene.Actors)
        {
            foreach (var component in actor.Components)
            {
                if (component is not ISkinnedRenderable skinned)
                {
                    continue;
                }
                if (skinned.Material.Shader != currentShader)
                {
                    _backend.BeginPass(skinned.Material.Shader, viewProjection, viewPosition, light);
                    currentShader = skinned.Material.Shader;
                }
                _backend.DrawSkinnedMeshInPass(
                    (ISkinnedMesh)skinned.Mesh,
                    skinned.Material,
                    actor.Transform.World,
                    skinned.Palette);
            }
        }

        _backend.EndFrame();
    }
}
