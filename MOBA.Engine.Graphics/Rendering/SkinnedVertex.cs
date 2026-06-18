using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace MOBA.Engine.Graphics.Rendering;

/// <summary>
/// Vertex layout for skinned meshes: position + normal + 4 bone influences
/// (index + weight per influence) + UV. Mirrors Madhav <i>Game Programming in
/// C++</i> ch.12 skinned vertex layout (<c>PosNormSkinTex</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct SkinnedVertex(
    Vector3D<float> Position,
    Vector3D<float> Normal,
    uint BonePacked,
    Vector4D<float> BoneWeights,
    Vector2D<float> Uv)
{
    /// <summary>
    /// Packs four byte-indexed bone references into one <see cref="uint"/>
    /// (i0 in the lowest byte, i3 in the highest). Matches the GLSL <c>uvec4</c>
    /// attribute layout when read as <c>UNSIGNED_BYTE</c>.
    /// </summary>
    public static uint PackBoneIndices(byte i0, byte i1, byte i2, byte i3) =>
        i0 | ((uint)i1 << 8) | ((uint)i2 << 16) | ((uint)i3 << 24);

    public const int SizeInBytes = (3 + 3) * sizeof(float) + sizeof(uint) + (4 + 2) * sizeof(float);
}
