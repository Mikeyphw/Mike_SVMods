using System;
using System.Linq;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Application.Animals;
using WildEggsHatching.Core.ShopAnchors;
using WildEggsHatching.Core.ShopProgression;
using Mikew.BirdsOfAFeather.Helpers.Content;
using Mikew.BirdsOfAFeather;
using Mikew.BirdsOfAFeather.Framework;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class FarmAnimalsEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.FarmAnimals;

    private const string DinosaurId = "Dinosaur";
    private const string OstrichId = "Ostrich";
    private const string VoidChickenId = "Void Chicken";
    private const string WhiteChickenId = "White Chicken";

    private const string ChickenEggId = "176";
    private const string VoidEggId = "305";

    private readonly ChosenEggAssignments _chosen;

    public FarmAnimalsEditor(ChosenEggAssignments chosen)
        => _chosen = chosen;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, FarmAnimalData>().Data;

        TryApplyConfiguredFullPrice(context, data, DinosaurId, context.Config.DinoPrice);
        TryApplyConfiguredFullPrice(context, data, OstrichId, context.Config.OstrichPrice);

        TryApplyVoidChickenPrice(context, data);

        // Diagnostics only; later phases will use this for parity + weighting.
        var anchors = ShopAnchorIndex.Build(
            data as System.Collections.Generic.IReadOnlyDictionary<string, FarmAnimalData>
            ?? new System.Collections.Generic.Dictionary<string, FarmAnimalData>(data, System.StringComparer.OrdinalIgnoreCase),
            warn: msg => context.Log(msg, StardewModdingAPI.LogLevel.Trace));
        context.Log($"ShopAnchorIndex: anchored_variants={anchors.Count}.", StardewModdingAPI.LogLevel.Trace);

        if (context.Step >= ProgressionStep.PartVI)
        {
            var shopSnapshot = FarmAnimalShopProgressionEdits.Capture(data);

            FarmAnimalShopProgressionEdits.ApplyLockedEdits(
                config: context.Config,
                managedAnimalIds: context.ModData.WildAnimals.Keys.ToList(),
                farmAnimals: data,
                anchors: anchors);

            FarmAnimalShopProgressionEdits.ApplyUnlockedEdits(
                config: context.Config,
                managedAnimalIds: context.ModData.WildAnimals.Keys.ToList(),
                farmAnimals: data,
                anchors: anchors,
                original: shopSnapshot);

            TryApplyLetMeHatchCloneInheritUnlock(context, data);

        }


        if (context.Step >= ProgressionStep.PartVI)
            ApplyChosenEggs(context, data, anchors);
    }

    private static void TryApplyLetMeHatchCloneInheritUnlock(ContentContext context, System.Collections.Generic.IDictionary<string, FarmAnimalData> data)
    {
        try
        {
            var api = context.Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh");
            if (api is null)
                return;

            var pairs = api.GetClonedAnimalsInfo();
            if (pairs is null || pairs.Count == 0)
                return;

            LetMeHatchCloneShopEdits.ApplyInheritUnlock(data, pairs);
        }
        catch (Exception ex)
        {
            context.Log($"LetMeHatch clone gating: best-effort apply failed: {ex.GetType().Name} {ex.Message}", StardewModdingAPI.LogLevel.Trace);
        }
    }

    private void ApplyChosenEggs(
        ContentContext context,
        System.Collections.Generic.IDictionary<string, FarmAnimalData> data,
        ShopAnchorIndex anchors)
    {
        try
        {
            var ro = data as System.Collections.Generic.IReadOnlyDictionary<string, FarmAnimalData>
                ?? new System.Collections.Generic.Dictionary<string, FarmAnimalData>(data, System.StringComparer.OrdinalIgnoreCase);

            _chosen.RecomputeIfNeeded(context.Step, ro, anchors);
            ApplyEggIdsToAnimals(context, data);
        }
        catch (Exception ex)
        {
            context.Log($"FarmAnimalsEditor: failed applying chosen-egg assignments. {ex}", StardewModdingAPI.LogLevel.Warn);
        }
    }

    private void ApplyEggIdsToAnimals(
        ContentContext context,
        System.Collections.Generic.IDictionary<string, FarmAnimalData> data)
    {
        var wildEggIds = new System.Collections.Generic.HashSet<string>(
            context.ModData.SpawnAreas.Keys,
            System.StringComparer.OrdinalIgnoreCase);

        // Remove previous wild-egg assignments.
        foreach (var a in data.Values)
        {
            if (a?.EggItemIds is null)
                continue;

            a.EggItemIds.RemoveAll(id => id is not null && wildEggIds.Contains(id));
        }

        // Apply current mapping (eggId -> animalId).
        foreach (var (eggId, animalId) in _chosen.Current)
        {
            if (string.IsNullOrWhiteSpace(eggId) || string.IsNullOrWhiteSpace(animalId))
                continue;

            if (!data.TryGetValue(animalId, out var animal) || animal is null)
                continue;

            animal.EggItemIds ??= new System.Collections.Generic.List<string>();
            if (!animal.EggItemIds.Contains(eggId, System.StringComparer.OrdinalIgnoreCase))
                animal.EggItemIds.Add(eggId);
        }
    }

    private static void TryApplyConfiguredFullPrice(
        ContentContext context,
        System.Collections.Generic.IDictionary<string, FarmAnimalData> data,
        string animalId,
        string configValue)
    {
        if (!TryParsePositiveInt(configValue, out var fullPrice))
        {
            context.Log($"FarmAnimalsEditor: invalid price '{configValue}' for '{animalId}', skipping.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        if (!data.TryGetValue(animalId, out var animal))
        {
            context.Log($"FarmAnimalsEditor: '{animalId}' not found in Data/FarmAnimals, skipping.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        var purchasePrice = NormalizePurchasePriceFromFull(fullPrice);

        if (animal.PurchasePrice >= 0 && animal.PurchasePrice != purchasePrice)
        {
            context.Log(
                $"FarmAnimalsEditor: overriding existing PurchasePrice for '{animalId}' from {animal.PurchasePrice} to {purchasePrice}.",
                StardewModdingAPI.LogLevel.Info
            );
        }

        ApplyPurchasePrice(animal, purchasePrice);
    }

    private static void TryApplyVoidChickenPrice(
        ContentContext context,
        System.Collections.Generic.IDictionary<string, FarmAnimalData> data)
    {
        var cfg = context.Config.VoidChickenPrice?.Trim();

        if (string.Equals(cfg, "default", StringComparison.OrdinalIgnoreCase))
        {
            TryApplyVoidChickenDefaultPrice(context, data);
            return;
        }

        if (!TryParsePositiveInt(cfg ?? string.Empty, out var fullPrice))
        {
            context.Log($"FarmAnimalsEditor: invalid VoidChickenPrice '{cfg}', skipping.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        if (!data.TryGetValue(VoidChickenId, out var voidChicken))
        {
            context.Log($"FarmAnimalsEditor: '{VoidChickenId}' not found in Data/FarmAnimals, skipping.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        ApplyPurchasePrice(voidChicken, NormalizePurchasePriceFromFull(fullPrice));
    }

    private static void TryApplyVoidChickenDefaultPrice(
        ContentContext context,
        System.Collections.Generic.IDictionary<string, FarmAnimalData> data)
    {
        if (!data.TryGetValue(WhiteChickenId, out var chicken))
        {
            context.Log($"FarmAnimalsEditor: '{WhiteChickenId}' not found; can't derive Void Chicken default price.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        if (!data.TryGetValue(VoidChickenId, out var voidChicken))
        {
            context.Log($"FarmAnimalsEditor: '{VoidChickenId}' not found; can't apply default price.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        if (Game1.objectData is null
            || !Game1.objectData.TryGetValue(ChickenEggId, out var chickenEgg)
            || !Game1.objectData.TryGetValue(VoidEggId, out var voidEgg)
            || chickenEgg.Price <= 0)
        {
            context.Log("FarmAnimalsEditor: missing egg price data; can't derive Void Chicken default price.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        var chickenPurchasePrice = (float)chicken.PurchasePrice;
        var chickenEggPrice = (float)chickenEgg.Price;
        var voidEggPrice = (float)voidEgg.Price;

        var derivedPurchasePrice = (int)(voidEggPrice / chickenEggPrice * chickenPurchasePrice / 5) * 5;
        derivedPurchasePrice = RoundDownTo5(derivedPurchasePrice);

        ApplyPurchasePrice(voidChicken, derivedPurchasePrice);
    }

    private static void ApplyPurchasePrice(FarmAnimalData animal, int purchasePrice)
    {
        animal.PurchasePrice = purchasePrice;
        animal.SellPrice = purchasePrice;
        animal.ShopDisplayName ??= animal.DisplayName;
    }

    private static int NormalizePurchasePriceFromFull(int fullPrice)
        => RoundDownTo5(fullPrice / 2);

    private static int RoundDownTo5(int value)
        => value <= 0 ? 0 : (value / 5) * 5;

    private static bool TryParsePositiveInt(string value, out int result)
        => int.TryParse(value, out result) && result > 0;
}
