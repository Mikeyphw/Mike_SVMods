using System;
using System.Text.RegularExpressions;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

namespace LetMeHatchThisEgg.Core.Cloning;

/// <summary>
/// Pure clone identifier rules (formatting + parsing).
/// </summary>
public static class CloneIdService
{
    private static readonly Regex AnyCloneRegex = new(@"^[^#]+#(?<animal>[^#]+)#(?<original>[^#]+)_(?<suffix>Cloned.*)$", RegexOptions.Compiled);
    private static readonly Regex AnyClonedEggRegex = new(@"^.*#(?<animal>.*)#(?<original>\d*)_Cloned.*Egg$", RegexOptions.Compiled);
    private static readonly Regex FertilizedEggRegex = new(@"^[^#]+#(?<animal>.*)#(?<original>.*)_ClonedFertilizedEgg$", RegexOptions.Compiled);

    public static bool IsClonedEgg(string clonedEggId) => !string.IsNullOrWhiteSpace(clonedEggId) && AnyClonedEggRegex.IsMatch(clonedEggId);

    public static bool IsClonedItem(string clonedItemId, CloneItemType? cloneItemType = null, bool fertilized = false)
    {
        if (string.IsNullOrWhiteSpace(clonedItemId))
            return false;

        if (cloneItemType is null)
            return AnyCloneRegex.IsMatch(clonedItemId);

        return cloneItemType switch
        {
            CloneItemType.Egg => fertilized ? FertilizedEggRegex.IsMatch(clonedItemId) : IsClonedEgg(clonedItemId),
            CloneItemType.Milk => Regex.IsMatch(clonedItemId, @"^[^#]+#(.*)#(.*)_Cloned.*Milk$", RegexOptions.IgnoreCase),
            CloneItemType.Misc => Regex.IsMatch(clonedItemId, @"^[^#]+#(.*)#(.*)_Cloned.*Misc$", RegexOptions.IgnoreCase),
            _ => false
        };
    }

    public static string GetClonedItemId(string modUniqueId, string animalKey, string originalItemId, CloneItemType cloneItemType, ItemType itemType = ItemType.Normal, bool isFertilized = false)
    {
        if (string.IsNullOrWhiteSpace(modUniqueId))
            throw new ArgumentException("Unique id must be non-empty.", nameof(modUniqueId));
        if (string.IsNullOrWhiteSpace(animalKey))
            throw new ArgumentException("Animal key must be non-empty.", nameof(animalKey));
        if (string.IsNullOrWhiteSpace(originalItemId))
            throw new ArgumentException("Original item id must be non-empty.", nameof(originalItemId));

        var suffix = cloneItemType switch
        {
            CloneItemType.Egg => isFertilized ? "ClonedFertilizedEgg" : $"Cloned{itemType}Egg",
            CloneItemType.Milk => $"Cloned{itemType}Milk",
            CloneItemType.Misc => $"Cloned{itemType}Misc",
            _ => throw new ArgumentOutOfRangeException(nameof(cloneItemType), cloneItemType, "Unknown clone item type.")
        };

        return $"{modUniqueId}_#{animalKey}#{originalItemId}_{suffix}";
    }

    public static bool TryParseCloneId(string clonedItemId, out string animalKey, out string originalItemId, out string suffix)
    {
        animalKey = string.Empty;
        originalItemId = string.Empty;
        suffix = string.Empty;

        if (string.IsNullOrWhiteSpace(clonedItemId))
            return false;

        var match = AnyCloneRegex.Match(clonedItemId);
        if (!match.Success)
            return false;

        animalKey = match.Groups["animal"].Value;
        originalItemId = match.Groups["original"].Value;
        suffix = match.Groups["suffix"].Value;
        return true;
    }

    public static bool TryParseCloneId(string clonedItemId, out CloneIdParts parts)
    {
        parts = default;

        if (!TryParseCloneId(clonedItemId, out var animalKey, out var originalItemId, out var suffix))
            return false;

        parts = new CloneIdParts(animalKey, originalItemId, suffix);
        return true;
    }

}
