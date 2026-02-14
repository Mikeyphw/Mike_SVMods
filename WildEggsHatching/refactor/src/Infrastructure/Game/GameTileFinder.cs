using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Framework.Random;
using StardewValley;
using WildEggsHatching.Application.Spawning;

namespace WildEggsHatching.Infrastructure.Game;

public sealed class GameTileFinder : ITileFinder
{
    public Vector2? FindTile(GameLocation location, SpawnJob job, IRandomProvider random, int maxAttempts, out TileSearchStats stats)
    {
        var attempts = 0;
        var outOfBounds = 0;
        var excluded = 0;
        var terrainRejected = 0;
        var waterRejected = 0;
        var notPlaceable = 0;

        var (mapWidth, mapHeight) = GetMapSize(location);

        if (mapWidth <= 0 || mapHeight <= 0)
        {
            stats = new TileSearchStats(0, 0, 0, 0, 0, 0);
            return null;
        }

        var includeRects = job.IncludeRects;
        var excludeRects = job.ExcludeRects;

        var rectWeights = includeRects.Count > 0 ? BuildRectWeights(includeRects) : null;

        for (var i = 0; i < maxAttempts; i++)
        {
            attempts++;

            var (x, y) = includeRects.Count > 0
                ? NextPointInRects(includeRects, rectWeights!, random)
                : (random.Next(mapWidth), random.Next(mapHeight));

            if (x < 0 || y < 0 || x >= mapWidth || y >= mapHeight)
            {
                outOfBounds++;
                continue;
            }

            if (excludeRects.Count > 0 && RectParsing.ContainsAny(excludeRects, x, y))
            {
                excluded++;
                continue;
            }

            var terrainType = location.doesTileHaveProperty(x, y, "Type", "Back") ?? "";
            if (job.IncludeTerrainTypes.Count > 0 && !ContainsIgnoreCase(job.IncludeTerrainTypes, terrainType))
            {
                terrainRejected++;
                continue;
            }

            if (job.ExcludeTerrainTypes.Count > 0 && ContainsIgnoreCase(job.ExcludeTerrainTypes, terrainType))
            {
                terrainRejected++;
                continue;
            }

            if (location.isWaterTile(x, y))
            {
                waterRejected++;
                continue;
            }

            var tile = new Vector2(x, y);
            if (!location.isTileLocationTotallyClearAndPlaceable(tile))
            {
                notPlaceable++;
                continue;
            }

            stats = new TileSearchStats(attempts, outOfBounds, excluded, terrainRejected, waterRejected, notPlaceable);
            return tile;
        }

        stats = new TileSearchStats(attempts, outOfBounds, excluded, terrainRejected, waterRejected, notPlaceable);
        return null;
    }

    private static (int width, int height) GetMapSize(GameLocation location)
    {
        try
        {
            var layer = location.Map?.Layers?[0];
            if (layer is null)
                return (0, 0);

            return (layer.LayerWidth, layer.LayerHeight);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static bool ContainsIgnoreCase(IReadOnlyList<string> values, string candidate)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], candidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static IReadOnlyList<(Rectangle rect, int cumulativeWeight)> BuildRectWeights(IReadOnlyList<Rectangle> rects)
    {
        var list = new List<(Rectangle, int)>(rects.Count);
        var cumulative = 0;

        for (var i = 0; i < rects.Count; i++)
        {
            var r = rects[i];
            var area = Math.Max(1, r.Width * r.Height);
            cumulative += area;
            list.Add((r, cumulative));
        }

        return list;
    }

    private static (int x, int y) NextPointInRects(
        IReadOnlyList<Rectangle> rects,
        IReadOnlyList<(Rectangle rect, int cumulativeWeight)> weights,
        IRandomProvider random)
    {
        var total = weights[^1].cumulativeWeight;
        var pick = random.Next(total);

        Rectangle chosen = rects[0];
        for (var i = 0; i < weights.Count; i++)
        {
            if (pick < weights[i].cumulativeWeight)
            {
                chosen = weights[i].rect;
                break;
            }
        }

        var x = random.Next(chosen.Left, chosen.Right);
        var y = random.Next(chosen.Top, chosen.Bottom);
        return (x, y);
    }
}
