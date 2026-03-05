using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.ModData.Models;
using Mikew.NestCore.Integrations.Gmcm;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

internal sealed class ExtraModAnimalKeysBulkState
{
    public string Raw { get; init; } = "";
    public Dictionary<string, IReadOnlyList<string>> Upserts { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Resets { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Warnings { get; init; } = new();
}

internal readonly record struct ExtraModAnimalKeysBulkPlanCounts(int PlannedSet, int PlannedClear, int PlannedNoop, bool AnyChangePlanned);

internal readonly record struct FarmAnimalKeyFixStats(int BlanksRemoved, int TrimmedCount, int DuplicatesRemoved, int UnknownRemoved);

internal static class WildAnimalsEditorHelpers
{
    internal static string BuildWildAnimalPageId(string wildAnimalKey)
        => $"wildAnimals.{GmcmFieldIds.Normalize(wildAnimalKey)}";

    internal static bool GetWildAnimalEffectiveEnabled(ModConfig cfg, string wildAnimalKey, bool defaultEnabled)
    {
        if (cfg.WildAnimalOverrides is not null
            && cfg.WildAnimalOverrides.TryGetValue(wildAnimalKey, out var ov)
            && ov?.Enabled is not null)
            return ov.Enabled.Value;

        return defaultEnabled;
    }

    internal static string GetWildAnimalEnabledSource(ModConfig cfg, string wildAnimalKey)
    {
        if (cfg.WildAnimalOverrides is not null
            && cfg.WildAnimalOverrides.TryGetValue(wildAnimalKey, out var ov)
            && ov?.Enabled is not null)
            return "override";

        return "default";
    }

    internal static bool IsEmptyOverride(WildAnimalOverrideConfig? ov)
        => ov is null
            || (ov.Enabled is null
                && ov.LargeEgg is null
                && ov.Price is null
                && ov.IsMagic is null
                && (ov.LocationTypes is null || ov.LocationTypes.Count == 0)
                && (ov.ExtraModAnimalKeys is null || ov.ExtraModAnimalKeys.Count == 0));

    internal static bool NormalizeLocationTypesOverrides(ModConfig cfg, IReadOnlySet<string> knownLocationTypeKeys)
    {
        if (cfg.WildAnimalOverrides is null || cfg.WildAnimalOverrides.Count == 0)
            return false;

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

                    var trimmed = k.Trim();
                    if (trimmed.Length == 0)
                    {
                        blockChanged = true;
                        continue;
                    }

                    if (knownLocationTypeKeys.Count > 0 && !knownLocationTypeKeys.Contains(trimmed))
                    {
                        blockChanged = true;
                        continue;
                    }

                    if (!newDict.TryAdd(trimmed, v))
                    {
                        blockChanged = true;
                        continue;
                    }

                    if (!string.Equals(k, trimmed, StringComparison.Ordinal))
                        blockChanged = true;
                }

                if (blockChanged)
                {
                    ov.LocationTypes = newDict.Count == 0 ? null : newDict;
                    anyChanged = true;
                }
            }

            if (!IsEmptyOverride(ov))
                continue;

            cfg.WildAnimalOverrides.Remove(wildKey);
            anyChanged = true;
        }

        return anyChanged;
    }

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
            var token = raw ?? string.Empty;

            if (fixTrim)
            {
                var trimmed = token.Trim();
                if (!string.Equals(token, trimmed, StringComparison.Ordinal))
                    trimmedCount++;

                token = trimmed;
            }

            if (token.Length == 0)
            {
                blanksRemoved++;
                continue;
            }

            if (knownFarmAnimalIds is not null && !knownFarmAnimalIds.Contains(token))
            {
                unknownRemoved++;
                continue;
            }

            if (seen is not null && !seen.Add(token))
            {
                duplicatesRemoved++;
                continue;
            }

            outKeys.Add(token);
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
            var hasOverride = ovNow?.ExtraModAnimalKeys is not null && ovNow.ExtraModAnimalKeys.ContainsKey(modId);
            if (hasOverride)
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
            var hasOverride = ovNow?.ExtraModAnimalKeys is not null
                && ovNow.ExtraModAnimalKeys.TryGetValue(modId, out existingBlock)
                && existingBlock is not null;

            var existingKeys = hasOverride && existingBlock!.FarmAnimalKeys is { } overrideKeys
                ? overrideKeys
                : new List<string>();

            var willClear = finalKeys.Count == 0 || matchesBase;
            if (willClear)
            {
                if (hasOverride)
                {
                    plannedClear++;
                    anyChangePlanned = true;
                }
                else
                {
                    plannedNoop++;
                }
            }
            else if (hasOverride && SequenceEqualIgnoreCase(existingKeys, finalKeys))
            {
                plannedNoop++;
            }
            else
            {
                plannedSet++;
                anyChangePlanned = true;
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
                && ovNow.ExtraModAnimalKeys.TryGetValue(modId, out var blockOverride)
                && blockOverride?.FarmAnimalKeys is { Count: > 0 } overrideKeys)
            {
                effectiveKeys = overrideKeys;
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
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
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
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
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
                var duplicateKeys = 0;
                var unknownKeys = 0;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var sampleDuplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var sampleUnknown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var part in keysRaw.Split(new[] { ',', ';' }, StringSplitOptions.None))
                {
                    var trimmed = (part ?? string.Empty).Trim();
                    if (trimmed.Length == 0)
                    {
                        blankKeys++;
                        continue;
                    }

                    if (!seen.Add(trimmed))
                    {
                        duplicateKeys++;
                        if (sampleDuplicates.Count < 3)
                            sampleDuplicates.Add(trimmed);
                    }

                    if (knownFarmAnimalIds is not null && !knownFarmAnimalIds.Contains(trimmed))
                    {
                        unknownKeys++;
                        if (sampleUnknown.Count < 3)
                            sampleUnknown.Add(trimmed);
                    }
                }

                if (blankKeys > 0)
                    state.Warnings.Add($"Bulk line contains {blankKeys} blank FarmAnimalKey token(s) (ignored). Line: '{line}'.");

                if (duplicateKeys > 0)
                {
                    var example = sampleDuplicates.Count == 0 ? "" : $" (e.g., {string.Join(", ", sampleDuplicates)})";
                    state.Warnings.Add($"Bulk line contains {duplicateKeys} duplicate FarmAnimalKey token(s) (ignored){example}. Line: '{line}'.");
                }

                if (knownFarmAnimalIds is not null && unknownKeys > 0)
                {
                    var example = sampleUnknown.Count == 0 ? "" : $" (e.g., {string.Join(", ", sampleUnknown)})";
                    state.Warnings.Add($"Bulk line contains {unknownKeys} FarmAnimalKey(s) not present in Data/FarmAnimals{example}. Line: '{line}'.");
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
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
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
                var candidate = token;

                if (fixTrim)
                {
                    var trimmed = candidate.Trim();
                    if (!string.Equals(candidate, trimmed, StringComparison.Ordinal))
                    {
                        whitespace++;
                        if (trimmed.Length > 0 && sampleTrimmed.Count < 10)
                            sampleTrimmed.Add(trimmed);
                    }

                    candidate = trimmed;
                }

                if (candidate.Length == 0)
                {
                    if (fixTrim)
                        blanksAfterTrim++;
                    continue;
                }

                if (dropUnknownActive && !knownFarmAnimalIds!.Contains(candidate))
                {
                    unknownKeys++;
                    if (sampleUnknown.Count < 10)
                        sampleUnknown.Add(candidate);
                    continue;
                }

                if (seen is not null && !seen.Add(candidate))
                {
                    duplicateOccurrences++;
                    if (sampleDuplicates.Count < 10)
                        sampleDuplicates.Add(candidate);
                }
            }
        }

        if (whitespace == 0 && blanksAfterTrim == 0 && duplicateOccurrences == 0 && unknownKeys == 0)
            return Array.Empty<string>();

        var lines = new List<string>
        {
            "Quick-fix preview (on apply):",
        };

        if (fixTrim)
        {
            lines.Add($" - Trim whitespace: {whitespace} token(s); drop blank-after-trim: {blanksAfterTrim}.");
            if (sampleTrimmed.Count > 0)
                lines.Add($"   e.g. {string.Join(", ", sampleTrimmed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
        }

        if (fixDedupe)
        {
            lines.Add($" - Remove duplicates: {duplicateOccurrences} duplicate occurrence(s).");
            if (sampleDuplicates.Count > 0)
                lines.Add($"   e.g. {string.Join(", ", sampleDuplicates.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
        }

        if (fixDropUnknown)
        {
            if (knownFarmAnimalIds is null)
            {
                lines.Add(" - Drop unknown FarmAnimalKeys: (requires loaded save)");
            }
            else
            {
                lines.Add($" - Drop unknown FarmAnimalKeys: {unknownKeys} key(s).");
                if (sampleUnknown.Count > 0)
                    lines.Add($"   e.g. {string.Join(", ", sampleUnknown.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}");
            }
        }

        return lines;
    }

    internal static IReadOnlyList<string> ParseCsvKeyList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        var outList = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0)
                continue;

            if (seen.Add(trimmed))
                outList.Add(trimmed);
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
