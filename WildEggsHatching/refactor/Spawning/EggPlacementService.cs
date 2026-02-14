using System;
using Microsoft.Xna.Framework;
using StardewValley;
using WildEggsHatching.Core.Events;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Infrastructure.Game;

namespace WildEggsHatching.Spawning;

/// <summary>
/// Places spawned eggs into the game world.
/// This service handles the actual object placement - the "hands" of spawning.
/// </summary>
public sealed class EggPlacementService
{
    /// <summary>The event bus for publishing placement events.</summary>
    private readonly EventBus _eventBus;

    /// <summary>The object placer for modifying the game world.</summary>
    private readonly IGameObjectPlacer _objectPlacer;

    /// <summary>
    /// Creates a new egg placement service.
    /// </summary>
    /// <param name="eventBus">The event bus for publishing events.</param>
    /// <param name="objectPlacer">The object placer for game world modifications.</param>
    public EggPlacementService(EventBus eventBus, IGameObjectPlacer objectPlacer)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _objectPlacer = objectPlacer ?? throw new ArgumentNullException(nameof(objectPlacer));
    }

    /// <summary>
    /// Places an egg into the game world at the specified location.
    /// </summary>
    /// <param name="result">The spawn result containing egg details.</param>
    /// <returns>True if placement was successful.</returns>
    public bool Place(SpawnResult result)
    {
        if (!result.ShouldSpawn)
        {
            return false;
        }

        var location = result.Location;
        var tile = result.Tile;

        if (location == null || tile == null)
        {
            return false;
        }

        // Create the egg object
        var eggObject = new StardewValley.Object(result.EggId!, 1)
        {
            Location = location,
            TileLocation = tile.Value,
            IsSpawnedObject = true,
            destroyOvernight = true,
        };

        // Use the object placer to add the egg to the location
        if (_objectPlacer.PlaceObject(location, tile.Value, eggObject))
        {
            // Publish event for other services to react to
            _eventBus.Publish(new EggSpawnedEvent(
                result.EggId!,
                result.AnimalId!,
                location,
                tile.Value
            ));

            ModEntry.LoggerHelper.Log(
                $"Successfully spawned egg {result.EggId} for animal {result.AnimalId} at {tile.Value} in {location.Name}",
                StardewModdingAPI.LogLevel.Info
            );

            return true;
        }
        else
        {
            ModEntry.LoggerHelper.Log(
                $"Failed to place egg {result.EggId} at {tile.Value} in {location.Name} - tile already occupied",
                StardewModdingAPI.LogLevel.Warn
            );

            return false;
        }
    }

    /// <summary>
    /// Attempts to find an alternative valid tile if the preferred one is blocked.
    /// </summary>
    /// <param name="location">The game location.</param>
    /// <param name="preferredTile">The preferred tile position.</param>
    /// <param name="searchRadius">How far to search for alternatives.</param>
    /// <returns>A valid tile, or null if none found.</returns>
    public Vector2? FindAlternativeTile(GameLocation location, Vector2 preferredTile, int searchRadius = 5)
    {
        if (location == null)
            return null;

        // Spiral search outward from preferred tile
        for (int radius = 1; radius <= searchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Only check tiles on the perimeter of the search radius
                    if (Math.Abs(x) != radius && Math.Abs(y) != radius)
                    {
                        continue;
                    }

                    var testTile = preferredTile + new Vector2(x, y);

                    if (_objectPlacer.IsValidPlacementTile(location, testTile))
                    {
                        return testTile;
                    }
                }
            }
        }

        return null;
    }
}

