using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

namespace WildEggsHatching.Application.DesertTomb.Spawning;

/// <summary>
/// Computes and consumes valid spawn tiles for an area, avoiding repeated random probing.
/// </summary>
public sealed class TombSpawnTileCache
{
    private readonly List<Point> _candidates;
    private readonly List<Point> _used = new();
    private readonly int _minSpacingTiles;

    public TombSpawnTileCache(List<Point> candidates, int minSpacingTiles)
    {
        _candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        _minSpacingTiles = Math.Max(0, minSpacingTiles);
    }

    public int Remaining => _candidates.Count;

    public bool TryTakeTile(MineShaft mineShaft, out Point tile)
    {
        if (mineShaft is null)
            throw new ArgumentNullException(nameof(mineShaft));

        while (_candidates.Count > 0)
        {
            var idx = mineShaft.mineRandom.Next(_candidates.Count);
            var candidate = SwapRemoveAt(_candidates, idx);

            if (mineShaft.isTileOccupied(new Vector2(candidate.X, candidate.Y)))
                continue;

            if (_minSpacingTiles > 0 && !IsFarEnough(candidate, _used, _minSpacingTiles))
                continue;

            _used.Add(candidate);
            tile = candidate;
            return true;
        }

        tile = default;
        return false;
    }

    public static List<Point> ComputeValidSpawnTiles(MineShaft mineShaft, Rectangle area)
    {
        if (mineShaft is null)
            throw new ArgumentNullException(nameof(mineShaft));

        var tiles = new List<Point>(capacity: Math.Max(16, area.Width * area.Height / 3));

        for (int x = area.X; x < area.Right; x++)
        {
            for (int y = area.Y; y < area.Bottom; y++)
            {
                if (!mineShaft.isTileOnMap(x, y))
                    continue;

                if (!mineShaft.isTileClearForMineObjects(x, y))
                    continue;

                tiles.Add(new Point(x, y));
            }
        }

        return tiles;
    }

    public static bool IsFarEnough(Point candidate, IReadOnlyList<Point> used, int minSpacingTiles)
    {
        if (minSpacingTiles <= 0)
            return true;

        var minDistSq = minSpacingTiles * minSpacingTiles;

        for (int i = 0; i < used.Count; i++)
        {
            var u = used[i];
            var dx = candidate.X - u.X;
            var dy = candidate.Y - u.Y;
            var distSq = dx * dx + dy * dy;

            if (distSq < minDistSq)
                return false;
        }

        return true;
    }

    private static Point SwapRemoveAt(List<Point> list, int index)
    {
        var last = list.Count - 1;
        var value = list[index];
        list[index] = list[last];
        list.RemoveAt(last);
        return value;
    }
}
