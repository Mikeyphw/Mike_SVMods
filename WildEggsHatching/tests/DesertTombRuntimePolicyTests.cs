using WildEggsHatching.Core.Enums;
using WildEggsHatching.DesertTomb.Core.Runtime;
using Xunit;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Refactor.Tests;

public sealed class DesertTombRuntimePolicyTests
{
    [Theory]
    [InlineData("Maps/Mines/Level77377", MineLevel.Level77377)]
    [InlineData("Maps\\Mines\\Level39", MineLevel.Level39)]
    [InlineData("Data/Mines/Level14", MineLevel.Level14)]
    [InlineData("data\\mines\\19", MineLevel.Level19)]
    [InlineData("Data/Mines/28.tmx", MineLevel.Level28)]
    public void TryParseMineLevel_ParsesKnownLayouts(string mapPath, MineLevel expected)
    {
        Assert.True(DesertTombRuntimePolicy.TryParseMineLevel(mapPath, out var level));
        Assert.Equal(expected, level);
    }

    [Theory]
    [InlineData(true, ProgressionStep.PartIII, false)]
    [InlineData(true, ProgressionStep.PartIV, true)]
    [InlineData(false, ProgressionStep.PartI, true)]
    public void IsEligibleByProgression_RespectsProgressionMode(bool progressionMode, ProgressionStep step, bool expected)
    {
        Assert.Equal(expected, DesertTombRuntimePolicy.IsEligibleByProgression(progressionMode, step));
    }

    [Theory]
    [InlineData(121, null, true)]
    [InlineData(5, "Maps/Mines/Level77377", true)]
    [InlineData(5, "Maps/Mines/SomeOtherMap", false)]
    public void IsTombCandidate_DetectsMineLevelOrMapPath(int mineLevel, string? mapPath, bool expected)
    {
        Assert.Equal(expected, DesertTombRuntimePolicy.IsTombCandidate(mineLevel, mapPath));
    }
}
