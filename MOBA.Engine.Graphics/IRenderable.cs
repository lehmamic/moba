namespace MOBA.Engine.Graphics;

/// <summary>
/// Implemented by any component that the <see cref="Renderer"/> can draw.
/// Exposed in MOBA.Engine.Graphics so the renderer iterates a scene without
/// knowing about MOBA.Game.Client's concrete component types.
/// </summary>
public interface IRenderable
{
    IMesh Mesh { get; }

    Material Material { get; }
}
