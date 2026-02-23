using System;
using System.Linq;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using Mikew.BirdsOfAFeather.EnumsAndStrings;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Application.Animals;
using WildEggsHatching.Core.ShopAnchors;
using WildEggsHatching.Core.ShopProgression;
using Mikew.BirdsOfAFeather.Helpers.Content;
using Mikew.BirdsOfAFeather;
using Mikew.BirdsOfAFeather.Framework;
using WildEggsHatching.Infrastructure.ModData;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class FarmAnimalsEditor : AssetEditorBase<ContentContext>, IPostPassAssetEditor<ContentContext>
{
    public override string AssetName => Strings.AssetNames.FarmAnimals;

    public System.Collections.Generic.IReadOnlyList<AssetPostPass<ContentContext>> PostPasses => new[]
    {
        new AssetPostPass<ContentContext>(
            debugName: "WildEggs.FarmAnimals.ApplyChosenEggs",
            priority: (AssetEditPriority)((int)AssetEditPriority.Late + 50),
            apply: (asset, ctx) => ApplyChosenEggsPost(asset, ctx)
        ),
    };

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
    }

    private static void TryApplyLetMeHatchCloneInheritUnlock(ContentContext context, System.Collections.Generic.IDictionary<string, FarmAnimalData> data)
    {
        try
        {
            var api = context.Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh");
            if (api is null)
                return;

            var pairs = api.GetClonedAnimalsInfo()?.ToList();
            if (pairs is null || pairs.Count == 0)
                return;

            LetMeHatchCloneShopEdits.ApplyInheritUnlock(data, pairs);
        }
        catch (Exception ex)
        {
            context.Log($"LetMeHatch clone gating: best-effort apply failed: {ex.GetType().Name} {ex.Message}", StardewModdingAPI.LogLevel.Trace);
        }
    }

    
private void ApplyChosenEggsPost(IAssetData asset, ContentContext context)
{
    var data = asset.AsDictionary<string, FarmAnimalData>().Data;

    var anchors = ShopAnchorIndex.Build(
        data as System.Collections.Generic.IReadOnlyDictionary<string, FarmAnimalData>
        ?? new System.Collections.Generic.Dictionary<string, FarmAnimalData>(data, System.StringComparer.OrdinalIgnoreCase),
        warn: msg => context.Log(msg, StardewModdingAPI.LogLevel.Trace));

    if (context.Step >= ProgressionStep.PartVI)
        ApplyChosenEggs(context, data, anchors);
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
            SpawnAreaEggIds.GetEggItemIds(context.ModData.SpawnAreas, context.Logger),
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

        if (animal.PurchasePrice != -1)
        {
            ApplyShopPresentation(context, animal, animalId);
            TryUpdateConfigFromExistingPurchasePrice(context, animalId, animal.PurchasePrice);
            context.Log($"Another mod has already made '{animalId}' purchasable; updating config price and leaving its current price intact.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        var purchasePrice = NormalizePurchasePriceFromFull(fullPrice);
        ApplyPurchasePrice(animal, purchasePrice);
        ApplyShopPresentation(context, animal, animalId);
    }

    private static void TryApplyVoidChickenPrice(
        ContentContext context,
        System.Collections.Generic.IDictionary<string, FarmAnimalData> data)
    {
        var cfg = context.Config.VoidChickenPrice?.Trim();

        if (string.Equals(cfg, "default", StringComparison.OrdinalIgnoreCase))
        {
            if (TryDeriveVoidChickenDefaultPurchasePrice(context, data, out var derivedPurchasePrice))
            {
                var derivedFullPrice = derivedPurchasePrice * 2;
                TryUpdateConfigFullPrice(context, VoidChickenId, derivedFullPrice);
                cfg = context.Config.VoidChickenPrice?.Trim();
            }
            else
            {
                return;
            }
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

        if (voidChicken.PurchasePrice != -1)
        {
            ApplyShopPresentation(context, voidChicken, VoidChickenId);
            TryUpdateConfigFromExistingPurchasePrice(context, VoidChickenId, voidChicken.PurchasePrice);
            context.Log($"Another mod has already made '{VoidChickenId}' purchasable; updating config price and leaving its current price intact.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        ApplyPurchasePrice(voidChicken, NormalizePurchasePriceFromFull(fullPrice));
        ApplyShopPresentation(context, voidChicken, VoidChickenId);
    }

    private static bool TryDeriveVoidChickenDefaultPurchasePrice(
        ContentContext context,
        System.Collections.Generic.IDictionary<string, FarmAnimalData> data,
        out int derivedPurchasePrice)
    {
        derivedPurchasePrice = 0;

        if (!data.TryGetValue(WhiteChickenId, out var chicken))
        {
            context.Log($"FarmAnimalsEditor: '{WhiteChickenId}' not found; can't derive Void Chicken default price.", StardewModdingAPI.LogLevel.Warn);
            return false;
        }

        if (Game1.objectData is null
            || !Game1.objectData.TryGetValue(ChickenEggId, out var chickenEgg)
            || !Game1.objectData.TryGetValue(VoidEggId, out var voidEgg)
            || chickenEgg.Price <= 0)
        {
            context.Log("FarmAnimalsEditor: missing egg price data; can't derive Void Chicken default price.", StardewModdingAPI.LogLevel.Warn);
            return false;
        }

        var chickenPurchasePrice = (float)chicken.PurchasePrice;
        var chickenEggPrice = (float)chickenEgg.Price;
        var voidEggPrice = (float)voidEgg.Price;

        var purchasePrice = (int)(voidEggPrice / chickenEggPrice * chickenPurchasePrice / 5) * 5;
        purchasePrice = RoundDownTo5(purchasePrice);

        derivedPurchasePrice = purchasePrice;
        return derivedPurchasePrice > 0;
    }

    private static void ApplyPurchasePrice(FarmAnimalData animal, int purchasePrice)
    {
        animal.PurchasePrice = purchasePrice;
        animal.SellPrice = purchasePrice;
        animal.ShopDisplayName ??= animal.DisplayName;
    }

    private static void ApplyShopPresentation(ContentContext context, FarmAnimalData animal, string animalId)
    {
        var key = animalId.Replace(" ", string.Empty);

        if (string.IsNullOrWhiteSpace(animal.ShopDescription))
        {
            var i18nKey = $"shop.description.{key}";
            var translated = context.I18n.Get(i18nKey).ToString();
            if (!string.Equals(translated, i18nKey, StringComparison.OrdinalIgnoreCase))
                animal.ShopDescription = translated;
        }

        if (string.IsNullOrWhiteSpace(animal.ShopTexture))
        {
            var icon = key switch
            {
                "Dinosaur" => "dinosaurShopIcon",
                "VoidChicken" => "voidChickenShopIcon",
                "Ostrich" => "ostrichShopIcon",
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(icon))
                animal.ShopTexture = $"Mods/{Constants.ModId}/misc/{icon}";
        }
    }

    private static void TryUpdateConfigFullPrice(ContentContext context, string animalId, int fullPrice)
    {
        var full = fullPrice <= 0 ? null : fullPrice.ToString();
        if (full is null)
            return;

        var changed = false;

        if (string.Equals(animalId, DinosaurId, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(context.Config.DinoPrice, full, StringComparison.OrdinalIgnoreCase))
            {
                context.Config.DinoPrice = full;
                changed = true;
            }
        }
        else if (string.Equals(animalId, VoidChickenId, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(context.Config.VoidChickenPrice, full, StringComparison.OrdinalIgnoreCase))
            {
                context.Config.VoidChickenPrice = full;
                changed = true;
            }
        }
        else if (string.Equals(animalId, OstrichId, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(context.Config.OstrichPrice, full, StringComparison.OrdinalIgnoreCase))
            {
                context.Config.OstrichPrice = full;
                changed = true;
            }
        }

        if (changed)
        {
            context.Helper.WriteConfig(context.Config);
            context.Log($"FarmAnimalsEditor: wrote back config full price for '{animalId}' => {full}.", StardewModdingAPI.LogLevel.Trace);
        }
    }

    private static void TryUpdateConfigFromExistingPurchasePrice(ContentContext context, string animalId, int purchasePrice)
        => TryUpdateConfigFullPrice(context, animalId, purchasePrice * 2);

    private static int NormalizePurchasePriceFromFull(int fullPrice)
        => RoundDownTo5(fullPrice / 2);

    private static int RoundDownTo5(int value)
        => value <= 0 ? 0 : (value / 5) * 5;

    private static bool TryParsePositiveInt(string value, out int result)
        => int.TryParse(value, out result) && result > 0;
}
