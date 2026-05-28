using System.Text.Json;
using MOBA.Engine.Core;

namespace MOBA.Game;

/// <summary>
/// Registers and reads game-data assets through a shared <see cref="AssetManager"/>.
/// Both server and client use the same registrations; the loader lambdas live here
/// (in MOBA.Game) so the underlying serialisation format stays consistent across
/// both processes.
/// </summary>
public static class AssetManagerExtensions
{
    public static AssetCache<string, MapDefinition> AddMapCache(this AssetManager assets) =>
        assets.AddCache<string, MapDefinition>(path =>
            JsonSerializer.Deserialize<MapDefinition>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Failed to deserialise map definition from '{path}'."));

    public static MapDefinition LoadMap(this AssetManager assets, string path) =>
        assets.Cache<string, MapDefinition>().GetOrLoad(path);
}
