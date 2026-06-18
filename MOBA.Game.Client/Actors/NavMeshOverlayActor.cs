using MOBA.Engine.Core.World;
using MOBA.Engine.Graphics;
using MOBA.Game.Client.Components;
using MOBA.Game.Models;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Actors;

/// <summary>
/// Debug overlay for the runtime <see cref="NavMesh"/> — modelled as a regular
/// <see cref="Actor"/> with one <see cref="MeshRendererComponent"/>, so the F2
/// toggle is just a flip of <see cref="IsVisible"/> and the existing two-pass
/// renderer draws it without any special hook. The edge mesh is uploaded once
/// at construction; the navmesh shape never changes at runtime, so a static
/// line mesh is sufficient.
/// </summary>
public sealed class NavMeshOverlayActor : Actor
{
    private readonly MeshRendererComponent _renderer;

    public NavMeshOverlayActor(IGraphicsBackend backend, NavMesh navMesh, Material wireframeMaterial)
    {
        var lineMesh = BuildLineMesh(backend, navMesh);
        _renderer = new MeshRendererComponent(this, lineMesh, wireframeMaterial)
        {
            IsVisible = false,
        };
    }

    /// <summary>Passthrough to the single renderer component — F2 flips this.</summary>
    public bool IsVisible
    {
        get => _renderer.IsVisible;
        set => _renderer.IsVisible = value;
    }

    private static IMesh BuildLineMesh(IGraphicsBackend backend, NavMesh navMesh)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        var zeroUv = new Vector2D<float>(0f, 0f);
        var up = Vector3D<float>.UnitY;
        foreach (var (a, b) in navMesh.EnumerateEdges())
        {
            indices.Add((uint)vertices.Count);
            vertices.Add(new Vertex(a, zeroUv, up));
            indices.Add((uint)vertices.Count);
            vertices.Add(new Vertex(b, zeroUv, up));
        }
        return backend.CreateLineMesh(vertices.ToArray(), indices.ToArray());
    }
}
