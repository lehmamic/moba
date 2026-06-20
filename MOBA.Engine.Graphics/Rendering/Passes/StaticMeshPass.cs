using MOBA.Engine.Graphics.Abstractions;

namespace MOBA.Engine.Graphics.Rendering.Passes;

/// <summary>
/// Draws every <see cref="IRenderable"/> component's non-skinned parts. A
/// skinned renderable can still contribute static parts here (a weapon not
/// bound to bones); the per-part <see cref="ISkinnedMesh"/> check defers
/// skinned parts to <see cref="SkinnedMeshPass"/>. Shader rebinds only when
/// the next part actually uses a different one, so for the current asset
/// set (everything Phong-textured) <c>BeginPass</c> typically fires exactly
/// once.
/// </summary>
public sealed class StaticMeshPass : IRenderPass
{
    public void Execute(RenderFrameContext context)
    {
        var backend = context.Backend;
        var viewProjection = context.Camera.ViewProjection;
        var viewPosition = context.Camera.Position;
        var light = context.Light;

        IShader? currentShader = null;
        foreach (var actor in context.Scene.Actors)
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
                        backend.BeginPass(part.Material.Shader, viewProjection, viewPosition, light);
                        currentShader = part.Material.Shader;
                    }

                    backend.DrawMeshInPass(part.Mesh, part.Material, part.LocalTransform * actor.Transform.World);
                }
            }
        }
    }
}
