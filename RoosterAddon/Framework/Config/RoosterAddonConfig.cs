using System;
using System.Collections.Generic;
using System.Globalization;
using RoosterAddon.Enums;
using RoosterAddon.Constants;
using RoosterAddon.Framework;

namespace RoosterAddon.Framework.Config;

/// <summary>
/// The persisted configuration schema for RoosterAddon (Phase 3).
/// </summary>
public sealed class RoosterAddonConfig : Mikew.BirdsOfAFeather.Framework.CoreModConfig
{
    public int SchemaVersion { get; set; } = ConfigSchema.CurrentVersion;

    public GeneralSettings General { get; set; } = new();

    /// <summary>Per-rooster settings. Only the base rooster types are used by the mod today.</summary>
    public Dictionary<RoosterType, RoosterSettings> Roosters { get; set; } = CreateDefaultRoosters();

    /// <summary>Female -> rooster type mapping for LMH clone-egg mode.</summary>
    public Dictionary<string, string> FemalesOptions { get; set; } = new();

    public static RoosterAddonConfig CreateDefault()
        => LegacyConfigAdapter.FromLegacy(new ModConfig());

    internal static Dictionary<RoosterType, RoosterSettings> CreateDefaultRoosters()
        => new()
        {
            [RoosterType.White] = new RoosterSettings(),
            [RoosterType.Brown] = new RoosterSettings(),
            [RoosterType.Blue] = new RoosterSettings(),
            [RoosterType.Void] = new RoosterSettings(),
            [RoosterType.Gold] = new RoosterSettings(),
        };
}

public sealed class GeneralSettings
{
    public UpdateRateMode UpdateRate { get; set; } = UpdateRateMode.OnDayStart;

    /// <summary>Percent (0-100).</summary>
    public int ChosenPercentageDailyLuckBonus { get; set; } = 10;

    public HatchingMode HatchingMode { get; set; } = HatchingMode.Original;

    public HarvestType HarvestType { get; set; } = HarvestType.DigUp;

    public bool BetterTrufflesCompat { get; set; } = true;

    public bool EnableStuckInside { get; set; } = true;

    public bool EnableEggProbability { get; set; } = true;

    public bool EnableLargeEggProbability { get; set; } = true;

    public bool EnableDebug { get; set; } = true;

    /// <summary>Percent (0-100).</summary>
    public int EggProbability { get; set; } = 20;

    /// <summary>Percent (0-100).</summary>
    public int LargeEggProbability { get; set; } = 40;

    /// <summary>Percent (0-100).</summary>
    public int FertilizedEggProbability { get; set; } = 60;

    /// <summary>Percent (0-100).</summary>
    public int RoosterToChickenRatio { get; set; } = 50;

    /// <summary>Percent (0-1000).</summary>
    public float BoostByRooster { get; set; } = 0.1f;
}

public sealed class RoosterSettings
{
    public RequiredBuilding BuildingRequired { get; set; } = RequiredBuilding.BigCoop;
    public bool Buyable { get; set; } = true;
    public int Price { get; set; } = 2400;

    public bool EnableRoosterHatching { get; set; } = true;
    public bool EditAllFemales { get; set; } = true;

    public int MatureDays { get; set; } = 4;
    public int ProduceDays { get; set; } = 1;

    public ProduceMode ProduceOption { get; set; } = ProduceMode.CustomizeBelow;
    public DeluxeProduceMode DeluxeProduceOption { get; set; } = DeluxeProduceMode.CustomizeBelow;

    public bool BypassBlueConditions { get; set; } = false;

    /// <summary>Blue egg item key to prefer (compat). Stored as an item key/string.</summary>
    public string BlueEggChoice { get; set; } = ItemIds.CustomItems.MearaBlueEgg;

    public List<ProduceEntry> ProduceEntries { get; set; } = ProduceEntry.CreateDefaultFour();
    public List<ProduceEntry> DeluxeProduceEntries { get; set; } = ProduceEntry.CreateDefaultFour();
}

public sealed class ProduceEntry
{
    /// <summary>
    /// Item string or the sentinel "Custom" to use <see cref="CustomItemName"/>.
    /// </summary>
    public string Choice { get; set; } = string.Empty;

    /// <summary>
    /// Free text used when <see cref="Choice"/> is "Custom".
    /// </summary>
    public string CustomItemName { get; set; } = "Custom";

    /// <summary>Percent (0-100).</summary>
    public int Probability { get; set; }

    public static List<ProduceEntry> CreateDefaultFour()
        => new()
        {
            new ProduceEntry(),
            new ProduceEntry(),
            new ProduceEntry(),
            new ProduceEntry(),
        };
}

public enum UpdateRateMode
{
    OnDayStart,
    OnLocationChange
}

public enum HatchingMode
{
    Original,
    OnlyFertilizedEggs
}

public enum HarvestType
{
    DigUp,
    DropOvernight
}

public enum RequiredBuilding
{
    Coop,
    BigCoop,
    DeluxeCoop
}

public enum ProduceMode
{
    KeepOriginalProduce,
    CustomizeBelow
}

public enum DeluxeProduceMode
{
    Off,
    KeepOriginalProduce,
    CustomizeBelow
}

/// <summary>String conversion helpers to preserve legacy config value text.</summary>
public static class ConfigStringConversions
{
    public static string ToLegacyString(UpdateRateMode mode)
        => mode switch
        {
            UpdateRateMode.OnDayStart => OptionValues.OnDayStart,
            UpdateRateMode.OnLocationChange => OptionValues.OnLocationChange,
            _ => OptionValues.OnDayStart
        };

    public static UpdateRateMode ParseUpdateRate(string value)
        => value switch
        {
            OptionValues.OnLocationChange => UpdateRateMode.OnLocationChange,
            _ => UpdateRateMode.OnDayStart
        };

    public static string ToLegacyString(HatchingMode mode)
        => mode switch
        {
            HatchingMode.OnlyFertilizedEggs => OptionValues.OnlyFertilizedEggs,
            _ => OptionValues.Original
        };

    public static HatchingMode ParseHatchingMode(string value)
        => value switch
        {
            OptionValues.OnlyFertilizedEggs => HatchingMode.OnlyFertilizedEggs,
            _ => HatchingMode.Original
        };

    public static string ToLegacyString(HarvestType type)
        => type switch
        {
            HarvestType.DropOvernight => OptionValues.DropOvernight,
            _ => OptionValues.DigUp
        };

    public static HarvestType ParseHarvestType(string value)
        => value switch
        {
            OptionValues.DropOvernight => HarvestType.DropOvernight,
            _ => HarvestType.DigUp
        };

    public static string ToLegacyString(RequiredBuilding building)
        => building switch
        {
            RequiredBuilding.Coop => OptionValues.Coop,
            RequiredBuilding.DeluxeCoop => OptionValues.DeluxeCoop,
            _ => OptionValues.BigCoop
        };

    public static RequiredBuilding ParseRequiredBuilding(string value)
        => value switch
        {
            OptionValues.Coop => RequiredBuilding.Coop,
            OptionValues.DeluxeCoop => RequiredBuilding.DeluxeCoop,
            _ => RequiredBuilding.BigCoop
        };

    public static string ToLegacyString(ProduceMode mode)
        => mode switch
        {
            ProduceMode.KeepOriginalProduce => OptionValues.KeepOriginalProduce,
            _ => OptionValues.CustomizeBelow
        };

    public static ProduceMode ParseProduceMode(string value)
        => value switch
        {
            OptionValues.KeepOriginalProduce => ProduceMode.KeepOriginalProduce,
            _ => ProduceMode.CustomizeBelow
        };

    public static string ToLegacyString(DeluxeProduceMode mode)
        => mode switch
        {
            DeluxeProduceMode.Off => OptionValues.Off,
            DeluxeProduceMode.KeepOriginalProduce => OptionValues.KeepOriginalProduce,
            _ => OptionValues.CustomizeBelow
        };

    public static DeluxeProduceMode ParseDeluxeProduceMode(string value)
        => value switch
        {
            OptionValues.Off => DeluxeProduceMode.Off,
            OptionValues.KeepOriginalProduce => DeluxeProduceMode.KeepOriginalProduce,
            _ => DeluxeProduceMode.CustomizeBelow
        };

    public static float ParseFloat(string value, float fallback)
        => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;

    public static int ParseInt(string value, int fallback)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : fallback;

    public static bool ParseBool(string value, bool fallback)
        => bool.TryParse(value, out var b) ? b : fallback;
}
