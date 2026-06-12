using MOBA.Engine.Core;

namespace MOBA.Game;

/// <summary>
/// Server-authoritative sim world. Lives on the server (headless) and — mirrored
/// for visualisation — inside the client process. Populates the <see cref="Scene"/>
/// with the static map geometry only; player actors are spawned dynamically by the
/// server's <c>PlayerConnectionSystem</c> in response to client join messages.
/// </summary>
public sealed class MobaWorld
{
    public MobaWorld(Map map) => Map = map;

    public Map Map { get; }

    public void Populate(Scene scene)
    {
        scene.AddActor(new GroundPlaneActor(Map));
        foreach (var building in Map.Buildings)
        {
            scene.AddActor(new BuildingActor(building));
        }
        foreach (var monster in Map.Monsters)
        {
            scene.AddActor(new MonsterActor(monster));
        }
    }
}
