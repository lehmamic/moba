using MOBA.Engine.Core;

namespace MOBA.Engine.Graphics;

/// <summary>
/// Per-frame scene renderer. Walks the actors of a <see cref="Scene"/>, picks every
/// component implementing <see cref="IRenderable"/>, and submits draws via the
/// abstract <see cref="IGraphicsBackend"/>. Not an <see cref="IEngineSystem"/>:
/// the renderer's cadence is the window's render callback, which is distinct from
/// the sim/tick cadence the host drives.
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
        foreach (var actor in scene.Actors)
        {
            foreach (var component in actor.Components)
            {
                if (component is IRenderable renderable)
                {
                    _backend.DrawMesh(
                        renderable.Mesh,
                        renderable.Material,
                        actor.Transform.World,
                        viewProjection,
                        viewPosition,
                        light);
                }
            }
        }
        _backend.EndFrame();
    }
}
