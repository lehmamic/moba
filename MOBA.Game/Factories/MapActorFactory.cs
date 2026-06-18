using System.Text.Json;
using MOBA.Engine.Core.Assets;
using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Actors;
using MOBA.Game.Models;
using MOBA.Game.Scenes;

namespace MOBA.Game.Factories;

/// <summary>
/// Sim factory for <see cref="MapActor"/>. Reads the map dimensions, the
/// terrain mesh reference and the precomputed navmesh file name from the
/// scene entry's <c>Properties</c>, loads the navmesh via the
/// constructor-injected <see cref="AssetManager"/>, and constructs the
/// runtime <see cref="MapActor"/>.
///
/// <para>
/// <see cref="Map"/> in the scene JSON is now a regular actor entry with
/// <c>"Type": "Map"</c> — no special top-level block in
/// <c>SceneDefinition</c>. Keeping it in the actors list means
/// adding / dropping maps follows the same pattern as adding / dropping
/// any other map-time actor.
/// </para>
/// </summary>
public sealed class MapActorFactory : IActorFactory
{
    private readonly AssetManager _assets;

    public MapActorFactory(AssetManager assets) => _assets = assets;

    public string TypeName => "Map";

    public Actor Create(ActorEntryDefinition entry, Actor sceneRoot)
    {
        var properties = entry.Properties.Deserialize<MapDefinition>()
            ?? throw new InvalidOperationException(
                $"Actor '{entry.Id ?? "<unnamed>"}': Map Properties failed to deserialise.");
        var map = Map.FromDefinition(properties);
        var navMesh = _assets.LoadNavMesh(map.NavMesh);
        return new MapActor(map, navMesh);
    }
}
