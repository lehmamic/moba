namespace MOBA.Game.Models;

/// <summary>
/// Static <see cref="AttackProfile"/> lookups per actor role — the single
/// source of truth for "how hard does X hit, how often, at what range." Mirrors
/// the <c>MinionWaveSchedule</c> style: hard-coded constants, no DI, swap to
/// data-driven sheets once balance becomes a thing.
/// </summary>
public static class AttackProfiles
{
    private static readonly AttackProfile Melee =
        new(Range: 0.8f, Damage: 12f, Cooldown: 0.8f, ProjectileTravel: 0f);

    private static readonly AttackProfile Caster =
        new(Range: 3.0f, Damage: 18f, Cooldown: 0.8f, ProjectileTravel: 0.4f);

    private static readonly AttackProfile Siege =
        new(Range: 1.5f, Damage: 40f, Cooldown: 1.5f, ProjectileTravel: 0.7f);

    private static readonly AttackProfile Tower =
        new(Range: 6.0f, Damage: 70f, Cooldown: 1.0f, ProjectileTravel: 0f);

    private static readonly AttackProfile Nexus =
        new(Range: 5.0f, Damage: 90f, Cooldown: 1.4f, ProjectileTravel: 0f);

    private static readonly AttackProfile Champion =
        new(Range: 1.5f, Damage: 55f, Cooldown: 0.65f, ProjectileTravel: 0f);

    public static AttackProfile ForMinion(MinionType type) => type switch
    {
        MinionType.Caster => Caster,
        MinionType.Siege => Siege,
        _ => Melee,
    };

    public static AttackProfile ForBuilding(string type) => type switch
    {
        "Nexus" => Nexus,
        _ => Tower,
    };

    public static AttackProfile ForChampion() => Champion;
}
