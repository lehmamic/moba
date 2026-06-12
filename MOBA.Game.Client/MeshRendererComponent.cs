using MOBA.Engine.Core;
using MOBA.Engine.Graphics;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// Render component (client-only). Binds one or more <see cref="ModelPart"/>s to a
/// sim actor. Single-mesh callers (procedural ground, sphere markers) use the
/// (mesh, material) constructor; callers with a multi-part <see cref="Model"/>
/// (a glTF or OBJ asset) use the model constructor. A material override is
/// provided for the common case of loading geometry-only assets and painting them
/// with one shared material (e.g. the LoL terrain extract on the grass material).
/// The server build does not contain this code — the headless server is sim-only
/// and does not reference MOBA.Engine.Graphics at all.
/// </summary>
public sealed class MeshRendererComponent : Component, IRenderable
{
    public MeshRendererComponent(Actor owner, IMesh mesh, Material material) : base(owner)
        => Parts = [new ModelPart(mesh, material, Matrix4X4<float>.Identity)];

    public MeshRendererComponent(Actor owner, Model model) : base(owner)
        => Parts = model.Parts;

    public MeshRendererComponent(Actor owner, Model model, Material materialOverride) : base(owner)
        => Parts = [.. model.Parts.Select(p => new ModelPart(p.Mesh, materialOverride, p.LocalTransform))];

    public IReadOnlyList<ModelPart> Parts { get; }
}
