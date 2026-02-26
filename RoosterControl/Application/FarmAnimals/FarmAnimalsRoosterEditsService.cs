using System;
using System.Collections.Generic;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using RoosterControl.Helpers;
using Mikew.NestCore.Helpers;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application.FarmAnimals;

/// <summary>
/// Applies RoosterControl FarmAnimals entry edits for roosters (pricing, strings, textures, unlocks),
/// delegating produce/building rules to dedicated services.
/// </summary>
internal sealed class FarmAnimalsRoosterEditsService
{
    private readonly FarmAnimalBuildingAndLocationRulesService _buildingRules = new();
    private readonly FarmAnimalProduceRulesService _produceRules = new();

    public void Apply(
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        RoosterControlConfig config,
        RoosterHelper roosterHelper,
        I18TranslationHelper i18n,
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
        RoosterControlConfig config,
        I18TranslationHelper i18n,
        IDictionary<Textures, Dictionary<TextureDetails, string>> textures
    )
    {
        if (rooster.Buyable)
        {
            entry.PurchasePrice =
                rooster.RoosterType == RoosterType.White
                    ? -1
                    : (int)Math.Round(rooster.Price / 2 / 5.0) * 5;

            entry.SellPrice = (int)Math.Round(rooster.Price / 2 / 5.0) * 5;

            if (rooster.RoosterType == RoosterType.Blue && config.Roosters[RoosterType.Blue].BypassBlueConditions)
                entry.UnlockCondition = null;

            if (rooster.RoosterType == RoosterType.Void)
            {
                entry.ShopDisplayName = i18n.GetString("shop.void.displayname");
                entry.ShopDescription = i18n.GetString("shop.void.description");
                entry.ShopTexture = textures[Textures.VoidShopIcon][TextureDetails.GamePath];
            }
            else if (rooster.RoosterType == RoosterType.Gold)
            {
                entry.ShopDisplayName = i18n.GetString("shop.gold.displayname");
                entry.ShopDescription = i18n.GetString("shop.gold.description");
                entry.ShopTexture = textures[Textures.GoldShopIcon][TextureDetails.GamePath];
            }
        }
        else
        {
            entry.PurchasePrice = -1;
        }
    }
}
