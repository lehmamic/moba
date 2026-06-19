using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Assets;
using MOBA.Engine.Graphics;
using MOBA.Engine.Graphics.Abstractions;
using MOBA.Game.Actors;
using MOBA.Game.Client.Components;
using MOBA.Game.Components;
using MOBA.Game.Factories;
using MOBA.Game.Models;
using MOBA.Game.Scenes;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Factories;

/// <summary>
/// Client-side <see cref="IActorFactory"/> for <c>"Building"</c>. Reuses the
/// sim factory's JSON parsing to build the typed <see cref="BuildingActor"/>,
/// then attaches the building's <see cref="MeshRendererComponent"/> + an HP-bar
/// billboard. Per-type bar size + height come from the building's
/// <see cref="Building.Type"/> — nexus bars are wider and float higher than
/// tower bars.
/// </summary>
public sealed class ClientBuildingActorFactory : IActorFactory
{
    private readonly AssetManager _assets;
    private readonly IMesh _barQuad;
    private readonly IShader _barShader;
    private readonly Scene _scene;

    public ClientBuildingActorFactory(
        AssetManager assets,
        IMesh barQuad,
        IShader barShader,
        Scene scene)
    {
        _assets = assets;
        _barQuad = barQuad;
        _barShader = barShader;
        _scene = scene;
    }

    public string TypeName => "Building";

    public Actor Create(ActorEntryDefinition entry, Actor sceneRoot)
    {
        var definition = BuildingActorFactory.ParseDefinition(entry);
        var actor = new BuildingActor(Building.FromDefinition(definition));
        _ = new MeshRendererComponent(actor, _assets.LoadModel(actor.Definition.MeshAsset));
        var health = actor.GetComponent<HealthComponent>()!;
        var (offsetY, size) = BarShapeForType(actor.Definition.Type);
        _ = new HealthBarVisualComponent(
            actor,
            health,
            _scene,
            _barQuad,
            _barShader,
            worldOffset: new Vector3D<float>(0f, offsetY, 0f),
            sizeWorldUnits: size);
        return actor;
    }

    private static (float OffsetY, Vector2D<float> Size) BarShapeForType(string type) => type switch
    {
        "Nexus" => (11f, new Vector2D<float>(5f, 0.45f)),
        "Inhibitor" => (10f, new Vector2D<float>(4f, 0.4f)),
        "Tower" => (9f, new Vector2D<float>(3f, 0.35f)),
        _ => (6f, new Vector2D<float>(2.5f, 0.3f)),
    };
}
