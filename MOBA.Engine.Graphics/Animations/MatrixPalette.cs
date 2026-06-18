using Silk.NET.Maths;

namespace MOBA.Engine.Graphics.Animations;

/// <summary>
/// Fixed-size palette of bone matrices uploaded to the skinning shader as
/// <c>u_palette[MAX_BONES]</c>. Mirrors Madhav <i>Game Programming in C++</i>
/// ch.12 <c>MatrixPalette</c>. <see cref="MaxBones"/> is the limit any skinned
/// asset may use; the knight rig has 41 bones, well under the cap.
/// </summary>
public sealed class MatrixPalette
{
    public const int MaxBones = 96;

    public Matrix4X4<float>[] Entries { get; } = new Matrix4X4<float>[MaxBones];

    public MatrixPalette()
    {
        for (var i = 0; i < MaxBones; i++)
        {
            Entries[i] = Matrix4X4<float>.Identity;
        }
    }
}
