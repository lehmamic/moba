using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vertex(Vector3D<float> Position, Vector2D<float> Uv)
{
    public const int SizeInBytes = 5 * sizeof(float);
}
