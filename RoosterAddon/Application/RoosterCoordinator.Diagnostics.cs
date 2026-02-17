using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoosterAddon.Constants;
using RoosterAddon.Framework.Config;
using RoosterAddon.Infrastructure.StateDumps;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private void FarmAnimalsCommand(string command, string[] args)
    {
        var verb = args.FirstOrDefault()?.Trim() ?? "help";

        if (verb.Equals("help", StringComparison.OrdinalIgnoreCase) || verb.Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "ra_farmanimals usage:\n" +
                "  ra_farmanimals dump [all]\n" +
                "    - writes a JSON dump to data/state-dumps and logs a short summary.\n" +
                "    - default filters to RoosterAddon-relevant keys; add 'all' to include full Data/FarmAnimals.\n" +
                "  ra_farmanimals explain <animalKey>\n" +
                "    - writes a per-key JSON dump and logs why the key is included/excluded/missing.",
                LogLevel.Info
            );
            return;
        }

        if (!Context.IsWorldReady)
        {
            Monitor.Log("Save not loaded; farm animals diagnostics require a loaded save.", LogLevel.Info);
            return;
        }

        if (verb.Equals("dump", StringComparison.OrdinalIgnoreCase))
        {
            var includeAll = args.Skip(1).Any(a => a.Equals("all", StringComparison.OrdinalIgnoreCase));
            DumpFarmAnimals(includeAll);
            return;
        }

        if (verb.Equals("explain", StringComparison.OrdinalIgnoreCase))
        {
            var key = args.Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(key))
            {
                Monitor.Log("Usage: ra_farmanimals explain <animalKey>", LogLevel.Info);
                return;
            }

            ExplainFarmAnimal(key.Trim());
            return;
        }

        Monitor.Log($"Unknown subcommand '{verb}'. Run: ra_farmanimals help", LogLevel.Warn);
    }

    private void DumpFarmAnimals(bool includeAll)
    {
        EnsureRoostersSetup();

        Dictionary<string, FarmAnimalData> raw;
        try
        {
            raw = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed to load '{FarmAnimalsAssetName}': {ex}", LogLevel.Error);
            return;
        }

        var data = new Dictionary<string, FarmAnimalData>(raw, StringComparer.OrdinalIgnoreCase);
        var relevantKeys = BuildRelevantAnimalKeySet();
        var missingRelevantKeys = relevantKeys.Where(k => !data.ContainsKey(k)).OrderBy(k => k).ToList();

        var selected = includeAll
            ? data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
            : data.Where(kvp => relevantKeys.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        var reasons = BuildReasonMap(includeAll ? selected.Keys : relevantKeys, relevantKeys, data);

        var payload = new
        {
            Timestamp = DateTimeOffset.Now,
            Save = StardewModdingAPI.Constants.SaveFolderName,
            Day = new { SDate.Now().DaysSinceStart, Game1.Date.Season, Game1.Date.DayOfMonth, Game1.Date.Year },
            Config = new { Config.General, Config.FemalesOptions, Config.Roosters },
            Exclusions = _animalExclusions,
            Keys = new { Total = data.Count, Relevant = relevantKeys.Count, Included = selected.Count, MissingRelevant = missingRelevantKeys },
            Roosters = new RoosterDataExport(RoosterHelper.Roosters),
            FarmAnimals = new FarmAnimalsExport(selected),
            Reasons = reasons
        };

        var now = DateTimeOffset.Now;
        var fileName = StateDumpJson.BuildFileName("roosteraddon_farmanimals_dump", now);

        if (!TryWriteStateDump(fileName, payload, out var error))
        {
            Monitor.Log($"Failed to write FarmAnimals dump '{fileName}': {error}", LogLevel.Error);
            return;
        }

        Monitor.Log(
            $"FarmAnimals dump written: data/state-dumps/{fileName} (included={selected.Count}, relevant={relevantKeys.Count}, missingRelevant={missingRelevantKeys.Count})",
            LogLevel.Info
        );
    }

    private void ExplainFarmAnimal(string animalKey)
    {
        EnsureRoostersSetup();

        Dictionary<string, FarmAnimalData> raw;
        try
        {
            raw = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed to load '{FarmAnimalsAssetName}': {ex}", LogLevel.Error);
            return;
        }

        var data = new Dictionary<string, FarmAnimalData>(raw, StringComparer.OrdinalIgnoreCase);
        var relevantKeys = BuildRelevantAnimalKeySet();
        var reasons = BuildReasonMap(new[] { animalKey }, relevantKeys, data);

        var present = data.TryGetValue(animalKey, out var animalData);

        var payload = new
        {
            Timestamp = DateTimeOffset.Now,
            Save = StardewModdingAPI.Constants.SaveFolderName,
            Key = animalKey,
            Present = present,
            Reasons = reasons.TryGetValue(animalKey, out var why) ? why : Array.Empty<string>(),
            ExcludedBy = _animalExclusions.AnimalExclusions?.Contains(animalKey, StringComparer.OrdinalIgnoreCase) == true ? "data/exclusions.json" : null,
            FarmAnimalData = present ? animalData : null
        };

        var now = DateTimeOffset.Now;
        var safeKey = SanitizeFileComponent(animalKey);
        var fileName = StateDumpJson.BuildFileName($"roosteraddon_farmanimals_explain_{safeKey}", now);

        if (!TryWriteStateDump(fileName, payload, out var error))
        {
            Monitor.Log($"Failed to write FarmAnimals explain dump '{fileName}': {error}", LogLevel.Error);
            return;
        }

        var reasonText = reasons.TryGetValue(animalKey, out var rs) ? string.Join(", ", rs) : "<none>";
        Monitor.Log($"FarmAnimals explain written: data/state-dumps/{fileName} (present={present}, reasons=[{reasonText}])", LogLevel.Info);
    }

    private void LogFarmAnimalsStartupValidationSummary()
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        ValidatePercent("General.ChosenPercentageDailyLuckBonus", Config.General.ChosenPercentageDailyLuckBonus, warnings);
        ValidatePercent("General.EggProbability", Config.General.EggProbability, warnings);
        ValidatePercent("General.LargeEggProbability", Config.General.LargeEggProbability, warnings);
        ValidatePercent("General.FertilizedEggProbability", Config.General.FertilizedEggProbability, warnings);
        ValidatePercent("General.RoosterToChickenRatio", Config.General.RoosterToChickenRatio, warnings);

        if (Config.General.BoostByRooster < 0 || Config.General.BoostByRooster > 1000)
            warnings.Add($"General.BoostByRooster out of expected range (0-1000): {Config.General.BoostByRooster}");

        foreach (var (rt, rs) in Config.Roosters)
        {
            if (rs.MatureDays < 0) warnings.Add($"{rt}: MatureDays < 0 ({rs.MatureDays})");
            if (rs.ProduceDays < 0) warnings.Add($"{rt}: ProduceDays < 0 ({rs.ProduceDays})");
            if (rs.Price < 0) warnings.Add($"{rt}: Price < 0 ({rs.Price})");

            if (rs.ProduceOption == ProduceMode.CustomizeBelow && !HasAnyUsableProduce(rs.ProduceEntries))
                warnings.Add($"{rt}: ProduceEntries appears empty/disabled (CustomizeBelow but no weighted non-empty choice).");

            if (rs.DeluxeProduceOption == DeluxeProduceMode.CustomizeBelow && !HasAnyUsableProduce(rs.DeluxeProduceEntries))
                warnings.Add($"{rt}: DeluxeProduceEntries appears empty/disabled (CustomizeBelow but no weighted non-empty choice).");
        }

        var excludedCount = _animalExclusions.AnimalExclusions?.Distinct(StringComparer.OrdinalIgnoreCase).Count() ?? 0;
        var modIdCount = _animalExclusions.AnimalExclusionsModID?.Distinct(StringComparer.OrdinalIgnoreCase).Count() ?? 0;

        try
        {
            var fa = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);

            var required = new[] { RoosterKeys.White, RoosterKeys.Brown, RoosterKeys.Blue, RoosterKeys.Void, RoosterKeys.Gold };
            var missingRequired = required.Where(k => !fa.ContainsKey(k)).ToList();
            if (missingRequired.Count > 0)
                errors.Add($"Missing required keys in {FarmAnimalsAssetName}: {string.Join(", ", missingRequired)} (BOAF missing/broken?)");

            var missingFemaleKeys = (Config.FemalesOptions?.Keys ?? Array.Empty<string>())
                .Where(k => !string.IsNullOrWhiteSpace(k) && !fa.ContainsKey(k))
                .OrderBy(k => k)
                .ToList();

            if (missingFemaleKeys.Count > 0)
                warnings.Add($"FemalesOptions references missing animal keys: {string.Join(", ", missingFemaleKeys.Take(20))}{(missingFemaleKeys.Count > 20 ? " ..." : "")}");
        }
        catch (Exception ex)
        {
            errors.Add($"Validation could not load {FarmAnimalsAssetName}: {ex.Message}");
        }

        Monitor.Log(
            $"Startup validation: roosters={Config.Roosters.Count}, femalesOptions={Config.FemalesOptions.Count}, exclusions={excludedCount}, exclusionModIDs={modIdCount}, warnings={warnings.Count}, errors={errors.Count}",
            LogLevel.Info
        );

        foreach (var w in warnings)
            Monitor.Log($"Validation: {w}", LogLevel.Warn);

        if (errors.Count > 0)
        {
            foreach (var err in errors)
                Monitor.Log($"Validation(FATAL): {err}", LogLevel.Error);

            throw new InvalidOperationException("RoosterAddon startup validation failed. See SMAPI log for details.");
        }
    }

    private HashSet<string> BuildRelevantAnimalKeySet()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (RoosterHelper?.Roosters is not null)
        {
            foreach (var rooster in RoosterHelper.Roosters.Values)
            {
                AddIfNotEmpty(keys, rooster.FarmAnimalName);
                AddIfNotEmpty(keys, rooster.Female);

                if (rooster.Females is not null)
                    foreach (var f in rooster.Females)
                        AddIfNotEmpty(keys, f);
            }
        }

        if (Config?.FemalesOptions is not null)
            foreach (var k in Config.FemalesOptions.Keys)
                AddIfNotEmpty(keys, k);

        return keys;
    }

    private Dictionary<string, string[]> BuildReasonMap(
        IEnumerable<string> keys,
        HashSet<string> relevantKeys,
        Dictionary<string, FarmAnimalData> data)
    {
        var roosterKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (RoosterHelper?.Roosters is not null)
        {
            foreach (var rooster in RoosterHelper.Roosters.Values)
            {
                AddIfNotEmpty(roosterKeys, rooster.FarmAnimalName);
                AddIfNotEmpty(roosterKeys, rooster.Female);

                if (rooster.Females is not null)
                    foreach (var f in rooster.Females)
                        AddIfNotEmpty(roosterKeys, f);
            }
        }

        var configFemaleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Config?.FemalesOptions is not null)
            foreach (var k in Config.FemalesOptions.Keys)
                AddIfNotEmpty(configFemaleKeys, k);

        var excludedSet = new HashSet<string>(_animalExclusions.AnimalExclusions ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var reasons = new List<string>();

            if (roosterKeys.Contains(key))
                reasons.Add("ReferencedByRoosterRuntimeData");

            if (configFemaleKeys.Contains(key))
            {
                if (Config.FemalesOptions.TryGetValue(key, out var assign))
                    reasons.Add($"FemalesOptions:{assign}");
                else
                    reasons.Add("FemalesOptions:<missing?>");
            }

            if (excludedSet.Contains(key))
                reasons.Add("ExcludedByExclusionsJson");

            if (relevantKeys.Contains(key))
                reasons.Add("RelevantKey");

            if (!data.ContainsKey(key))
                reasons.Add("MissingFromData/FarmAnimals");

            result[key] = reasons.ToArray();
        }

        return result;
    }

    private static void AddIfNotEmpty(ISet<string> set, string? key)
    {
        if (!string.IsNullOrWhiteSpace(key))
            set.Add(key.Trim());
    }

    private static void ValidatePercent(string name, int value, List<string> warnings)
    {
        if (value < 0 || value > 100)
            warnings.Add($"{name} out of range (0-100): {value}");
    }

    private static bool HasAnyUsableProduce(List<ProduceEntry> entries)
    {
        entries ??= new List<ProduceEntry>();
        foreach (var e in entries)
        {
            if (e is null)
                continue;

            var choice = (e.Choice ?? string.Empty).Trim();
            if (choice.Equals(ItemIds.CustomItems.Custom, StringComparison.OrdinalIgnoreCase))
                choice = (e.CustomItemName ?? string.Empty).Trim();

            if (e.Probability > 0 && !string.IsNullOrWhiteSpace(choice))
                return true;
        }

        return false;
    }

    private bool TryWriteStateDump(string fileName, object payload, out string error)
    {
        try
        {
            WriteStateDump(fileName, payload);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }

    private static string SanitizeFileComponent(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "key" : sanitized;
    }
}
