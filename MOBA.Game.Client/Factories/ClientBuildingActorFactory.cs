using MOBA.Engine.Core;
using MOBA.Game.Actors;
using MOBA.Game.Scenes;
using MOBA.Game.Factories;
using MOBA.Game.Models;
using MOBA.Engine.Graphics;
using MOBA.Game.Client.Components;

namespace MOBA.Game.Client.Factories;

/// <summary>
/// Client-side <see cref="IActorFactory"/> for <c>"Building"</c>. Reuses the
/// sim factory's JSON parsing to build the typed <see cref="BuildingActor"/>,
/// then attaches the building's <see cref="MeshRendererComponent"/> via the
/// constructor-injected <see cref="AssetManager"/>. No client-side actor
/// subclass — render decoration is the factory's responsibility.
/// </summary>
public sealed class ClientBuildingActorFactory : IActorFactory
{
    private readonly AssetManager _assets;

    public ClientBuildingActorFactory(AssetManager assets) => _assets = assets;

    public string TypeName => "Building";

    public Actor Create(ActorEntryDefinition entry, Actor sceneRoot)
    {
        var definition = BuildingActorFactory.ParseDefinition(entry);
        var actor = new BuildingActor(Building.FromDefinition(definition));
        _ = new MeshRendererComponent(actor, _assets.LoadModel(actor.Definition.MeshAsset));
        return actor;
    }
}
