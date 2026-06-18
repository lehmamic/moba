using MOBA.Engine.Graphics.Rendering;
namespace MOBA.Engine.Graphics.Abstractions;

/// <summary>
/// Implemented by any component the <see cref="Renderer"/> can draw. A renderable
/// exposes one or more <see cref="ModelPart"/>s — single-mesh components yield a
/// single-element list, multi-part models (loaded from glTF or OBJ) yield one entry
/// per primitive. Exposed in MOBA.Engine.Graphics so the renderer iterates a scene
/// without knowing about MOBA.Game.Client's concrete component types.
/// </summary>
public interface IRenderable
{
    IReadOnlyList<ModelPart> Parts { get; }

    /// <summary>
    /// When false, the renderer skips every part of this component for the current
    /// frame. Defaults to true so existing implementations don't have to opt in.
    /// Used today to suppress mob rendering while NavMesh-only iterations land.
    /// </summary>
    bool IsVisible => true;
}
