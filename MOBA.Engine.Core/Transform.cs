using Silk.NET.Maths;

namespace MOBA.Engine.Core;

/// <summary>
/// Position, rotation, and scale for an <see cref="Actor"/>, plus the composed
/// world matrix. Bundling these on a dedicated class (rather than as flat
/// properties on Actor) reads better at call sites — <c>actor.Transform.Position</c>
/// instead of <c>actor.Position</c> — and gives a natural home for transform
/// helpers (look-at, lerp, parenting) when they arrive.
/// </summary>
public sealed class Transform
{
    public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;

    public Quaternion<float> Rotation { get; set; } = Quaternion<float>.Identity;

    public Vector3D<float> Scale { get; set; } = Vector3D<float>.One;

    /// <summary>Composed world matrix: <c>S * R * T</c> in row-vector convention.</summary>
    public Matrix4X4<float> World =>
        Matrix4X4.CreateScale(Scale)
        * Matrix4X4.CreateFromQuaternion(Rotation)
        * Matrix4X4.CreateTranslation(Position);
}
