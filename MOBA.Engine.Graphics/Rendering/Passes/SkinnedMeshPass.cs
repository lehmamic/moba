using MOBA.Engine.Graphics.Abstractions;

namespace MOBA.Engine.Graphics.Rendering.Passes;

/// <summary>
/// Draws every <see cref="ISkinnedRenderable"/> component's skinned parts,
/// sharing the component's matrix palette across every skinned part of the
/// same character. Mirrors Madhav <i>Game Programming in C++</i> ch.12
/// (<c>mSkeletalMeshComps</c>) loop.
/// </summary>
public sealed class SkinnedMeshPass : IRenderPass
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
                        backend.BeginPass(part.Material.Shader, viewProjection, viewPosition, light);
                        currentShader = part.Material.Shader;
                    }

                    backend.DrawSkinnedMeshInPass(
                        skinnedMesh,
                        part.Material,
                        part.LocalTransform * actor.Transform.World,
                        skinned.Palette);
                }
            }
        }
    }
}
