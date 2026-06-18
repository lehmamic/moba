using MOBA.Engine.Core.World;
using Silk.NET.Maths;

namespace MOBA.Game.Components;

/// <summary>
/// Snapshot of the most recent server-broadcast path for one networked actor.
/// Populated on the client by <c>NetworkSyncSystem</c> when a
/// <c>MovePathMessage</c> arrives; replaced wholesale on each write and
/// <see cref="Version"/> increments so rendering systems can detect changes
/// without diffing the waypoint list. Lives in MOBA.Game (not the client
/// project) because the data shape — read-only path snapshot — is not
/// rendering-specific and a future minimap or server-side replay tool may
/// consume the same component.
/// </summary>
public sealed class ReplicatedPathComponent : Component
{
    public ReplicatedPathComponent(Actor owner) : base(owner) { }

    public IReadOnlyList<Vector3D<float>> Waypoints { get; private set; } = [];

    /// <summary>Bumped on every <see cref="SetWaypoints"/>. Used as a dirty marker by overlays.</summary>
    public uint Version { get; private set; }

    public void SetWaypoints(IReadOnlyList<Vector3D<float>> waypoints)
    {
        Waypoints = waypoints;
        Version++;
    }
}
