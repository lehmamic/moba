using System.Text.Json;
using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Actors;
using MOBA.Game.Scenes;
using Silk.NET.Maths;

namespace MOBA.Game.Factories;

/// <summary>
/// Factory for <c>{ "Type": "Team" }</c> actor entries. Both server and
/// client register this factory — teams have no render component, so the
/// same sim type is used on both sides.
/// </summary>
public sealed class TeamActorFactory : IActorFactory
{
    public string TypeName => "Team";

    public Actor Create(ActorEntryDefinition entry, Actor sceneRoot)
    {
        var definition = entry.Properties.Deserialize<TeamDefinition>()
            ?? throw new InvalidOperationException(
                $"Actor '{entry.Id ?? "<unnamed>"}': Team properties failed to deserialise.");
        if (definition.SpawnAreaCenter is not { Length: 3 } c)
        {
            throw new InvalidOperationException(
                $"Team '{definition.Name}' requires a 3-element SpawnAreaCenter.");
        }
        return new TeamActor(definition.Name, new Vector3D<float>(c[0], c[1], c[2]));
    }
}
