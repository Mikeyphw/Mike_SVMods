using StardewModdingAPI;

namespace LetMeHatch.Infrastructure.GameData;

/// <summary>Wraps SMAPI asset data into repository adapters.</summary>
public interface IFarmAnimalAssetAdapter
{
    /// <summary>Create a repository view over the requested FarmAnimals asset.</summary>
    IFarmAnimalDataRepository Wrap(IAssetData asset);
}
