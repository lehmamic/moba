using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using Silk.NET.Maths;

namespace MOBA.Game;

/// <summary>
/// Runtime navmesh — the same precomputed file is loaded by both server (movement
/// validation) and client (movement snap + F2 wireframe overlay). The build is
/// done out-of-game by <c>tools/MOBA.Tools.NavMeshGen</c>; this class is read-only
/// at runtime.
/// </summary>
public sealed class NavMesh
{
    private const int MaxVertsPerPoly = 6;

    // Wider Y search than X/Z so we tolerate the slight gap between the agent's
    // ground-plane click (Y = 0 from MousePicker.PickGround) and the actual
    // navmesh surface which can sit a fraction above or below.
    private static readonly RcVec3f DefaultSearchExtents = new(5f, 20f, 5f);

    private readonly DtNavMesh _navMesh;
    private readonly DtNavMeshQuery _query;
    private readonly IDtQueryFilter _filter;

    private NavMesh(DtNavMesh navMesh)
    {
        _navMesh = navMesh;
        _query = new DtNavMeshQuery(navMesh);
        _filter = new DtQueryDefaultFilter();
    }

    /// <summary>Total polygon count summed across every tile — useful as a determinism check.</summary>
    public int PolyCount
    {
        get
        {
            var n = 0;
            for (var i = 0; i < _navMesh.GetMaxTiles(); i++)
            {
                var tile = _navMesh.GetTile(i);
                if (tile?.data?.header is not null)
                {
                    n += tile.data.header.polyCount;
                }
            }
            return n;
        }
    }

    public static NavMesh Load(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);
        var dt = new DtMeshSetReader().Read(br, MaxVertsPerPoly);
        return new NavMesh(dt);
    }

    /// <summary>
    /// Projects <paramref name="target"/> to the nearest point on the navmesh
    /// within a fixed search box. Returns false if no polygon is reachable inside
    /// the box (e.g. the click landed in deep skybox or on a tower's footprint
    /// that's not near a walkable cell).
    /// </summary>
    public bool TryFindNearestPoint(Vector3D<float> target, out Vector3D<float> snapped)
    {
        var center = new RcVec3f(target.X, target.Y, target.Z);
        var status = _query.FindNearestPoly(center, DefaultSearchExtents, _filter,
            out var nearestRef, out var nearestPt, out _);
        if (status.Failed() || nearestRef == 0)
        {
            snapped = target;
            return false;
        }
        snapped = new Vector3D<float>(nearestPt.X, nearestPt.Y, nearestPt.Z);
        return true;
    }

    /// <summary>
    /// Yields every polygon edge in the mesh as a pair of world-space endpoints.
    /// Edges shared by two polygons are emitted twice (once per polygon) — fine
    /// for the F2 wireframe overlay; dedupe later if it shows up in profiling.
    /// </summary>
    public IEnumerable<(Vector3D<float> A, Vector3D<float> B)> EnumerateEdges()
    {
        for (var t = 0; t < _navMesh.GetMaxTiles(); t++)
        {
            var tile = _navMesh.GetTile(t);
            if (tile?.data?.header is null)
            {
                continue;
            }
            var header = tile.data.header;
            var verts = tile.data.verts;
            for (var p = 0; p < header.polyCount; p++)
            {
                var poly = tile.data.polys[p];
                var vc = poly.vertCount;
                for (var i = 0; i < vc; i++)
                {
                    var a = poly.verts[i] * 3;
                    var b = poly.verts[(i + 1) % vc] * 3;
                    yield return (
                        new Vector3D<float>(verts[a], verts[a + 1], verts[a + 2]),
                        new Vector3D<float>(verts[b], verts[b + 1], verts[b + 2]));
                }
            }
        }
    }
}
