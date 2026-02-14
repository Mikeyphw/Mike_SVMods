using System;
using System.Collections.Generic;
using RoosterAddon.Enums;
using RoosterAddon.Framework;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon
{
    /// <summary>The mod entry point.</summary
    public class RoosterData
    {
        public string ProduceOption { get; set; } ="";
        public string DeluxeProduceOption { get; set; } = "";
        public Dictionary<
            ProduceTypeEntries,
            Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>
        > AllProduces { get; set; } = new();
        public Dictionary<
            EggTypeEntries,
            Dictionary<RoosterItemDataEntries, string>
        > Eggs { get; set; } = new();

        //public string[] Eggs { get; set; }
        // public string FertilizedEgg { get; set; }
        public string HarvestType { get; set; } = "";
        public int ProduceDays { get; set; } = 0;
        public int MatureDays { get; set; } = 0;
        public bool Buyable { get; set; } = false;
        public string BuildingRequired { get; set; } = "";
        public string Female { get; set; } = "";
        public List<string> Females {get; set;} = new ();

        public string FarmAnimalName { get; set; } = "";
        public int Price { get; set; } = 0;
        public int Quantity { get; set; } = 0;

        public RoosterData() { }
    }

    public sealed class RoosterDataExport
    {
        public Dictionary<RoosterType, RoosterData> Roosters { get; set; }
        public RoosterDataExport(Dictionary<RoosterType, RoosterData> _roosters)
        {
            Roosters = _roosters;
        }
    }
    public sealed class FarmAnimalsExport
    {
        public Dictionary<string, FarmAnimalData>? FarmAnimals { get; set; }
        public FarmAnimalsExport(Dictionary<string, FarmAnimalData>? _farmAnimalData)
        {
            FarmAnimals = _farmAnimalData;
        }
    }


    
    
}
