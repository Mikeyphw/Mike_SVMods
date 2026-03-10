using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using RoosterControl.Constants;
using RoosterControl.Enums;
using StardewModdingAPI;
using Mikew.NestCore.Helpers.Logging;

using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace RoosterControl.Framework.Config;

internal static class LegacyConfigAdapter
{
    private static ILog? _log;
    public static bool TryMigrateFromFile(string path, IMonitor monitor, out RoosterControlConfig migrated)
    {
        _log = new SmapiLog(monitor);
        migrated = RoosterControlConfig.CreateDefault();

        if (!File.Exists(path))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return false;

            if (!LooksLikeLegacyConfig(root))
                return false;

            var cfg = RoosterControlConfig.CreateDefault();
            MapGeneral(root, cfg);
            MapRoosters(root, cfg);
            cfg.SchemaVersion = ConfigSchema.CurrentVersion;

            migrated = cfg;
            LogScopedHelper.Log(_log, ModRoosterControlCategory.SaveData, "RoosterControl: migrated legacy dictionary-based config to typed schema.", LogLevel.Info);
            return true;
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.SaveData, $"RoosterControl: failed to migrate legacy config: {ex}", LogLevel.Warn);
            return false;
        }
    }

    private static bool LooksLikeLegacyConfig(JsonElement root)
    {
        if (!TryGetProperty(root, "Options", out var options) || options.ValueKind != JsonValueKind.Object)
            return false;

        return TryGetProperty(root, "RoosterOptions", out var roosterOptions)
            && roosterOptions.ValueKind == JsonValueKind.Object;
    }

    private static void MapGeneral(JsonElement root, RoosterControlConfig cfg)
    {
        if (!TryGetProperty(root, "Options", out var options))
            return;

        var g = cfg.General;

        g.UpdateRate = ParseUpdateRate(GetLegacyValue(options, "UpdateRate"), g.UpdateRate);
        g.ChosenPercentageDailyLuckBonus = ParseInt(GetLegacyValue(options, "ChosenPercentage_DailyLuckBonus"), g.ChosenPercentageDailyLuckBonus);
        g.HatchingMode = ParseHatchingMode(GetLegacyValue(options, "HatchingMode"), g.HatchingMode);
        g.HarvestType = ParseHarvestType(GetLegacyValue(options, "HarvestType"), g.HarvestType);

        g.BetterTrufflesCompat = ParseBool(GetLegacyValue(options, "BetterTrufflesCompat"), g.BetterTrufflesCompat);
        g.EnableStuckInside = ParseBool(GetLegacyValue(options, "EnableStuckInside"), g.EnableStuckInside);
        g.EnableEggProbability = ParseBool(GetLegacyValue(options, "EnableEggProbability"), g.EnableEggProbability);
        g.EnableLargeEggProbability = ParseBool(GetLegacyValue(options, "EnableLargeEggProbability"), g.EnableLargeEggProbability);
        g.EnableDebug = ParseBool(GetLegacyValue(options, "EnableDebug"), g.EnableDebug);

        g.EggProbability = ParseInt(GetLegacyValue(options, "EggProbability"), g.EggProbability);
        g.LargeEggProbability = ParseInt(GetLegacyValue(options, "LargeEggProbability"), g.LargeEggProbability);
        g.FertilizedEggProbability = ParseInt(GetLegacyValue(options, "FertilizedEggProbability"), g.FertilizedEggProbability);
        g.RoosterToChickenRatio = ParseInt(GetLegacyValue(options, "RoosterToChickenRatio"), g.RoosterToChickenRatio);
        g.BoostByRooster = ParseFloat(GetLegacyValue(options, "BoostByRooster"), g.BoostByRooster);
    }

    private static void MapRoosters(JsonElement root, RoosterControlConfig cfg)
    {
        if (!TryGetProperty(root, "RoosterOptions", out var roosters) || roosters.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in roosters.EnumerateObject())
        {
            if (!TryParseRoosterType(prop.Name, out var roosterType))
                continue;

            if (prop.Value.ValueKind != JsonValueKind.Object)
                continue;

            if (!cfg.Roosters.TryGetValue(roosterType, out var settings) || settings is null)
            {
                settings = new RoosterSettings();
                cfg.Roosters[roosterType] = settings;
            }

            settings.BuildingRequired = ParseRequiredBuilding(GetLegacyValue(prop.Value, "BuildingRequired"), settings.BuildingRequired);
            settings.Buyable = ParseBool(GetLegacyValue(prop.Value, "Buyable"), settings.Buyable);
            settings.Price = ParseInt(GetLegacyValue(prop.Value, "Price"), settings.Price);
            settings.EnableRoosterHatching = ParseBool(GetLegacyValue(prop.Value, "EnableRoosterHatching"), settings.EnableRoosterHatching);
            settings.EditAllFemales = ParseBool(GetLegacyValue(prop.Value, "EditAllFemales"), settings.EditAllFemales);
            settings.MatureDays = ParseInt(GetLegacyValue(prop.Value, "MatureDays"), settings.MatureDays);
            settings.ProduceDays = ParseInt(GetLegacyValue(prop.Value, "ProduceDays"), settings.ProduceDays);
            settings.ProduceOption = ParseProduceMode(GetLegacyValue(prop.Value, "ProduceOption"), settings.ProduceOption);
            settings.DeluxeProduceOption = ParseDeluxeProduceMode(GetLegacyValue(prop.Value, "DeluxeProduceOption"), settings.DeluxeProduceOption);
            settings.BypassBlueConditions = ParseBool(GetLegacyValue(prop.Value, "BypassBlueConditions"), settings.BypassBlueConditions);

            var blueEggChoice = GetLegacyValue(prop.Value, "BlueEggChoice");
            if (!string.IsNullOrWhiteSpace(blueEggChoice))
                settings.BlueEggChoice = blueEggChoice.Trim();

            settings.ProduceEntries = BuildEntries(prop.Value, deluxe: false);
            settings.DeluxeProduceEntries = BuildEntries(prop.Value, deluxe: true);
        }
    }

    private static List<ProduceEntry> BuildEntries(JsonElement rooster, bool deluxe)
    {
        var result = new List<ProduceEntry>(4);
        var prefix = deluxe ? "Deluxe" : string.Empty;
        var customPrefix = deluxe ? "CustomDeluxe" : "Custom";

        for (var i = 1; i <= 4; i++)
        {
            var choice = GetLegacyValue(rooster, $"{prefix}Produce_{i}");
            var custom = GetLegacyValue(rooster, $"{customPrefix}Produce_{i}");
            var prob = ParseInt(GetLegacyValue(rooster, $"{prefix}Produce_{i}_Prob"), 0);

            result.Add(new ProduceEntry
            {
                Choice = string.IsNullOrWhiteSpace(choice) ? ItemIds.CustomItems.Custom : choice.Trim(),
                CustomItemName = string.IsNullOrWhiteSpace(custom) ? ItemIds.CustomItems.Custom : custom.Trim(),
                Probability = Math.Clamp(prob, 0, 100),
            });
        }

        return result;
    }

    private static string GetLegacyValue(JsonElement options, string optionName)
    {
        if (!TryGetProperty(options, optionName, out var optionNode) || optionNode.ValueKind != JsonValueKind.Object)
            return string.Empty;

        if (!TryGetProperty(optionNode, "Value", out var valueNode))
            return string.Empty;

        return valueNode.ValueKind switch
        {
            JsonValueKind.String => valueNode.GetString() ?? string.Empty,
            JsonValueKind.Number => valueNode.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => string.Empty
        };
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static bool TryParseRoosterType(string raw, out RoosterType roosterType)
    {
        if (Enum.TryParse(raw, ignoreCase: true, out roosterType))
            return true;

        roosterType = raw.Trim().ToLowerInvariant() switch
        {
            "cockwhite" => RoosterType.White,
            "cockbrown" => RoosterType.Brown,
            "cockblue" => RoosterType.Blue,
            "cockvoid" => RoosterType.Void,
            "cockgold" => RoosterType.Gold,
            _ => default
        };

        return raw.Trim().Equals("cockwhite", StringComparison.OrdinalIgnoreCase)
            || raw.Trim().Equals("cockbrown", StringComparison.OrdinalIgnoreCase)
            || raw.Trim().Equals("cockblue", StringComparison.OrdinalIgnoreCase)
            || raw.Trim().Equals("cockvoid", StringComparison.OrdinalIgnoreCase)
            || raw.Trim().Equals("cockgold", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ParseBool(string raw, bool fallback)
        => bool.TryParse(raw?.Trim(), out var value) ? value : fallback;

    private static int ParseInt(string raw, int fallback)
        => int.TryParse(raw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static float ParseFloat(string raw, float fallback)
        => float.TryParse(raw?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static UpdateRateMode ParseUpdateRate(string raw, UpdateRateMode fallback)
    {
        var text = raw?.Trim();
        if (string.IsNullOrEmpty(text))
            return fallback;

        return text.Equals("On Location Change", StringComparison.OrdinalIgnoreCase)
            ? UpdateRateMode.OnLocationChange
            : UpdateRateMode.OnDayStart;
    }

    private static HatchingMode ParseHatchingMode(string raw, HatchingMode fallback)
    {
        var text = raw?.Trim();
        if (string.IsNullOrEmpty(text))
            return fallback;

        return text.Equals("Only Fertilized Eggs", StringComparison.OrdinalIgnoreCase)
            ? HatchingMode.OnlyFertilizedEggs
            : HatchingMode.Original;
    }

    private static HarvestType ParseHarvestType(string raw, HarvestType fallback)
    {
        var text = raw?.Trim();
        if (string.IsNullOrEmpty(text))
            return fallback;

        return text.Equals("Drop Overnight", StringComparison.OrdinalIgnoreCase)
            ? HarvestType.DropOvernight
            : HarvestType.DigUp;
    }

    private static RequiredBuilding ParseRequiredBuilding(string raw, RequiredBuilding fallback)
    {
        var text = raw?.Trim();
        if (string.IsNullOrEmpty(text))
            return fallback;

        return text switch
        {
            var _ when text.Equals("Coop", StringComparison.OrdinalIgnoreCase) => RequiredBuilding.Coop,
            var _ when text.Equals("Deluxe Coop", StringComparison.OrdinalIgnoreCase) => RequiredBuilding.DeluxeCoop,
            _ => RequiredBuilding.BigCoop,
        };
    }

    private static ProduceMode ParseProduceMode(string raw, ProduceMode fallback)
    {
        var text = raw?.Trim();
        if (string.IsNullOrEmpty(text))
            return fallback;

        return text.Equals("Keep Original Produce", StringComparison.OrdinalIgnoreCase)
            ? ProduceMode.KeepOriginalProduce
            : ProduceMode.CustomizeBelow;
    }

    private static DeluxeProduceMode ParseDeluxeProduceMode(string raw, DeluxeProduceMode fallback)
    {
        var text = raw?.Trim();
        if (string.IsNullOrEmpty(text))
            return fallback;

        if (text.Equals("Off", StringComparison.OrdinalIgnoreCase))
            return DeluxeProduceMode.Off;

        return text.Equals("Keep Original Produce", StringComparison.OrdinalIgnoreCase)
            ? DeluxeProduceMode.KeepOriginalProduce
            : DeluxeProduceMode.CustomizeBelow;
    }

}
