using MOBA.Engine.Core.Abstractions;

namespace MOBA.Game.Components;

/// <summary>
/// Current / Max hit points of a destructible actor (towers, nexus, players,
/// future minions / monsters). Damage is applied authoritatively on the server
/// via <see cref="ApplyDamage"/> / <see cref="SetCurrent"/>; the client
/// displays the value via the client-only HP-bar billboard. <see cref="Ratio"/>
/// is clamped to [0, 1] so consumers don't need to guard against overshoot
/// from healing.
///
/// <para>
/// <see cref="Version"/> is a monotonic dirty bit incremented every time
/// <see cref="Current"/> changes through one of the mutators on this
/// component. The replication layer compares the value against a per-actor
/// "last sent" cache to decide whether to broadcast an
/// <c>ActorHealthMessage</c> this tick — same pattern as
/// <c>ReplicatedPathComponent.Version</c>.
/// </para>
/// </summary>
public sealed class HealthComponent : Component
{
    public HealthComponent(Actor owner, float max) : base(owner)
    {
        Max = max;
        Current = max;
    }

    public float Current { get; private set; }

    public float Max { get; }

    public float Ratio => Max > 0f ? Math.Clamp(Current / Max, 0f, 1f) : 0f;

    /// <summary>Monotonic counter — bumped on every <see cref="Current"/> mutation.</summary>
    public uint Version { get; private set; }

    /// <summary>
    /// Subtracts <paramref name="amount"/> from <see cref="Current"/>, clamped
    /// at 0. No-op once <see cref="Current"/> has reached 0 (a dead actor stays
    /// dead — let the despawn pipeline pick it up). Bumps <see cref="Version"/>
    /// only when <see cref="Current"/> actually changed, so the replication
    /// scan can't be tricked into broadcasting on a stale damage call.
    /// </summary>
    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || Current <= 0f)
        {
            return;
        }
        Current = Math.Max(0f, Current - amount);
        Version++;
    }

    /// <summary>
    /// Server → client setter used by <c>NetworkSyncSystem</c> when an
    /// <c>ActorHealthMessage</c> arrives. Mirrors the server's <see cref="Current"/>
    /// verbatim and bumps <see cref="Version"/> so any local observer driven by
    /// the dirty bit (e.g. the death-clip animation hook) ticks once per change.
    /// </summary>
    public void SetCurrent(float current)
    {
        var clamped = Math.Clamp(current, 0f, Max);
        if (clamped == Current)
        {
            return;
        }
        Current = clamped;
        Version++;
    }
}
