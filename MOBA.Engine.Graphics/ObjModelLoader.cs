using System.Globalization;
using MOBA.Engine.Core;
using MOBA.Utilities;
using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

/// <summary>
/// Loads Wavefront OBJ files into a <see cref="Model"/>. Parallel to
/// <see cref="GltfModelLoader"/>; lets the asset pipeline consume OBJ directly
/// without a Blender -> glTF round-trip. One <c>o</c> (or <c>g</c>) group becomes
/// one <see cref="ModelPart"/>; faces with more than three corners are
/// triangulated as fans. Material directives (<c>mtllib</c> / <c>usemtl</c>) are
/// ignored — callers wrap the resulting parts with a material override (typical
/// for geometry-only assets like extracted terrain).
/// </summary>
internal static class ObjModelLoader
{
    private const string DefaultShaderName = "phong_textured";

    public static Model Load(
        AbsolutePath filePath,
        IGraphicsBackend backend,
        AssetManager assets)
    {
        if (!filePath.FileExists)
        {
            throw new FileNotFoundException($"OBJ file '{filePath}' not found.", filePath);
        }

        var positions = new List<Vector3D<float>>();
        var uvs = new List<Vector2D<float>>();
        var normals = new List<Vector3D<float>>();
        var groups = new List<RawGroup>();
        var currentGroup = new RawGroup("default");
        groups.Add(currentGroup);

        var inv = CultureInfo.InvariantCulture;
        foreach (var line in File.ReadLines(filePath))
        {
            var span = line.AsSpan().Trim();
            if (span.IsEmpty || span[0] == '#')
            {
                continue;
            }
            var firstSpace = span.IndexOf(' ');
            if (firstSpace < 0)
            {
                continue;
            }
            var prefix = span[..firstSpace];
            var rest = span[(firstSpace + 1)..].Trim();
            switch (prefix)
            {
                case "v":
                    positions.Add(ParseVec3(rest, inv));
                    break;
                case "vt":
                    uvs.Add(ParseVec2(rest, inv));
                    break;
                case "vn":
                    normals.Add(ParseVec3(rest, inv));
                    break;
                case "o":
                case "g":
                    if (currentGroup.Faces.Count == 0)
                    {
                        currentGroup.Name = new string(rest);
                    }
                    else
                    {
                        currentGroup = new RawGroup(new string(rest));
                        groups.Add(currentGroup);
                    }
                    break;
                case "f":
                    currentGroup.Faces.Add(line);
                    break;
            }
        }

        var shader = assets.LoadShader(DefaultShaderName);
        var parts = new List<ModelPart>();
        foreach (var group in groups)
        {
            if (group.Faces.Count == 0)
            {
                continue;
            }
            var (verts, indices) = BuildPart(group.Faces, positions, uvs, normals);
            if (verts.Length == 0)
            {
                continue;
            }
            var mesh = backend.CreateMesh(verts, indices);
            parts.Add(new ModelPart(mesh, new Material(shader, null), Matrix4X4<float>.Identity));
        }

        if (parts.Count == 0)
        {
            throw new InvalidDataException($"OBJ file '{filePath}' contains no faces.");
        }

        return new Model(parts);
    }

    private static (Vertex[] Vertices, uint[] Indices) BuildPart(
        List<string> rawFaces,
        List<Vector3D<float>> positions,
        List<Vector2D<float>> uvs,
        List<Vector3D<float>> normals)
    {
        // Deduplicate vertices per unique (positionIdx, uvIdx, normalIdx) tuple —
        // OBJ allows independent indexing into the three pools, GPU meshes need
        // one Vertex per unique combination.
        var dedup = new Dictionary<(int P, int T, int N), uint>();
        var verts = new List<Vertex>();
        var indices = new List<uint>();

        foreach (var raw in rawFaces)
        {
            var tokens = raw.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            // tokens[0] == "f"; the rest are vertex corners.
            var cornerCount = tokens.Length - 1;
            if (cornerCount < 3)
            {
                continue;
            }

            uint Resolve(string tok)
            {
                var slash = tok.Split('/');
                var pi = ResolveIndex(slash[0], positions.Count);
                var ti = slash.Length > 1 && slash[1].Length > 0 ? ResolveIndex(slash[1], uvs.Count) : -1;
                var ni = slash.Length > 2 && slash[2].Length > 0 ? ResolveIndex(slash[2], normals.Count) : -1;
                var key = (pi, ti, ni);
                if (dedup.TryGetValue(key, out var existing))
                {
                    return existing;
                }
                var pos = positions[pi];
                var uv = ti >= 0 ? uvs[ti] : new Vector2D<float>(0f, 0f);
                var nrm = ni >= 0 ? normals[ni] : new Vector3D<float>(0f, 1f, 0f);
                var idx = (uint)verts.Count;
                verts.Add(new Vertex(pos, uv, nrm));
                dedup[key] = idx;
                return idx;
            }

            // Fan triangulation: (v0, vN-1, vN) for N = 2..cornerCount-1.
            var v0 = Resolve(tokens[1]);
            for (var i = 2; i < cornerCount; i++)
            {
                indices.Add(v0);
                indices.Add(Resolve(tokens[i]));
                indices.Add(Resolve(tokens[i + 1]));
            }
        }

        return (verts.ToArray(), indices.ToArray());
    }

    // OBJ indices are 1-based; negative values count back from the current pool
    // size (so -1 = last). The OBJ specification permits both.
    private static int ResolveIndex(string token, int poolSize)
    {
        var raw = int.Parse(token, NumberStyles.Integer, CultureInfo.InvariantCulture);
        return raw > 0 ? raw - 1 : poolSize + raw;
    }

    private static Vector3D<float> ParseVec3(ReadOnlySpan<char> s, IFormatProvider fp)
    {
        var i = 0;
        var x = NextFloat(s, ref i, fp);
        var y = NextFloat(s, ref i, fp);
        var z = NextFloat(s, ref i, fp);
        return new Vector3D<float>(x, y, z);
    }

    private static Vector2D<float> ParseVec2(ReadOnlySpan<char> s, IFormatProvider fp)
    {
        var i = 0;
        var x = NextFloat(s, ref i, fp);
        var y = NextFloat(s, ref i, fp);
        return new Vector2D<float>(x, y);
    }

    private static float NextFloat(ReadOnlySpan<char> s, ref int start, IFormatProvider fp)
    {
        while (start < s.Length && (s[start] == ' ' || s[start] == '\t'))
        {
            start++;
        }
        var end = start;
        while (end < s.Length && s[end] != ' ' && s[end] != '\t')
        {
            end++;
        }
        var value = float.Parse(s[start..end], NumberStyles.Float, fp);
        start = end;
        return value;
    }

    private sealed class RawGroup(string name)
    {
        public string Name { get; set; } = name;

        public List<string> Faces { get; } = [];
    }
}
