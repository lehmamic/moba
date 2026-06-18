namespace MOBA.Game.Scenes;

/// <summary>
/// JSON-serialisable description of a map: dimensions, the terrain mesh
/// reference and the precomputed navmesh file name. Loaded from
/// <c>assets/scenes/*.json</c> as the <c>Properties</c> payload of a
/// <c>{ "Type": "Map" }</c> actor entry; passed through
/// <c>Map.FromDefinition</c> to construct the runtime
/// <c>Map</c> instance.
/// </summary>
public sealed record MapDefinition(
    float Width,
    float Length,
    AttributionDefinition? Attribution = null,
    string? TerrainMesh = null,
    string? NavMesh = null);
