using LetMeHatchThisEgg.Models;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Infrastructure.Cloning;

/// <summary>
/// Tracks clone texture decisions and records any content-pack texture paths back onto clone metadata.
/// </summary>
public interface ICloneTextureBookkeeper
{
    /// <summary>
    /// Apply resolved texture values onto <paramref name="cloneData"/> and record any content-pack texture paths
    /// onto <paramref name="cloneInfo"/> (for later loading/caching).
    /// </summary>
    void ApplyTextures(
        IContentPack contentPack,
        ClonedAnimalInfo cloneInfo,
        FarmAnimalData cloneData,
        FarmAnimalData originalData,
        FarmAnimalData placeholderData
    );
}
