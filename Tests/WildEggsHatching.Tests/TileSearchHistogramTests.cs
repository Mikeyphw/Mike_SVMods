using WildEggsHatching.Application.Spawning;
using WildEggsHatching.Application.Spawning.Diagnostics;
using Xunit;

namespace WildEggsHatching.Tests;

public sealed class TileSearchHistogramTests
{
    [Fact]
    public void TopRejections_UsesFailureOnlyAggregation_SuccessesDoNotAffectCounts()
    {
        var h = new TileSearchHistogram();

        var failure = new TileSearchStats(
            Attempts: 10,
            RejectedOutOfBounds: 3,
            RejectedExcludedRect: 2,
            RejectedTerrain: 1,
            RejectedWater: 0,
            RejectedOccupied: 4,
            RejectedImpassable: 0);

        var success = new TileSearchStats(
            Attempts: 5,
            RejectedOutOfBounds: 999,
            RejectedExcludedRect: 999,
            RejectedTerrain: 999,
            RejectedWater: 999,
            RejectedOccupied: 999,
            RejectedImpassable: 999);

        h.Record("Forest", "EggA", failure, foundTile: false);
        h.Record("Forest", "EggA", success, foundTile: true);

        var top = h.GetTopRejections("Forest", "EggA", topN: 3);

        Assert.Equal(3, top.Count);
        Assert.Equal(("occupied", 4), top[0]);
        Assert.Equal(("outOfBounds", 3), top[1]);
        Assert.Equal(("excluded", 2), top[2]);
    }

    [Fact]
    public void TopRejectionsAcrossFailures_SumsAcrossAllBuckets_AndIgnoresSuccessCounts()
    {
        var h = new TileSearchHistogram();

        h.Record("Forest", "EggA", new TileSearchStats(10, 1, 0, 0, 0, 5, 0), foundTile: false); // occupied=5, outOfBounds=1
        h.Record("Beach", "EggB", new TileSearchStats(10, 0, 0, 0, 7, 0, 0), foundTile: false); // water=7
        h.Record("Beach", "EggB", new TileSearchStats(10, 0, 0, 0, 999, 999, 999), foundTile: true); // should not count

        var top = h.GetTopRejectionsAcrossFailures(topN: 3);

        Assert.Equal(3, top.Count);
        Assert.Equal(("water", 7), top[0]);
        Assert.Equal(("occupied", 5), top[1]);
        Assert.Equal(("outOfBounds", 1), top[2]);
    }

    [Fact]
    public void Reset_ClearsBuckets()
    {
        var h = new TileSearchHistogram();
        h.Record("Forest", "EggA", new TileSearchStats(10, 1, 0, 0, 0, 5, 0), foundTile: false);

        Assert.NotEmpty(h.GetTopRejectionsAcrossFailures());

        h.Reset();

        Assert.Empty(h.GetTopRejectionsAcrossFailures());
        Assert.Empty(h.GetTopRejections("Forest", "EggA"));
    }
}
