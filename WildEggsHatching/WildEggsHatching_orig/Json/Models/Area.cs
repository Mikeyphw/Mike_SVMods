using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.Models
{
    public class Area
    {
        public LocationType Location { get; set; }
        public string MapName { get; set; }
        public string ModID { get; set; }
        public int MinEggs { get; set; }
        public int MaxEggs { get; set; }
        public int EggsToSpawn { get; set; }
        public float RandomChance { get; set; }
        public HashSet<string> IncludeTerrainTypes { get; set; }
        public HashSet<string> ExcludeTerrainTypes { get; set; }
        public HashSet<string> IncludeCoordinates { get; set; }
        public HashSet<string> ExcludeCoordinates { get; set; }
        public ExtraConditions ExtraConditions { get; set; }
        public int SpawnedEggs { get; set; }
        public bool HitMaxSpawnTries { get; set; }
        public bool UseBaseRandomChance { get; set; }

        public Area()
        {
            HitMaxSpawnTries = false;
            UseBaseRandomChance = true;
            MaxEggs = 1;
            MinEggs = 1;
            SpawnedEggs = 0;
            RandomChance = 0.15f;
            IncludeTerrainTypes = new();
            ExcludeTerrainTypes = new() { "Wood" };
            IncludeCoordinates = new();
            ExcludeCoordinates = new();
            MapName = "Default";
            Location = LocationType.Farm;
            ModID = "Vanilla";
            ExtraConditions = new ExtraConditions(); // Initialize with default conditions
        }

        public Area DeepClone()
        {
            // Create a new Area object
            Area clonedArea = new()
            {
                EggsToSpawn = this.EggsToSpawn,
                UseBaseRandomChance = this.UseBaseRandomChance,
                HitMaxSpawnTries = this.HitMaxSpawnTries,
                MaxEggs = this.MaxEggs,
                MinEggs = this.MinEggs,
                Location = this.Location,
                MapName = this.MapName,
                ModID = this.ModID,
                RandomChance = this.RandomChance,
                SpawnedEggs = this.SpawnedEggs,
                IncludeTerrainTypes = new HashSet<string>(this.IncludeTerrainTypes), // Clone
                ExcludeTerrainTypes = new HashSet<string>(this.ExcludeTerrainTypes), // Clone
                IncludeCoordinates = new HashSet<string>(this.IncludeCoordinates), // Create a new HashSet
                ExcludeCoordinates = new HashSet<string>(this.ExcludeCoordinates), // Create a new HashSet
                ExtraConditions = this.ExtraConditions.DeepClone(), // Deep clone ExtraConditions
            };

            return clonedArea;
        }
    }
}
