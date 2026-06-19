namespace MOBA.Game.Models;

/// <summary>
/// Per-actor basic-attack tuning: how close you have to be (<see cref="Range"/>),
/// how often you can fire (<see cref="Cooldown"/>), how hard each shot lands
/// (<see cref="Damage"/>), and — for ranged attackers — how long the
/// projectile-equivalent damage takes to arrive (<see cref="ProjectileTravel"/>;
/// melee leaves it at 0). One record covers minions, towers, nexus and
/// champions — only the values differ.
/// </summary>
public readonly record struct AttackProfile(
    float Range,
    float Damage,
    float Cooldown,
    float ProjectileTravel);
