using Microsoft.Xna.Framework;
using EggsInWild.Application.Spawning;

namespace EggsInWild.Infrastructure.Game.TileRules;

public interface ITileRule
{
    bool Accept(ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason);
}
