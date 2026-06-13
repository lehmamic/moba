using System.Numerics;
using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using DotRecast.Recast;
using DotRecast.Recast.Geom;

namespace MOBA.Tools.NavMeshGen;

/// <summary>
/// Drives Recast over the extracted terrain triangles + tower obstacle list and
/// serialises the resulting Detour mesh into a self-contained <c>.navmesh</c> file.
/// The structure follows the recast4j / DotRecast "solo mesh" sample.
/// </summary>
internal static class NavMeshBuild
{
    /// <summary>One tower / nexus obstacle as a convex polygon clipped against the navmesh.</summary>
    public sealed record Obstacle(Vector3[] Polygon, float YMin, float YMax);

    // Agent / map sizing tuned for the dimension-rift terrain (150×150 playable,
    // player visual ~6 units tall after the *3 scale at NetworkSyncSystem:137).
    private const float CellSize = 0.3f;
    private const float CellHeight = 0.2f;
    private const float AgentMaxSlopeDegrees = 45f;
    private const float AgentHeight = 6.0f;
    private const float AgentRadius = 0.6f;
    private const float AgentMaxClimb = 0.4f;
    private const int RegionMinSize = 8;
    private const int RegionMergeSize = 20;
    private const float EdgeMaxLen = 12f;
    private const float EdgeMaxError = 1.3f;
    private const int VertsPerPoly = 6;
    private const float DetailSampleDist = 6f;
    private const float DetailSampleMaxError = 1f;

    // Area / flag constants — replicate the toolset's "ground / walk" convention
    // since the Toolset assembly is not on NuGet.
    private const int AreaGround = 1;
    private const int PolyFlagsWalk = 1;
    private const int AreaNullBlocked = 0;
    private static readonly RcAreaModification WalkableMod = new(AreaGround);

    public static (byte[] FileBytes, BuildStats Stats) Build(
        MeshExtractor.TriangleSoup terrain,
        IReadOnlyList<Obstacle> obstacles)
    {
        // 1. Geom provider: flat float[] of XYZ, flat int[] of vertex indices per triangle
        var verts = new float[terrain.Vertices.Length * 3];
        for (var i = 0; i < terrain.Vertices.Length; i++)
        {
            verts[i * 3 + 0] = terrain.Vertices[i].X;
            verts[i * 3 + 1] = terrain.Vertices[i].Y;
            verts[i * 3 + 2] = terrain.Vertices[i].Z;
        }
        var geom = new RcSampleInputGeomProvider(verts, terrain.Indices);

        // Towers + nexus become null-area convex polys so Recast carves holes there.
        foreach (var ob in obstacles)
        {
            var flat = new float[ob.Polygon.Length * 3];
            for (var i = 0; i < ob.Polygon.Length; i++)
            {
                flat[i * 3 + 0] = ob.Polygon[i].X;
                flat[i * 3 + 1] = ob.Polygon[i].Y;
                flat[i * 3 + 2] = ob.Polygon[i].Z;
            }
            geom.AddConvexVolume(flat, ob.YMin, ob.YMax, new RcAreaModification(AreaNullBlocked));
        }

        // 2. RcConfig + RcBuilderConfig
        var cfg = new RcConfig(
            RcPartition.WATERSHED,
            CellSize, CellHeight,
            AgentMaxSlopeDegrees, AgentHeight, AgentRadius, AgentMaxClimb,
            RegionMinSize, RegionMergeSize,
            EdgeMaxLen, EdgeMaxError,
            VertsPerPoly,
            DetailSampleDist, DetailSampleMaxError,
            filterLowHangingObstacles: true,
            filterLedgeSpans: true,
            filterWalkableLowHeightSpans: true,
            WalkableMod,
            buildMeshDetail: true);

        var bcfg = new RcBuilderConfig(cfg, geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax());

        // 3. Pipeline (rasterise → filter → compact → regions → contours → polymesh → detail).
        var builder = new RcBuilder();
        var rcResult = builder.Build(geom, bcfg, keepInterResults: false);

        var pmesh = rcResult.Mesh;
        var dmesh = rcResult.MeshDetail;
        if (pmesh is null || pmesh.npolys == 0)
        {
            throw new InvalidOperationException(
                "Recast produced an empty polymesh — terrain bounds or agent params likely wrong.");
        }

        // 4. Mark every polygon as walkable (flag = WALK).
        for (var i = 0; i < pmesh.npolys; i++)
        {
            pmesh.flags[i] = PolyFlagsWalk;
        }

        // 5. Build Detour mesh data (mirrors DemoNavMeshBuilder.GetNavMeshCreateParams).
        var option = new DtNavMeshCreateParams
        {
            verts = pmesh.verts,
            vertCount = pmesh.nverts,
            polys = pmesh.polys,
            polyAreas = pmesh.areas,
            polyFlags = pmesh.flags,
            polyCount = pmesh.npolys,
            nvp = pmesh.nvp,
            detailMeshes = dmesh?.meshes,
            detailVerts = dmesh?.verts,
            detailVertsCount = dmesh?.nverts ?? 0,
            detailTris = dmesh?.tris,
            detailTriCount = dmesh?.ntris ?? 0,
            walkableHeight = AgentHeight,
            walkableRadius = AgentRadius,
            walkableClimb = AgentMaxClimb,
            bmin = pmesh.bmin,
            bmax = pmesh.bmax,
            cs = CellSize,
            ch = CellHeight,
            buildBvTree = true,
        };
        var meshData = DtNavMeshBuilder.CreateNavMeshData(option)
            ?? throw new InvalidOperationException("DtNavMeshBuilder.CreateNavMeshData returned null.");

        // 6. Initialise a DtNavMesh (single-tile) and serialise it with DtMeshSetWriter
        //    so the runtime loader can deserialise the same way.
        var navMesh = new DtNavMesh();
        var status = navMesh.Init(meshData, VertsPerPoly, 0);
        if (status.Failed())
        {
            throw new InvalidOperationException($"DtNavMesh.Init failed: {status.Value}");
        }

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        new DtMeshSetWriter().Write(bw, navMesh, RcByteOrder.LITTLE_ENDIAN, cCompatibility: false);

        var stats = new BuildStats(
            TerrainTriangles: terrain.TriangleCount,
            Obstacles: obstacles.Count,
            PolyCount: pmesh.npolys,
            VertexCount: pmesh.nverts,
            BoundsMin: new Vector3(pmesh.bmin.X, pmesh.bmin.Y, pmesh.bmin.Z),
            BoundsMax: new Vector3(pmesh.bmax.X, pmesh.bmax.Y, pmesh.bmax.Z));
        return (ms.ToArray(), stats);
    }

    public sealed record BuildStats(
        int TerrainTriangles,
        int Obstacles,
        int PolyCount,
        int VertexCount,
        Vector3 BoundsMin,
        Vector3 BoundsMax);
}
