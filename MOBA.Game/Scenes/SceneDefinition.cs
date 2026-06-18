namespace MOBA.Game.Scenes;

/// <summary>
/// JSON-serialisable description of a complete scene — server and client load
/// the same file via <see cref="MOBA.Engine.Core.AssetCache{TKey,TAsset}"/>.
/// A scene is the unit the <c>SceneManager</c> consumes; everything that lives
/// in the scene at start-of-day is described here. Network-spawned actors
/// (players, markers) are NOT in the scene file — they arrive at runtime via
/// the transport.
///
/// <para>
/// Only <see cref="Name"/>, <see cref="Type"/>, and <see cref="Actors"/> are
/// required. <see cref="Environment"/> and <see cref="Cameras"/> apply to
/// <see cref="SceneType.Game"/> scenes; <see cref="SceneType.Loading"/> /
/// <see cref="SceneType.Menu"/> scenes typically leave them null. The map
/// is just another actor — a <c>{ "Type": "Map" }</c> entry in
/// <see cref="Actors"/> — no special top-level block.
/// </para>
/// </summary>
public sealed record SceneDefinition(
    string Name,
    SceneType Type,
    IReadOnlyList<ActorEntryDefinition> Actors,
    EnvironmentDefinition? Environment = null,
    CamerasDefinition? Cameras = null);
