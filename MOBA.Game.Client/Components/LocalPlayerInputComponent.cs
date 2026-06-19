using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Input;
using MOBA.Engine.Networking;
using MOBA.Game.Actors;
using MOBA.Game.Client.Cameras;
using MOBA.Game.Client.Input;
using MOBA.Game.Components;
using MOBA.Game.Messages;
using MOBA.Game.Models;
using Silk.NET.Maths;

namespace MOBA.Game.Client.Components;

/// <summary>
/// Local-player input. Attached only to the player actor that represents the
/// local client (id assigned via <see cref="AssignLocalActorMessage"/>); remote
/// players never receive this component. Reacts to per-frame
/// <see cref="InputState"/> via <see cref="OnProcessInput"/>: on left-mouse-
/// just-pressed it unprojects the cursor onto the ground plane with
/// <see cref="MousePicker"/>, then chooses one of two commands:
/// <list type="bullet">
///   <item>If the cursor lands close enough to a networked enemy actor (alive,
///         different team), send an <see cref="AttackCommandMessage"/> with that
///         actor's network id.</item>
///   <item>Otherwise snap the hit to the nearest navmesh point and send a
///         <see cref="MoveCommandMessage"/>.</item>
/// </list>
/// The server re-validates both (it is authoritative); the client-side picks
/// exist for instant UI feedback. This component never mutates the owner's
/// transform.
/// </summary>
public sealed class LocalPlayerInputComponent : Component
{
    private readonly CameraSwitcher _cameras;
    private readonly INetTransport _transport;
    private readonly NavMesh _navMesh;
    private readonly Scene _scene;

    public LocalPlayerInputComponent(
        Actor owner,
        CameraSwitcher cameras,
        INetTransport transport,
        NavMesh navMesh,
        Scene scene)
        : base(owner)
    {
        _cameras = cameras;
        _transport = transport;
        _navMesh = navMesh;
        _scene = scene;
    }

    public override void OnProcessInput(InputState state)
    {
        if (!state.LeftMouseJustPressed)
        {
            return;
        }

        var hit = MousePicker.PickGround(state.MousePosition, state.FramebufferSize, _cameras.ActiveCamera);
        if (hit is not { } target)
        {
            return;
        }

        if (TryPickEnemy(target) is { } enemyId)
        {
            _transport.Send(NetChannel.Reliable, new AttackCommandMessage(enemyId).Serialize());
            return;
        }

        // Snap to the nearest navmesh poly so the click "feels" valid even if it
        // landed slightly into a tower / off-map / over a cliff. The server runs
        // the same snap on its copy of the navmesh, so both ends converge.
        if (!_navMesh.TryFindNearestPoint(target, out var snapped))
        {
            return;
        }
        _transport.Send(NetChannel.Reliable, new MoveCommandMessage(snapped.X, snapped.Z).Serialize());
    }

    /// <summary>
    /// Returns the network id of the closest alive enemy actor whose footprint
    /// covers the ground-pick point, or null if nothing was clicked. The
    /// per-kind <see cref="FootprintRadius"/> is the in-world click radius
    /// around the actor's origin — generous on buildings, tighter on minions.
    /// </summary>
    private uint? TryPickEnemy(Vector3D<float> groundHit)
    {
        var myTeam = Owner.GetComponent<TeamComponent>()?.Team;
        if (myTeam is null)
        {
            return null;
        }

        uint? bestId = null;
        var bestDistanceSq = float.MaxValue;
        foreach (var actor in _scene.Actors)
        {
            if (actor.GetComponent<NetworkIdentityComponent>() is not { } netId
                || actor.GetComponent<HealthComponent>() is not { Current: > 0f })
            {
                continue;
            }
            var team = actor.GetComponent<TeamComponent>()?.Team;
            if (team is null || string.Equals(team, myTeam, StringComparison.Ordinal))
            {
                continue;
            }

            var dx = actor.Transform.Position.X - groundHit.X;
            var dz = actor.Transform.Position.Z - groundHit.Z;
            var distSq = (dx * dx) + (dz * dz);
            var footprint = FootprintRadius(actor);

            if (distSq > footprint * footprint || distSq >= bestDistanceSq)
            {
                continue;
            }

            bestId = netId.Id;
            bestDistanceSq = distSq;
        }

        return bestId;
    }

    private static float FootprintRadius(Actor actor) => actor switch
    {
        BuildingActor b when string.Equals(b.Definition.Type, "Nexus", StringComparison.Ordinal) => 5f,
        BuildingActor => 3f,
        PlayerActor => 1.5f,
        MinionActor => 1.0f,
        _ => 1.5f,
    };
}
