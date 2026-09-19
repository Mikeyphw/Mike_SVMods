using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Models;
using Mikew.NestCore.Helpers;
using StardewValley.GameData.FarmAnimals;
using static Mikew.NestCore.EnumsAndStrings.Strings.ItemIds;

namespace LetMeHatch.Infrastructure.Cloning;

/// <summary>
/// Centralizes clone → purchase mapping rules (alternate purchase targets, original purchase animal resolution).
/// </summary>
public sealed class CloneMappingService : ICloneMappingService
{
    public void ApplyAlternatePurchaseRules(
        ClonedAnimalInfo clonedAnimalInfo,
        IDictionary<string, FarmAnimalData> farmAnimalsData
    )
    {
        if (clonedAnimalInfo is null)
        {
            throw new ArgumentNullException(nameof(clonedAnimalInfo));
        }

        if (farmAnimalsData is null)
        {
            throw new ArgumentNullException(nameof(farmAnimalsData));
        }

        var femaleKey = clonedAnimalInfo.FemaleKey;
        var maleKey = clonedAnimalInfo.MaleKey;

        if (IsVoidChickenUnpurchasable(femaleKey, farmAnimalsData))
        {
            AddAlternatePurchaseType(
                farmAnimalsData,
                Mikew.NestCore.EnumsAndStrings.Strings.VanillaAnimals.WhiteChicken,
                maleKey,
                femaleKey
            );
            return;
        }

        if (ShouldAddAsAlternatePurchase(clonedAnimalInfo, farmAnimalsData, femaleKey))
        {
            if (farmAnimalsData.TryGetValue(maleKey, out var maleData))
            {
                maleData.PurchasePrice = -1;
            }

            AddAlternatePurchaseType(farmAnimalsData, femaleKey, maleKey);
        }
    }

    public bool ShouldAddAsAlternatePurchase(
        ClonedAnimalInfo clonedAnimalInfo,
        IDictionary<string, FarmAnimalData> farmAnimalsData,
        string femaleKey
    )
    {
        if (clonedAnimalInfo is null)
        {
            throw new ArgumentNullException(nameof(clonedAnimalInfo));
        }

        if (!farmAnimalsData.TryGetValue(femaleKey, out var femaleData))
        {
            return clonedAnimalInfo.AddAsAlternatePurchase;
        }

        return clonedAnimalInfo.AddAsAlternatePurchase
            || (!clonedAnimalInfo.AddAsAlternatePurchase && femaleData.PurchasePrice == -1);
    }

    private static bool IsVoidChickenUnpurchasable(
        string femaleKey,
        IDictionary<string, FarmAnimalData> farmAnimalsData
    )
    {
        return femaleKey == Mikew.NestCore.EnumsAndStrings.Strings.VanillaAnimals.VoidChicken
            && farmAnimalsData.TryGetValue(Mikew.NestCore.EnumsAndStrings.Strings.VanillaAnimals.VoidChicken, out var voidChicken)
            && voidChicken.PurchasePrice == -1;
    }

    private static void AddAlternatePurchaseType(
        IDictionary<string, FarmAnimalData> farmAnimalsData,
        string targetKey,
        string maleKey,
        string? femaleKey = null
    )
    {
        var targetShopAnimalEntry = GetTargetShopAnimalEntry(
            farmAnimalsData,
            targetKey,
            femaleKey
        );

        targetShopAnimalEntry.AlternatePurchaseTypes ??= new();

        var unconditional = targetShopAnimalEntry.AlternatePurchaseTypes.FirstOrDefault(x =>
            x.Condition == null
        );

        if (unconditional != null)
        {
            unconditional.AnimalIds ??= new();
            if (!unconditional.AnimalIds.Contains(maleKey))
            {
                unconditional.AnimalIds.Add(maleKey);
            }

            return;
        }

        targetShopAnimalEntry.AlternatePurchaseTypes.Add(
            new()
            {
                AnimalIds = new() { maleKey },
                Id = maleKey
            }
        );
    }

    private static FarmAnimalData GetTargetShopAnimalEntry(
        IDictionary<string, FarmAnimalData> farmAnimalsData,
        string targetKey,
        string? femaleKey = null
    )
    {
        if (string.IsNullOrWhiteSpace(femaleKey))
        {
            return farmAnimalsData[targetKey];
        }

        var originalPurchaseAnimal = farmAnimalsData.GetOriginalPurchaseAnimal(femaleKey);

        return originalPurchaseAnimal != null && originalPurchaseAnimal.Value.Key != null
            ? originalPurchaseAnimal.Value.Value
            : farmAnimalsData[targetKey];
    }
}
