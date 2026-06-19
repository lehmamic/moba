using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Assets;
using MOBA.Engine.Graphics;
using MOBA.Game.Actors;
using MOBA.Game.Client.Components;
using MOBA.Game.Factories;
using MOBA.Game.Messages;
using MOBA.Game.Models;
using MOBA.Game.Scenes;
using Silk.NET.Maths;

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

    /// <summary>
    /// Assembles a render-ready minion for a network spawn. The server sends only
    /// the <see cref="MinionType"/> + <see cref="TeamId"/>; the mesh asset key
    /// convention lives here so "how to build a minion" stays in one place.
    /// </summary>
    public Actor CreateNetworkedMinion(MinionType type, TeamId team, Vector3D<float> position)
    {
        var minion = new MinionActor(position, team.ToName() ?? "Unknown", type);
        _ = new SkeletalMeshRendererComponent(minion, _assets.LoadModel(MinionAssetKey(type, team)));
        return minion;
    }

    private static string MinionAssetKey(MinionType type, TeamId team)
    {
        var role = type switch
        {
            MinionType.Caster => "ranged",
            MinionType.Siege => "siege",
            _ => "melee",
        };
        var side = team == TeamId.Chaos ? "chaos" : "order";
        return $"monsters/{role}_minion_{side}";
    }
}
