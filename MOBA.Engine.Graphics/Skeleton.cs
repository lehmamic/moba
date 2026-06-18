using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

/// <summary>
/// A hierarchical bone skeleton, as loaded from a glTF skin. Mirrors Madhav
/// <i>Game Programming in C++</i> ch.12 <c>Skeleton</c> — each bone carries a name,
/// a parent index (-1 for the root), and its local bind pose; the
/// <see cref="GlobalInverseBindPoses"/> array is the parallel data the skinning
/// shader needs at draw time (multiplied by the current global pose to produce
/// the matrix palette).
///
/// <para>
/// Unlike Madhav, who computes <c>GlobalInverseBindPoses</c> from the local bind
/// poses, glTF stores them directly in the skin's <c>inverseBindMatrices</c>
/// accessor. We pass them in at construction.
/// </para>
/// </summary>
public sealed class Skeleton
{
    public Skeleton(
        IReadOnlyList<Bone> bones,
        IReadOnlyList<Matrix4X4<float>> globalInverseBindPoses)
    {
        if (bones.Count != globalInverseBindPoses.Count)
        {
            throw new ArgumentException(
                $"Skeleton bone count ({bones.Count}) and inverse-bind-pose count "
                + $"({globalInverseBindPoses.Count}) disagree.");
        }
        Bones = bones;
        GlobalInverseBindPoses = globalInverseBindPoses;
    }

    public IReadOnlyList<Bone> Bones { get; }

    public IReadOnlyList<Matrix4X4<float>> GlobalInverseBindPoses { get; }

    public int NumBones => Bones.Count;

    /// <summary>
    /// Skeleton bone metadata. <see cref="Parent"/> is the index of the parent bone
    /// in the <see cref="Skeleton.Bones"/> list, or -1 for the root.
    /// </summary>
    public sealed record Bone(string Name, int Parent, BoneTransform LocalBindPose);
}
