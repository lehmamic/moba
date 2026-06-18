using MOBA.Engine.Core.Abstractions;
using MOBA.Game.Components;
using Silk.NET.Maths;

namespace MOBA.Game.Actors;

/// <summary>
/// A networked player character. Created on the server when a client sends
/// <see cref="Messages.JoinMessage"/>, replicated to all clients via
/// <see cref="Messages.ActorSpawnMessage"/> of kind
/// <see cref="Messages.ActorKind.Player"/>. The local client's player gets
/// a <c>LocalPlayerInputComponent</c> attached (client-side); remote players
/// stay visual + position-driven only.
///
/// <para>
/// <see cref="Team"/> is the team name (<c>"Blue"</c> / <c>"Red"</c>) the
/// server assigned at join time. Used for spawn placement today; future
/// damage routing, projectile filtering and HUD colouring will read it too.
/// </para>
/// </summary>
public sealed class PlayerActor : Actor
{
    public PlayerActor(Vector3D<float> spawnPosition, string team)
    {
        Team = team;
        Transform.Position = spawnPosition;
        _ = new TransformComponent(this);
    }

    public string Team { get; }
}
