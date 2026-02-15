using Microsoft.Xna.Framework;
using WildEggsHatching.Application.Spawning;

namespace WildEggsHatching.Infrastructure.Game.TileRules;

public sealed class ExcludedRectsRule : ITileRule
{
    public bool Accept(ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason)
    {
        if (job.ExcludeRects.Count > 0 && RectParsing.ContainsAny(job.ExcludeRects, (int)tile.X, (int)tile.Y))
        {
            reason = TileRejectionReason.ExcludedRect;
            return false;
        }

        reason = TileRejectionReason.None;
        return true;
    }
}
