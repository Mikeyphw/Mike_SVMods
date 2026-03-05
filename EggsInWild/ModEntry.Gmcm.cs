using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Helpers.SaveData;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Application.Animals;
using EggsInWild.Application.Facade;
using EggsInWild.Application.Progression;
using EggsInWild.Core.Enums;
using EggsInWild.Core.ShopProgression;
using EggsInWild.Infrastructure.Api;
using EggsInWild.Infrastructure.Content;
using EggsInWild.Infrastructure.Content.Editors;
using EggsInWild.Infrastructure.Commands;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.Game;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;
using EggsInWild.Application.DesertTomb.Runtime;
using EggsInWild.Application.DesertTomb.Generation;
using EggsInWild.Application.DesertTomb.Spawning;
using EggsInWild.Infrastructure.DesertTomb.Harmony;
using EggsInWild.Infrastructure.DesertTomb.Loot;
using EggsInWild.Infrastructure.DesertTomb.Loot.Definitions;
using EggsInWild.Infrastructure.DesertTomb.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Assets;
using EggsInWild.Infrastructure.Integrations.Gmcm;

namespace EggsInWild;

public sealed partial class ModEntry
{

    private void OnGmcmSaved()
    {
        if (_config is null)
            return;

        var cfg = _config.Get();
        if (!NormalizeLocationTypesOverrides(cfg))
            return;

        _config.Save();
        InvalidateGameAssets();
    }

    private static bool IsEmptyOverride(WildAnimalOverrideConfig? ov)
        => ov is null
            || (ov.Enabled is null
                && ov.LargeEgg is null
                && ov.Price is null
                && ov.IsMagic is null
                && (ov.LocationTypes is null || ov.LocationTypes.Count == 0)
                && (ov.ExtraModAnimalKeys is null || ov.ExtraModAnimalKeys.Count == 0));

    private bool NormalizeLocationTypesOverrides(ModConfig cfg)
    {
        if (cfg.WildAnimalOverrides is null || cfg.WildAnimalOverrides.Count == 0)
            return false;

        var known = _knownLocationTypeKeysForNormalization;
        var anyChanged = false;

        foreach (var (wildKey, ov) in cfg.WildAnimalOverrides.ToList())
        {
            if (ov is null)
                continue;

            if (ov.LocationTypes is { Count: > 0 })
            {
                var blockChanged = false;
                var newDict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                foreach (var (k, v) in ov.LocationTypes)
                {
                    if (string.IsNullOrWhiteSpace(k))
                    {
                        blockChanged = true;
                        continue;
                    }

                    var t = k.Trim();
                    if (t.Length == 0)
                    {
                        blockChanged = true;
                        continue;
                    }

                    if (known.Count > 0 && !known.Contains(t))
                    {
                        blockChanged = true;
                        continue;
                    }

                    if (!newDict.TryAdd(t, v))
                    {
                        blockChanged = true;
                        continue;
                    }

                    if (!string.Equals(k, t, StringComparison.Ordinal))
                        blockChanged = true;
                }

                if (blockChanged)
                {
                    ov.LocationTypes = newDict.Count == 0 ? null : newDict;
                    anyChanged = true;
                }
            }

            if (IsEmptyOverride(ov))
            {
                cfg.WildAnimalOverrides.Remove(wildKey);
                anyChanged = true;
            }
        }

        return anyChanged;
    }
    private void RebuildGmcmLayout(bool force)
    {
        if (_gmcmHost is null)
            return;

        var i18n = new I18TranslationHelper(Helper);

        // Create the GMCM context
        var ctx = new EiwGmcmContext(
            Helper,
            Monitor,
            ModManifest,
            () => _config!.Get(),
            () => _config!.Save()
        );

#if WEGG_DEV
        // Pass debug services to the context
        ctx.DevJsonTuning = _devJsonTuning;
        ctx.DevMonsterDefinitions = DevMonsterDefinitions;
        ctx.MutateDevMonsterDefinitions = MutateDevMonsterDefinitions;
        ctx.NormalizeDevJsonKey = NormalizeDevJsonKey;
#endif

        // Create the menu and rebuild layout
        var menu = new EiwGmcmMenu(ctx);
        _gmcmHost.RebuildLayoutSaveAware(
            define: layout =>
            {
                menu.RebuildLayout(layout, i18n, force);
                _knownLocationTypeKeysForNormalization = ctx.KnownLocationTypeKeys;
            },
            force: force,
            rebuildOnSaveLoaded: true,
            rebuildOnReturnedToTitle: true);
    }

    internal static string Translate(I18TranslationHelper i18n, string key, string fallback, params object[] tokens)
    {
        var translated = tokens.Length == 0
            ? i18n.GetString(key)
            : i18n.GetString(key, tokens);

        if (!string.IsNullOrWhiteSpace(translated) && !string.Equals(translated, key, StringComparison.OrdinalIgnoreCase))
            return translated;

        return tokens.Length == 0 ? fallback : string.Format(fallback, tokens);
    }


internal static string BuildWildAnimalPageId(string wildAnimalKey)
    => $"wildAnimals.{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(wildAnimalKey)}";

internal static bool GetWildAnimalEffectiveEnabled(ModConfig cfg, string wildAnimalKey, bool defaultEnabled)
{
    if (cfg.WildAnimalOverrides is not null
        && cfg.WildAnimalOverrides.TryGetValue(wildAnimalKey, out var ov)
        && ov?.Enabled is not null)
        return ov.Enabled.Value;

    return defaultEnabled;
}

internal static string GetWildAnimalEnabledSource(ModConfig cfg, string wildAnimalKey, bool defaultEnabled)
{
    if (cfg.WildAnimalOverrides is not null
        && cfg.WildAnimalOverrides.TryGetValue(wildAnimalKey, out var ov)
        && ov?.Enabled is not null)
        return "override";

    return "default";
}


    internal sealed class ExtraModAnimalKeysBulkState
    {
        public string Raw { get; init; } = "";
        public Dictionary<string, IReadOnlyList<string>> Upserts { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Resets { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Warnings { get; init; } = new();
    }


    internal readonly record struct ExtraModAnimalKeysBulkPlanCounts(int PlannedSet, int PlannedClear, int PlannedNoop, bool AnyChangePlanned);

    internal readonly record struct FarmAnimalKeyFixStats(int BlanksRemoved, int TrimmedCount, int DuplicatesRemoved, int UnknownRemoved);

    internal static string CanonicalizeModId(string modIdRaw, IEnumerable<string> knownModIds)
    {
        foreach (var existing in knownModIds)
        {
            if (string.Equals(existing, modIdRaw, StringComparison.OrdinalIgnoreCase))
                return existing;
        }

        return (modIdRaw ?? string.Empty).Trim();
    }

    internal static IReadOnlyList<string> ApplyFarmAnimalKeyFixes(
        IReadOnlyList<string> rawKeys,
        bool fixTrim,
        bool fixDedupe,
        ISet<string>? knownFarmAnimalIds)
        => ApplyFarmAnimalKeyFixes(rawKeys, fixTrim, fixDedupe, knownFarmAnimalIds, out _);

    internal static IReadOnlyList<string> ApplyFarmAnimalKeyFixes(
        IReadOnlyList<string> rawKeys,
        bool fixTrim,
        bool fixDedupe,
        ISet<string>? knownFarmAnimalIds,
        out FarmAnimalKeyFixStats stats)
    {
        if (!fixTrim && !fixDedupe && knownFarmAnimalIds is null)
        {
            stats = default;
            return rawKeys;
        }

        var outKeys = new List<string>(rawKeys.Count);
        HashSet<string>? seen = fixDedupe ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : null;

        var blanksRemoved = 0;
        var trimmedCount = 0;
        var duplicatesRemoved = 0;
        var unknownRemoved = 0;

        foreach (var raw in rawKeys)
        {
            var t = raw ?? string.Empty;

            if (fixTrim)
            {
                var trimmed = t.Trim();
                if (!string.Equals(t, trimmed, StringComparison.Ordinal))
                    trimmedCount++;
                t = trimmed;
            }

            if (t.Length == 0)
            {
                blanksRemoved++;
                continue;
            }

            if (knownFarmAnimalIds is not null && !knownFarmAnimalIds.Contains(t))
            {
                unknownRemoved++;
                continue;
            }

            if (seen is not null && !seen.Add(t))
            {
                duplicatesRemoved++;
                continue;
            }

            outKeys.Add(t);
        }

        stats = new FarmAnimalKeyFixStats(blanksRemoved, trimmedCount, duplicatesRemoved, unknownRemoved);
        return outKeys;
    }

    internal static ExtraModAnimalKeysBulkPlanCounts ComputeExtraModAnimalKeysBulkPlanCounts(
        ExtraModAnimalKeysBulkState st,
        IEnumerable<string> knownModIds,
        WildAnimalJson? definition,
        WildAnimalOverrideConfig? ovNow,
        bool fixTrim,
        bool fixDedupe,
        ISet<string>? knownFarmAnimalIds)
    {
        var plannedSet = 0;
        var plannedClear = 0;
        var plannedNoop = 0;

        var anyChangePlanned = false;

        foreach (var modIdRaw in st.Resets)
        {
            var modId = CanonicalizeModId(modIdRaw, knownModIds);
            var hasOv = ovNow?.ExtraModAnimalKeys is not null && ovNow.ExtraModAnimalKeys.ContainsKey(modId);
            if (hasOv)
            {
                plannedClear++;
                anyChangePlanned = true;
            }
            else
            {
                plannedNoop++;
            }
        }

        foreach (var (modIdRaw, rawKeys) in st.Upserts)
        {
            var modId = CanonicalizeModId(modIdRaw, knownModIds);
            var finalKeys = ApplyFarmAnimalKeyFixes(rawKeys, fixTrim, fixDedupe, knownFarmAnimalIds);

            var baseKeys = definition?.ExtraModAnimalKeys is not null
                && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                ? baseBlock.GetEffectiveFarmAnimalKeys()
                : Array.Empty<string>();

            var matchesBase = finalKeys.Count > 0 && SequenceEqualIgnoreCase(finalKeys, baseKeys);

            ExtraModAnimalKeysOverrideConfig? existingBlock = null;
            var hasOv = ovNow?.ExtraModAnimalKeys is not null
                && ovNow.ExtraModAnimalKeys.TryGetValue(modId, out existingBlock)
                && existingBlock is not null;

            var existingKeys = hasOv && existingBlock!.FarmAnimalKeys is { } ovKeys ? ovKeys : new List<string>();

            var willClear = finalKeys.Count == 0 || matchesBase;
            if (willClear)
            {
                if (hasOv)
                {
                    plannedClear++;
                    anyChangePlanned = true;
                }
                else
                {
                    plannedNoop++;
                }
            }
            else
            {
                if (hasOv && SequenceEqualIgnoreCase(existingKeys, finalKeys))
                {
                    plannedNoop++;
                }
                else
                {
                    plannedSet++;
                    anyChangePlanned = true;
                }
            }
        }

        return new ExtraModAnimalKeysBulkPlanCounts(plannedSet, plannedClear, plannedNoop, anyChangePlanned);
    }

    internal static List<string> BuildExtraModAnimalKeysEffectiveExportBodyLines(
        IEnumerable<string> orderedModIds,
        WildAnimalJson? definition,
        WildAnimalOverrideConfig? ovNow)
    {
        var outLines = new List<string>();

        foreach (var modId in orderedModIds)
        {
            IReadOnlyList<string> effectiveKeys;

            if (ovNow?.ExtraModAnimalKeys is not null
                && ovNow.ExtraModAnimalKeys.TryGetValue(modId, out var blockOv)
                && blockOv?.FarmAnimalKeys is { Count: > 0 } ovKeys)
            {
                effectiveKeys = ovKeys;
            }
            else
            {
                effectiveKeys = definition?.ExtraModAnimalKeys is not null
                    && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                    ? baseBlock.GetEffectiveFarmAnimalKeys()
                    : Array.Empty<string>();
            }

            if (effectiveKeys.Count == 0)
                continue;

            outLines.Add($"{modId} | {string.Join(", ", effectiveKeys)}");
        }

        return outLines;
    }


    internal static ExtraModAnimalKeysBulkState ParseExtraModAnimalKeysBulk(string? raw, ISet<string>? knownModIds = null, ISet<string>? knownFarmAnimalIds = null)
    {
        var state = new ExtraModAnimalKeysBulkState
        {
            Raw = raw ?? string.Empty,
        };

        if (string.IsNullOrWhiteSpace(raw))
            return state;

        var lines = raw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            var sepIndex = line.IndexOf('|');
            if (sepIndex < 0) sepIndex = line.IndexOf(':');
            if (sepIndex < 0) sepIndex = line.IndexOf('=');

            if (sepIndex <= 0)
            {
                state.Warnings.Add($"Bulk line ignored (missing separator): '{line}'. Expected: modId | csvKeys");
                continue;
            }

            var modIdsRaw = line[..sepIndex].Trim();
            var keysRaw = sepIndex >= line.Length - 1 ? "" : line[(sepIndex + 1)..].Trim();

            if (modIdsRaw.Length == 0)
            {
                state.Warnings.Add($"Bulk line ignored (blank modId): '{line}'.");
                continue;
            }

            var modIds = modIdsRaw
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (modIds.Count == 0)
            {
                state.Warnings.Add($"Bulk line ignored (no valid modIds): '{line}'.");
                continue;
            }

            var isReset = keysRaw.Length == 0
                || keysRaw == "*"
                || string.Equals(keysRaw, "reset", StringComparison.OrdinalIgnoreCase)
                || string.Equals(keysRaw, "remove", StringComparison.OrdinalIgnoreCase);

            var parsedKeys = isReset ? Array.Empty<string>() : ParseCsvKeyList(keysRaw);

            if (knownModIds is not null)
            {
                var unknownMods = modIds.Where(mid => !knownModIds.Contains(mid)).ToList();
                if (unknownMods.Count > 0)
                {
                    var sample = string.Join(", ", unknownMods.Take(3));
                    var more = unknownMods.Count > 3 ? $" (+{unknownMods.Count - 3} more)" : "";
                    state.Warnings.Add($"Bulk line has unknown modId(s) (not on this page): {sample}{more}. Line: '{line}'.");
                }
            }

            if (!isReset)
            {
                var blankKeys = 0;
                var dupKeys = 0;
                var unknownKeys = 0;

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var sampleDup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var sampleUnknown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var part in keysRaw.Split(new[] { ',', ';' }, StringSplitOptions.None))
                {
                    var t = (part ?? string.Empty).Trim();
                    if (t.Length == 0)
                    {
                        blankKeys++;
                        continue;
                    }

                    if (!seen.Add(t))
                    {
                        dupKeys++;
                        if (sampleDup.Count < 3)
                            sampleDup.Add(t);
                    }

                    if (knownFarmAnimalIds is not null && !knownFarmAnimalIds.Contains(t))
                    {
                        unknownKeys++;
                        if (sampleUnknown.Count < 3)
                            sampleUnknown.Add(t);
                    }
                }

                if (blankKeys > 0)
                    state.Warnings.Add($"Bulk line contains {blankKeys} blank FarmAnimalKey token(s) (ignored). Line: '{line}'.");

                if (dupKeys > 0)
                {
                    var ex = sampleDup.Count == 0 ? "" : $" (e.g., {string.Join(", ", sampleDup)})";
                    state.Warnings.Add($"Bulk line contains {dupKeys} duplicate FarmAnimalKey token(s) (ignored){ex}. Line: '{line}'.");
                }

                if (knownFarmAnimalIds is not null && unknownKeys > 0)
                {
                    var ex = sampleUnknown.Count == 0 ? "" : $" (e.g., {string.Join(", ", sampleUnknown)})";
                    state.Warnings.Add($"Bulk line contains {unknownKeys} FarmAnimalKey(s) not present in Data/FarmAnimals{ex}. Line: '{line}'.");
                }

                if (parsedKeys.Count == 0)
                    state.Warnings.Add($"Bulk line has no valid FarmAnimalKeys; treating as reset/remove. Line: '{line}'.");
            }


            foreach (var modId in modIds)
            {
                if (isReset || parsedKeys.Count == 0)
                {
                    state.Upserts.Remove(modId);
                    state.Resets.Add(modId);
                }
                else
                {
                    state.Resets.Remove(modId);
                    state.Upserts[modId] = parsedKeys;
                }
            }
        }

        return state;
    }

    internal static IReadOnlyList<string> BuildExtraModAnimalKeysBulkFixPreviewLines(
        string raw,
        bool fixTrim,
        bool fixDedupe,
        bool fixDropUnknown,
        ISet<string>? knownFarmAnimalIds)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        var dropUnknownActive = fixDropUnknown && knownFarmAnimalIds is not null;
        if (!fixTrim && !fixDedupe && !dropUnknownActive)
            return Array.Empty<string>();

        var whitespace = 0;
        var blanksAfterTrim = 0;
        var duplicateOccurrences = 0;
        var unknownKeys = 0;

        var sampleTrimmed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sampleDuplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sampleUnknown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in raw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            var sepIndex = line.IndexOf('|');
            if (sepIndex < 0) sepIndex = line.IndexOf(':');
            if (sepIndex < 0) sepIndex = line.IndexOf('=');

            if (sepIndex <= 0)
                continue;

            var keysRaw = sepIndex >= line.Length - 1 ? "" : line[(sepIndex + 1)..];
            var keysRawTrim = keysRaw.Trim();
            var isReset = keysRawTrim.Length == 0
                || keysRawTrim == "*"
                || string.Equals(keysRawTrim, "reset", StringComparison.OrdinalIgnoreCase)
                || string.Equals(keysRawTrim, "remove", StringComparison.OrdinalIgnoreCase);

            if (isReset)
                continue;

            var seen = fixDedupe ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : null;

            foreach (var part in keysRaw.Split(new[] { ',', ';' }, StringSplitOptions.None))
            {
                var token = part ?? string.Empty;
                var t = token;

                if (fixTrim)
                {
                    var trimmed = t.Trim();
                    if (!string.Equals(t, trimmed, StringComparison.Ordinal))
                    {
                        whitespace++;
                        if (trimmed.Length > 0 && sampleTrimmed.Count < 10)
                            sampleTrimmed.Add(trimmed);
                    }

                    t = trimmed;
                }

                if (t.Length == 0)
                {
                    if (fixTrim)
                        blanksAfterTrim++;
                    continue;
                }

                if (dropUnknownActive && !knownFarmAnimalIds!.Contains(t))
                {
                    unknownKeys++;
                    if (sampleUnknown.Count < 10)
                        sampleUnknown.Add(t);
                    continue;
                }

                if (seen is not null && !seen.Add(t))
                {
                    duplicateOccurrences++;
                    if (sampleDuplicates.Count < 10)
                        sampleDuplicates.Add(t);
                }
            }
        }

        if (whitespace == 0 && blanksAfterTrim == 0 && duplicateOccurrences == 0 && unknownKeys == 0)
            return Array.Empty<string>();

        var outLines = new List<string>
        {
            "Quick-fix preview (on apply):",
        };

        if (fixTrim)
        {
            outLines.Add($" - Trim whitespace: {whitespace} token(s); drop blank-after-trim: {blanksAfterTrim}.");
            if (sampleTrimmed.Count > 0)
                outLines.Add($"   e.g. {string.Join(", ", sampleTrimmed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
        }

        if (fixDedupe)
        {
            outLines.Add($" - Remove duplicates: {duplicateOccurrences} duplicate occurrence(s).");
            if (sampleDuplicates.Count > 0)
                outLines.Add($"   e.g. {string.Join(", ", sampleDuplicates.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
        }

        if (fixDropUnknown)
        {
            if (knownFarmAnimalIds is null)
            {
                outLines.Add(" - Drop unknown FarmAnimalKeys: (requires loaded save)");
            }
            else
            {
                outLines.Add($" - Drop unknown FarmAnimalKeys: {unknownKeys} key(s).");
                if (sampleUnknown.Count > 0)
                    outLines.Add($"   e.g. {string.Join(", ", sampleUnknown.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
            }
        }

        return outLines;
    }

    internal static IReadOnlyList<string> ParseCsvKeyList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        var outList = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim();
            if (t.Length == 0)
                continue;

            if (seen.Add(t))
                outList.Add(t);
        }

        return outList;
    }

    internal static bool SequenceEqualIgnoreCase(IEnumerable<string> left, IEnumerable<string> right)
    {
        if (ReferenceEquals(left, right))
            return true;

        left ??= Array.Empty<string>();
        right ??= Array.Empty<string>();

        return left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);
    }
}
