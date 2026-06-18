using System.Text.Json;
using MOBA.Engine.Core;
using MOBA.Game.Actors;
using MOBA.Game.Scenes;
using MOBA.Game.Models;

namespace MOBA.Game.Factories;

/// <summary>
/// Sim factory for <see cref="BuildingActor"/>. Reads the typed
/// <see cref="BuildingDefinition"/> out of the scene entry and runs it through the
/// existing <see cref="Building.FromDefinition"/> factory; the resulting
/// runtime <see cref="Building"/> carries position / rotation / scale +
/// asset reference for the sim layer.
/// </summary>
public sealed class BuildingActorFactory : IActorFactory
{
    public string TypeName => "Building";

    public Actor Create(ActorEntryDefinition entry, Actor sceneRoot) =>
        new BuildingActor(Building.FromDefinition(ParseDefinition(entry)));

    public static BuildingDefinition ParseDefinition(ActorEntryDefinition entry)
    {
        if (entry.Transform is null)
        {
            throw new InvalidOperationException(
                $"Actor '{entry.Id ?? "<unnamed>"}' of type Building requires a Transform.");
        }

        var props = entry.Properties.Deserialize<Properties>()
            ?? throw new InvalidOperationException(
                $"Actor '{entry.Id ?? "<unnamed>"}': Building Properties failed to deserialise.");

        return new BuildingDefinition(
            entry.Id ?? throw new InvalidOperationException("Building actor needs entry.Id."),
            props.BuildingType,
            entry.Transform,
            props.File,
            props.Team);
    }

    private sealed record Properties(string BuildingType, string File, string? Team = null);
}
