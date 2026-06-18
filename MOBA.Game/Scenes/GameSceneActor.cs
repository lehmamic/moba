using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Assets;
using MOBA.Game.Components;
using MOBA.Game.Factories;

namespace MOBA.Game.Scenes;

/// <summary>
/// Root actor for a <see cref="SceneType.Game"/> scene. Attaches the
/// scene-level configuration components (<see cref="EnvironmentComponent"/>,
/// <see cref="CamerasComponent"/>) and lets the
/// <see cref="ActorFactoryRegistry"/> populate every map-time actor — the
/// map itself, towers, monsters, … — as children. Map is no longer a
/// special top-level block: it's a regular actor entry with
/// <c>"Type": "Map"</c> and goes through the same factory pipeline as
/// everything else.
///
/// <para>
/// Server uses this class directly. The client adds render decoration in a
/// builder lambda (see <c>ClientGame.BuildClientGameScene</c>) rather than
/// through a subclass — the actor itself stays pure sim.
/// </para>
/// </summary>
public class GameSceneActor : Actor
{
    public GameSceneActor(
        SceneDefinition definition,
        ActorFactoryRegistry registry,
        AssetManager assets)
    {
        Name = definition.Name;

        if (definition.Environment is not null)
        {
            _ = new EnvironmentComponent(this, definition.Environment);
        }
        if (definition.Cameras is not null)
        {
            _ = new CamerasComponent(this, definition.Cameras);
        }

        foreach (var entry in definition.Actors)
        {
            var factory = registry.Get(entry.Type);
            AddChild(factory.Create(entry, this));
        }
    }

    /// <summary>Human-readable scene name from the JSON. Useful for log lines / debug HUD.</summary>
    public string Name { get; }
}
