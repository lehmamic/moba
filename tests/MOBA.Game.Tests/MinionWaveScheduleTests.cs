using MOBA.Game.Components;
using MOBA.Game.Models;
using Xunit;

namespace MOBA.Game.Tests;

public class MinionWaveScheduleTests
{
    [Theory]
    [InlineData(1, 65f)]
    [InlineData(2, 95f)]
    [InlineData(3, 125f)]
    public void WaveTime_is_first_wave_plus_interval(int wave, float expected) =>
        Assert.Equal(expected, MinionWaveSchedule.WaveTime(wave));

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(6, true)]
    public void HasSiege_every_third_wave_before_15min(int wave, bool expected) =>
        Assert.Equal(expected, MinionWaveSchedule.HasSiege(wave));

    [Fact]
    public void HasSiege_tightens_to_every_second_wave_after_15min()
    {
        // wave 29 -> 905s (>=900, <1500), odd -> none; wave 30 -> even -> siege.
        Assert.False(MinionWaveSchedule.HasSiege(29));
        Assert.True(MinionWaveSchedule.HasSiege(30));
    }

    [Fact]
    public void HasSiege_every_wave_after_25min()
    {
        // wave 49 -> 1505s (>=1500) -> every wave.
        Assert.True(MinionWaveSchedule.HasSiege(49));
        Assert.True(MinionWaveSchedule.HasSiege(50));
    }

    [Fact]
    public void LaneComposition_wave1_is_three_melee_three_caster_no_siege()
    {
        var composition = MinionWaveSchedule.LaneComposition(1);
        Assert.Equal(6, composition.Count);
        Assert.Equal(3, composition.Count(t => t == MinionType.Melee));
        Assert.Equal(3, composition.Count(t => t == MinionType.Caster));
        Assert.DoesNotContain(MinionType.Siege, composition);
    }

    [Fact]
    public void LaneComposition_siege_wave_adds_one_siege()
    {
        var composition = MinionWaveSchedule.LaneComposition(3);
        Assert.Equal(7, composition.Count);
        Assert.Single(composition, t => t == MinionType.Siege);
    }
}
