using System;
using System.Collections.Generic;
using System.Linq;

namespace WildEggsHatching.Application.Spawning.Diagnostics;

public sealed class TileSearchHistogram : ITileSearchHistogram
{
    private sealed class Bucket
    {
        public long Searches;
        public long Successes;

        public long FailedSearches;

        public long Attempts;
        public long FailedAttempts;

        public long OutOfBounds;
        public long ExcludedRect;
        public long Terrain;
        public long Water;
        public long Occupied;
        public long Impassable;

        public long FailedOutOfBounds;
        public long FailedExcludedRect;
        public long FailedTerrain;
        public long FailedWater;
        public long FailedOccupied;
        public long FailedImpassable;
    }

    private readonly Dictionary<(string Map, string Egg), Bucket> _buckets = new();

    public void Record(string mapName, string eggItemId, TileSearchStats stats, bool foundTile)
    {
        var key = (Map: mapName ?? "", Egg: eggItemId ?? "");
        if (!_buckets.TryGetValue(key, out var b))
        {
            b = new Bucket();
            _buckets[key] = b;
        }

        b.Searches++;
        if (foundTile)
            b.Successes++;
        else
            b.FailedSearches++;

        b.Attempts += stats.Attempts;
        b.OutOfBounds += stats.RejectedOutOfBounds;
        b.ExcludedRect += stats.RejectedExcludedRect;
        b.Terrain += stats.RejectedTerrain;
        b.Water += stats.RejectedWater;
        b.Occupied += stats.RejectedOccupied;
        b.Impassable += stats.RejectedImpassable;

        if (!foundTile)
        {
            b.FailedAttempts += stats.Attempts;
            b.FailedOutOfBounds += stats.RejectedOutOfBounds;
            b.FailedExcludedRect += stats.RejectedExcludedRect;
            b.FailedTerrain += stats.RejectedTerrain;
            b.FailedWater += stats.RejectedWater;
            b.FailedOccupied += stats.RejectedOccupied;
            b.FailedImpassable += stats.RejectedImpassable;
        }
    }

    public IReadOnlyList<(string Reason, int Count)> GetTopRejections(string mapName, string eggItemId, int topN = 3)
    {
        var key = (Map: mapName ?? "", Egg: eggItemId ?? "");
        if (!_buckets.TryGetValue(key, out var b))
            return Array.Empty<(string, int)>();

        return TopFromCounts(
            outOfBounds: b.FailedOutOfBounds,
            excluded: b.FailedExcludedRect,
            terrain: b.FailedTerrain,
            water: b.FailedWater,
            occupied: b.FailedOccupied,
            impassable: b.FailedImpassable,
            topN: topN);
    }

    public string FormatTopRejections(string mapName, string eggItemId, int topN = 3)
    {
        var top = GetTopRejections(mapName, eggItemId, topN);
        return top.Count == 0 ? "" : string.Join(", ", top.Select(x => $"{x.Reason}={x.Count}"));
    }

    public IReadOnlyList<(string Reason, int Count)> GetTopRejectionsAcrossFailures(int topN = 3)
    {
        if (_buckets.Count == 0)
            return Array.Empty<(string, int)>();

        long outOfBounds = 0;
        long excluded = 0;
        long terrain = 0;
        long water = 0;
        long occupied = 0;
        long impassable = 0;

        foreach (var b in _buckets.Values)
        {
            outOfBounds += b.FailedOutOfBounds;
            excluded += b.FailedExcludedRect;
            terrain += b.FailedTerrain;
            water += b.FailedWater;
            occupied += b.FailedOccupied;
            impassable += b.FailedImpassable;
        }

        return TopFromCounts(outOfBounds, excluded, terrain, water, occupied, impassable, topN);
    }

    public string FormatTopRejectionsAcrossFailures(int topN = 3)
    {
        var top = GetTopRejectionsAcrossFailures(topN);
        return top.Count == 0 ? "" : string.Join(", ", top.Select(x => $"{x.Reason}={x.Count}"));
    }

    public void Reset()
    {
        _buckets.Clear();
    }

    private static IReadOnlyList<(string Reason, int Count)> TopFromCounts(
        long outOfBounds,
        long excluded,
        long terrain,
        long water,
        long occupied,
        long impassable,
        int topN)
    {
        var pairs = new List<(string Reason, long Count)>
        {
            ("occupied", occupied),
            ("impassable", impassable),
            ("terrain", terrain),
            ("excluded", excluded),
            ("water", water),
            ("outOfBounds", outOfBounds),
        };

        return pairs
            .Where(p => p.Count > 0)
            .OrderByDescending(p => p.Count)
            .Take(Math.Max(0, topN))
            .Select(p => (p.Reason, (int)Math.Min(int.MaxValue, p.Count)))
            .ToList();
    }
}
