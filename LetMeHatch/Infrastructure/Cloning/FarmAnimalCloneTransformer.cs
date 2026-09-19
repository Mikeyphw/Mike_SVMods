using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StardewValley.GameData.FarmAnimals;
using static Mikew.NestCore.EnumsAndStrings.Strings.ItemIds;

namespace LetMeHatch.Infrastructure.Cloning;

/// <summary>
/// Applies non-localized clone rules to a farm animal clone (male clone data), keeping SMAPI- and Content Patcher-specific
/// concerns in Infrastructure.
/// </summary>
public sealed class FarmAnimalCloneTransformer
{
    private const string EggIdPattern = @".*egg.*";

    /// <summary>
    /// Apply the invariant clone defaults that should be set before any localization/token resolution.
    /// </summary>
    public void ApplyPreLocalization(FarmAnimalData clone)
    {
        if (clone is null)
        {
            throw new ArgumentNullException(nameof(clone));
        }

        clone.Gender = FarmAnimalGender.Male;
        clone.HarvestType = FarmAnimalHarvestType.DropOvernight;

        // Preserve existing behavior: clear first, then ensure non-null later.
        clone.EggItemIds = null;

        // Alternate purchase types are controlled by mapping rules, not per-clone data.
        clone.AlternatePurchaseTypes = null;
    }

    /// <summary>
    /// Apply defaults and produce-list filtering after localized fields are resolved.
    /// Caller provides an optional egg-category predicate to avoid Core taking a dependency on <c>Game1</c>.
    /// </summary>
    public void ApplyPostLocalization(
        FarmAnimalData clone,
        FarmAnimalData original,
        Func<string, bool>? isEggCategoryObjectId = null
    )
    {
        if (clone is null)
        {
            throw new ArgumentNullException(nameof(clone));
        }

        if (original is null)
        {
            throw new ArgumentNullException(nameof(original));
        }

        CopyNullableDefaults(clone, original);
        CopyCollectionDefaults(clone, original);
        EnsureProduceLists(clone, original, isEggCategoryObjectId);
    }

    private static void CopyNullableDefaults(FarmAnimalData clone, FarmAnimalData original)
    {
        clone.ShadowWhenBabySwims ??= original.ShadowWhenBabySwims;
        clone.ShadowWhenBaby ??= original.ShadowWhenBaby;
        clone.ShadowWhenAdultSwims ??= original.ShadowWhenAdultSwims;
        clone.ShadowWhenAdult ??= original.ShadowWhenAdult;
    }

    private static void CopyCollectionDefaults(FarmAnimalData clone, FarmAnimalData original)
    {
        clone.Skins ??= original.Skins != null ? new(original.Skins) : new();

        clone.StatToIncrementOnProduce ??=
            original.StatToIncrementOnProduce != null ? new(original.StatToIncrementOnProduce) : new();

        // Preserve prior behavior: overwrite (not ??=) to avoid keeping placeholder junk.
        clone.CustomFields = original.CustomFields != null ? new(original.CustomFields) : new();

        // Preserve prior behavior: ensure non-null.
        clone.EggItemIds ??= new();
    }

    private static void EnsureProduceLists(
        FarmAnimalData clone,
        FarmAnimalData original,
        Func<string, bool>? isEggCategoryObjectId
    )
    {
        clone.ProduceItemIds ??= new();
        clone.DeluxeProduceItemIds ??= new();

        if (clone.ProduceItemIds.Count == 0)
        {
            clone.ProduceItemIds =
                original.ProduceItemIds != null ? new List<FarmAnimalProduce>(original.ProduceItemIds) : new();

            clone.ProduceItemIds.RemoveAll(p => IsEggLikeId(p.ItemId, isEggCategoryObjectId));
        }

        if (clone.DeluxeProduceItemIds.Count == 0)
        {
            clone.DeluxeProduceItemIds =
                original.DeluxeProduceItemIds != null
                    ? new List<FarmAnimalProduce>(original.DeluxeProduceItemIds)
                    : new();

            clone.DeluxeProduceItemIds.RemoveAll(p => IsEggLikeId(p.ItemId, isEggCategoryObjectId));
        }
    }

    private static bool IsEggLikeId(string? itemId, Func<string, bool>? isEggCategoryObjectId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        // NestCore string constants (avoid local duplicates).
        if (AnimalProducts.VanillaEggs.Contains(itemId))
        {
            return true;
        }

        if (Regex.IsMatch(itemId, EggIdPattern))
        {
            return true;
        }

        return isEggCategoryObjectId?.Invoke(itemId) ?? false;
    }
}
