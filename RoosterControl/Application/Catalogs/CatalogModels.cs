using System.Collections.Generic;

namespace RoosterControl.Application.Catalogs;

public sealed class CatalogBundle
{
    public RoosterCatalog Roosters { get; set; } = new();
    public ProduceCatalog Produce { get; set; } = new();
    public HatchingCatalog Hatching { get; set; } = new();
}

public sealed class RoosterCatalog
{
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Keyed by rooster type name (e.g. "White").</summary>
    public Dictionary<string, RoosterCatalogEntry> Roosters { get; set; } = new();
}

public sealed class RoosterCatalogEntry
{
    public string FarmAnimalKey { get; set; } = string.Empty;
    public EggCatalog Eggs { get; set; } = new();
}

public sealed class EggCatalog
{
    public string Normal { get; set; } = string.Empty;
    public string Large { get; set; } = string.Empty;
    public string Fertilized { get; set; } = string.Empty;
}

public sealed class ProduceCatalog
{
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Random group key -> list of choice keys.</summary>
    public Dictionary<string, List<string>> RandomGroups { get; set; } = new();

    /// <summary>Keyed by rooster type name (e.g. "White").</summary>
    public Dictionary<string, ProduceCatalogEntry> Roosters { get; set; } = new();
}

public sealed class ProduceCatalogEntry
{
    /// <summary>Optional override for Blue egg choice key (compat).</summary>
    public string? BlueEggChoice { get; set; }

    public List<ProduceEntryDto> Produce { get; set; } = new();
    public List<ProduceEntryDto> Deluxe { get; set; } = new();
}

public sealed class ProduceEntryDto
{
    public string Choice { get; set; } = string.Empty;
    public string CustomItemName { get; set; } = string.Empty;
    public int Probability { get; set; }
}

public sealed class HatchingCatalog
{
    public int SchemaVersion { get; set; } = 1;
    public HatchingGeneralCatalog General { get; set; } = new();
    public Dictionary<string, HatchingRoosterCatalog> Roosters { get; set; } = new();
}

public sealed class HatchingGeneralCatalog
{
    public string? UpdateRate { get; set; }

    /// <summary>Percent (0-100).</summary>
    public int? ChosenPercentageDailyLuckBonus { get; set; }

    public string? HarvestType { get; set; }
    public string? HatchingMode { get; set; }

    public bool? BetterTrufflesCompat { get; set; }
    public bool? EnableStuckInside { get; set; }
    public bool? EnableEggProbability { get; set; }
    public bool? EnableLargeEggProbability { get; set; }
    public bool? EnableDebug { get; set; }

    /// <summary>Percent (0-100).</summary>
    public int? EggProbability { get; set; }

    /// <summary>Percent (0-100).</summary>
    public int? LargeEggProbability { get; set; }

    /// <summary>Percent (0-100).</summary>
    public int? FertilizedEggProbability { get; set; }

    /// <summary>Percent (0-100).</summary>
    public int? RoosterToChickenRatio { get; set; }

    /// <summary>Percent (0-1000).</summary>
    public float? BoostByRooster { get; set; }
}

public sealed class HatchingRoosterCatalog
{
    public string? BuildingRequired { get; set; }
    public bool? Buyable { get; set; }
    public int? Price { get; set; }

    public bool? EnableRoosterHatching { get; set; }
    public bool? EditAllFemales { get; set; }

    public int? MatureDays { get; set; }
    public int? ProduceDays { get; set; }

    public string? ProduceOption { get; set; }
    public string? DeluxeProduceOption { get; set; }

    public bool? BypassBlueConditions { get; set; }
}
