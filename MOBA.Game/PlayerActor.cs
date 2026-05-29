using MOBA.Engine.Core;
using Silk.NET.Maths;

namespace MOBA.Game;

/// <summary>
/// A networked player character. Created on the server when a client sends
/// <see cref="Messages.JoinMessage"/>, replicated to all clients via
/// <see cref="Messages.ActorSpawnMessage"/> of kind
/// <see cref="Messages.ActorKind.Player"/>. The local client's player gets
/// a <c>LocalPlayerInputComponent</c> attached (client-side); remote players
/// stay visual + position-driven only.
/// </summary>
public sealed class PlayerActor : Actor
{
    public PlayerActor(Vector3D<float> spawnPosition)
    {
        Transform.Position = spawnPosition;
        _ = new TransformComponent(this);
    }
}
