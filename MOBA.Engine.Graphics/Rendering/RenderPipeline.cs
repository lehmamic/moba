namespace MOBA.Engine.Graphics.Rendering;

/// <summary>
/// Ordered list of <see cref="IRenderPass"/>es the <see cref="Renderer"/> runs
/// each frame between <c>BeginFrame</c> and <c>EndFrame</c>. The same passes
/// reusable across deployments (game viewport, editor viewport, future tools)
/// — they only differ in which pipeline composition they instantiate.
/// </summary>
public sealed class RenderPipeline
{
    private readonly IRenderPass[] _passes;

    public RenderPipeline(IReadOnlyList<IRenderPass> passes) =>
        _passes = [.. passes];

    public void Execute(RenderFrameContext context)
    {
        foreach (var pass in _passes)
        {
            pass.Execute(context);
        }
    }
}
