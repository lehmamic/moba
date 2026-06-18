using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace MOBA.Engine.Graphics.Rendering;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vertex(
    Vector3D<float> Position,
    Vector2D<float> Uv,
    Vector3D<float> Normal)
{
    public const int SizeInBytes = 8 * sizeof(float);
}
