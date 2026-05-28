namespace MOBA.Game;

/// <summary>
/// JSON-serialisable description of a map. Loaded from <c>assets/maps/*.json</c>
/// by both server and client via <see cref="MOBA.Engine.Core.AssetCache{TKey,TAsset}"/>;
/// passed through <see cref="Map.FromDefinition"/> to construct the runtime
/// <see cref="Map"/> instance.
/// </summary>
public sealed record MapDefinition(float Width, float Length);
