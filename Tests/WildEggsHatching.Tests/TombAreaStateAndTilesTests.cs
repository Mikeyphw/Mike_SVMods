using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WildEggsHatching.Application.DesertTomb.Spawning;
using Xunit;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Tests;

public sealed class TombAreaStateAndTilesTests
{
    [Fact]
    public void ComputeAreaId_IsStableAndSensitiveToInputs()
    {
        var rect = new Rectangle(1, 2, 10, 11);
        var id1 = TombAreaState.ComputeAreaId(rect, TombAreaType.NormalArea);
        var id2 = TombAreaState.ComputeAreaId(rect, TombAreaType.NormalArea);
        Assert.Equal(id1, id2);

        var idDifferentType = TombAreaState.ComputeAreaId(rect, TombAreaType.MummyArea);
        Assert.NotEqual(id1, idDifferentType);

        var rect2 = new Rectangle(1, 2, 10, 12);
        var idDifferentRect = TombAreaState.ComputeAreaId(rect2, TombAreaType.NormalArea);
        Assert.NotEqual(id1, idDifferentRect);
    }

    [Fact]
    public void IsFarEnough_RespectsMinSpacing()
    {
        var used = new List<Point> { new(0, 0) };

        Assert.False(TombSpawnTileCache.IsFarEnough(new Point(1, 0), used, minSpacingTiles: 2));
        Assert.True(TombSpawnTileCache.IsFarEnough(new Point(2, 0), used, minSpacingTiles: 2));
        Assert.True(TombSpawnTileCache.IsFarEnough(new Point(10, 10), used, minSpacingTiles: 2));
    }
}
