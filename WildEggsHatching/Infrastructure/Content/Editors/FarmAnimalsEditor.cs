using System;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class FarmAnimalsEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.FarmAnimals;

    private const string DinosaurId = "Dinosaur";
    private const string OstrichId = "Ostrich";
    private const string VoidChickenId = "Void Chicken";
    private const string WhiteChickenId = "White Chicken";

    private const string ChickenEggId = "176";
    private const string VoidEggId = "305";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, FarmAnimalData>().Data;

        TryApplyConfiguredFullPrice(context, data, DinosaurId, context.Config.DinoPrice);
        TryApplyConfiguredFullPrice(context, data, OstrichId, context.Config.OstrichPrice);

        TryApplyVoidChickenPrice(context, data);
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
