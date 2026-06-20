using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Graphics.Abstractions;

namespace MOBA.Engine.Graphics.Rendering;

/// <summary>
/// Per-frame renderer. Wraps a <see cref="RenderPipeline"/> in the
/// backend's <c>BeginFrame</c> / <c>EndFrame</c> bracket — pipelines compose
/// freely (game viewport vs editor viewport vs future tools), every host
/// reuses the same renderer + backend pair underneath. Modelled on Madhav
/// <i>Game Programming in C++</i> ch.6 / ch.12 for the static + skinned
/// passes; the pipeline-of-passes shape itself is borrowed from
/// Godot / Unity URP / bgfx — the engine targets two apps with different
/// overlay needs, so the monolithic <c>GenerateOutput</c> from the book
/// doesn't fit.
///
/// <para>
/// Not an <see cref="IEngineSystem"/>: the renderer's cadence is the
/// window's render callback, distinct from the sim/tick cadence the host
/// drives.
/// </para>
/// </summary>
public sealed class Renderer
{
    private readonly IGraphicsBackend _backend;
    private readonly RenderPipeline _pipeline;

    public Renderer(IGraphicsBackend backend, RenderPipeline pipeline)
    {
        _backend = backend;
        _pipeline = pipeline;
    }

    public (float R, float G, float B) ClearColor { get; set; } = (0.1f, 0.15f, 0.2f);

    public void RenderFrame(Scene scene, Camera camera, DirectionalLight light)
    {
        _backend.BeginFrame(ClearColor.R, ClearColor.G, ClearColor.B);
        _pipeline.Execute(new RenderFrameContext(_backend, scene, camera, light));
        _backend.EndFrame();
    }
}
