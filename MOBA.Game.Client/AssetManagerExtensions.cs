using System.Globalization;
using MOBA.Engine.Core.Assets;
using MOBA.Engine.Graphics.Abstractions;
using MOBA.Game.Client.Meshes;

namespace MOBA.Game.Client;

/// <summary>
/// Registers and reads procedurally generated meshes through the shared
/// <see cref="AssetManager"/>. A single <see cref="AssetCache{TKey,TAsset}"/>
/// (<c>string → IMesh</c>) holds every mesh; the construction parameters are
/// flattened into the key string. Cube needs no parameters, ground encodes
/// width, length and tile size, sphere encodes radius and segment counts;
/// future mesh types extend the same key scheme.
/// </summary>
public static class AssetManagerExtensions
{
    private const string CubeKey = "cube";
    private const string GroundPrefix = "ground/";
    private const string SpherePrefix = "sphere/";

    public static AssetCache<string, IMesh> AddMeshCache(this AssetManager assets, IGraphicsBackend backend) =>
        assets.AddCache<string, IMesh>(key =>
        {
            if (key == CubeKey)
            {
                return CubeMesh.CreateUnitCube(backend);
            }
            if (key.StartsWith(GroundPrefix, StringComparison.Ordinal))
            {
                var parts = key[GroundPrefix.Length..].Split('/');
                if (parts.Length != 3)
                {
                    throw new ArgumentException($"Malformed ground mesh key '{key}'.", nameof(key));
                }
                var width = float.Parse(parts[0], CultureInfo.InvariantCulture);
                var length = float.Parse(parts[1], CultureInfo.InvariantCulture);
                var tile = float.Parse(parts[2], CultureInfo.InvariantCulture);
                return GroundMesh.CreatePlane(backend, width, length, tile);
            }
            if (key.StartsWith(SpherePrefix, StringComparison.Ordinal))
            {
                var parts = key[SpherePrefix.Length..].Split('/');
                if (parts.Length != 3)
                {
                    throw new ArgumentException($"Malformed sphere mesh key '{key}'.", nameof(key));
                }
                var radius = float.Parse(parts[0], CultureInfo.InvariantCulture);
                var lon = int.Parse(parts[1], CultureInfo.InvariantCulture);
                var lat = int.Parse(parts[2], CultureInfo.InvariantCulture);
                return SphereMesh.Create(backend, radius, lon, lat);
            }
            throw new ArgumentException($"Unknown mesh key '{key}'.", nameof(key));
        });

    public static IMesh LoadCubeMesh(this AssetManager assets) =>
        assets.Cache<string, IMesh>().GetOrLoad(CubeKey);

    public static IMesh LoadGroundMesh(this AssetManager assets, float width, float length, float worldUnitsPerTile)
    {
        var culture = CultureInfo.InvariantCulture;
        var key = $"{GroundPrefix}{width.ToString(culture)}/{length.ToString(culture)}/{worldUnitsPerTile.ToString(culture)}";
        return assets.Cache<string, IMesh>().GetOrLoad(key);
    }

    public static IMesh LoadSphereMesh(this AssetManager assets, float radius = 0.5f, int lonSegments = 16, int latSegments = 8)
    {
        var culture = CultureInfo.InvariantCulture;
        var key = $"{SpherePrefix}{radius.ToString(culture)}/{lonSegments.ToString(culture)}/{latSegments.ToString(culture)}";
        return assets.Cache<string, IMesh>().GetOrLoad(key);
    }
}
