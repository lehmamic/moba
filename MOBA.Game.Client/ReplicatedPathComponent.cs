using MOBA.Engine.Core;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// Client-side snapshot of the most recent server-broadcast path for one
/// networked actor. Replaced wholesale every time a new <c>MovePathMessage</c>
/// arrives for this actor; <see cref="Version"/> increments on each write so
/// rendering systems can detect changes without diffing the waypoint list.
/// Read-only data — the client never pathfinds itself, the server is the
/// single source of truth.
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
