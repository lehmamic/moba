using MOBA.Engine.Core;
using MOBA.Game.Actors;
using MOBA.Game.Scenes;
using MOBA.Game.Factories;
using MOBA.Game.Models;
using MOBA.Engine.Graphics;
using MOBA.Game.Client.Components;

namespace MOBA.Game.Client.Factories;

/// <summary>
/// Client-side <see cref="IActorFactory"/> for <c>"Monster"</c>. Builds the
/// sim <see cref="MonsterActor"/>, then attaches a
/// <see cref="MeshRendererComponent"/> with <c>IsVisible = false</c> by
/// default — monsters stay in the scene (out of the navmesh, available for
/// sim) but render off while the gameplay iteration is in progress. Flip the
/// flag (or remove it) once jungle mobs become a gameplay focus.
/// </summary>
public sealed class ClientMonsterActorFactory : IActorFactory
{
    private readonly AssetManager _assets;

    public ClientMonsterActorFactory(AssetManager assets) => _assets = assets;

    public string TypeName => "Monster";

    public Actor Create(ActorEntryDefinition entry, Actor sceneRoot)
    {
        var definition = MonsterActorFactory.ParseDefinition(entry);
        var actor = new MonsterActor(Monster.FromDefinition(definition));
        _ = new MeshRendererComponent(actor, _assets.LoadModel(actor.Definition.MeshAsset))
        {
            IsVisible = false,
        };
        return actor;
    }
}
