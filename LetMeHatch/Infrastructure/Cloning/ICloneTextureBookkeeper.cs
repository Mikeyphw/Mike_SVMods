using LetMeHatch.Models;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Infrastructure.Cloning;

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
        string contentPackId,
        ClonedAnimalInfo cloneInfo,
        FarmAnimalData cloneData,
        FarmAnimalData originalData,
        FarmAnimalData placeholderData
    );
}
