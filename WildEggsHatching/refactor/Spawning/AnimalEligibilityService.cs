using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Data;
using WildEggsHatching.EnumsAndStrings;

namespace WildEggsHatching.Spawning;

/// <summary>
/// Encapsulates all animal eligibility logic for spawning.
/// This service decouples spawn decisions from eligibility rules.
/// </summary>
public sealed class AnimalEligibilityService
{
    private readonly AnimalCatalog _animalCatalog;
    private readonly PlayerWildState _playerState;

    /// <summary>
    /// Creates a new animal eligibility service.
    /// </summary>
    /// <param name="animalCatalog">The animal catalog for looking up animals.</param>
    /// <param name="playerState">The player's progression state.</param>
    public AnimalEligibilityService(AnimalCatalog animalCatalog, PlayerWildState playerState)
    {
        _animalCatalog = animalCatalog ?? throw new System.ArgumentNullException(nameof(animalCatalog));
        _playerState = playerState ?? throw new System.ArgumentNullException(nameof(playerState));
    }

    /// <summary>
    /// Gets all eligible animals for spawning at the given context.
    /// </summary>
    /// <param name="context">The spawn context.</param>
    /// <returns>Dictionary of animal IDs to their spawn weights.</returns>
    public Dictionary<string, int> GetEligibleAnimals(SpawnContext context)
    {
        return GetEligibleAnimalsByType(context)
            .Where(kvp => IsAnimalEligible(kvp.Key, context))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Checks if a specific animal is eligible for spawning.
    /// </summary>
    /// <param name="animalId">The animal ID to check.</param>
    /// <param name="context">The spawn context.</param>
    /// <returns>True if the animal can spawn.</returns>
    public bool IsAnimalEligible(string animalId, SpawnContext context)
    {
        // Rule 1: Animal must be unlocked for the player
        if (!_playerState.IsUnlocked(animalId))
        {
            return false;
        }

        // Rule 2: Animal must be eligible based on progression
        if (!IsProgressionEligible(animalId, context))
        {
            return false;
        }

        // Rule 3: Animal must match location requirements
        if (!IsLocationEligible(animalId, context))
        {
            return false;
        }

        // Rule 4: Check spawn limits (not already spawned too many)
        if (!IsSpawnLimitEligible(animalId, context))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks progression-based eligibility.
    /// </summary>
    private bool IsProgressionEligible(string animalId, SpawnContext context)
    {
        // Get the animal definition
        if (!_animalCatalog.TryGet(animalId, out var animal))
        {
            return false;
        }

        // Check if the animal has a minimum progression step requirement
        // If the animal's min step is higher than current, not eligible
        // This would use animal.MinProgressionStep in a full implementation
        
        return true; // Simplified - progression checked via unlocked status
    }

    /// <summary>
    /// Checks location-based eligibility.
    /// </summary>
    private bool IsLocationEligible(string animalId, SpawnContext context)
    {
        // Get the animal definition
        if (!_animalCatalog.TryGet(animalId, out var animal))
        {
            return false;
        }

        var location = context.Location;
        
        if (location == null)
        {
            return false;
        }

        // Check if the animal can spawn in this location type
        // Some animals might only spawn in specific locations
        
        return true; // Simplified - all locations valid by default
    }

    /// <summary>
    /// Checks spawn limit eligibility.
    /// </summary>
    private bool IsSpawnLimitEligible(string animalId, SpawnContext context)
    {
        // Check how many times this animal has already spawned today
        if (context.SpawnedEggsInLocation != null)
        {
            if (context.SpawnedEggsInLocation.TryGetValue(animalId, out var count))
            {
                // Limit to 3 spawns per animal per day
                if (count >= 3)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Gets eligible animals grouped by type.
    /// </summary>
    private Dictionary<string, int> GetEligibleAnimalsByType(SpawnContext context)
    {
        var weights = new Dictionary<string, int>();

        // Get animals based on egg type and location
        var isMagic = IsMagicEggType(context.EggType);
        var isLarge = IsLargeEggType(context.EggType);

        var animals = _animalCatalog.GetByType(isMagic, isLarge);

        foreach (var animal in animals)
        {
            weights[animal.Id] = animal.BaseSpawnWeight;
        }

        return weights;
    }

    /// <summary>
    /// Determines if the egg type represents a magic egg.
    /// </summary>
    private bool IsMagicEggType(EnumsAndStrings.EggType eggType)
    {
        return eggType switch
        {
            EnumsAndStrings.EggType.magicWildEgg => true,
            EnumsAndStrings.EggType.largeMagicWildEgg => true,
            _ => false
        };
    }

    /// <summary>
    /// Determines if the egg type represents a large egg.
    /// </summary>
    private bool IsLargeEggType(EnumsAndStrings.EggType eggType)
    {
        return eggType switch
        {
            EnumsAndStrings.EggType.largeWildEgg => true,
            EnumsAndStrings.EggType.largeMagicWildEgg => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a list of animals that are NOT yet unlocked but could be.
    /// Useful for showing locked content in UI.
    /// </summary>
    /// <param name="context">The spawn context.</param>
    /// <returns>List of locked animal IDs.</returns>
    public List<string> GetLockedAnimals(SpawnContext context)
    {
        var locked = new List<string>();

        foreach (var animal in _animalCatalog.All())
        {
            if (!_playerState.IsUnlocked(animal.Id))
            {
                locked.Add(animal.Id);
            }
        }

        return locked;
    }

    /// <summary>
    /// Gets animals that have been discovered but not yet spawned.
    /// </summary>
    /// <returns>List of discovered but not spawned animal IDs.</returns>
    public List<string> GetDiscoveredNotSpawned()
    {
        var discovered = new List<string>();

        foreach (var animalId in _playerState.DiscoveredAnimals)
        {
            // Animal is discovered but hasn't spawned an egg yet
            // This is tracked differently in a full implementation
        }

        return discovered;
    }

    /// <summary>
    /// Gets the next animal in the progression sequence.
    /// </summary>
    /// <returns>The next animal ID to unlock, or null if all unlocked.</returns>
    public string? GetNextLockedAnimal()
    {
        // Define progression order
        var progressionOrder = new[]
        {
            Strings.Animals.Chicken,
            Strings.Animals.Duck,
            Strings.Animals.Dinosaur,
            Strings.Animals.VoidChicken,
        };

        foreach (var animalId in progressionOrder)
        {
            if (!_playerState.IsUnlocked(animalId))
            {
                return animalId;
            }
        }

        return null;
    }
}

