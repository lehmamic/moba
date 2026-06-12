using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

/// <summary>
/// A renderable asset assembled from one or more <see cref="ModelPart"/>s
/// (mesh + material), plus optional skeletal data: a <see cref="Skeleton"/>
/// and a set of named <see cref="Animation"/> clips. For the common
/// single-part case, use the <see cref="Mesh"/> and <see cref="Material"/>
/// convenience accessors; multi-part assets walk <see cref="Parts"/> directly.
///
/// <para>
/// <b>Ownership.</b> A <see cref="Model"/> owns the <see cref="IMesh"/> (which
/// may itself be an <see cref="ISkinnedMesh"/>) and any <see cref="ITexture"/>
/// stored in its parts — both are constructed at load time solely for this model.
/// <see cref="IShader"/> is <em>not</em> owned: shaders are shared from the shader
/// cache, which disposes them on its own shutdown.
/// </para>
/// </summary>
public sealed class Model : IDisposable
{
    public Model(
        IReadOnlyList<ModelPart> parts,
        Skeleton? skeleton = null,
        IReadOnlyDictionary<string, Animation>? animations = null)
    {
        if (parts.Count == 0)
        {
            throw new ArgumentException("Model must have at least one part.", nameof(parts));
        }
        Parts = parts;
        Skeleton = skeleton;
        Animations = animations ?? new Dictionary<string, Animation>();
    }

    public IReadOnlyList<ModelPart> Parts { get; }

    /// <summary>The skeleton, or null for non-skinned models.</summary>
    public Skeleton? Skeleton { get; }

    /// <summary>Animation clips keyed by glTF clip name (empty for non-animated models).</summary>
    public IReadOnlyDictionary<string, Animation> Animations { get; }

    /// <summary>The mesh of the first part — convenience for single-part assets.</summary>
    public IMesh Mesh => Parts[0].Mesh;

    /// <summary>The material of the first part — convenience for single-part assets.</summary>
    public Material Material => Parts[0].Material;

    public void Dispose()
    {
        foreach (var part in Parts)
        {
            part.Mesh.Dispose();
            part.Material.Texture?.Dispose();
            // part.Material.Shader is cache-shared; the shader cache disposes it.
        }
    }
}

/// <summary>
/// One drawable primitive of a <see cref="Model"/>: its mesh, its material, and the
/// local transform that positions it inside the model's hierarchy. For single-mesh
/// assets the transform is <see cref="Matrix4X4{T}.Identity"/>; for multi-part assets
/// loaded from a glTF scene graph it carries the cumulative node-to-root world matrix
/// so each primitive renders at its authored position relative to the owning actor.
/// The renderer composes this with <c>actor.Transform.World</c> when drawing.
/// </summary>
public readonly record struct ModelPart(IMesh Mesh, Material Material, Matrix4X4<float> LocalTransform);
