using Microsoft.Xna.Framework;

namespace EggsInWild.Infrastructure.Game.TileRules;

/// <summary>
/// Minimal tile query surface used by tile rules.
/// This indirection keeps rule logic unit-testable without the game runtime.
/// </summary>
public interface ITileQuery
{
    string GetTerrainType(int x, int y);
    bool IsWaterTile(int x, int y);
    bool IsTotallyClearAndPlaceable(Vector2 tile);
    bool IsTilePassable(Vector2 tile);
}
