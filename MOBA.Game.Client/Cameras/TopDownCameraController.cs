using MOBA.Engine.Graphics;
using MOBA.Game.Scenes;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Cameras;

/// <summary>
/// Fixed MOBA top-down view (~60° pitch). Static in the first skeleton — pan/edge-scroll
/// will be added once we have a player character.
/// </summary>
public sealed class TopDownCameraController
{
    public TopDownCameraController(float aspectRatio, CameraDefinition? config = null)
    {
        var pos = config?.Position is { Length: 3 } p
            ? new Vector3D<float>(p[0], p[1], p[2])
            : new Vector3D<float>(0f, 60f, 30f);
        var target = config?.Target is { Length: 3 } t
            ? new Vector3D<float>(t[0], t[1], t[2])
            : Vector3D<float>.Zero;

        Camera = new Camera
        {
            Position = pos,
            Target = target,
            Up = Vector3D<float>.UnitY,
            FieldOfViewRadians = config?.FieldOfViewRadians ?? MathF.PI / 4f,
            AspectRatio = aspectRatio,
        };
    }

    public Camera Camera { get; }

    public void Update(float deltaSeconds)
    {
        // Intentionally empty — fixed camera. Pan/zoom will arrive with gameplay.
        _ = deltaSeconds;
    }
}
