using System.Text.Json.Nodes;
using MOBA.Engine.Core.Assets;
using MOBA.Utilities;
using SharpGLTF.Schema2;
using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

/// <summary>
/// Loads glTF 2.0 binary models (<c>.glb</c>) via SharpGLTF and assembles them into a
/// <see cref="Model"/>. Each glTF mesh primitive becomes one <see cref="ModelPart"/>;
/// when a primitive has a skin, its vertices use <see cref="SkinnedVertex"/> + an
/// <see cref="ISkinnedMesh"/> and the model carries a <see cref="Skeleton"/> + a
/// dictionary of <see cref="Animation"/> clips resampled at <see cref="TargetFps"/>.
/// Materials reference a shader by short name via <see cref="AssetManager"/>'s shader
/// cache; asset files never ship their own GLSL.
/// </summary>
internal static class GltfModelLoader
{
    private const string DefaultStaticShaderName = "phong_textured";
    private const string DefaultSkinnedShaderName = "phong_skinned";
    private const int TargetFps = 30;

    public static Model Load(
        AbsolutePath filePath,
        IGraphicsBackend backend,
        AssetManager assets)
    {
        var modelRoot = ModelRoot.Load(filePath);
        var parts = new List<ModelPart>();
        Skeleton? skeleton = null;
        Skin? gltfSkin = null;

        // Iterate scene-graph nodes (not just unique meshes). A node carries the
        // world matrix that positions its mesh inside the model — for multi-part
        // assets like the dimension-rift terrain this is the only place the
        // per-instance offset/rotation lives. The same LogicalMesh can be
        // referenced by many nodes (repeated tower instances etc.); each becomes
        // its own ModelPart with its own LocalTransform sharing the same IMesh
        // is not yet supported by the backend so we re-upload, but that's a
        // memory optimisation for later.
        foreach (var node in modelRoot.LogicalNodes)
        {
            var mesh = node.Mesh;
            if (mesh is null)
            {
                continue;
            }

            var skin = node.Skin;
            var nodeWorld = ToSilk(node.WorldMatrix);

            foreach (var prim in mesh.Primitives)
            {
                var primIsSkinned = skin != null && prim.GetVertexAccessor("JOINTS_0") != null;

                IMesh glMesh;
                bool partIsSkinned;
                Matrix4X4<float> partTransform;
                if (primIsSkinned)
                {
                    // Skinned primitives are positioned by the skeleton's bone
                    // matrices, not the node's world matrix — applying it here
                    // would double-transform every vertex.
                    glMesh = backend.CreateSkinnedMesh(ExtractSkinnedVertices(prim), ExtractIndices(prim));
                    partIsSkinned = true;
                    partTransform = Matrix4X4<float>.Identity;
                    gltfSkin ??= skin;
                }
                else
                {
                    glMesh = backend.CreateMesh(ExtractVertices(prim), ExtractIndices(prim));
                    partIsSkinned = false;
                    partTransform = nodeWorld;
                }

                var material = BuildMaterial(prim.Material, backend, assets, partIsSkinned);
                parts.Add(new ModelPart(glMesh, material, partTransform));
            }
        }

        if (parts.Count == 0)
        {
            throw new InvalidDataException($"glTF file '{filePath}' contains no mesh primitives.");
        }

        IReadOnlyDictionary<string, Animation>? animations = null;
        if (gltfSkin != null)
        {
            skeleton = BuildSkeleton(gltfSkin);
            animations = BuildAnimations(modelRoot.LogicalAnimations, gltfSkin, skeleton);
        }

        return new Model(parts, skeleton, animations);
    }

    // ─── Vertex / index extraction ────────────────────────────────────────────────

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

    private static SkinnedVertex[] ExtractSkinnedVertices(MeshPrimitive primitive)
    {
        var positionsAcc = primitive.GetVertexAccessor("POSITION")
            ?? throw new InvalidDataException("glTF primitive lacks POSITION accessor.");
        var positions = positionsAcc.AsVector3Array();
        var uvs = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
        var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
        var joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array()
            ?? throw new InvalidDataException("glTF skinned primitive lacks JOINTS_0 accessor.");
        var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array()
            ?? throw new InvalidDataException("glTF skinned primitive lacks WEIGHTS_0 accessor.");

        var verts = new SkinnedVertex[positions.Count];
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            var uv = uvs is not null ? uvs[i] : new System.Numerics.Vector2(0, 0);
            var n = normals is not null ? normals[i] : new System.Numerics.Vector3(0, 1, 0);
            var j = joints[i];
            var w = weights[i];

            verts[i] = new SkinnedVertex(
                Position: new Vector3D<float>(p.X, p.Y, p.Z),
                Normal: new Vector3D<float>(n.X, n.Y, n.Z),
                BonePacked: SkinnedVertex.PackBoneIndices((byte)j.X, (byte)j.Y, (byte)j.Z, (byte)j.W),
                BoneWeights: new Vector4D<float>(w.X, w.Y, w.Z, w.W),
                Uv: new Vector2D<float>(uv.X, uv.Y));
        }
        return verts;
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

    // ─── Material ─────────────────────────────────────────────────────────────────

    private static Material BuildMaterial(
        SharpGLTF.Schema2.Material? gltfMaterial,
        IGraphicsBackend backend,
        AssetManager assets,
        bool isSkinned)
    {
        var shaderKey = ResolveShaderKey(gltfMaterial, isSkinned);
        var shader = assets.LoadShader(shaderKey);
        var texture = ExtractBaseColorTexture(gltfMaterial, backend);
        return new Material(shader, texture);
    }

    private static string ResolveShaderKey(SharpGLTF.Schema2.Material? gltfMaterial, bool isSkinned)
    {
        if (gltfMaterial?.Extras is JsonObject obj
            && obj.TryGetPropertyValue("shader", out var node)
            && node is JsonValue val
            && val.TryGetValue<string>(out var key))
        {
            return key;
        }
        return isSkinned ? DefaultSkinnedShaderName : DefaultStaticShaderName;
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
        // image, the same as PNG/JPG byte layout, so no flip is needed.
        var decoded = TextureLoader.LoadRgba(encoded, flipVertically: false);
        return backend.CreateTexture(decoded.Pixels, decoded.Width, decoded.Height);
    }

    // ─── Skeleton + animations ────────────────────────────────────────────────────

    private static Skeleton BuildSkeleton(Skin gltfSkin)
    {
        var jointsCount = gltfSkin.JointsCount;

        // Map each joint Node back to its index in this skin's joint list.
        var nodeToIdx = new Dictionary<Node, int>();
        for (var i = 0; i < jointsCount; i++)
        {
            nodeToIdx[gltfSkin.GetJoint(i).Joint] = i;
        }

        var bones = new Skeleton.Bone[jointsCount];
        var globalInvBindPoses = new Matrix4X4<float>[jointsCount];

        for (var i = 0; i < jointsCount; i++)
        {
            var jt = gltfSkin.GetJoint(i);
            var joint = jt.Joint;
            var invBind = jt.InverseBindMatrix;

            // Find the parent joint index by walking up the node tree until we hit
            // another joint of the same skin. -1 = no parent (the skeleton root).
            var parentIdx = -1;
            var parent = joint.VisualParent;
            while (parent != null)
            {
                if (nodeToIdx.TryGetValue(parent, out var p))
                {
                    parentIdx = p;
                    break;
                }
                parent = parent.VisualParent;
            }

            var localTransform = joint.LocalTransform;
            var localBindPose = new BoneTransform(
                Rotation: ToSilk(localTransform.Rotation),
                Translation: ToSilk(localTransform.Translation));

            bones[i] = new Skeleton.Bone(joint.Name ?? $"bone_{i}", parentIdx, localBindPose);
            globalInvBindPoses[i] = ToSilk(invBind);
        }

        return new Skeleton(bones, globalInvBindPoses);
    }

    private static IReadOnlyDictionary<string, Animation> BuildAnimations(
        IReadOnlyList<SharpGLTF.Schema2.Animation> gltfAnimations,
        Skin gltfSkin,
        Skeleton skeleton)
    {
        var result = new Dictionary<string, Animation>(gltfAnimations.Count);
        for (var i = 0; i < gltfAnimations.Count; i++)
        {
            var a = gltfAnimations[i];
            var name = string.IsNullOrEmpty(a.Name) ? $"clip_{i}" : a.Name;
            result[name] = ResampleAnimation(a, gltfSkin, skeleton);
        }
        return result;
    }

    private static Animation ResampleAnimation(
        SharpGLTF.Schema2.Animation gltfAnim,
        Skin gltfSkin,
        Skeleton skeleton)
    {
        var duration = (float)gltfAnim.Duration;
        if (duration <= 0f)
        {
            duration = 1f / TargetFps;
        }
        var numFrames = Math.Max(2, (int)Math.Ceiling(duration * TargetFps) + 1);
        var frameDuration = duration / (numFrames - 1);

        var nodeToIdx = new Dictionary<Node, int>();
        for (var i = 0; i < gltfSkin.JointsCount; i++)
        {
            nodeToIdx[gltfSkin.GetJoint(i).Joint] = i;
        }

        // Per-joint sampler caches for translation and rotation. We ignore scale —
        // matches the BoneTransform shape (rotation + translation only) and the
        // book's chapter 12 approach.
        var transSamplers = new SharpGLTF.Animations.ICurveSampler<System.Numerics.Vector3>?[skeleton.NumBones];
        var rotSamplers = new SharpGLTF.Animations.ICurveSampler<System.Numerics.Quaternion>?[skeleton.NumBones];

        foreach (var ch in gltfAnim.Channels)
        {
            if (ch.TargetNode is null || !nodeToIdx.TryGetValue(ch.TargetNode, out var jointIdx))
            {
                continue;
            }
            // scale + weights channels are ignored on purpose — BoneTransform
            // tracks only rotation + translation (matches ch.12 in the book).
            switch (ch.TargetNodePath)
            {
                case PropertyPath.translation:
                    transSamplers[jointIdx] = ch.GetTranslationSampler().CreateCurveSampler();
                    break;
                case PropertyPath.rotation:
                    rotSamplers[jointIdx] = ch.GetRotationSampler().CreateCurveSampler();
                    break;
            }
        }

        // Allocate flat track storage. Pre-fill each bone's track with its bind
        // pose so bones without channels animate to a sensible default.
        var tracks = new BoneTransform[skeleton.NumBones][];
        for (var bone = 0; bone < skeleton.NumBones; bone++)
        {
            tracks[bone] = new BoneTransform[numFrames];
            var bindPose = skeleton.Bones[bone].LocalBindPose;
            for (var f = 0; f < numFrames; f++)
            {
                var time = f * frameDuration;
                var translation = transSamplers[bone] is { } ts
                    ? ToSilk(ts.GetPoint(time))
                    : bindPose.Translation;
                var rotation = rotSamplers[bone] is { } rs
                    ? ToSilk(rs.GetPoint(time))
                    : bindPose.Rotation;
                tracks[bone][f] = new BoneTransform(rotation, translation);
            }
        }

        var tracksRo = new IReadOnlyList<BoneTransform>[skeleton.NumBones];
        for (var bone = 0; bone < skeleton.NumBones; bone++)
        {
            tracksRo[bone] = tracks[bone];
        }
        return new Animation(gltfAnim.Name ?? string.Empty, skeleton.NumBones, numFrames, duration, tracksRo);
    }

    // ─── Conversions between System.Numerics (SharpGLTF) and Silk.NET.Maths ───────

    private static Vector3D<float> ToSilk(System.Numerics.Vector3 v) =>
        new(v.X, v.Y, v.Z);

    private static Quaternion<float> ToSilk(System.Numerics.Quaternion q) =>
        new(q.X, q.Y, q.Z, q.W);

    private static Matrix4X4<float> ToSilk(System.Numerics.Matrix4x4 m) =>
        new(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44);
}
