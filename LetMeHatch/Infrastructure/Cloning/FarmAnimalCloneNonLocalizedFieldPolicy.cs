using LetMeHatch.Core.Placeholders;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Infrastructure.Cloning;

/// <summary>
/// Placeholder-aware replacement policy for non-localized <see cref="FarmAnimalData"/> fields.
/// This policy is pure: it does not evaluate i18n or Content Patcher tokens.
/// </summary>
public static class FarmAnimalCloneNonLocalizedFieldPolicy
{
    /// <summary>Replace placeholder sentinel values in the clone with the original values.</summary>
    public static void Apply(FarmAnimalData clone, FarmAnimalData original, FarmAnimalData placeholder)
    {
        clone.House = PlaceholderReplacementPolicy.ReplaceIfPlaceholder(
            current: clone.House,
            original: original.House,
            placeholder: placeholder.House
        );

        clone.RequiredBuilding = PlaceholderReplacementPolicy.ReplaceIfPlaceholder(
            current: clone.RequiredBuilding,
            original: original.RequiredBuilding,
            placeholder: placeholder.RequiredBuilding
        );

        clone.UnlockCondition = PlaceholderReplacementPolicy.ReplaceIfPlaceholder(
            current: clone.UnlockCondition,
            original: original.UnlockCondition,
            placeholder: placeholder.UnlockCondition
        );

        clone.HarvestTool = PlaceholderReplacementPolicy.ReplaceIfPlaceholder(
            current: clone.HarvestTool,
            original: original.HarvestTool,
            placeholder: placeholder.HarvestTool
        );

        clone.Sound = PlaceholderReplacementPolicy.ReplaceIfPlaceholder(
            current: clone.Sound,
            original: original.Sound,
            placeholder: placeholder.Sound
        );

        clone.BabySound = PlaceholderReplacementPolicy.ReplaceIfPlaceholder(
            current: clone.BabySound,
            original: original.BabySound,
            placeholder: placeholder.BabySound
        );
    }
}
