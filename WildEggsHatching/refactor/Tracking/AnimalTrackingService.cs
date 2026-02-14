using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using WildEggsHatching.Core.Events;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.Smapi;

namespace WildEggsHatching.Tracking;

/// <summary>
/// Detects when wild eggs hatch and publishes AnimalHatchedEvents.
/// This is the "eyes" that watch for hatching events.
/// Uses IUpdateTicker abstraction for testability.
/// </summary>
public sealed class AnimalTrackingService
{
    /// <summary>The event bus for publishing hatch events.</summary>
    private readonly EventBus _eventBus;

    /// <summary>Interface for reading game state.</summary>
    private readonly IGameStateReader _gameStateReader;

    /// <summary>Abstraction for update tick registration.</summary>
    private readonly IUpdateTicker _updateTicker;

    /// <summary>Known wild egg item IDs.</summary>
    private readonly HashSet<string> _wildEggIds;

    /// <summary>
    /// Creates a new animal tracking service.
    /// </summary>
    /// <param name="eventBus">The event bus for publishing events.</param>
    /// <param name="gameStateReader">The game state reader.</param>
    /// <param name="updateTicker">The update ticker abstraction.</param>
    public AnimalTrackingService(
        EventBus eventBus,
        IGameStateReader gameStateReader,
        IUpdateTicker? updateTicker = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _gameStateReader = gameStateReader ?? throw new ArgumentNullException(nameof(gameStateReader));
        _updateTicker = updateTicker ?? new UpdateTicker(ModEntry.HelperInstance.Events);

        // Initialize known wild egg IDs
        _wildEggIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "yourdorkbrains.cp_wildegg",           // Wild Egg
            "Mikew.wildeggshatching_farmLWEgg",    // Large Wild Egg
            "Mikew.wildeggshatching_farmMWEgg",    // Magic Wild Egg
            "Mikew.wildeggshatching_farmLMWegg",   // Large Magic Wild Egg
        };
    }

    /// <summary>
    /// Checks if an object is a wild egg that has hatched.
    /// Called when objects are updated in the game world.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <param name="location">The game location.</param>
    /// <returns>True if a hatch event was published.</returns>
    public bool CheckForHatch(StardewValley.Object obj, GameLocation location)
    {
        if (obj == null || location == null)
        {
            return false;
        }

        // Check if this is a wild egg
        if (!IsWildEgg(obj))
        {
            return false;
        }

        // Check if this egg has hatched (ready to hatch)
        if (CanHatch(obj))
        {
            return PublishHatchEvent(obj, location);
        }

        return false;
    }

    /// <summary>
    /// Checks if an object is a wild egg.
    /// </summary>
    private bool IsWildEgg(StardewValley.Object obj)
    {
        // Check if object has wild egg context tag
        if (obj.HasContextTag("wildegg_item"))
        {
            return true;
        }

        // Check if object is one of the known egg IDs
        return _wildEggIds.Contains(obj.ItemId);
    }

    /// <summary>
    /// Checks if an egg can hatch.
    /// In a full implementation, this would check incubation time.
    /// </summary>
    private bool CanHatch(StardewValley.Object egg)
    {
        // Simplified: All wild eggs can hatch
        return true;
    }

    /// <summary>
    /// Publishes a hatch event for the given egg.
    /// </summary>
    private bool PublishHatchEvent(StardewValley.Object egg, GameLocation location)
    {
        // Determine the animal type from the egg
        var animalId = GetAnimalFromEgg(egg);

        if (animalId == null)
        {
            ModEntry.LoggerHelper.Log(
                $"Could not determine animal type for egg: {egg.ItemId}",
                StardewModdingAPI.LogLevel.Warn
            );
            return false;
        }

        // Remove the egg from the world
        location.Objects.Remove(egg.TileLocation);

        // Publish hatch event
        _eventBus.Publish(new AnimalHatchedEvent(
            animalId,
            egg.ItemId,
            location,
            egg.TileLocation
        ));

        ModEntry.LoggerHelper.Log(
            $"Animal hatched: {animalId} from egg {egg.ItemId} at {egg.TileLocation}",
            StardewModdingAPI.LogLevel.Info
        );

        return true;
    }

    /// <summary>
    /// Gets the animal ID that hatches from an egg.
    /// </summary>
    private string? GetAnimalFromEgg(StardewValley.Object egg)
    {
        // Simplified: Map egg IDs to animal IDs
        var eggId = egg.ItemId;

        return eggId switch
        {
            "yourdorkbrains.cp_wildegg" => "Chicken",
            "Mikew.wildeggshatching_farmLWEgg" => "Duck",
            "Mikew.wildeggshatching_farmMWEgg" => "Void Chicken",
            "Mikew.wildeggshatching_farmLMWegg" => "Dinosaur",
            _ => null
        };
    }

    /// <summary>
    /// Registers to receive update ticks for checking egg hatching.
    /// Call this during mod initialization.
    /// </summary>
    public void RegisterForUpdates()
    {
        _updateTicker.Register(OnUpdateTicked);
    }

    /// <summary>
    /// Unregisters from update ticks.
    /// Call this during mod cleanup.
    /// </summary>
    public void UnregisterFromUpdates()
    {
        _updateTicker.Unregister(OnUpdateTicked);
    }

    /// <summary>
    /// Called every game tick to check for hatching eggs.
    /// </summary>
    private void OnUpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
    {
        // Check all locations for hatching eggs
        var locations = _gameStateReader.GetAllLocations();
        foreach (var location in locations)
        {
            CheckLocationForHatchingEggs(location);
        }
    }

    /// <summary>
    /// Checks a location for eggs that might hatch.
    /// </summary>
    public void CheckLocationForHatchingEggs(GameLocation location)
    {
        if (location == null)
        {
            return;
        }

        // Create a list of keys to iterate (to avoid modification issues)
        var keys = location.Objects.Keys.ToList();

        foreach (var tile in keys)
        {
            if (location.Objects.TryGetValue(tile, out var obj))
            {
                CheckForHatch(obj, location);
            }
        }
    }
}

