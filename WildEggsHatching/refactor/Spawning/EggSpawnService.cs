using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Events;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Core.Random;
using WildEggsHatching.Data;
using WildEggsHatching.Infrastructure.Content;
using WildEggsHatching.Infrastructure.Game;

namespace WildEggsHatching.Spawning;

/// <summary>
/// Decides whether an egg should spawn and which animal it contains.
/// This is the "brain" of spawning - pure logic, no side effects.
/// Uses seeded RNG for deterministic, testable behavior.
/// </summary>
public sealed class EggSpawnService
{
    /// <summary>Calculates spawn chances using simplified modifier system.</summary>
    private readonly SpawnChanceCalculator _chanceCalculator;

    /// <summary>The event bus for publishing spawn events.</summary>
    private readonly EventBus _eventBus;

    /// <summary>Provides access to animal definitions.</summary>
    private readonly AnimalCatalog _animalCatalog;

    /// <summary>Provides access to game state for spawn decisions.</summary>
    private readonly IGameStateReader _gameStateReader;

    /// <summary>Provides seeded RNG for deterministic spawns.</summary>
    private readonly IRandomProvider _randomProvider;

    /// <summary>
    /// Creates a new egg spawn service.
    /// </summary>
    /// <param name="chanceCalculator">The chance calculator to use.</param>
    /// <param name="eventBus">The event bus for publishing events.</param>
    /// <param name="animalCatalog">The animal catalog for animal lookups.</param>
    /// <param name="gameStateReader">The game state reader for location information.</param>
    /// <param name="randomProvider">The random provider for seeded RNG.</param>
    public EggSpawnService(
        SpawnChanceCalculator chanceCalculator,
        EventBus eventBus,
        AnimalCatalog animalCatalog,
        IGameStateReader? gameStateReader = null,
        IRandomProvider? randomProvider = null)
    {
        _chanceCalculator = chanceCalculator ?? throw new ArgumentNullException(nameof(chanceCalculator));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _animalCatalog = animalCatalog ?? throw new ArgumentNullException(nameof(animalCatalog));
        _gameStateReader = gameStateReader ?? new GameStateReader();
        _randomProvider = randomProvider ?? new RandomProvider();
    }

    /// <summary>
    /// Attempts to spawn an egg at the given location.
    /// This is the main entry point for spawning decisions.
    /// </summary>
    /// <param name="context">The spawn context containing all relevant information.</param>
    /// <returns>A SpawnResult indicating whether spawning occurred.</returns>
    public SpawnResult TrySpawn(SpawnContext context)
    {
        // 1. Get the egg type for this location
        var eggType = GetEggTypeForLocation(context.Location);

        // 2. Calculate spawn chance (simplified 3-layer system)
        float chance = _chanceCalculator.Calculate(eggType, context.DailyLuck);

        // 3. Roll for spawn using seeded RNG
        if (_randomProvider.NextDouble() >= chance)
        {
            return SpawnResult.None();
        }

        // 4. Select which animal to spawn
        var animalId = SelectAnimal(context);

        if (animalId == null)
        {
            return SpawnResult.None();
        }

        // 5. Find a valid tile for spawning
        var tile = FindValidTile(context);

        if (tile == null)
        {
            return SpawnResult.None();
        }

        // 6. Return success result (placement is handled by EggPlacementService)
        var eggId = GetEggIdForType(eggType);

        return SpawnResult.Success(eggId!, animalId, context.Location, tile.Value, eggType);
    }

    /// <summary>
    /// Determines the egg type for the given location.
    /// Based on location properties (building type, etc.).
    /// </summary>
    private EggType GetEggTypeForLocation(GameLocation location)
    {
        if (location == null)
            return EggType.wildEgg;

        // Simplified: Determine egg type based on location characteristics
        if (location is Farm)
        {
            // Farm can have various egg types
            return EggType.wildEgg;
        }

        // Check location name for special areas
        return location.Name switch
        {
            "Coop" or "Barn" => EggType.wildEgg,
            _ => EggType.wildEgg
        };
    }

    /// <summary>
    /// Selects an eligible animal for spawning.
    /// Uses weighted random selection from eligible animals.
    /// </summary>
    private string? SelectAnimal(SpawnContext context)
    {
        // Get eligible animals based on progression and location
        var eligibleAnimals = GetEligibleAnimals(context);

        if (!eligibleAnimals.Any())
        {
            return null;
        }

        // Use weighted table for clean selection with seeded RNG
        var table = new WeightedTable<string>(eligibleAnimals, _randomProvider);
        return table.Roll();
    }

    /// <summary>
    /// Gets the list of eligible animals for spawning at the given context.
    /// Filters by progression step and location type.
    /// </summary>
    private Dictionary<string, int> GetEligibleAnimals(SpawnContext context)
    {
        var weights = new Dictionary<string, int>();

        // Get animals from catalog filtered by progression step
        var animals = _animalCatalog.All();

        foreach (var animal in animals)
        {
            // Check if animal is unlocked based on progression
            if (IsAnimalUnlockedForStep(animal, context.CurrentStep))
            {
                weights[animal.Id] = animal.SpawnWeight;
            }
        }

        // Fallback for empty catalog
        if (!weights.Any())
        {
            weights["Chicken"] = 60;
            weights["Duck"] = 40;
        }

        return weights;
    }

    /// <summary>
    /// Checks if an animal should be available based on progression step.
    /// </summary>
    private bool IsAnimalUnlockedForStep(WildAnimalData animal, ProgressionStep step)
    {
        // Simple unlock progression
        // Part I: Basic animals (Chicken, Duck)
        // Part II: More animals unlock
        // etc.
        return step >= animal.MinProgressionStep;
    }

    /// <summary>
    /// Gets the egg item ID for the given egg type.
    /// </summary>
    private string? GetEggIdForType(EggType eggType)
    {
        return eggType switch
        {
            EggType.wildEgg => WildEggConstants.CreateEggId("Farm", isMagic: false, isLarge: false),
            EggType.largeWildEgg => WildEggConstants.CreateEggId("Farm", isMagic: false, isLarge: true),
            EggType.magicWildEgg => WildEggConstants.CreateEggId("Farm", isMagic: true, isLarge: false),
            EggType.largeMagicWildEgg => WildEggConstants.CreateEggId("Farm", isMagic: true, isLarge: true),
            _ => WildEggConstants.CreateEggId("Farm", isMagic: false, isLarge: false)
        };
    }

    /// <summary>
    /// Finds a valid tile for spawning in the given area.
    /// Uses game state reader for location information.
    /// </summary>
    private Vector2? FindValidTile(SpawnContext context)
    {
        var location = context.Location;
        if (location == null)
            return null;

        var mapWidth = location.Map.Layers[0].LayerWidth;
        var mapHeight = location.Map.Layers[0].LayerHeight;

        int maxAttempts = context.Area?.MaxEggs ?? 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int x = _randomProvider.Next(mapWidth);
            int y = _randomProvider.Next(mapHeight);
            var tile = new Vector2(x, y);

            // Check if tile is valid for spawning
            if (IsValidSpawnTile(location, tile))
            {
                return tile;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a tile is valid for spawning an egg.
    /// </summary>
    private bool IsValidSpawnTile(GameLocation location, Vector2 tile)
    {
        if (location == null)
            return false;

        // Check if tile is on the map
        if (!location.Map.IsTileOnMap(tile))
            return false;

        // Check if tile is already occupied
        if (location.Objects.ContainsKey(tile))
            return false;

        // Check if tile is passable
        if (!location.isTilePassable(tile))
            return false;

        return true;
    }
}

