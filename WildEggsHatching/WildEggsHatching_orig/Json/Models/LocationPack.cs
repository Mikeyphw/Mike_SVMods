using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.Models
{
    public class LocationPack
    {
        public string ModID { get; set; }
        public int NumberOfSpawnedEggs { get; set; }
        public int MinimumSpawnsPerDay { get; set; }
        public int MaximumSpawnsPerDay { get; set; }
        public Dictionary<LocationType, HashSet<string>> Locations { get; set; }

        public LocationPack()
        {
            Locations = new Dictionary<LocationType, HashSet<string>>();
            ModID = string.Empty;
            MaximumSpawnsPerDay = 1;
            MinimumSpawnsPerDay = 0;
            NumberOfSpawnedEggs = 0;
        }
    }
}
