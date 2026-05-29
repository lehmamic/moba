namespace MOBA.Engine.Graphics;

/// <summary>
/// A renderable asset assembled from one or more <see cref="ModelPart"/>s
/// (mesh + material). For the common single-part case, use the
/// <see cref="Mesh"/> and <see cref="Material"/> convenience accessors;
/// multi-part assets walk <see cref="Parts"/> directly.
///
/// <para>
/// <b>Ownership.</b> A <see cref="Model"/> owns the <see cref="IMesh"/> and any
/// <see cref="ITexture"/> stored in its parts — both are constructed at load time
/// solely for this model and have no other consumers. <see cref="IShader"/> is
/// <em>not</em> owned: shaders are shared resources from the shader cache, which
/// disposes them on its own shutdown. <see cref="Dispose"/> reflects this split.
/// </para>
///
/// Future iterations attach skeleton, animation clips, and other sub-assets
/// here without changing the loader API.
/// </summary>
public sealed class Model : IDisposable
{
    public Model(IReadOnlyList<ModelPart> parts)
    {
        if (parts.Count == 0)
        {
            throw new ArgumentException("Model must have at least one part.", nameof(parts));
        }
        Parts = parts;
    }

    public IReadOnlyList<ModelPart> Parts { get; }

    /// <summary>The mesh of the first part — convenience for single-part assets.</summary>
    public IMesh Mesh => Parts[0].Mesh;

    /// <summary>The material of the first part — convenience for single-part assets.</summary>
    public Material Material => Parts[0].Material;

    public void Dispose()
    {
        foreach (var part in Parts)
        {
            part.Mesh.Dispose();
            // The texture (if any) was decoded from the glTF's embedded image bytes
            // and is owned by this Model — nothing else references it.
            part.Material.Texture?.Dispose();
            // part.Material.Shader is cache-shared; the shader cache disposes it.
        }
    }
}

public readonly record struct ModelPart(IMesh Mesh, Material Material);