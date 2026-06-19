using MOBA.Engine.Core.Abstractions;
using MOBA.Engine.Core.Hosting;

namespace MOBA.Game.Components;

/// <summary>
/// Removes the owning actor from the scene a short while after its
/// <see cref="HealthComponent"/> hits zero, so the client has time to play
/// the death animation before the despawn message arrives. The hold window
/// (<see cref="DeathHoldSeconds"/>) is tuned to the longest death clip on
/// the assets we ship today; bump it once a longer clip lands.
///
/// <para>
/// Attached only to actors that are expected to vanish on death (minions
/// today; towers / nexus when match-end logic lands). Players intentionally
/// do <b>not</b> wear it — they stay at HP 0 with the defeated pose until a
/// future respawn iteration.
/// </para>
/// </summary>
public sealed class DespawnOnDeathComponent : Component
{
    public const float DeathHoldSeconds = 1.2f;

    private readonly Scene _scene;
    private double? _deathAt;

    public DespawnOnDeathComponent(Actor owner, Scene scene) : base(owner) =>
        _scene = scene;

    public override void OnUpdate(GameTime time)
    {
        if (Owner.GetComponent<HealthComponent>() is not { Current: <= 0f })
        {
            return;
        }

        _deathAt ??= time.TotalSeconds;
        if (time.TotalSeconds - _deathAt.Value >= DeathHoldSeconds)
        {
            // Safe to call mid-Update — Scene queues the removal until after
            // the iteration finishes.
            _scene.RemoveActor(Owner);
        }
    }
}
