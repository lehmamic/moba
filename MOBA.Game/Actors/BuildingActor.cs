using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Components;
using MOBA.Game.Models;

namespace MOBA.Game.Actors;

/// <summary>
/// Static destructible-building actor (Tower, Nexus, Inhibitor) spawned from
/// the map's <see cref="Building"/> list. Carries the per-instance transform
/// + identity metadata; the client-side <c>ClientBuildingActorFactory</c>
/// attaches the <c>MeshRendererComponent</c> after construction.
/// </summary>
public sealed class BuildingActor : Actor
{
    public BuildingActor(Building definition)
    {
        Definition = definition;
        Transform.Position = definition.Position;
        Transform.Rotation = definition.Rotation;
        Transform.Scale = definition.Scale;
        _ = new TransformComponent(this);
        _ = new HealthComponent(this, MaxHealthByType(definition.Type));
        if (definition.Team is not null)
        {
            _ = new TeamComponent(this, definition.Team);
        }
    }

    public Building Definition { get; }

    private static float MaxHealthByType(string type) => type switch
    {
        "Nexus" => 5500f,
        "Inhibitor" => 4000f,
        "Tower" => 3000f,
        _ => 1000f,
    };
}
