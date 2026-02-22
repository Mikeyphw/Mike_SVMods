using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Infrastructure.ModData;

/// <summary>
/// Synthesizes stable wild-egg item IDs from spawn-area JSON fields, not from code enums.
/// </summary>
public static class SpawnAreaEggIds
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>Return unique egg item IDs derived from JSON (Location + EggTypeToken).</summary>
    public static IReadOnlyList<string> GetEggItemIds(
        IReadOnlyDictionary<string, List<SpawnAreaRuleJson>> spawnAreas,
        ILog? log = null)
    {
        var ids = new HashSet<string>(Comparer);

        foreach (var (key, rules) in spawnAreas)
        {
            if (!TrySynthesizeEggItemId(key, rules, out var eggId, log))
                continue;

            ids.Add(eggId);
        }

        return ids.OrderBy(x => x, Comparer).ToList();
    }

    /// <summary>
    /// Normalize spawn areas to be keyed by synthesized egg item IDs (Location + EggTypeToken).
    /// If multiple groups synthesize the same egg ID, their rules are merged.
    /// </summary>
    public static IReadOnlyDictionary<string, List<SpawnAreaRuleJson>> Normalize(
        IReadOnlyDictionary<string, List<SpawnAreaRuleJson>> spawnAreas,
        ILog? log = null)
    {
        var normalized = new Dictionary<string, List<SpawnAreaRuleJson>>(Comparer);

        foreach (var (key, rules) in spawnAreas)
        {
            if (!TrySynthesizeEggItemId(key, rules, out var eggId, log))
                continue;

            if (!normalized.TryGetValue(eggId, out var list))
            {
                list = new List<SpawnAreaRuleJson>();
                normalized[eggId] = list;
            }

            list.AddRange(rules.Where(r => r is not null));
        }

        return normalized;
    }

    private static bool TrySynthesizeEggItemId(
        string groupKey,
        IReadOnlyList<SpawnAreaRuleJson>? rules,
        out string eggItemId,
        ILog? log)
    {
        eggItemId = "";

        var first = rules?.FirstOrDefault(r => r is not null);
        if (first is null)
        {
            log?.Log($"SpawnAreas group '{groupKey}' has no rules; skipping.", StardewModdingAPI.LogLevel.Warn);
            return false;
        }

        var locationToken = SanitizeToken(first.Location);
        var typeToken = SanitizeToken(first.EggTypeToken);

        if (string.IsNullOrWhiteSpace(locationToken))
            locationToken = SanitizeToken(ExtractRegionTokenFromKey(groupKey));

        if (string.IsNullOrWhiteSpace(typeToken))
            typeToken = SanitizeToken(ExtractEggTypeTokenFromKey(groupKey));

        if (string.IsNullOrWhiteSpace(locationToken) || string.IsNullOrWhiteSpace(typeToken))
        {
            log?.Log($"SpawnAreas group '{groupKey}' missing Location/EggTypeToken; falling back to key '{groupKey}'.", StardewModdingAPI.LogLevel.Warn);
            eggItemId = groupKey;
            return true;
        }

        eggItemId = $"{Constants.ModId}_{locationToken}{typeToken}";
        return true;
    }

    private static string SanitizeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "";

        // Keep IDs deterministic: strip spaces and punctuation, preserve casing.
        var chars = token.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray();
        return new string(chars);
    }

    private static string ExtractRegionTokenFromKey(string eggItemId)
    {
        var prefix = $"{Constants.ModId}_";

        var token = eggItemId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? eggItemId[prefix.Length..]
            : eggItemId;

        foreach (var suffix in new[] { "LMWEgg", "LWEgg", "MWEgg", "WEgg" })
        {
            if (token.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return token[..^suffix.Length];
        }

        return token;
    }

    private static string ExtractEggTypeTokenFromKey(string eggItemId)
    {
        foreach (var suffix in new[] { "LMWEgg", "LWEgg", "MWEgg", "WEgg" })
        {
            if (eggItemId.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return suffix;
        }

        return "";
    }
}
