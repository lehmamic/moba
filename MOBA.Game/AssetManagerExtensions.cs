using System.Text.Json;
using MOBA.Engine.Core;
using MOBA.Utilities;

namespace MOBA.Game;

/// <summary>
/// Registers and reads game-data assets through a shared <see cref="AssetManager"/>.
/// Both server and client use the same registrations; the loader lambdas live here
/// (in MOBA.Game) so the underlying serialisation format stays consistent across
/// both processes. Mirrors the convention used by the graphics asset caches:
/// register a root folder once, look up by filename.
/// </summary>
public static class AssetManagerExtensions
{
    public static AssetCache<string, MapDefinition> AddMapCache(
        this AssetManager assets,
        AbsolutePath mapsRoot) =>
        assets.AddCache<string, MapDefinition>(filename =>
            JsonSerializer.Deserialize<MapDefinition>(File.ReadAllText(mapsRoot / filename))
            ?? throw new InvalidOperationException(
                $"Failed to deserialise map definition '{filename}' from '{mapsRoot}'."));

    public static MapDefinition LoadMap(this AssetManager assets, string filename) =>
        assets.Cache<string, MapDefinition>().GetOrLoad(filename);

    /// <summary>
    /// Registers a navmesh cache keyed by filename (e.g. <c>dimension-rift.navmesh</c>)
    /// under <paramref name="mapsRoot"/>. The on-disk blob is generated out-of-game
    /// by <c>tools/MOBA.Tools.NavMeshGen</c>; both server and client load via this
    /// cache so the AssetManager owns the lifetime of every navmesh just like every
    /// other game asset.
    /// </summary>
    public static AssetCache<string, NavMesh> AddNavMeshCache(
        this AssetManager assets,
        AbsolutePath mapsRoot) =>
        assets.AddCache<string, NavMesh>(filename =>
            NavMesh.Load(mapsRoot / filename));

    public static NavMesh LoadNavMesh(this AssetManager assets, string filename) =>
        assets.Cache<string, NavMesh>().GetOrLoad(filename);
}
