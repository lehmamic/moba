using MOBA.Engine.Core;
using Silk.NET.Maths;

namespace MOBA.Game;

/// <summary>
/// Visual destination marker. Plain transform + <see cref="NetworkIdentityComponent"/>;
/// the render-side <c>MeshRendererComponent</c> + sphere mesh + magenta material
/// are attached client-side in <c>NetworkSyncSystem</c>.
/// </summary>
public sealed class MarkerActor : Actor
{
    public MarkerActor(uint id, Vector3D<float> position)
    {
        Transform.Position = position;
        _ = new NetworkIdentityComponent(this, id);
    }
}
