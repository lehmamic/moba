using MOBA.Engine.Graphics;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// Fixed MOBA top-down view (~60° pitch). Static in the first skeleton — pan/edge-scroll
/// will be added once we have a player character.
/// </summary>
public sealed class TopDownCameraController
{
    public TopDownCameraController(float aspectRatio)
    {
        Camera = new Camera
        {
            Position = new Vector3D<float>(0f, 60f, 30f),
            Target = Vector3D<float>.Zero,
            Up = Vector3D<float>.UnitY,
            FieldOfViewRadians = MathF.PI / 4f,
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
