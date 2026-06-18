using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Graphics;
using MOBA.Game.Client.Components;
using MOBA.Game.Components;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Actors;

/// <summary>
/// Debug overlay for every player's currently-active server-replicated path.
/// Modelled as a regular <see cref="Actor"/> with one <see cref="MeshRendererComponent"/>;
/// while visible, the actor rebuilds the shared line mesh every tick so the
/// remaining-portion-of-the-path shrinks live as the player walks. Per player
/// the rendered polyline goes <c>player.Transform.Position → next waypoint →
/// last waypoint</c>: a perpendicular projection onto every segment picks the
/// closest one, and only the waypoints after that segment are emitted. F3
/// flips <see cref="IsVisible"/>; while invisible the actor does no work at all.
/// </summary>
public sealed class PathOverlayActor : Actor
{
    private const float YOffset = 0.05f;
    private const float ArrivalThresholdSq = 0.25f * 0.25f;

    private readonly IGraphicsBackend _backend;
    private readonly Scene _scene;
    private readonly MeshRendererComponent _renderer;
    private IMesh _currentMesh;

    public PathOverlayActor(IGraphicsBackend backend, Scene scene, Material pathMaterial)
    {
        _backend = backend;
        _scene = scene;
        _currentMesh = backend.CreateLineMesh([], []);
        _renderer = new MeshRendererComponent(this, _currentMesh, pathMaterial)
        {
            IsVisible = false,
        };
    }

    public bool IsVisible
    {
        get => _renderer.IsVisible;
        set => _renderer.IsVisible = value;
    }

    protected override void UpdateActor(GameTime time)
    {
        if (!_renderer.IsVisible)
        {
            return;
        }
        Rebuild();
    }

    private void Rebuild()
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        var zeroUv = new Vector2D<float>(0f, 0f);
        var up = Vector3D<float>.UnitY;

        foreach (var actor in _scene.Actors)
        {
            var path = actor.GetComponent<ReplicatedPathComponent>();
            if (path is null || path.Waypoints.Count < 2)
            {
                continue;
            }

            var ws = path.Waypoints;
            var pos = actor.Transform.Position;

            // Hide the line entirely once the player is essentially at the
            // destination — otherwise an idle player has a 0-length stub at
            // the last waypoint sitting in the F3 overlay.
            if ((ws[^1] - pos).LengthSquared <= ArrivalThresholdSq)
            {
                continue;
            }

            // Pick the segment whose perpendicular projection is closest to the
            // current position — that's the segment the player is currently on.
            // Render: player → next waypoint, then the remaining segments.
            var closestIdx = 0;
            var minDistSq = float.MaxValue;
            for (var i = 0; i < ws.Count - 1; i++)
            {
                var distSq = SquaredDistanceToSegment(pos, ws[i], ws[i + 1]);
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    closestIdx = i;
                }
            }

            var lift = new Vector3D<float>(0f, YOffset, 0f);
            EmitLine(vertices, indices, pos + lift, ws[closestIdx + 1] + lift, zeroUv, up);
            for (var i = closestIdx + 1; i < ws.Count - 1; i++)
            {
                EmitLine(vertices, indices, ws[i] + lift, ws[i + 1] + lift, zeroUv, up);
            }
        }

        var oldMesh = _currentMesh;
        _currentMesh = _backend.CreateLineMesh(vertices.ToArray(), indices.ToArray());
        _renderer.ReplaceSingleMesh(_currentMesh);
        oldMesh.Dispose();
    }

    private static void EmitLine(
        List<Vertex> vertices,
        List<uint> indices,
        Vector3D<float> a,
        Vector3D<float> b,
        Vector2D<float> zeroUv,
        Vector3D<float> up)
    {
        indices.Add((uint)vertices.Count);
        vertices.Add(new Vertex(a, zeroUv, up));
        indices.Add((uint)vertices.Count);
        vertices.Add(new Vertex(b, zeroUv, up));
    }

    private static float SquaredDistanceToSegment(Vector3D<float> p, Vector3D<float> a, Vector3D<float> b)
    {
        var ab = b - a;
        var lenSq = Vector3D.Dot(ab, ab);
        if (lenSq <= 0f)
        {
            return (p - a).LengthSquared;
        }
        var t = Math.Clamp(Vector3D.Dot(p - a, ab) / lenSq, 0f, 1f);
        var closest = a + ab * t;
        return (p - closest).LengthSquared;
    }
}
