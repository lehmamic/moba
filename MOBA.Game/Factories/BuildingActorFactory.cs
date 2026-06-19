using System.Text.Json;
using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Actors;
using MOBA.Game.Components;
using MOBA.Game.Models;
using MOBA.Game.Scenes;

namespace MOBA.Game.Factories;

/// <summary>
/// Sim factory for <see cref="BuildingActor"/>. Reads the typed
/// <see cref="BuildingDefinition"/> out of the scene entry and runs it through the
/// existing <see cref="Building.FromDefinition"/> factory; the resulting
/// runtime <see cref="Building"/> carries position / rotation / scale +
/// asset reference for the sim layer.
///
/// <para>
/// Towers and the nexus also pick up a server-side combat surface here
/// (<see cref="AggroComponent"/> + <see cref="AttackComponent"/>) with the
/// tower / nexus <see cref="AttackProfiles"/> and <see cref="AggroProfiles"/>.
/// Stationary attackers carry no <c>MoveTargetComponent</c> and the chase
/// path-finding never runs, so no navmesh injection is needed.
/// </para>
/// </summary>
public sealed class BuildingActorFactory : IActorFactory
{
    private readonly Scene _scene;

    public BuildingActorFactory(Scene scene) => _scene = scene;

    public string TypeName => "Building";

    public Actor Create(ActorEntryDefinition entry, Actor sceneRoot)
    {
        var building = new BuildingActor(Building.FromDefinition(ParseDefinition(entry)));
        if (building.Definition.Type is "Tower" or "Nexus")
        {
            _ = new AggroComponent(building, AggroProfiles.ForBuilding(building.Definition.Type), _scene);
            _ = new AttackComponent(building, AttackProfiles.ForBuilding(building.Definition.Type));
        }
        return building;
    }

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
