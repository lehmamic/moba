using System.Numerics;
using System.Text.Json;
using MOBA.Game;

namespace MOBA.Tools.NavMeshGen;

/// <summary>
/// Out-of-game NavMesh generator. Reads the terrain GLB + tower obstacle list referenced
/// by a map JSON, runs the Recast solo-mesh pipeline, and writes a binary <c>.navmesh</c>
/// file that the runtime NavMesh loader in MOBA.Game consumes on both server and client.
/// </summary>
internal static class Program
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private static int Main(string[] args)
    {
        var options = CliOptions.Parse(args);
        if (options is null)
        {
            return 1;
        }

        Console.WriteLine($"[NavMeshGen] map     = {options.MapJson}");
        Console.WriteLine($"[NavMeshGen] assets  = {options.AssetsRoot}");
        Console.WriteLine($"[NavMeshGen] output  = {options.OutputPath}");

        var map = JsonSerializer.Deserialize<MapDefinition>(
            File.ReadAllText(options.MapJson),
            JsonOpts) ?? throw new InvalidDataException($"Could not parse map JSON: {options.MapJson}");

        var terrainPath = Path.Combine(options.AssetsRoot, "maps", $"{map.TerrainMesh}.glb");
        Console.WriteLine($"[NavMeshGen] terrain = {terrainPath}");

        var terrain = MeshExtractor.Load(terrainPath);
        Console.WriteLine($"  triangles   = {terrain.TriangleCount}");

        var (tmin, tmax) = MeshExtractor.Bounds(terrain);
        Console.WriteLine($"  bounds      = ({tmin.X:F1},{tmin.Y:F1},{tmin.Z:F1}) → ({tmax.X:F1},{tmax.Y:F1},{tmax.Z:F1})");

        // Each Building (Tower / Nexus) becomes an obstacle: read the prefab GLB once
        // per type, compute its XZ footprint as an octagon, place at instance position.
        var obstacles = BuildObstacles(map, options.AssetsRoot);
        Console.WriteLine($"  obstacles   = {obstacles.Count}");

        // Build + serialise.
        var (bytes, stats) = NavMeshBuild.Build(terrain, obstacles);
        Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);
        File.WriteAllBytes(options.OutputPath, bytes);

        Console.WriteLine($"[NavMeshGen] wrote {options.OutputPath} ({bytes.Length / 1024.0:F1} KB)");
        Console.WriteLine($"  polys       = {stats.PolyCount}");
        Console.WriteLine($"  poly-verts  = {stats.VertexCount}");
        Console.WriteLine($"  navmesh-box = ({stats.BoundsMin.X:F1},{stats.BoundsMin.Y:F1},{stats.BoundsMin.Z:F1}) → ({stats.BoundsMax.X:F1},{stats.BoundsMax.Y:F1},{stats.BoundsMax.Z:F1})");

        return 0;
    }

    private static List<NavMeshBuild.Obstacle> BuildObstacles(MapDefinition map, string assetsRoot)
    {
        // Cache prefab XZ footprints by File path so we only load each prefab GLB once.
        var footprintCache = new Dictionary<string, float>();
        var obstacles = new List<NavMeshBuild.Obstacle>();
        foreach (var building in map.Buildings ?? [])
        {
            if (!footprintCache.TryGetValue(building.File, out var radius))
            {
                var prefabPath = ResolveAssetPath(building.File, assetsRoot);
                var soup = MeshExtractor.Load(prefabPath);
                var (mn, mx) = MeshExtractor.Bounds(soup);
                // Prefabs are centered around origin (from the extraction pass) so the
                // half-XZ-diagonal is a reasonable footprint radius for a cylinder
                // approximation.
                var halfX = MathF.Max(MathF.Abs(mn.X), MathF.Abs(mx.X));
                var halfZ = MathF.Max(MathF.Abs(mn.Z), MathF.Abs(mx.Z));
                radius = MathF.Sqrt(halfX * halfX + halfZ * halfZ);
                footprintCache[building.File] = radius;
            }

            // Place an octagon polygon at the building's world position. Y range is
            // generous enough to cover terrain undulations plus the building's vertical
            // extent — Recast clips per voxel anyway.
            var px = building.Transform.Position[0];
            var pz = building.Transform.Position[2];
            const int sides = 8;
            var poly = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var theta = i * (MathF.PI * 2f / sides);
                poly[i] = new Vector3(px + radius * MathF.Cos(theta), 0f, pz + radius * MathF.Sin(theta));
            }
            obstacles.Add(new NavMeshBuild.Obstacle(poly, YMin: -10f, YMax: 30f));
        }
        return obstacles;
    }

    private static string ResolveAssetPath(string repoRelative, string assetsRoot)
    {
        // JSON stores paths with the "assets/" prefix; we treat the prefix as
        // "<assetsRoot>/" so the same JSON works whether the tool is invoked from
        // the repo root or with an explicit --assets path.
        var s = repoRelative.Replace('\\', '/');
        const string prefix = "assets/";
        if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            s = s[prefix.Length..];
        }
        return Path.Combine(assetsRoot, s);
    }
}

internal sealed record CliOptions(string MapJson, string AssetsRoot, string OutputPath)
{
    public static CliOptions? Parse(string[] args)
    {
        string? map = null, assets = null, output = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--map" when i + 1 < args.Length: map = args[++i]; break;
                case "--assets" when i + 1 < args.Length: assets = args[++i]; break;
                case "--output" when i + 1 < args.Length: output = args[++i]; break;
                case "--help" or "-h":
                    PrintHelp();
                    return null;
                default:
                    Console.Error.WriteLine($"[NavMeshGen] unknown argument: {args[i]}");
                    PrintHelp();
                    return null;
            }
        }
        if (map is null || assets is null || output is null)
        {
            Console.Error.WriteLine("[NavMeshGen] missing required argument.");
            PrintHelp();
            return null;
        }
        return new CliOptions(map, assets, output);
    }

    private static void PrintHelp()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  dotnet run --project tools/MOBA.Tools.NavMeshGen -- \\");
        Console.Error.WriteLine("    --map assets/maps/dimension-rift.json \\");
        Console.Error.WriteLine("    --assets assets/ \\");
        Console.Error.WriteLine("    --output assets/maps/dimension-rift.navmesh");
    }
}
