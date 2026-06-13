using MOBA.Engine.Core;
using MOBA.Engine.Graphics;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// Debug overlay for every player's currently-active server-replicated path.
/// Modelled as a regular <see cref="Actor"/> with one <see cref="MeshRendererComponent"/>;
/// per tick the actor scans the scene for <see cref="ReplicatedPathComponent"/>s,
/// detects changes via the cheap aggregate-version checksum, and rebuilds the
/// shared line mesh when something differs. F3 flips <see cref="IsVisible"/> —
/// the line mesh continues being maintained while invisible (the per-actor cost
/// is just a version sum), so toggling back on shows the current paths
/// immediately.
/// </summary>
public sealed class PathOverlayActor : Actor
{
    private readonly IGraphicsBackend _backend;
    private readonly Scene _scene;
    private readonly MeshRendererComponent _renderer;
    private IMesh _currentMesh;
    private long _lastVersionChecksum = -1;

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
        // Cheap checksum of all currently-known paths — a sum of (actorId * 1009 + version)
        // detects waypoint changes (new path), actor spawns/despawns (set membership),
        // and version bumps (in-place replacement) without diffing waypoint lists.
        long checksum = 0;
        foreach (var actor in _scene.Actors)
        {
            var path = actor.GetComponent<ReplicatedPathComponent>();
            var netId = actor.GetComponent<NetworkIdentityComponent>();
            if (path is null || netId is null || path.Waypoints.Count == 0)
            {
                continue;
            }
            checksum += netId.Id * 1009L + path.Version;
        }

        if (checksum == _lastVersionChecksum)
        {
            return;
        }
        _lastVersionChecksum = checksum;
        Rebuild();
    }

    private void Rebuild()
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        var zeroUv = new Vector2D<float>(0f, 0f);
        var up = Vector3D<float>.UnitY;

        // One open polyline per replicated path. Lifted slightly above ground so the
        // line doesn't z-fight with the terrain or the F2 navmesh wireframe.
        const float yOffset = 0.05f;
        foreach (var actor in _scene.Actors)
        {
            var path = actor.GetComponent<ReplicatedPathComponent>();
            if (path is null || path.Waypoints.Count < 2)
            {
                continue;
            }
            var ws = path.Waypoints;
            for (var i = 0; i < ws.Count - 1; i++)
            {
                var a = ws[i] + new Vector3D<float>(0f, yOffset, 0f);
                var b = ws[i + 1] + new Vector3D<float>(0f, yOffset, 0f);
                indices.Add((uint)vertices.Count);
                vertices.Add(new Vertex(a, zeroUv, up));
                indices.Add((uint)vertices.Count);
                vertices.Add(new Vertex(b, zeroUv, up));
            }
        }

        var oldMesh = _currentMesh;
        _currentMesh = _backend.CreateLineMesh(vertices.ToArray(), indices.ToArray());
        _renderer.ReplaceSingleMesh(_currentMesh);
        oldMesh.Dispose();
    }
}
