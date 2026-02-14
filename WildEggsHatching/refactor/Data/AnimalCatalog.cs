using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.Core.Models;

namespace WildEggsHatching.Data;

/// <summary>
/// Loads and provides access to all wild animal definitions.
/// Replaces the complex logic in WildListManager.
/// </summary>
public sealed class AnimalCatalog
{
    /// <summary>All loaded animal definitions keyed by animal ID.</summary>
    private readonly Dictionary<string, WildAnimalData> _animals;

    /// <summary>
    /// Creates a new animal catalog from loaded animals.
    /// </summary>
    /// <param name="animals">The list of animal definitions to catalog.</param>
    public AnimalCatalog(IEnumerable<WildAnimalData> animals)
    {
        _animals = animals.ToDictionary(a => a.Id, a => a);
    }

    /// <summary>
    /// Gets an animal definition by ID.
    /// </summary>
    /// <param name="id">The animal ID to look up.</param>
    /// <returns>The animal definition.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when animal ID is not found.</exception>
    public WildAnimalData Get(string id)
    {
        return _animals[id];
    }

    /// <summary>
    /// Tries to get an animal definition by ID.
    /// </summary>
    /// <param name="id">The animal ID to look up.</param>
    /// <param name="animal">The animal definition if found.</param>
    /// <returns>True if the animal was found.</returns>
    public bool TryGet(string id, out WildAnimalData? animal)
    {
        return _animals.TryGetValue(id, out animal);
    }

    /// <summary>
    /// Gets all animal definitions.
    /// </summary>
    public IEnumerable<WildAnimalData> All() => _animals.Values;

    /// <summary>
    /// Gets all animals of a specific type.
    /// </summary>
    /// <param name="isMagic">Whether to include magic animals.</param>
    /// <param name="isLarge">Whether to include large animals.</param>
    /// <param name="modOnly">Whether to only include modded animals.</param>
    /// <returns>Filtered list of animals.</returns>
    public IEnumerable<WildAnimalData> GetByType(bool isMagic, bool isLarge, bool modOnly = false)
    {
        return _animals.Values
            .Where(a => a.IsMagic == isMagic && a.LargeEgg == isLarge)
            .Where(a => modOnly ? a.ModID != "Vanilla" : a.ModID == "Vanilla" || !modOnly);
    }

    /// <summary>
    /// Gets the count of all animals in the catalog.
    /// </summary>
    public int Count => _animals.Count;
}


