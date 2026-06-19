using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Assets;
using MOBA.Engine.Graphics;
using MOBA.Engine.Graphics.Abstractions;
using MOBA.Game.Actors;
using MOBA.Game.Client.Components;
using MOBA.Game.Components;
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
    private readonly IMesh _barQuad;
    private readonly IShader _barShader;
    private readonly Scene _scene;

    public ClientMonsterActorFactory(AssetManager assets, IMesh barQuad, IShader barShader, Scene scene)
    {
        _assets = assets;
        _barQuad = barQuad;
        _barShader = barShader;
        _scene = scene;
    }

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
        var health = minion.GetComponent<HealthComponent>()!;
        _ = new HealthBarVisualComponent(
            minion,
            health,
            _scene,
            _barQuad,
            _barShader,
            worldOffset: new Vector3D<float>(0f, 1.5f, 0f),
            sizeWorldUnits: new Vector2D<float>(1.4f, 0.18f));
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
