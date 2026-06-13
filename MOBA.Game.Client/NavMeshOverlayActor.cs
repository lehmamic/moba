using MOBA.Engine.Core;
using MOBA.Engine.Graphics;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// Debug overlay for the runtime <see cref="NavMesh"/> — modelled as a regular
/// <see cref="Actor"/> with one <see cref="MeshRendererComponent"/>, so the F2
/// toggle is just a flip of the renderer's <see cref="MeshRendererComponent.IsVisible"/>
/// flag and the existing two-pass renderer draws it without any special hook.
/// The edge mesh is uploaded once at construction; the navmesh shape never
/// changes at runtime, so a static line mesh is sufficient.
/// </summary>
public sealed class NavMeshOverlayActor : Actor
{
    public NavMeshOverlayActor(IGraphicsBackend backend, NavMesh navMesh, Material wireframeMaterial)
    {
        var lineMesh = BuildLineMesh(backend, navMesh);
        Renderer = new MeshRendererComponent(this, lineMesh, wireframeMaterial)
        {
            IsVisible = false,
        };
    }

    /// <summary>The component the F2 toggle flips — kept as a typed handle to avoid a GetComponent lookup.</summary>
    public MeshRendererComponent Renderer { get; }

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
