using System.Text.Json.Nodes;
using MOBA.Engine.Core;
using MOBA.Utilities;
using Silk.NET.Maths;
using SharpGLTF.Schema2;

namespace MOBA.Engine.Graphics;

/// <summary>
/// Loads glTF 2.0 binary models (<c>.glb</c>) via SharpGLTF and assembles them into a
/// <see cref="Model"/>. Each glTF mesh primitive becomes one <see cref="ModelPart"/>
/// (mesh + material). The model is read in bind pose only — skeletal joints, skin
/// weights, and animation channels are ignored (those land in later iterations).
/// Materials reference a shader by short name via <see cref="AssetManager"/>'s shader
/// cache; asset files are never allowed to ship their own GLSL.
/// </summary>
internal static class GltfModelLoader
{
    private const string DefaultShaderName = "phong_textured";

    public static Model Load(
        AbsolutePath filePath,
        IGraphicsBackend backend,
        AssetManager assets)
    {
        var modelRoot = ModelRoot.Load(filePath);
        var parts = new List<ModelPart>();

        foreach (var mesh in modelRoot.LogicalMeshes)
        {
            foreach (var prim in mesh.Primitives)
            {
                var vertices = ExtractVertices(prim);
                var indices = ExtractIndices(prim);
                var glMesh = backend.CreateMesh(vertices, indices);
                var material = BuildMaterial(prim.Material, backend, assets);
                parts.Add(new ModelPart(glMesh, material));
            }
        }

        if (parts.Count == 0)
        {
            throw new InvalidDataException($"glTF file '{filePath}' contains no mesh primitives.");
        }

        return new Model(parts);
    }

    private static Vertex[] ExtractVertices(MeshPrimitive primitive)
    {
        var positionsAcc = primitive.GetVertexAccessor("POSITION")
            ?? throw new InvalidDataException("glTF primitive lacks POSITION accessor.");
        var positions = positionsAcc.AsVector3Array();
        var uvs = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
        var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();

        var vertices = new Vertex[positions.Count];
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            var uv = uvs is not null ? uvs[i] : new System.Numerics.Vector2(0, 0);
            var n = normals is not null ? normals[i] : new System.Numerics.Vector3(0, 1, 0);
            vertices[i] = new Vertex(
                new Vector3D<float>(p.X, p.Y, p.Z),
                new Vector2D<float>(uv.X, uv.Y),
                new Vector3D<float>(n.X, n.Y, n.Z));
        }
        return vertices;
    }

    private static uint[] ExtractIndices(MeshPrimitive primitive)
    {
        var src = primitive.GetIndices();
        var dst = new uint[src.Count];
        for (var i = 0; i < src.Count; i++)
        {
            dst[i] = src[i];
        }
        return dst;
    }

    private static Material BuildMaterial(
        SharpGLTF.Schema2.Material? gltfMaterial,
        IGraphicsBackend backend,
        AssetManager assets)
    {
        var shaderKey = ResolveShaderKey(gltfMaterial);
        var shader = assets.LoadShader(shaderKey);
        var texture = ExtractBaseColorTexture(gltfMaterial, backend);
        return new Material(shader, texture);
    }

    private static string ResolveShaderKey(SharpGLTF.Schema2.Material? gltfMaterial)
    {
        if (gltfMaterial?.Extras is JsonObject obj
            && obj.TryGetPropertyValue("shader", out var node)
            && node is JsonValue val
            && val.TryGetValue<string>(out var key))
        {
            return key;
        }
        return DefaultShaderName;
    }

    private static ITexture? ExtractBaseColorTexture(
        SharpGLTF.Schema2.Material? gltfMaterial,
        IGraphicsBackend backend)
    {
        var channel = gltfMaterial?.FindChannel("BaseColor");
        var image = channel?.Texture?.PrimaryImage;
        if (image is null)
        {
            return null;
        }
        var encoded = image.Content.Content.Span;
        if (encoded.IsEmpty)
        {
            return null;
        }
        // glTF 2.0 spec: texture coordinate origin is the upper-left corner of the source
        // image. The procedural mesh pipeline (PNGs viewed via image tools) uses
        // bottom-left UVs and asks TextureLoader to flip the pixel rows so the image
        // displays "right-side-up". For glTF we must NOT flip — otherwise the V axis
        // is inverted and the texture atlas appears scrambled on the model.
        var decoded = TextureLoader.LoadRgba(encoded, flipVertically: false);
        return backend.CreateTexture(decoded.Pixels, decoded.Width, decoded.Height);
    }
}
