using MOBA.Game.Scenes;
using Silk.NET.Maths;

namespace MOBA.Game.Models;

/// <summary>
/// One destructible building instance on the map (a Tower, Nexus or Inhibitor).
/// Decoded from <see cref="BuildingDefinition"/>; carries the parsed
/// <see cref="Position"/>/<see cref="Rotation"/>/<see cref="Scale"/> plus the
/// short mesh-asset name the renderer uses to look up the prefab GLB.
/// </summary>
public sealed class Building
{
    public Building(
        string id,
        string type,
        string? team,
        Vector3D<float> position,
        Quaternion<float> rotation,
        Vector3D<float> scale,
        string meshAsset)
    {
        Id = id;
        Type = type;
        Team = team;
        Position = position;
        Rotation = rotation;
        Scale = scale;
        MeshAsset = meshAsset;
    }

    public string Id { get; }

    public string Type { get; }

    public string? Team { get; }

    public Vector3D<float> Position { get; }

    public Quaternion<float> Rotation { get; }

    public Vector3D<float> Scale { get; }

    /// <summary>Short asset name (filename without extension), e.g. <c>blue-tower</c>.</summary>
    public string MeshAsset { get; }

    public static Building FromDefinition(BuildingDefinition definition) =>
        new(
            definition.Id,
            definition.Type,
            definition.Team,
            EntityTransform.ParsePosition(definition.Transform),
            EntityTransform.ParseRotation(definition.Transform),
            EntityTransform.ParseScale(definition.Transform),
            EntityTransform.MeshAssetFromFile(definition.File));
}
