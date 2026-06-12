namespace MOBA.Game;

/// <summary>
/// JSON-serialisable description of a map. Loaded from <c>assets/maps/*.json</c>
/// by both server and client via <see cref="MOBA.Engine.Core.AssetCache{TKey,TAsset}"/>;
/// passed through <see cref="Map.FromDefinition"/> to construct the runtime
/// <see cref="Map"/> instance.
///
/// <para>
/// <see cref="Buildings"/> + <see cref="Monsters"/> arrays carry per-instance
/// <see cref="TransformDef"/>s extracted from the source Blender scene. The
/// extracted-prefab GLBs themselves are centered at origin; runtime spawn
/// applies the per-instance transform on top.
/// </para>
/// </summary>
public sealed record MapDefinition(
    float Width,
    float Length,
    AttributionDef? Attribution = null,
    string? TerrainMesh = null,
    IReadOnlyList<BuildingDef>? Buildings = null,
    IReadOnlyList<MonsterDef>? Monsters = null);

/// <summary>
/// Asset attribution metadata for third-party-sourced map content. Not used
/// at runtime; documents provenance + licence terms for the map's asset chain.
/// </summary>
public sealed record AttributionDef(string Source, string URL, string License, string LicenseURL);

/// <summary>
/// Per-instance transform as serialised in the map JSON. Position is the
/// world-space spawn point (game Y-up). Rotation is Euler angles in degrees
/// applied as yaw (Y), pitch (X), roll (Z). Scale is uniform per-axis.
/// </summary>
public sealed record TransformDef(float[] Position, float[] Rotation, float[] Scale);

/// <summary>
/// Destructible building instance (Nexus, Tower, Inhibitor). The <see cref="File"/>
/// is a repo-relative path to the prefab GLB; the runtime <see cref="Building"/>
/// derives the short asset name from it.
/// </summary>
public sealed record BuildingDef(
    string Id,
    string Type,
    TransformDef Transform,
    string File,
    string? Team = null);

/// <summary>
/// Mob / monster spawn entry. Same shape as <see cref="BuildingDef"/>; kept as
/// a distinct type so the map definition can pivot on the role of an entity.
/// </summary>
public sealed record MonsterDef(
    string Id,
    string Type,
    TransformDef Transform,
    string File,
    string? Team = null);
