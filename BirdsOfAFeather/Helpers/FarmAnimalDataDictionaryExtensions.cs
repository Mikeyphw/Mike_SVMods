using System.Collections.Generic;
using System.Linq;
using StardewValley.GameData.FarmAnimals;

namespace Mikew.BirdsOfAFeather.Helpers;

public static class FarmAnimalDataDictionaryExtensions
{
    public static KeyValuePair<string, FarmAnimalData>? GetOriginalPurchaseAnimal(
        this IDictionary<string, FarmAnimalData> animalData,
        string animal
    )
    {
        return animalData.FirstOrDefault(a =>
            a.Key != animal
            && a.Key != null
            && a.Value.AlternatePurchaseTypes != null
            && a.Value.AlternatePurchaseTypes.SelectMany(ap => ap.AnimalIds).Contains(animal)
        );
    }

    public static KeyValuePair<string, FarmAnimalData>? GetOriginalPurchaseAnimal(
        this Dictionary<string, FarmAnimalData> animalData,
        string animal
    )
    {
        return animalData.FirstOrDefault(a =>
            a.Key != animal
            && a.Key != null
            && a.Value.AlternatePurchaseTypes != null
            && a.Value.AlternatePurchaseTypes.SelectMany(ap => ap.AnimalIds).Contains(animal)
        );
    }
}
