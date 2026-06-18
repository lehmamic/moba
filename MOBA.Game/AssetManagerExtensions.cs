using System.Text.Json;
using System.Text.Json.Serialization;
using MOBA.Engine.Core;
using MOBA.Utilities;
using MOBA.Game.Scenes;
using MOBA.Game.Models;

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
    private static readonly JsonSerializerOptions SceneJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Registers a <see cref="SceneDefinition"/> cache keyed by filename
    /// (e.g. <c>dimension-rift.json</c>) under <paramref name="scenesRoot"/>.
    /// Same file format the server and the client both load; the cache makes
    /// double-load (Server + Client in the same process, tests, scene-switch)
    /// cheap.
    /// </summary>
    public static AssetCache<string, SceneDefinition> AddSceneCache(
        this AssetManager assets,
        AbsolutePath scenesRoot) =>
        assets.AddCache<string, SceneDefinition>(filename =>
            JsonSerializer.Deserialize<SceneDefinition>(File.ReadAllText(scenesRoot / filename), SceneJsonOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialise scene definition '{filename}' from '{scenesRoot}'."));

    public static SceneDefinition LoadScene(this AssetManager assets, string filename) =>
        assets.Cache<string, SceneDefinition>().GetOrLoad(filename);

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
