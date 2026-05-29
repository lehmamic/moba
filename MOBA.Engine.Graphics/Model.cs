namespace MOBA.Engine.Graphics;

/// <summary>
/// A renderable asset assembled from one or more <see cref="ModelPart"/>s
/// (mesh + material). For the common single-part case, use the
/// <see cref="Mesh"/> and <see cref="Material"/> convenience accessors;
/// multi-part assets walk <see cref="Parts"/> directly.
///
/// Future iterations attach skeleton, animation clips, and other sub-assets
/// here without changing the loader API.
/// </summary>
public sealed class Model
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
}

public readonly record struct ModelPart(IMesh Mesh, Material Material);