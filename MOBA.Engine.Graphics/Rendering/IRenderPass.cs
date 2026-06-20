namespace MOBA.Engine.Graphics.Rendering;

/// <summary>
/// One step in a <see cref="RenderPipeline"/> — typically a self-contained draw
/// loop over the scene that handles a single rendering concern (a mesh kind,
/// an overlay, a post-process). Passes read the per-frame
/// <see cref="RenderFrameContext"/> and submit draws via the context's backend;
/// they own no per-frame state.
/// </summary>
public interface IRenderPass
{
    void Execute(RenderFrameContext context);
}
