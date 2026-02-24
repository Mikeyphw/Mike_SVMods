using EggsInWild.Core.Enums;
using EggsInWild.DesertTomb.Core.Runtime;
using Xunit;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;
using EggsInWild.Core.Assets;

namespace EggsInWild.Tests;

public sealed class DesertTombRuntimePolicyTests
{
    [Theory]
    [InlineData(WeggGameAssetNames.DesertTombMap, MineLevel.Level77377)]
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
    [InlineData(true, false, ProgressionStep.PartIII, false)]
    [InlineData(true, false, ProgressionStep.PartIV, true)]
    [InlineData(true, false, ProgressionStep.PartV, false)]
    [InlineData(true, true, ProgressionStep.PartIV, true)]
    [InlineData(true, true, ProgressionStep.PartV, true)]
    [InlineData(false, false, ProgressionStep.PartI, true)]
    public void IsEligibleByProgression_RespectsProgressionModeAndKeepMineLevel(bool progressionMode, bool keepMineLevel, ProgressionStep step, bool expected)
    {
        Assert.Equal(expected, DesertTombRuntimePolicy.IsEligibleByProgression(progressionMode, keepMineLevel, step));
    }


    [Theory]
    [InlineData(121, true)]
    [InlineData(120, false)]
    [InlineData(5, false)]
    public void IsTombCandidate_RequiresMineLevelAbove120(int mineLevel, bool expected)
    {
        Assert.Equal(expected, DesertTombRuntimePolicy.IsTombCandidate(mineLevel));
    }
}