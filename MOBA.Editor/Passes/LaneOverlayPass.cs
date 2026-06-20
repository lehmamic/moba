using MOBA.Engine.Graphics.Abstractions;
using MOBA.Engine.Graphics.Rendering;
using MOBA.Game.Actors;
using Silk.NET.Maths;

namespace MOBA.Editor.Passes;

/// <summary>
/// Editor-only render pass. Reads <see cref="TeamActor.Lanes"/> off every
/// <see cref="TeamActor"/> in the scene and draws each lane as a line strip
/// lifted slightly above the ground. Builds the line mesh lazily on the
/// first <see cref="Execute"/> call and caches it — lanes are static JSON
/// data today; once the editor edits them in-place we'll add a dirty bit
/// here and rebuild on change.
/// </summary>
public sealed class LaneOverlayPass : IRenderPass
{
    private const float LiftAboveGround = 0.1f;

    // The path shader's u_color uniform persists in the GL program across
    // BeginPass calls — set it explicitly so we don't inherit whatever the
    // last pass (grid) wrote.
    private static readonly Vector3D<float> LaneColor = new(0.2f, 1.0f, 0.9f);

    private readonly Material _material;
    private IMesh? _mesh;

    public LaneOverlayPass(IShader shader) =>
        _material = new Material(shader);

    public void Execute(RenderFrameContext context)
    {
        _mesh ??= TryBuild(context.Backend, context.Scene);
        if (_mesh is null)
        {
            return;
        }
        var backend = context.Backend;
        backend.BeginPass(
            _material.Shader,
            context.Camera.ViewProjection,
            context.Camera.Position,
            context.Light);
        _material.Shader.SetUniform("u_color", LaneColor);
        backend.DrawMeshInPass(_mesh, _material, Matrix4X4<float>.Identity);
    }

    private static IMesh? TryBuild(IGraphicsBackend backend, MOBA.Engine.Core.Abstractions.Scene scene)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        foreach (var team in scene.Actors.OfType<TeamActor>())
        {
            foreach (var lane in team.Lanes)
            {
                for (var i = 0; i < lane.Waypoints.Count - 1; i++)
                {
                    var a = lane.Waypoints[i];
                    var b = lane.Waypoints[i + 1];
                    indices.Add((uint)vertices.Count);
                    vertices.Add(new Vertex(new Vector3D<float>(a.X, LiftAboveGround, a.Z), default, default));
                    indices.Add((uint)vertices.Count);
                    vertices.Add(new Vertex(new Vector3D<float>(b.X, LiftAboveGround, b.Z), default, default));
                }
            }
        }
        if (vertices.Count == 0)
        {
            return null;
        }
        return backend.CreateLineMesh([.. vertices], [.. indices]);
    }
}
