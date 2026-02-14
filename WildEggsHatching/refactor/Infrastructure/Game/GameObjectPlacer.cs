using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace WildEggsHatching.Infrastructure.Game;

/// <summary>
/// Implementation of IGameObjectPlacer that directly modifies the game world.
/// This is the only place that directly manipulates game objects.
/// </summary>
public sealed class GameObjectPlacer : IGameObjectPlacer
{
    /// <inheritdoc />
    public bool PlaceObject(GameLocation location, Vector2 tile, StardewValley.Object obj)
    {
        if (location == null || obj == null)
            return false;

        try
        {
            obj.Location = location;
            obj.TileLocation = tile;

            return location.Objects.TryAdd(tile, obj);
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error placing object at {tile}: {ex.Message}",
                LogLevel.Error
            );
            return false;
        }
    }

    /// <inheritdoc />
    public bool RemoveObject(GameLocation location, Vector2 tile)
    {
        if (location == null)
            return false;

        return location.Objects.Remove(tile);
    }

    /// <inheritdoc />
    public bool IsValidPlacementTile(GameLocation location, Vector2 tile)
    {
        if (location == null)
            return false;

        // Must be on the map
        if (!location.Map.IsTileOnMap(tile))
            return false;

        // Must not be occupied
        if (location.Objects.ContainsKey(tile))
            return false;

        // Must be passable terrain
        var tilePos = new xTile.Dimensions.Location(
            (int)tile.X * Game1.tileSize,
            (int)tile.Y * Game1.tileSize
        );

        // Check if tile is on a bridge or platform that can have objects
        var building = location.getBuildingAt(tilePos);
        if (building != null)
        {
            // Buildings can have objects on them
            return true;
        }

        // Check terrain features
        var terrainFeature = location.getTerrainFeatureAt(tilePos);
        if (terrainFeature != null)
        {
            // Some terrain features can have objects (like flooring)
            // Others cannot (like trees, grass)
            // Simplified check - adjust as needed
        }

        return true;
    }

    /// <inheritdoc />
    public Vector2? FindRandomValidTile(GameLocation location, int maxAttempts)
    {
        if (location == null)
            return null;

        var mapWidth = location.Map.Layers[0].LayerWidth;
        var mapHeight = location.Map.Layers[0].LayerHeight;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int x = Game1.random.Next(mapWidth);
            int y = Game1.random.Next(mapHeight);
            var tile = new Vector2(x, y);

            if (IsValidPlacementTile(location, tile))
            {
                return tile;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public bool AddCharacter<T>(GameLocation location, T character) where T : NPC
    {
        if (location == null || character == null)
            return false;

        try
        {
            location.addCharacter(character);
            return true;
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error adding character {character.Name}: {ex.Message}",
                LogLevel.Error
            );
            return false;
        }
    }

    /// <inheritdoc />
    public bool RemoveCharacter(GameLocation location, NPC character)
    {
        if (location == null || character == null)
            return false;

        try
        {
            location.characters.Remove(character);
            return true;
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error removing character {character.Name}: {ex.Message}",
                LogLevel.Error
            );
            return false;
        }
    }
}

