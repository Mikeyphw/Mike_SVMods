using Microsoft.Xna.Framework;
using WildEggsHatching.Application.Spawning;

namespace WildEggsHatching.Infrastructure.Game.TileRules;

public sealed class PlaceableRule : ITileRule
{
    public bool Accept(ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason)
    {
        if (tiles.IsTotallyClearAndPlaceable(tile))
        {
            reason = TileRejectionReason.None;
            return true;
        }

        // Classification only: do not tighten acceptance vs previous behavior.
        reason = tiles.IsTilePassable(tile) ? TileRejectionReason.Occupied : TileRejectionReason.Impassable;
        return false;
    }
}
