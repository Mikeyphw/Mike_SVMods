using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WildEggsHatching.Application.Spawning;

namespace WildEggsHatching.Infrastructure.Game.TileRules;

public sealed class TerrainRule : ITileRule
{
    public bool Accept(ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason)
    {
        if (job.IncludeTerrainTypes.Count == 0 && job.ExcludeTerrainTypes.Count == 0)
        {
            reason = TileRejectionReason.None;
            return true;
        }

        var x = (int)tile.X;
        var y = (int)tile.Y;

        var terrainType = tiles.GetTerrainType(x, y);

        if (job.IncludeTerrainTypes.Count > 0 && !ContainsIgnoreCase(job.IncludeTerrainTypes, terrainType))
        {
            reason = TileRejectionReason.Terrain;
            return false;
        }

        if (job.ExcludeTerrainTypes.Count > 0 && ContainsIgnoreCase(job.ExcludeTerrainTypes, terrainType))
        {
            reason = TileRejectionReason.Terrain;
            return false;
        }

        reason = TileRejectionReason.None;
        return true;
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
}
