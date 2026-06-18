using MOBA.Game.Scenes;
using Silk.NET.Maths;

namespace MOBA.Game.Models;

/// <summary>
/// Shared parsing helpers for <see cref="TransformDefinition"/>. Rotation
/// arrives as Euler degrees in <c>[pitch (X), yaw (Y), roll (Z)]</c> order;
/// built into a quaternion via <see cref="Quaternion{T}.CreateFromYawPitchRoll"/>.
/// </summary>
internal static class EntityTransform
{
    public static Vector3D<float> ParsePosition(TransformDefinition t) =>
        new(t.Position[0], t.Position[1], t.Position[2]);

    public static Vector3D<float> ParseScale(TransformDefinition t) =>
        new(t.Scale[0], t.Scale[1], t.Scale[2]);

    public static Quaternion<float> ParseRotation(TransformDefinition t)
    {
        var pitch = float.DegreesToRadians(t.Rotation[0]);
        var yaw = float.DegreesToRadians(t.Rotation[1]);
        var roll = float.DegreesToRadians(t.Rotation[2]);
        return Quaternion<float>.CreateFromYawPitchRoll(yaw, pitch, roll);
    }

    /// <summary>
    /// Strips the leading <c>assets/</c> and the file extension so the result is
    /// the key the unified model cache expects: e.g.
    /// <c>"assets/buildings/blue-tower.glb"</c> → <c>"buildings/blue-tower"</c>.
    /// </summary>
    public static string MeshAssetFromFile(string file)
    {
        var s = file.Replace('\\', '/');
        const string prefix = "assets/";
        if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            s = s[prefix.Length..];
        }
        var dotIdx = s.LastIndexOf('.');
        var slashIdx = s.LastIndexOf('/');
        if (dotIdx > slashIdx)
        {
            s = s[..dotIdx];
        }
        return s;
    }
}
