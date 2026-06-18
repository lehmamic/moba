using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

/// <summary>
/// A keyframe animation clip resampled to a fixed framerate, as in Madhav
/// <i>Game Programming in C++</i> ch.12 <c>Animation</c>. Each bone has its own
/// per-frame <see cref="BoneTransform"/> track; sampling at time <c>t</c> picks
/// the surrounding frame indices, slerps/lerps each bone's pose, and composes
/// the per-bone matrices into global poses by walking the skeleton tree.
///
/// <para>
/// glTF stores animations per-channel with channel-specific keyframe times,
/// which is more flexible than Madhav's uniform-frame format. We resample on
/// load (see <see cref="GltfModelLoader"/>) at a fixed frame interval so the
/// runtime evaluation matches the book exactly.
/// </para>
/// </summary>
public sealed class Animation
{
    public Animation(
        string name,
        int numBones,
        int numFrames,
        float duration,
        IReadOnlyList<IReadOnlyList<BoneTransform>> tracks)
    {
        if (tracks.Count != numBones)
        {
            throw new ArgumentException(
                $"Animation '{name}' has {tracks.Count} tracks but {numBones} bones.");
        }
        if (numFrames < 2)
        {
            throw new ArgumentException(
                $"Animation '{name}' must have at least 2 frames (got {numFrames}).");
        }
        Name = name;
        NumBones = numBones;
        NumFrames = numFrames;
        Duration = duration;
        FrameDuration = duration / (numFrames - 1);
        Tracks = tracks;
    }

    public string Name { get; }

    public int NumBones { get; }

    public int NumFrames { get; }

    public float Duration { get; }

    public float FrameDuration { get; }

    /// <summary>
    /// Outer index = bone, inner index = frame. Every track has exactly
    /// <see cref="NumFrames"/> entries.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<BoneTransform>> Tracks { get; }

    /// <summary>
    /// Fills <paramref name="outPoses"/> with the global pose matrix of each bone
    /// at <paramref name="time"/>. Time is assumed to be in
    /// <c>[0, <see cref="Duration"/>]</c>. The caller multiplies these by the
    /// skeleton's <see cref="Skeleton.GlobalInverseBindPoses"/> to get the matrix
    /// palette the skinning shader consumes.
    /// </summary>
    public void GetGlobalPoseAtTime(
        Skeleton skeleton,
        float time,
        Span<Matrix4X4<float>> outPoses)
    {
        if (outPoses.Length < NumBones)
        {
            throw new ArgumentException(
                $"outPoses span length ({outPoses.Length}) < bone count ({NumBones}).");
        }

        var frame = (int)(time / FrameDuration);
        if (frame >= NumFrames - 1)
        {
            frame = NumFrames - 2;
        }
        var nextFrame = frame + 1;
        var pct = (time / FrameDuration) - frame;

        // Root.
        outPoses[0] = Tracks[0].Count > 0
            ? BoneTransform.Interpolate(Tracks[0][frame], Tracks[0][nextFrame], pct).ToMatrix()
            : Matrix4X4<float>.Identity;

        // Rest of the hierarchy: each bone's global pose = its local pose at time
        // times its parent's global pose (row-vector convention, same as ADR-002).
        for (var bone = 1; bone < NumBones; bone++)
        {
            var localMat = Tracks[bone].Count > 0
                ? BoneTransform.Interpolate(Tracks[bone][frame], Tracks[bone][nextFrame], pct).ToMatrix()
                : Matrix4X4<float>.Identity;
            outPoses[bone] = localMat * outPoses[skeleton.Bones[bone].Parent];
        }
    }
}
