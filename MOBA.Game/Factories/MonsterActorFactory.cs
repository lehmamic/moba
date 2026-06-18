using System.Text.Json;
using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Actors;
using MOBA.Game.Models;
using MOBA.Game.Scenes;

namespace MOBA.Game.Factories;

/// <summary>
/// Sim factory for <see cref="MonsterActor"/>. Mirrors
/// <see cref="BuildingActorFactory"/>; kept separate so future sim systems can
/// pivot on the role (respawn timers etc.).
/// </summary>
public sealed class MonsterActorFactory : IActorFactory
{
    public string TypeName => "Monster";

    public Actor Create(ActorEntryDefinition entry, Actor sceneRoot) =>
        new MonsterActor(Monster.FromDefinition(ParseDefinition(entry)));

    public static MonsterDefinition ParseDefinition(ActorEntryDefinition entry)
    {
        if (entry.Transform is null)
        {
            throw new InvalidOperationException(
                $"Actor '{entry.Id ?? "<unnamed>"}' of type Monster requires a Transform.");
        }

        var props = entry.Properties.Deserialize<Properties>()
            ?? throw new InvalidOperationException($"Actor '{entry.Id ?? "<unnamed>"}': Monster Properties failed to deserialise.");

        return new MonsterDefinition(
            entry.Id ?? throw new InvalidOperationException("Monster actor needs entry.Id."),
            props.MonsterType,
            entry.Transform,
            props.File,
            props.Team);
    }

    private sealed record Properties(string MonsterType, string File, string? Team = null);
}
