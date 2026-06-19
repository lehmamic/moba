using MOBA.Engine.Core.Abstractions;

namespace MOBA.Game.Components;

/// <summary>
/// Current / Max hit points of a destructible actor (towers, nexus, players,
/// future minions / monsters). Damage is applied authoritatively on the server;
/// the client displays the value via the client-only
/// <c>HealthBarBillboardComponent</c>. <see cref="Ratio"/> is clamped to [0, 1]
/// so consumers don't need to guard against overshoot from healing.
/// </summary>
public sealed class HealthComponent : Component
{
    public HealthComponent(Actor owner, float max) : base(owner)
    {
        Max = max;
        Current = max;
    }

    public float Current { get; set; }

    public float Max { get; }

    public float Ratio => Max > 0f ? Math.Clamp(Current / Max, 0f, 1f) : 0f;
}
