using MOBA.Engine.Core;
using MOBA.Engine.Graphics;

namespace MOBA.Game.Client;

/// <summary>
/// Render component (client-only). Binds an <see cref="IMesh"/> + <see cref="Material"/>
/// to a sim actor. The server build does not contain this code — the headless server is
/// sim-only and does not reference MOBA.Engine.Graphics at all.
/// </summary>
public sealed class MeshRendererComponent : Component, IRenderable
{
    public MeshRendererComponent(Actor owner, IMesh mesh, Material material) : base(owner)
    {
        Mesh = mesh;
        Material = material;
    }

    public IMesh Mesh { get; }

    public Material Material { get; }
}
