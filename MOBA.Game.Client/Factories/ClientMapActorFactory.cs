using System.Text.Json;
using MOBA.Engine.Core.Assets;
using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Graphics;
using MOBA.Game.Actors;
using MOBA.Game.Client.Components;
using MOBA.Game.Factories;
using MOBA.Game.Models;
using MOBA.Game.Scenes;

namespace MOBA.Game.Client.Factories;

/// <summary>
/// Client-side <see cref="IActorFactory"/> for <c>"Map"</c>. Builds the sim
/// <see cref="MapActor"/> (Map + NavMesh + TransformComponent) then attaches
/// the terrain <see cref="MeshRendererComponent"/>. The terrain GLB is
/// pre-scaled in Blender; <see cref="Actor.Transform"/> stays at identity.
/// </summary>
public sealed class ClientMapActorFactory : IActorFactory
{
    private readonly AssetManager _assets;

    public ClientMapActorFactory(AssetManager assets) => _assets = assets;

    public string TypeName => "Map";

    public Actor Create(ActorEntryDefinition entry, Actor sceneRoot)
    {
        var properties = entry.Properties.Deserialize<MapDefinition>()
            ?? throw new InvalidOperationException(
                $"Actor '{entry.Id ?? "<unnamed>"}': Map Properties failed to deserialise.");
        var map = Map.FromDefinition(properties);
        var navMesh = _assets.LoadNavMesh(map.NavMesh);
        var actor = new MapActor(map, navMesh);
        _ = new MeshRendererComponent(actor, _assets.LoadModel($"maps/{map.TerrainMesh}"));
        return actor;
    }
}
