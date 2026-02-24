using Microsoft.Xna.Framework;
using EggsInWild.Application.Spawning;

namespace EggsInWild.Infrastructure.Game.TileRules;

public sealed class BoundsRule : ITileRule
{
    public bool Accept(ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason)
    {
        var x = (int)tile.X;
        var y = (int)tile.Y;

        if (x < 0 || y < 0 || x >= mapWidth || y >= mapHeight)
        {
            reason = TileRejectionReason.OutOfBounds;
            return false;
        }

        reason = TileRejectionReason.None;
        return true;
    }
}
