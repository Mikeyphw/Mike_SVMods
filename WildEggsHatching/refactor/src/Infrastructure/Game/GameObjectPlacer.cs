using Microsoft.Xna.Framework;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

public sealed class GameObjectPlacer : IGameObjectPlacer
{
    public bool IsTilePlaceable(GameLocation location, Vector2 tile)
        => location.isTileLocationTotallyClearAndPlaceable(tile);

    public bool TryPlaceObject(GameLocation location, Vector2 tile, int parentSheetIndex)
    {
        if (!IsTilePlaceable(location, tile))
            return false;

        var obj = new StardewValley.Object(tile, parentSheetIndex, 1);
        location.objects[tile] = obj;
        return true;
    }

    public bool TryPlaceObject(GameLocation location, Vector2 tile, string itemId, int stack = 1)
    {
        if (!IsTilePlaceable(location, tile))
            return false;

        var obj = new StardewValley.Object(itemId, stack)
        {
            Location = location,
            TileLocation = tile,
            IsSpawnedObject = true,
            destroyOvernight = true,
        };

        return location.objects.TryAdd(tile, obj);
    }
}
