using Silk.NET.Maths;

namespace MOBA.Engine.Graphics.Animations;

/// <summary>
/// Local TR of a skeleton bone (rotation + translation). Mirrors Madhav
/// <i>Game Programming in C++</i> ch.12 <c>BoneTransform</c>. Scale is intentionally
/// omitted — Mixamo / Tripo / most game-pipeline rigs don't bake scale at the bone
/// level, and the book doesn't either.
/// </summary>
public readonly record struct BoneTransform(
    Quaternion<float> Rotation,
    Vector3D<float> Translation)
{
    public Matrix4X4<float> ToMatrix() =>
        Matrix4X4.CreateFromQuaternion(Rotation)
        * Matrix4X4.CreateTranslation(Translation);

    /// <summary>
    /// Slerp rotation, lerp translation. The classical pair used everywhere from
    /// Madhav's textbook to Mecanim.
    /// </summary>
    public static BoneTransform Interpolate(BoneTransform a, BoneTransform b, float t) =>
        new(
            Quaternion<float>.Slerp(a.Rotation, b.Rotation, t),
            Vector3D.Lerp(a.Translation, b.Translation, t));
}
