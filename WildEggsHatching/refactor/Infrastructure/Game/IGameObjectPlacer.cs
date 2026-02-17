using Microsoft.Xna.Framework;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

public interface IGameObjectPlacer
{
    bool IsTilePlaceable(GameLocation location, Vector2 tile);

    bool TryPlaceObject(GameLocation location, Vector2 tile, int parentSheetIndex);

    /// <summary>Place an object by item ID (1.6 data/qualified IDs).</summary>
    bool TryPlaceObject(GameLocation location, Vector2 tile, string itemId, int stack = 1, bool destroyOvernight = true, bool isSpawnedObject = true);
}
