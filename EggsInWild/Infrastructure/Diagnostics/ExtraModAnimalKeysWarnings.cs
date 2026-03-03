using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.Mods;
using EggsInWild.Infrastructure.ModData.Models;

namespace EggsInWild.Infrastructure.Diagnostics;

public static class ExtraModAnimalKeysWarnings
{
    private enum Severity
    {
        Error = 0,
        Warning = 1,
        Info = 2,
    }

    private sealed record WarningItem(Severity Severity, string Code, string? ModId, string Message);

    public static List<string> Build(
        Dictionary<string, ExtraModAnimalKeysJson> modGroups,
        IModAvailability mods,
        ISet<string>? knownFarmAnimalIds)
    {
        var items = new List<WarningItem>();

        void Add(Severity severity, string code, string? modId, string message) =>
            items.Add(new WarningItem(severity, code, modId, message));

        foreach (var (modId, block) in modGroups.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(modId))
            {
                Add(Severity.Error, "EAMK_BLANK_MODID", null, "ExtraModAnimalKeys contains a blank modId key; block will be ignored.");
                continue;
            }

            var isVanilla = string.Equals(modId, "Vanilla", StringComparison.OrdinalIgnoreCase);
            if (!isVanilla && !mods.IsLoaded(modId))
                Add(Severity.Warning, "EAMK_UNLOADED_MODID", modId, $"ExtraModAnimalKeys modId '{modId}' is not loaded; its keys will be ignored.");

            var raw = block.FarmAnimalKeys ?? new List<string>();
            if (raw.Count == 0)
            {
                Add(Severity.Info, "EAMK_EMPTY_KEYS", modId, $"ExtraModAnimalKeys '{modId}' has no FarmAnimalKeys.");
                continue;
            }

            var blanks = raw.Count(s => string.IsNullOrWhiteSpace(s));
            if (blanks > 0)
                Add(Severity.Warning, "EAMK_BLANK_KEY", modId, $"ExtraModAnimalKeys '{modId}' has {blanks} blank FarmAnimalKeys entries.");

            var whitespace = raw.Count(s => !string.IsNullOrWhiteSpace(s) && s != s.Trim());
            if (whitespace > 0)
                Add(Severity.Info, "EAMK_WHITESPACE", modId, $"ExtraModAnimalKeys '{modId}' has {whitespace} keys with leading/trailing whitespace.");

            var trimmed = raw
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            var dupes = trimmed
                .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

            if (dupes.Count > 0)
                Add(Severity.Info, "EAMK_DUPES", modId, $"ExtraModAnimalKeys '{modId}' has duplicate keys (case-insensitive): {string.Join(", ", dupes)}.");

            var effective = block.GetEffectiveFarmAnimalKeys();
            if (knownFarmAnimalIds is not null)
            {
                var unknown = effective
                    .Where(x => !knownFarmAnimalIds.Contains(x))
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToList();

                if (unknown.Count > 0)
                    Add(Severity.Warning, "EAMK_UNKNOWN_ID", modId, $"ExtraModAnimalKeys '{modId}' has FarmAnimalKeys not present in Data/FarmAnimals: {string.Join(", ", unknown)}.");
            }

            if (effective.Count == 0)
                Add(Severity.Warning, "EAMK_NO_EFFECTIVE", modId, $"ExtraModAnimalKeys '{modId}' has no effective FarmAnimalKeys after normalization.");
        }

        static string Prefix(Severity severity) => severity switch
        {
            Severity.Error => "[ERROR]",
            Severity.Warning => "[WARNING]",
            _ => "[INFO]",
        };

        return items
            .Distinct()
            .OrderBy(x => x.Severity)
            .ThenBy(x => x.ModId is null ? 1 : 0)
            .ThenBy(x => x.ModId ?? "", StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Message, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{Prefix(x.Severity)} {x.Message}")
            .ToList();
    }
}
