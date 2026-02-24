using Microsoft.Xna.Framework;
using StardewValley;

namespace EggsInWild.Infrastructure.Game.TileRules;

/// <summary>Adapter over <see cref="GameLocation"/> for tile queries.</summary>
public sealed class GameTileQuery : ITileQuery
{
    private readonly GameLocation _location;

    public GameTileQuery(GameLocation location)
        => _location = location;

    public string GetTerrainType(int x, int y)
        => _location.doesTileHaveProperty(x, y, "Type", "Back") ?? "";

    public bool IsWaterTile(int x, int y)
        => _location.isWaterTile(x, y);

    public bool IsTotallyClearAndPlaceable(Vector2 tile)
        => !_location.objects.ContainsKey(tile) && _location.isTilePassable(tile);

    public bool IsTilePassable(Vector2 tile)
        => _location.isTilePassable(tile);
}
