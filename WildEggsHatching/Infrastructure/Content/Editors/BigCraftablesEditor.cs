using System;
using System.Collections.Generic;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using Mikew.BirdsOfAFeather.Helpers.Content;
using Mikew.BirdsOfAFeather;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class BigCraftablesEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.BigCraftables;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, BigCraftableData>().Data;
        data[ModItemIds.WildEggIncubator] = CreateWildEggIncubator(context);
    }

    private static BigCraftableData CreateWildEggIncubator(ContentContext context)
        => new()
        {
            Name = ModItemIds.WildEggIncubator,
            DisplayName = TranslateOrFallback(context, "object.WildEggIncubator.name", "Wild Egg Incubator"),
            Description = TranslateOrFallback(
                context,
                "object.WildEggIncubator.description",
                "A sturdy incubator designed to hatch wild eggs."
            ),
            Texture = $"Mods/{Constants.ModId}/bigcraftables/wildeggincubator",
            SpriteIndex = 0,
            CanBePlacedIndoors = true,
            CanBePlacedOutdoors = false,
            Fragility = 0,
            Price = 0,
            ContextTags = new List<string> { "wildegg_machine" },
        };

    private static string TranslateOrFallback(ContentContext context, string key, string fallback)
        => context.I18n.GetOrDefault(key, fallback);
}
