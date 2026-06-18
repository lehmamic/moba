using MOBA.Game.Scenes;
using Silk.NET.Maths;

namespace MOBA.Game.Models;

/// <summary>
/// Mob / monster spawn instance. Same shape as <see cref="Building"/>; the
/// distinction lives in <see cref="Type"/> + the JSON section it came from.
/// </summary>
public sealed class Monster
{
    public Monster(
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

    public string MeshAsset { get; }

    public static Monster FromDefinition(MonsterDefinition definition) =>
        new(
            definition.Id,
            definition.Type,
            definition.Team,
            EntityTransform.ParsePosition(definition.Transform),
            EntityTransform.ParseRotation(definition.Transform),
            EntityTransform.ParseScale(definition.Transform),
            EntityTransform.MeshAssetFromFile(definition.File));
}
