using System.Collections.Generic;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon;

/// <summary>Runtime rooster data used to apply game-data edits.</summary>
public sealed class RoosterData
{
    public RoosterType RoosterType { get; set; }

    public ProduceMode ProduceOption { get; set; } = ProduceMode.CustomizeBelow;
    public DeluxeProduceMode DeluxeProduceOption { get; set; } = DeluxeProduceMode.CustomizeBelow;

    public Dictionary<ProduceTypeEntries, Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>> AllProduces { get; set; } = new();

    public Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>> Eggs { get; set; } = new();

    public HarvestType HarvestType { get; set; } = HarvestType.DigUp;

    public int ProduceDays { get; set; }

    public int MatureDays { get; set; }

    public bool Buyable { get; set; }

    public RequiredBuilding BuildingRequired { get; set; } = RequiredBuilding.BigCoop;

    public string Female { get; set; } = string.Empty;

    public List<string> Females { get; set; } = new();

    public string FarmAnimalName { get; set; } = string.Empty;

    public int Price { get; set; }

    public int Quantity { get; set; }
}

public sealed class RoosterDataExport
{
    public Dictionary<RoosterType, RoosterData> Roosters { get; set; }

    public RoosterDataExport(Dictionary<RoosterType, RoosterData> roosters)
        => Roosters = roosters;
}

public sealed class FarmAnimalsExport
{
    public Dictionary<string, FarmAnimalData>? FarmAnimals { get; set; }

    public FarmAnimalsExport(Dictionary<string, FarmAnimalData>? farmAnimalData)
        => FarmAnimals = farmAnimalData;
}
