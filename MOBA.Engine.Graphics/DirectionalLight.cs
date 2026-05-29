using Silk.NET.Maths;

namespace MOBA.Engine.Graphics;

/// <summary>
/// One scene-wide directional light. <see cref="Direction"/> is the unit vector
/// pointing <em>toward</em> the light source (i.e. opposite the photon travel
/// direction). For a sun overhead, that's roughly +Y.
/// </summary>
public readonly record struct DirectionalLight(
    Vector3D<float> Direction,
    Vector3D<float> Color,
    Vector3D<float> AmbientColor,
    float SpecularStrength,
    float Shininess);
