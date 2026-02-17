using System;
using System.Collections.Generic;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;
using RoosterAddon.Helpers;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application.FarmAnimals;

/// <summary>
/// Applies RoosterAddon FarmAnimals entry edits for roosters (pricing, strings, textures, unlocks),
/// delegating produce/building rules to dedicated services.
/// </summary>
internal sealed class FarmAnimalsRoosterEditsService
{
    private readonly FarmAnimalBuildingAndLocationRulesService _buildingRules = new();
    private readonly FarmAnimalProduceRulesService _produceRules = new();

    public void Apply(
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        RoosterAddonConfig config,
        RoosterControl roosterHelper,
        ITranslationHelper i18n,
        IDictionary<Textures, Dictionary<TextureDetails, string>> textures,
        bool enableStuckInside,
        bool isStuckInside,
        bool blueEggModInstalled
    )
    {
        if (rooster is null) throw new ArgumentNullException(nameof(rooster));
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (roosterHelper is null) throw new ArgumentNullException(nameof(roosterHelper));
        if (i18n is null) throw new ArgumentNullException(nameof(i18n));
        if (textures is null) throw new ArgumentNullException(nameof(textures));

        if (!data.TryGetValue(rooster.FarmAnimalName, out var entry))
            return;

        _buildingRules.Apply(rooster, entry, enableStuckInside, isStuckInside);

        _produceRules.ApplyProduceEdits(rooster, data, config, roosterHelper, blueEggModInstalled);

        ApplyPricingAndShop(rooster, entry, config, i18n, textures);
    }

    private static void ApplyPricingAndShop(
        RoosterData rooster,
        FarmAnimalData entry,
        RoosterAddonConfig config,
        ITranslationHelper i18n,
        IDictionary<Textures, Dictionary<TextureDetails, string>> textures
    )
    {
        if (rooster.Buyable)
        {
            entry.PurchasePrice =
                rooster.FarmAnimalName == RoosterKeys.White
                    ? -1
                    : (int)Math.Round(rooster.Price / 2 / 5.0) * 5;

            entry.SellPrice = (int)Math.Round(rooster.Price / 2 / 5.0) * 5;

            if (rooster.FarmAnimalName == RoosterKeys.Blue && config.Roosters[RoosterType.Blue].BypassBlueConditions)
                entry.UnlockCondition = null;

            if (rooster.FarmAnimalName == RoosterKeys.Void)
            {
                entry.ShopDisplayName = i18n.Get("shop.void.displayname");
                entry.ShopDescription = i18n.Get("shop.void.description");
                entry.ShopTexture = textures[Textures.VoidShopIcon][TextureDetails.GamePath];
            }
            else if (rooster.FarmAnimalName == RoosterKeys.Gold)
            {
                entry.ShopDisplayName = i18n.Get("shop.gold.displayname");
                entry.ShopDescription = i18n.Get("shop.gold.description");
                entry.ShopTexture = textures[Textures.GoldShopIcon][TextureDetails.GamePath];
            }
        }
        else
        {
            entry.PurchasePrice = -1;
        }
    }
}
