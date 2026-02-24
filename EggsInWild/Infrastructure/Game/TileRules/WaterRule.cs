using Microsoft.Xna.Framework;
using EggsInWild.Application.Spawning;

namespace EggsInWild.Infrastructure.Game.TileRules;

public sealed class WaterRule : ITileRule
{
    public bool Accept(ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason)
    {
        var x = (int)tile.X;
        var y = (int)tile.Y;

        if (tiles.IsWaterTile(x, y))
        {
            reason = TileRejectionReason.Water;
            return false;
        }

        reason = TileRejectionReason.None;
        return true;
    }
}
