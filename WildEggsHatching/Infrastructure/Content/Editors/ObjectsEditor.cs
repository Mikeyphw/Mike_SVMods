using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.GameData.Objects;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using WildEggsHatching.Infrastructure.ModData;
using Mikew.BirdsOfAFeather.Helpers.Content;
using Mikew.BirdsOfAFeather;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class ObjectsEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.Objects;

    private const string PrismaticValleyColorKey = "JollyLlama.PrismaticValleyFramework/Color";
    private const string PrismaticValleyPaletteKey = "JollyLlama.PrismaticValleyFramework/Palette";
    private const string PrismaticValleyCustomPalette = "Custom Palette";
    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, ObjectData>().Data;

        foreach (var (id, obj) in BuildObjects(context))
            data[id] = obj;
    }

    private static IReadOnlyDictionary<string, ObjectData> BuildObjects(ContentContext context)
    {
        var items = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in GetEggDefinitions(context))
        {
            // Keep magic eggs progression-gated for cleaner validation/diagnosis.
            if (def.Kind.IsMagic() && context.Step < ProgressionStep.PartVI)
                continue;

            items[def.EggItemId] = CreateEggObjectData(def, context);
        }

        if (context.Step >= ProgressionStep.PartIII)
            items[ModItemIds.OsirisScroll] = CreateOsirisScroll(context);

        if (context.Step >= ProgressionStep.PartIV)
            items[ModItemIds.ElixirOfRevitalizedEssence] = CreateElixir(context);

        if (context.Step >= ProgressionStep.PartVI)
        {
            foreach (var animalKey in GetAnimalKeys(context))
            {
                var noteId = WildEggsHatching.Application.AnimalNotes.AnimalNoteIds.NoteItemId(animalKey);
                items[noteId] = CreateJunimoNote(context, animalKey, noteId);
            }
        }

        return items;
    }

    private static ObjectData CreateEggObjectData(EggDefinition def, ContentContext context)
    {
        var regionDisplay = HumanizeRegion(def.LocationToken);
        var size = def.Kind.IsLarge() ? "Large " : "";
        var magic = def.Kind.IsMagic() ? "Magic " : "";

        var eggTypeName = EggTypeNameForContextTag(def.Kind);
        var colorTag = def.Kind.IsMagic() ? "color_purple" : "color_white";

        return new ObjectData
        {
            Name = def.EggItemId,
            DisplayName = $"{regionDisplay} {size}{magic}Wild Egg".Trim(),
            Description = $"A mysterious wild egg found in the {regionDisplay}.",
            Type = "AnimalProduct",
            Category = -5,
            Price = EggPrice(def.Kind),
            Texture = $"Mods/Mikew.wildeggshatching/objects/eggs",
            SpriteIndex = EggSpriteIndex(def.Kind),
            Edibility = 15,
            IsDrink = false,
            ExcludeFromRandomSale = true,
            ContextTags = new List<string>
            {
                colorTag,
                "forage_item",
                "category_egg",
                "wildegg_item",
                "wildegg_" + eggTypeName,
            },
            CustomFields = new Dictionary<string, string>
            {
                ["WildEggs.Region"] = regionDisplay,
                ["WildEggs.Kind"] = def.Kind.ToString(),
                ["wildEggLocation"] = def.LocationToken,
            }
        };
    }

    private static string EggTypeNameForContextTag(EggKind kind)
    {
        if (kind.IsMagic() && kind.IsLarge())
            return "largeMagicWildEgg";

        if (kind.IsMagic())
            return "magicWildEgg";

        if (kind.IsLarge())
            return "largeWildEgg";

        return "wildEgg";
    }

    private static int EggPrice(EggKind kind)
    {
        if (kind.IsMagic() && kind.IsLarge())
            return 200;

        if (kind.IsLarge())
            return 150;

        if (kind.IsMagic())
            return 100;

        return 100;
    }
        };
    }

    private static ObjectData CreateOsirisScroll(ContentContext context)
        => new()
        {
            Name = ModItemIds.OsirisScroll,
            DisplayName = "Osiris Scroll",
            Description = "An ancient scroll covered in strange symbols.",
            Type = "Basic",
            Category = 0,
            Price = 0,
            Texture = $"Mods/Mikew.wildeggshatching/objects/scroll",
            SpriteIndex = 0,
            Edibility = 15,
            IsDrink = false,
            ExcludeFromRandomSale = true,
            ContextTags = new List<string> { "wildegg_item" },
            CustomFields = new Dictionary<string, string>
            {
                [PrismaticValleyColorKey] = PrismaticValleyCustomPalette,
                [PrismaticValleyPaletteKey] = " #FFC080, #E67E73, #342B1F",
            },

        };

    
    private static ObjectData CreateJunimoNote(ContentContext context, string animalKey, string noteId)
        => new()
        {
            Name = noteId,
            DisplayName = $"{TranslateOrFallback(context, "object.JunimoNote.name", "Leafwhisper Note")}: {animalKey}",
            Description = TranslateOrFallback(context, "object.JunimoNote.description", "A worn parchment, penned by the Junimos."),
            Type = "Basic",
            Category = 0,
            Price = 0,
            Texture = $"Mods/Mikew.wildeggshatching/objects/note",
            SpriteIndex = 0,
            Edibility = -300,
            IsDrink = false,
            ExcludeFromRandomSale = true,
            ContextTags = new List<string> { "wildegg_item", "wildegg_note" },
            CustomFields = new Dictionary<string, string>
            {
                ["WildEggs.Animal"] = animalKey,
                ["WildEggs.IsNote"] = "true",
                [PrismaticValleyColorKey] = PrismaticValleyCustomPalette,
                [PrismaticValleyPaletteKey] = " #34C759, #8BC34A, #2E865F",
                ["Animal"] = animalKey,
            },
        };


    private static ObjectData CreateElixir(ContentContext context)
        => new()
        {
            Name = ModItemIds.ElixirOfRevitalizedEssence,
            DisplayName = "Elixir Of Revitalized Essence",
            Description = "A potent elixir that radiates revitalizing energy.",
            Type = "Basic",
            Category = 0,
            Price = 0,
            Texture = $"Mods/Mikew.wildeggshatching/objects/elixir",
            SpriteIndex = 0,
            Edibility = 15,
            IsDrink = false,
            ExcludeFromRandomSale = true,
            ContextTags = new List<string> { "wildegg_item" },
            CustomFields = new Dictionary<string, string>
            {
                [PrismaticValleyColorKey] = PrismaticValleyCustomPalette,
                [PrismaticValleyPaletteKey] = " #FFC080, #E67E73, #342B1F",
            },

        };

    private static int EggSpriteIndex(EggKind kind)
        => (kind.IsMagic() ? 2 : 0) + (kind.IsLarge() ? 1 : 0);

    private static string ExtractRegionToken(string eggItemId)
    {
        const string prefix = "Mikew.wildeggshatching_";

        var token = eggItemId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? eggItemId[prefix.Length..]
            : eggItemId;

        // Expected suffixes: WEgg, LWEgg, MWEgg, LMWEgg
        foreach (var suffix in new[] { "LMWEgg", "LWEgg", "MWEgg", "WEgg" })
        {
            if (token.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return token[..^suffix.Length];
        }

        return token;
    }

    private static string HumanizeRegion(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "Unknown";

        // Insert spaces for CamelCase tokens (IslandBeach -> Island Beach).
        var chars = new List<char>(token.Length + 8) { token[0] };
        for (var i = 1; i < token.Length; i++)
        {
            var c = token[i];
            var prev = token[i - 1];

            if (char.IsUpper(c) && char.IsLower(prev))
                chars.Add(' ');

            chars.Add(c);
        }

        return new string(chars.ToArray());
    }
    private static IReadOnlyList<string> GetAnimalKeys(ContentContext context)
    {
        return context.ModData.WildAnimals.Keys
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }



    private static string TranslateOrFallback(ContentContext context, string key, string fallback)
        => context.I18n.GetOrDefault(key, fallback);
    private static IReadOnlyList<EggDefinition> GetEggDefinitions(ContentContext context)
    {
        var ids = SpawnAreaEggIds.GetEggItemIds(context.ModData.SpawnAreas);

        return ids
            .Select(id => new EggDefinition(
                EggItemId: id,
                LocationToken: ExtractRegionToken(id),
                Kind: EggKindExtensions.FromEggItemId(id)))
            .ToList();
    }


    private sealed record EggDefinition(string EggItemId, string LocationToken, EggKind Kind);
}

