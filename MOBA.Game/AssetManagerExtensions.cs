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
}
