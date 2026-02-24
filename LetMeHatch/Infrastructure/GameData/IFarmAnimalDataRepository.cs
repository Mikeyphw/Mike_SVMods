using System.Collections.Generic;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Infrastructure.GameData;

/// <summary>Abstraction over the FarmAnimals asset dictionary to support in-memory testing and asset adapters.</summary>
public interface IFarmAnimalDataRepository
{
    /// <summary>The underlying mutable data map.</summary>
    IDictionary<string, FarmAnimalData> Data { get; }

    /// <summary>Get an animal entry by key.</summary>
    FarmAnimalData Get(string key);

    /// <summary>Replace an animal entry by key.</summary>
    void Set(string key, FarmAnimalData value);
}
