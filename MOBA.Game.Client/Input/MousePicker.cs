using MOBA.Engine.Graphics.Rendering;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Input;

/// <summary>
/// Converts a screen-pixel mouse position into a world-space hit on the
/// <c>y = 0</c> ground plane: unproject NDC at near and far through the camera's
/// inverse view-projection matrix, ray-cast from the near point along
/// (far − near), intersect with the ground plane. Returns null when the ray is
/// parallel to the plane or points away from the ground.
/// </summary>
public static class MousePicker
{
    public static Vector3D<float>? PickGround(
        Vector2D<float> screenPosition,
        Vector2D<int> framebufferSize,
        Camera camera)
    {
        if (framebufferSize.X <= 0 || framebufferSize.Y <= 0)
        {
            return null;
        }

        var ndcX = (2f * screenPosition.X / framebufferSize.X) - 1f;
        var ndcY = 1f - (2f * screenPosition.Y / framebufferSize.Y);

        if (!Matrix4X4.Invert(camera.ViewProjection, out var inverseViewProj))
        {
            return null;
        }

        var nearWorld = UnprojectThroughW(new Vector4D<float>(ndcX, ndcY, -1f, 1f), inverseViewProj);
        var farWorld = UnprojectThroughW(new Vector4D<float>(ndcX, ndcY, 1f, 1f), inverseViewProj);

        var origin = new Vector3D<float>(nearWorld.X, nearWorld.Y, nearWorld.Z);
        var direction = Vector3D.Normalize(
            new Vector3D<float>(farWorld.X, farWorld.Y, farWorld.Z) - origin);
        var ray = new Ray3D<float>(origin, direction);

        if (MathF.Abs(ray.Direction.Y) < 1e-6f)
        {
            return null;
        }

        var t = -ray.Origin.Y / ray.Direction.Y;
        if (t < 0f)
        {
            return null;
        }

        var hit = ray.GetPoint(t);
        return new Vector3D<float>(hit.X, 0f, hit.Z);
    }

    private static Vector4D<float> UnprojectThroughW(Vector4D<float> ndcPoint, Matrix4X4<float> inverseViewProj)
    {
        // Row-vector convention to match Silk.NET.Maths defaults.
        var world = Vector4D.Transform(ndcPoint, inverseViewProj);
        return world / world.W;
    }
}
