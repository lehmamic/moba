using Silk.NET.Maths;

namespace MOBA.Engine.Graphics.Abstractions;

/// <summary>
/// Camera-facing screen-aligned billboard with a horizontal fill segment —
/// the primitive behind every MOBA-style health bar above an actor's head.
/// The renderer's third pass walks these and submits one draw per visible
/// instance via <see cref="IGraphicsBackend.DrawBillboardInPass"/>. Position
/// is implicit: the owning actor's world translation plus
/// <see cref="WorldOffset"/>; the quad is rotated into the camera plane by
/// the shader, not by the actor's rotation.
/// </summary>
public interface IBillboardRenderable
{
    bool IsVisible => true;

    IMesh Mesh { get; }

    IShader Shader { get; }

    /// <summary>Local-space offset added to the owner's world translation.</summary>
    Vector3D<float> WorldOffset { get; }

    /// <summary>Width / height of the billboard in world units.</summary>
    Vector2D<float> SizeWorldUnits { get; }

    /// <summary>Fraction of the bar (along U) filled with <see cref="FillColor"/>; the rest uses <see cref="BackgroundColor"/>.</summary>
    float FillRatio { get; }

    Vector3D<float> FillColor { get; }

    Vector3D<float> BackgroundColor { get; }

    Vector3D<float> OutlineColor { get; }

    /// <summary>Outline thickness as a fraction of the bar (≈ 0.05 = 5 % rim on each edge).</summary>
    float OutlineWidthFraction { get; }
}
