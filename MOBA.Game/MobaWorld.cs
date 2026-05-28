using MOBA.Engine.Core;

namespace MOBA.Game;

/// <summary>
/// Server-authoritative sim world. Lives on the server (headless) and — for loopback in the
/// first slice — also inside the client process. Creates the actors and populates the
/// <see cref="Scene"/>.
/// </summary>
public sealed class MobaWorld
{
    public MobaWorld(Map map) => Map = map;

    public Map Map { get; }

    public void Populate(Scene scene)
    {
        scene.AddActor(new GroundPlaneActor(Map));
        scene.AddActor(new TestCubeActor());
    }
}
