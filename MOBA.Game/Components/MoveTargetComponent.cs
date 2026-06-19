using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;
using Silk.NET.Maths;

namespace MOBA.Game.Components;

/// <summary>
/// Movement intent on an actor: the navmesh-following waypoint queue, walking
/// speed, and the destination marker the server has linked to this move. The
/// per-tick movement physics lives here in <see cref="OnUpdate"/> — the actor's
/// <c>UpdateComponents</c> ticks this component and walks the owner's
/// <see cref="Actor.Transform"/> along the queue. Networking (broadcasting
/// position updates, despawning the marker on arrival, sending the path
/// snapshot to all clients) is the concern of <c>MovementSystem</c>,
/// which inspects this component's state in its
/// <c>IPostUpdateSystem.OnPostUpdate</c> after the sim has run.
///
/// <para>
/// The component is intentionally narrow: it walks whatever queue is set.
/// Higher-level orchestration (e.g. "switch from lane-walking to chasing a
/// combat target") lives in the component that owns that decision —
/// <c>AttackComponent</c> for combat — and is expressed by calling
/// <see cref="StashPath"/>, then <see cref="SetPath"/> with the new corridor,
/// then <see cref="RestorePath"/> when the higher-level goal goes away.
/// Single-slot push/pop is enough for our use case (one lane goal interrupted
/// by one combat episode at a time).
/// </para>
/// </summary>
public sealed class MoveTargetComponent : Component
{
    public const float ArrivalThreshold = 0.1f;

    private readonly Queue<Vector3D<float>> _path = new();
    private Queue<Vector3D<float>>? _stashedPath;

    public MoveTargetComponent(Actor owner, float speed = 10f) : base(owner) =>
        Speed = speed;

    public uint? MarkerId { get; set; }

    public float Speed { get; }

    /// <summary>True while the queue has at least one waypoint left to walk to.</summary>
    public bool HasPath => _path.Count > 0;

    /// <summary>True while the live queue is empty and a stashed lane queue is waiting to be resumed.</summary>
    public bool HasStashedPath => _stashedPath is not null;

    /// <summary>
    /// Replaces the current waypoint queue with <paramref name="waypoints"/>.
    /// Used by <c>MovementSystem.HandleMoveCommand</c> after a successful
    /// <c>NavMesh.TryFindPath</c>, and by combat orchestrators when they
    /// push a fresh chase corridor.
    /// </summary>
    public void SetPath(IReadOnlyList<Vector3D<float>> waypoints)
    {
        _path.Clear();
        foreach (var w in waypoints)
        {
            _path.Enqueue(w);
        }
    }

    /// <summary>Empties the live queue without touching the stash. Used by callers that want the actor to stand still.</summary>
    public void ClearPath() => _path.Clear();

    /// <summary>
    /// Copies the live waypoint queue into a single-slot stash and clears the
    /// live queue. Caller pairs this with <see cref="RestorePath"/> when the
    /// higher-level goal (e.g. combat chase) ends. A second stash before the
    /// first restore overwrites silently — single-slot by design.
    /// </summary>
    public void StashPath()
    {
        _stashedPath = new Queue<Vector3D<float>>(_path);
        _path.Clear();
    }

    /// <summary>
    /// Pops the stashed queue back into the live queue, picking up walking
    /// from exactly the waypoint that was next when the stash happened.
    /// No-op if nothing was stashed.
    /// </summary>
    public void RestorePath()
    {
        if (_stashedPath is null)
        {
            return;
        }
        _path.Clear();
        foreach (var w in _stashedPath)
        {
            _path.Enqueue(w);
        }
        _stashedPath = null;
    }

    public override void OnUpdate(GameTime time)
    {
        if (_path.Count == 0)
        {
            return;
        }

        // Walk multiple waypoints per tick if speed * dt overshoots the next
        // corner — otherwise tight corner sequences would visibly stutter as
        // each waypoint takes a full tick to consume.
        var remaining = Speed * time.DeltaSeconds;
        var position = Owner.Transform.Position;
        while (_path.Count > 0 && remaining > 0f)
        {
            var next = _path.Peek();
            var delta = next - position;
            var distance = delta.Length;
            if (distance <= ArrivalThreshold || remaining >= distance)
            {
                position = next;
                _path.Dequeue();
                remaining -= distance;
            }
            else
            {
                position += Vector3D.Normalize(delta) * remaining;
                remaining = 0f;
            }
        }
        Owner.Transform.Position = position;

        // Face the next waypoint (or the current direction of travel if we
        // arrived mid-tick). The actor's bind-pose forward is +X (see
        // Transform XML doc); we want a Y-axis rotation that takes +X to
        // (dx, 0, dz). Silk's Y-rotation in row-vector form sends
        // (1, 0, 0) → (cos yaw, 0, -sin yaw), so yaw = atan2(-deltaZ, deltaX).
        if (_path.Count > 0)
        {
            var look = _path.Peek() - position;
            if (look.X != 0f || look.Z != 0f)
            {
                var yaw = MathF.Atan2(-look.Z, look.X);
                Owner.Transform.Rotation = Quaternion<float>.CreateFromYawPitchRoll(yaw, 0f, 0f);
            }
        }
    }
}
