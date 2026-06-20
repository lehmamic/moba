using MOBA.Engine.Graphics.Abstractions;
using MOBA.Engine.Graphics.Rendering;
using MOBA.Game.Actors;
using Silk.NET.Maths;

namespace MOBA.Editor.Passes;

/// <summary>
/// Editor-only render pass. Drops a coloured sphere at every
/// <see cref="TeamActor.SpawnAreaCenter"/> so the author can see where
/// minion waves originate. The mesh + material are caller-supplied; the
/// pass just walks the scene each frame, scales the sphere by
/// <see cref="MarkerScale"/> and submits one draw per team.
/// </summary>
public sealed class SpawnPointMarkerPass : IRenderPass
{
    private const float MarkerScale = 2.5f;

    private readonly IMesh _sphere;
    private readonly Material _material;

    public SpawnPointMarkerPass(IMesh sphere, Material material)
    {
        _sphere = sphere;
        _material = material;
    }

    public void Execute(RenderFrameContext context)
    {
        var teams = context.Scene.Actors.OfType<TeamActor>().ToList();
        if (teams.Count == 0)
        {
            return;
        }

        var backend = context.Backend;
        backend.BeginPass(
            _material.Shader,
            context.Camera.ViewProjection,
            context.Camera.Position,
            context.Light);

        var scale = Matrix4X4.CreateScale(MarkerScale);
        foreach (var team in teams)
        {
            var model = scale * Matrix4X4.CreateTranslation(team.SpawnAreaCenter);
            backend.DrawMeshInPass(_sphere, _material, model);
        }
    }
}
