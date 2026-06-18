using Silk.NET.Maths;

namespace MOBA.Engine.Graphics.Rendering;

/// <summary>
/// Right-handed, Y-up. View and Projection use Silk.NET.Maths defaults (RH, OpenGL Z-range).
/// For Vulkan later: the backend will patch Y-flip + Z-range. The Camera API stays the same.
/// </summary>
public class Camera
{
    public Vector3D<float> Position { get; set; } = new(0f, 5f, 10f);

    public Vector3D<float> Target { get; set; } = Vector3D<float>.Zero;

    public Vector3D<float> Up { get; set; } = Vector3D<float>.UnitY;

    public float FieldOfViewRadians { get; set; } = MathF.PI / 3f;

    public float AspectRatio { get; set; } = 16f / 9f;

    public float NearPlane { get; set; } = 0.1f;

    public float FarPlane { get; set; } = 1000f;

    public Matrix4X4<float> View => Matrix4X4.CreateLookAt(Position, Target, Up);

    public Matrix4X4<float> Projection =>
        Matrix4X4.CreatePerspectiveFieldOfView(FieldOfViewRadians, AspectRatio, NearPlane, FarPlane);

    public Matrix4X4<float> ViewProjection => View * Projection;
}
