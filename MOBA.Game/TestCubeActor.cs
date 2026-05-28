using MOBA.Engine.Core;
using Silk.NET.Maths;

namespace MOBA.Game;

public sealed class TestCubeActor : Actor
{
    public TestCubeActor()
    {
        // Unit cube (extent ±0.5) scaled by 2; placed at Y=1 so its bottom edge sits exactly
        // on Y=0 (on the ground plane).
        Transform.Position = new Vector3D<float>(0f, 1f, 0f);
        Transform.Scale = new Vector3D<float>(2f, 2f, 2f);
        _ = new TransformComponent(this);
    }
}
