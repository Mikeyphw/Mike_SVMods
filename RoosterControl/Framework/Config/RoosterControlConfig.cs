using System;
using System.Collections.Generic;
using RoosterControl.Constants;
using RoosterControl.Enums;

namespace RoosterControl.Framework.Config;

/// <summary>
/// The persisted configuration schema for RoosterControl.
/// </summary>
public sealed class RoosterControlConfig
{
    public bool VerboseLogging;
    public bool TestingLogging;

    public int SchemaVersion { get; set; } = ConfigSchema.CurrentVersion;

    public GeneralSettings General { get; set; } = new();

    /// <summary>Per-rooster settings. Only base rooster types are used by the mod today.</summary>
    public Dictionary<RoosterType, RoosterSettings> Roosters { get; set; } = CreateDefaultRoosters();

    /// <summary>Female animal key -> assigned rooster type (or <see cref="FemaleRoosterAssignment.Off"/>).</summary>
    public Dictionary<string, FemaleRoosterAssignment> FemalesOptions { get; set; } = new();

    public static RoosterControlConfig CreateDefault() => new();

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

    

    public CatalogApplyMode CatalogApplyMode { get; set; } = CatalogApplyMode.RepairInvalid;

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
    public string CustomItemName { get; set; } = ItemIds.CustomItems.Custom;

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


public enum CatalogApplyMode
{
    /// <summary>Only apply catalogs when config is newly created.</summary>
    SeedOnly,

    /// <summary>Repair clearly-invalid entries only (default).</summary>
    RepairInvalid,

    /// <summary>Fill missing values without overwriting user edits.</summary>
    MergeMissing,

    /// <summary>Catalogs overwrite config values each load/reload.</summary>
    Authoritative
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

public enum FemaleRoosterAssignment
{
    Off,
    White,
    Brown,
    Blue,
    Void,
    Gold
}
