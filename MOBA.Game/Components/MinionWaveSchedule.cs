using MOBA.Game.Models;

namespace MOBA.Game.Components;

/// <summary>
/// Pure, deterministic LoL-style minion-wave cadence. First wave at
/// <see cref="FirstWaveTime"/> (1:05), then one every <see cref="WaveInterval"/>
/// seconds. Each lane gets <see cref="MeleePerLane"/> melee + <see cref="CasterPerLane"/>
/// caster minions every wave; a siege minion joins on a cadence that tightens
/// over the match. Kept side-effect-free so it is unit-testable on its own.
/// </summary>
public static class MinionWaveSchedule
{
    public const float FirstWaveTime = 65f;
    public const float WaveInterval = 30f;
    public const int MeleePerLane = 3;
    public const int CasterPerLane = 3;

    /// <summary>Scheduled sim-time (seconds) at which wave <paramref name="waveNumber"/> (1-based) spawns.</summary>
    public static float WaveTime(int waveNumber) => FirstWaveTime + (waveNumber - 1) * WaveInterval;

    /// <summary>
    /// Whether wave <paramref name="waveNumber"/> includes a siege minion:
    /// every 3rd wave before 15:00, every 2nd before 25:00, every wave after.
    /// </summary>
    public static bool HasSiege(int waveNumber)
    {
        var t = WaveTime(waveNumber);
        if (t < 900f)
        {
            return waveNumber % 3 == 0;
        }
        if (t < 1500f)
        {
            return waveNumber % 2 == 0;
        }
        return true;
    }

    /// <summary>The minion roles spawned per lane for the given wave, in spawn order.</summary>
    public static IReadOnlyList<MinionType> LaneComposition(int waveNumber)
    {
        var composition = new List<MinionType>(MeleePerLane + CasterPerLane + 1);
        for (var i = 0; i < MeleePerLane; i++)
        {
            composition.Add(MinionType.Melee);
        }
        for (var i = 0; i < CasterPerLane; i++)
        {
            composition.Add(MinionType.Caster);
        }
        if (HasSiege(waveNumber))
        {
            composition.Add(MinionType.Siege);
        }
        return composition;
    }
}
