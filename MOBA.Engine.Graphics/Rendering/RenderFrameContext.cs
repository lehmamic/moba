using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Graphics.Abstractions;

namespace MOBA.Engine.Graphics.Rendering;

/// <summary>
/// Per-frame inputs every <see cref="IRenderPass"/> reads: the backend it
/// submits draws against, the scene to walk, the camera that builds the
/// view-projection / view position, and the active directional light passes
/// can upload into their shaders. Constructed once per <see cref="Renderer.RenderFrame"/>
/// and handed to every pass in the pipeline unchanged.
/// </summary>
public sealed record class RenderFrameContext(
    IGraphicsBackend Backend,
    Scene Scene,
    Camera Camera,
    DirectionalLight Light);
