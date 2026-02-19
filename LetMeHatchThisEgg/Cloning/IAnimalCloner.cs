using LetMeHatchThisEgg.Models;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Cloning;

/// <summary>Abstraction over clone editing to allow unit testing orchestration.</summary>
public interface IAnimalCloner
{
    void EditClonedAnimal(
        IContentPack contentPack,
        ClonedAnimalInfo cloneInfo,
        FarmAnimalData originalAnimalData,
        FarmAnimalData currentCloneData
    );
}
