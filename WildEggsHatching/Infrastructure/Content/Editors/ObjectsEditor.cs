using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class ObjectsEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.Objects;

    private static readonly object LockObj = new();
    private static IReadOnlyList<string>? _eggItemIds;
    private static IReadOnlyList<string>? _animalKeys;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, ObjectData>().Data;

        foreach (var (id, obj) in BuildObjects(context))
            data[id] = obj;
    }

    private static IReadOnlyDictionary<string, ObjectData> BuildObjects(ContentContext context)
    {
        var items = new Dictionary<string, ObjectData>(StringComparer.OrdinalIgnoreCase);

        foreach (var eggId in GetEggItemIds(context))
        {
            var kind = EggKindExtensions.FromEggItemId(eggId);

            // Keep magic eggs progression-gated for cleaner parity/diagnosis.
            if (kind.IsMagic() && context.Step < ProgressionStep.PartVI)
                continue;

            items[eggId] = CreateEggObjectData(eggId, kind);
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

    private static ObjectData CreateEggObjectData(string eggId, EggKind kind)
    {
        var region = HumanizeRegion(ExtractRegionToken(eggId));
        var size = kind.IsLarge() ? "Large " : "";
        var magic = kind.IsMagic() ? "Magic " : "";

        return new ObjectData
        {
            Name = eggId,
            DisplayName = $"{region} {size}{magic}Wild Egg".Trim(),
            Description = $"A mysterious wild egg found in the {region}.",
            Type = "AnimalProduct",
            Category = -5,
            Price = 0,
            Texture = $"Mods/Mikew.wildeggshatching/eggs",
            SpriteIndex = EggSpriteIndex(kind),
            Edibility = 15,
            IsDrink = false,
            ExcludeFromRandomSale = true,
            ContextTags = new List<string> { "category_egg", "wildegg_item" },
            CustomFields = new Dictionary<string, string>
            {
                ["WildEggs.Region"] = region,
                ["WildEggs.Kind"] = kind.ToString()
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
            Texture = $"Mods/Mikew.wildeggshatching/scroll",
            SpriteIndex = 0,
            Edibility = 15,
            IsDrink = false,
            ExcludeFromRandomSale = true,
            ContextTags = new List<string> { "wildegg_item" },
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
            Texture = $"Mods/Mikew.wildeggshatching/note",
            SpriteIndex = 0,
            Edibility = -300,
            IsDrink = false,
            ExcludeFromRandomSale = true,
            ContextTags = new List<string> { "wildegg_item", "wildegg_note" },
            CustomFields = new Dictionary<string, string>
            {
                ["WildEggs.Animal"] = animalKey,
                ["WildEggs.IsNote"] = "true",
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
            Texture = $"Mods/Mikew.wildeggshatching/elixir",
            SpriteIndex = 0,
            Edibility = 15,
            IsDrink = false,
            ExcludeFromRandomSale = true,
            ContextTags = new List<string> { "wildegg_item" },
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
        if (_animalKeys is not null)
            return _animalKeys;

        lock (LockObj)
        {
            if (_animalKeys is not null)
                return _animalKeys;

            var path = Path.Combine(context.Helper.DirectoryPath, "ModData", "WildAnimalsList.json");
            if (!File.Exists(path))
            {
                context.Log($"ObjectsEditor: WildAnimalsList.json not found at '{path}'.", StardewModdingAPI.LogLevel.Warn);
                _animalKeys = Array.Empty<string>();
                return _animalKeys;
            }

            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);

            var keys = new List<string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                keys.Add(prop.Name);

            _animalKeys = keys;
            return _animalKeys;
        }
    }


    private static string TranslateOrFallback(ContentContext context, string key, string fallback)
        => context.I18n.GetOrDefault(key, fallback);


    private static IReadOnlyList<string> GetEggItemIds(ContentContext context)
    {
        if (_eggItemIds is not null)
            return _eggItemIds;

        lock (LockObj)
        {
            if (_eggItemIds is not null)
                return _eggItemIds;

            var path = Path.Combine(context.Helper.DirectoryPath, "ModData", "SpawnAreas.json");
            if (!File.Exists(path))
            {
                context.Log($"ObjectsEditor: SpawnAreas.json not found at '{path}'.", StardewModdingAPI.LogLevel.Warn);
                _eggItemIds = Array.Empty<string>();
                return _eggItemIds;
            }

            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);

            var ids = new List<string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                ids.Add(prop.Name);

            _eggItemIds = ids;
            return _eggItemIds;
        }
    }
}
