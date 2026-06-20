using MOBA.Engine.Graphics.Abstractions;
using Silk.NET.Maths;

namespace MOBA.Engine.Graphics.Rendering.Passes;

/// <summary>
/// Draws every <see cref="IBillboardRenderable"/> component (HP bars today)
/// as a camera-facing quad. Builds the camera right / up basis once from the
/// view matrix and uploads them with the per-pass uniforms; the shader rotates
/// the unit XY quad into the camera plane around the actor's world position.
/// Depth writes are disabled by the backend so bars draw on top of geometry.
/// </summary>
public sealed class BillboardPass : IRenderPass
{
    public void Execute(RenderFrameContext context)
    {
        var backend = context.Backend;
        var camera = context.Camera;
        var viewProjection = camera.ViewProjection;
        var forward = Vector3D.Normalize(camera.Target - camera.Position);
        var cameraRight = Vector3D.Normalize(Vector3D.Cross(forward, camera.Up));
        var cameraUp = Vector3D.Cross(cameraRight, forward);

        IShader? currentShader = null;
        foreach (var actor in context.Scene.Actors)
        {
            foreach (var component in actor.Components)
            {
                if (component is not IBillboardRenderable billboard || !billboard.IsVisible)
                {
                    continue;
                }

                if (billboard.Shader != currentShader)
                {
                    backend.BeginBillboardPass(billboard.Shader, viewProjection, cameraRight, cameraUp);
                    currentShader = billboard.Shader;
                }

                var worldTranslation = ExtractTranslation(actor.Transform.World);
                var model = Matrix4X4.CreateTranslation(worldTranslation + billboard.WorldOffset);
                backend.DrawBillboardInPass(
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
    }

    /// <summary>Row-vector convention: translation lives in the bottom row (M41..M43).</summary>
    private static Vector3D<float> ExtractTranslation(Matrix4X4<float> m) => new(m.M41, m.M42, m.M43);
}
