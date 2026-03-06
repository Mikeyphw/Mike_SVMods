using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LetMeHatch.Config;
using LetMeHatch.Helpers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;

namespace LetMeHatch.Integrations.StardewUi.Colors;

public sealed class LmhColorsEditorService
{
    public LmhColorsEditorCatalog LoadCatalog(IModHelper helper, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(config);

        var paletteGroups = GetPaletteColorGroups()
            .Select(key => new LmhColorPaletteGroupRecord(key, ToDisplayName(key)))
            .ToList();

        var eggs = GetEggOptions(helper, config)
            .Select(egg => new LmhColorOverrideRecord(
                egg.Id,
                egg.Label,
                EnsureEggPalette(config, egg.Id, paletteGroups.Select(x => x.Key).ToList())
                    .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase),
                config.EggColors_ColorObject.TryGetValue(egg.Id, out var color) ? color : Color.Transparent))
            .ToList();

        return new LmhColorsEditorCatalog(paletteGroups, eggs);
    }

    public LmhColorsEditorDraft CreateDraft(LmhColorsEditorCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return new LmhColorsEditorDraft(
            catalog.Overrides.ToDictionary(
                x => x.EggId,
                x => new Dictionary<string, bool>(x.EnabledPaletteGroups, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase),
            catalog.Overrides.ToDictionary(
                x => x.EggId,
                x => x.OverrideColor,
                StringComparer.OrdinalIgnoreCase));
    }

    public void ApplyDraft(ModConfig config, LmhColorsEditorDraft draft)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(draft);

        config.EggColors = draft.PaletteSelections.ToDictionary(
            x => x.Key,
            x => new Dictionary<string, bool>(x.Value, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        config.EggColors_ColorObject = draft.OverrideColors
            .Where(x => x.Value != Color.Transparent)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    public static string FormatColor(Color color)
        => color == Color.Transparent
            ? string.Empty
            : $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static bool TryParseColor(string text, out Color color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            color = Color.Transparent;
            return true;
        }

        var normalized = text.Trim();
        if (normalized.StartsWith('#'))
            normalized = normalized[1..];

        if (normalized.Length == 6
            && byte.TryParse(normalized[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
            && byte.TryParse(normalized[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
            && byte.TryParse(normalized[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            color = new Color(r, g, b);
            return true;
        }

        color = Color.Transparent;
        return false;
    }

    private static IReadOnlyList<LmhEggOption> GetEggOptions(IModHelper helper, ModConfig cfg)
    {
        var eggs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static void Add(Dictionary<string, string> dst, string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            dst.TryAdd(id, string.IsNullOrWhiteSpace(label) ? id : label);
        }

        try
        {
            var animals = helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.FarmAnimals);
            var objects = helper.GameContent.Load<Dictionary<string, ObjectData>>(Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.Objects);

            foreach (var animal in animals.Values)
            {
                if (animal?.ProduceItemIds is not null)
                {
                    foreach (var produce in animal.ProduceItemIds)
                    {
                        var id = produce.ItemId;
                        if (!string.IsNullOrWhiteSpace(id) && objects.TryGetValue(id, out var obj))
                        {
                            var name = obj?.DisplayName ?? obj?.Name ?? id;
                            if (name.Contains("Egg", StringComparison.OrdinalIgnoreCase))
                                Add(eggs, id, name);
                        }
                    }
                }

                if (animal?.DeluxeProduceItemIds is not null)
                {
                    foreach (var produce in animal.DeluxeProduceItemIds)
                    {
                        var id = produce.ItemId;
                        if (!string.IsNullOrWhiteSpace(id) && objects.TryGetValue(id, out var obj))
                        {
                            var name = obj?.DisplayName ?? obj?.Name ?? id;
                            if (name.Contains("Egg", StringComparison.OrdinalIgnoreCase))
                                Add(eggs, id, name);
                        }
                    }
                }
            }
        }
        catch
        {
            // best-effort
        }

        foreach (var id in cfg.EggColors.Keys)
            Add(eggs, id, id);
        foreach (var id in cfg.EggColors_ColorObject.Keys)
            Add(eggs, id, id);

        return eggs
            .Select(x => new LmhEggOption(x.Key, x.Value))
            .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> GetPaletteColorGroups()
    {
        var saved = new SavedColors();
        return saved._colorDictionary.Keys
            .Where(k => !string.Equals(k, "gold", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, bool> EnsureEggPalette(ModConfig cfg, string eggId, IReadOnlyList<string> colorGroups)
    {
        if (!cfg.EggColors.TryGetValue(eggId, out var map) || map is null)
        {
            return colorGroups.ToDictionary(k => k, _ => true, StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, bool>(map, StringComparer.OrdinalIgnoreCase);
        foreach (var key in colorGroups)
        {
            if (!result.ContainsKey(key))
                result[key] = true;
        }

        return result;
    }

    private static string ToDisplayName(string key)
        => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key.Replace('_', ' ').Replace('-', ' '));

    private sealed record LmhEggOption(string Id, string Label);
}
