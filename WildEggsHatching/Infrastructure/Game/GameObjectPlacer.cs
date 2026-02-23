using Microsoft.Xna.Framework;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

public sealed class GameObjectPlacer : IGameObjectPlacer
{
    public bool IsTilePlaceable(GameLocation location, Vector2 tile)
        => !location.objects.ContainsKey(tile) && location.isTilePassable(tile);

    public bool TryPlaceObject(GameLocation location, Vector2 tile, int parentSheetIndex)
    {
        if (!IsTilePlaceable(location, tile))
            return false;

        var obj = new StardewValley.Object(parentSheetIndex.ToString(), 1)
        {
            Location = location,
            TileLocation = tile,
        };
        location.objects[tile] = obj;
        return true;
    }

    public bool TryPlaceObject(GameLocation location, Vector2 tile, string itemId, int stack = 1, bool destroyOvernight = true, bool isSpawnedObject = true)
    {
        if (!IsTilePlaceable(location, tile))
            return false;

        var obj = new StardewValley.Object(itemId, stack)
        {
            Location = location,
            TileLocation = tile,
            IsSpawnedObject = isSpawnedObject,
            destroyOvernight = destroyOvernight,
        };

        return location.objects.TryAdd(tile, obj);
    }
}
