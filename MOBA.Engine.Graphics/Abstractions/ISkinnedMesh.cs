namespace MOBA.Engine.Graphics.Abstractions;

/// <summary>
/// A GPU mesh whose vertices carry skinning information (bone indices + weights).
/// Backend-agnostic handle, dispatched by the renderer to the skinning draw path.
/// </summary>
public interface ISkinnedMesh : IMesh
{
}
