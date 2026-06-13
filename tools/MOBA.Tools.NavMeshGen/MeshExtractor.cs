using System.Numerics;
using SharpGLTF.Schema2;

namespace MOBA.Tools.NavMeshGen;

/// <summary>
/// Pulls a flat triangle list (positions + indices in world space) out of a glTF binary.
/// Mirrors the per-node WorldMatrix bake the runtime <c>GltfModelLoader</c> does, but
/// keeps the result on the CPU so Recast can rasterise it without needing a graphics
/// backend.
/// </summary>
internal static class MeshExtractor
{
    /// <summary>
    /// Substrings (case-insensitive) of node names whose geometry must NOT enter the
    /// navmesh input. The terrain GLB packs grass tufts and other foliage as separate
    /// nodes ("PGD_M_20FoliageGroup_*", "...FoliageGroupR_*"); their small upright
    /// silhouettes get rasterised as ledge spans by Recast and turn into impassable
    /// pillars — but visually they are clearly walk-through. Filtering by node name
    /// is the cheapest way to keep them out of the input without splitting the GLB.
    /// </summary>
    private static readonly string[] SkipNamePatterns = ["Foliage"];

    public sealed record TriangleSoup(Vector3[] Vertices, int[] Indices)
    {
        public int TriangleCount => Indices.Length / 3;
    }

    /// <summary>
    /// Loads every primitive of every node-with-mesh in the file, transforms positions
    /// by the node's world matrix, concatenates into a single (positions, indices)
    /// pair. The glTF Y-up convention is preserved — Recast expects Y-up too.
    /// Nodes whose name matches <see cref="SkipNamePatterns"/> are skipped wholesale.
    /// </summary>
    public static TriangleSoup Load(string glbPath)
    {
        var model = ModelRoot.Load(glbPath);
        var verts = new List<Vector3>();
        var indices = new List<int>();
        var skippedNodes = 0;
        var skippedTris = 0;

        foreach (var node in model.LogicalNodes)
        {
            var mesh = node.Mesh;
            if (mesh is null)
            {
                continue;
            }

            var name = node.Name ?? string.Empty;
            if (IsSkipped(name))
            {
                var skip = 0;
                foreach (var prim in mesh.Primitives)
                {
                    skip += prim.GetIndices().Count / 3;
                }
                skippedNodes++;
                skippedTris += skip;
                continue;
            }

            var world = node.WorldMatrix;
            foreach (var prim in mesh.Primitives)
            {
                var posAcc = prim.GetVertexAccessor("POSITION")
                    ?? throw new InvalidDataException(
                        $"glTF primitive without POSITION in '{glbPath}'.");
                var positions = posAcc.AsVector3Array();
                var src = prim.GetIndices();

                var baseIndex = verts.Count;
                foreach (var p in positions)
                {
                    verts.Add(Vector3.Transform(p, world));
                }
                for (var i = 0; i < src.Count; i++)
                {
                    indices.Add(baseIndex + (int)src[i]);
                }
            }
        }

        if (skippedNodes > 0)
        {
            Console.WriteLine($"[extract] skipped {skippedNodes} nodes / {skippedTris} tris matching {{{string.Join(",", SkipNamePatterns)}}}");
        }

        return new TriangleSoup(verts.ToArray(), indices.ToArray());
    }

    private static bool IsSkipped(string name)
    {
        foreach (var pat in SkipNamePatterns)
        {
            if (name.Contains(pat, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Computes the axis-aligned bounding box of a triangle soup in world space.
    /// </summary>
    public static (Vector3 Min, Vector3 Max) Bounds(TriangleSoup soup)
    {
        if (soup.Vertices.Length == 0)
        {
            return (Vector3.Zero, Vector3.Zero);
        }
        var min = soup.Vertices[0];
        var max = soup.Vertices[0];
        for (var i = 1; i < soup.Vertices.Length; i++)
        {
            min = Vector3.Min(min, soup.Vertices[i]);
            max = Vector3.Max(max, soup.Vertices[i]);
        }
        return (min, max);
    }
}
