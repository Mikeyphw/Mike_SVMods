using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mikew.NestCore.Framework.Random;
using StardewValley;
using EggsInWild.Application.Spawning;
using EggsInWild.Infrastructure.Game.TileRules;

namespace EggsInWild.Infrastructure.Game;

public sealed class GameTileFinder : ITileFinder
{
    private static readonly ITileRule[] DefaultRules =
    {
        new BoundsRule(),
        new ExcludedRectsRule(),
        new TerrainRule(),
        new WaterRule(),
        new PlaceableRule(),
    };

    public Vector2? FindTile(GameLocation location, SpawnJob job, IRandomProvider random, int maxAttempts, out TileSearchStats stats)
    {
        var attempts = 0;
        var outOfBounds = 0;
        var excluded = 0;
        var terrainRejected = 0;
        var waterRejected = 0;
        var occupiedRejected = 0;
        var impassableRejected = 0;

        var (mapWidth, mapHeight) = GetMapSize(location);

        if (mapWidth <= 0 || mapHeight <= 0)
        {
            stats = new TileSearchStats(0, 0, 0, 0, 0, 0, 0);
            return null;
        }

        var includeRects = job.IncludeRects;
        var rectWeights = includeRects.Count > 0 ? BuildRectWeights(includeRects) : null;

        var tiles = new GameTileQuery(location);

        for (var i = 0; i < maxAttempts; i++)
        {
            attempts++;

            var (x, y) = includeRects.Count > 0
                ? NextPointInRects(includeRects, rectWeights!, random)
                : (random.Next(mapWidth), random.Next(mapHeight));

            var tile = new Vector2(x, y);

            if (!TryAccept(tiles, job, mapWidth, mapHeight, tile, out var reason))
            {
                switch (reason)
                {
                    case TileRejectionReason.OutOfBounds:
                        outOfBounds++;
                        break;
                    case TileRejectionReason.ExcludedRect:
                        excluded++;
                        break;
                    case TileRejectionReason.Terrain:
                        terrainRejected++;
                        break;
                    case TileRejectionReason.Water:
                        waterRejected++;
                        break;
                    case TileRejectionReason.Occupied:
                        occupiedRejected++;
                        break;
                    case TileRejectionReason.Impassable:
                        impassableRejected++;
                        break;
                }

                continue;
            }

            stats = new TileSearchStats(attempts, outOfBounds, excluded, terrainRejected, waterRejected, occupiedRejected, impassableRejected);
            return tile;
        }

        stats = new TileSearchStats(attempts, outOfBounds, excluded, terrainRejected, waterRejected, occupiedRejected, impassableRejected);
        return null;
    }

    private static bool TryAccept(ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason)
    {
        for (var i = 0; i < DefaultRules.Length; i++)
        {
            if (!DefaultRules[i].Accept(tiles, job, mapWidth, mapHeight, tile, out reason))
                return false;
        }

        reason = TileRejectionReason.None;
        return true;
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
