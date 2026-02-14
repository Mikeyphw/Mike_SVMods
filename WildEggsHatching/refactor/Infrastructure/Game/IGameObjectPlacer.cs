using Microsoft.Xna.Framework;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

/// <summary>
/// Provides methods for placing objects in the game world.
/// All object placement should go through this interface.
/// </summary>
public interface IGameObjectPlacer
{
    /// <summary>
    /// Attempts to place an object at the specified tile.
    /// </summary>
    /// <param name="location">The game location.</param>
    /// <param name="tile">The tile position.</param>
    /// <param name="obj">The object to place.</param>
    /// <returns>True if placement was successful.</returns>
    bool PlaceObject(GameLocation location, Vector2 tile, StardewValley.Object obj);

    /// <summary>
    /// Removes an object at the specified tile.
    /// </summary>
    /// <param name="location">The game location.</param>
    /// <param name="tile">The tile position.</param>
    /// <returns>True if an object was removed.</returns>
    bool RemoveObject(GameLocation location, Vector2 tile);

    /// <summary>
    /// Checks if a tile is valid for object placement.
    /// </summary>
    /// <param name="location">The game location.</param>
    /// <param name="tile">The tile position.</param>
    /// <returns>True if the tile can accept an object.</returns>
    bool IsValidPlacementTile(GameLocation location, Vector2 tile);

    /// <summary>
    /// Finds a random valid placement tile in the given area.
    /// </summary>
    /// <param name="location">The game location.</param>
    /// <param name="maxAttempts">Maximum number of attempts to find a valid tile.</param>
    /// <returns>A valid tile position, or null if none found.</returns>
    Vector2? FindRandomValidTile(GameLocation location, int maxAttempts = 100);

    /// <summary>
    /// Adds a character (NPC/monster) to a location.
    /// </summary>
    /// <typeparam name="T">The character type.</typeparam>
    /// <param name="location">The game location.</param>
    /// <param name="character">The character to add.</param>
    /// <returns>True if the character was added.</returns>
    bool AddCharacter<T>(GameLocation location, T character) where T : NPC;

    /// <summary>
    /// Removes a character from a location.
    /// </summary>
    /// <param name="location">The game location.</param>
    /// <param name="character">The character to remove.</param>
    /// <returns>True if the character was removed.</returns>
    bool RemoveCharacter(GameLocation location, NPC character);
}

