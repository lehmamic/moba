namespace MOBA.Engine.Graphics;

/// <summary>
/// A renderable backed by an <see cref="ISkinnedMesh"/> plus a per-frame
/// <see cref="MatrixPalette"/>. The renderer dispatches these through
/// <see cref="IGraphicsBackend.DrawSkinnedMeshInPass"/>. Implementations supply
/// the palette by sampling an <see cref="Animation"/> at the current play time.
/// </summary>
public interface ISkinnedRenderable : IRenderable
{
    MatrixPalette Palette { get; }
}