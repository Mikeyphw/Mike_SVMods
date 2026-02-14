using LocationType = WildEggsHatching.EnumsAndStrings.Enums.LocationType;

namespace WildEggsHatching.Models
{
    public class WildEggExporter
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public bool IsMagic { get; set; }
        public bool IsLarge { get; set; }
        public bool Spawned { get; set; }

        //public ChosenAnimal? ChosenAnimalData { get; set; }
        public HashSet<string> GameLocations { get; set; }
        public LocationType Location { get; set; }
        public HashSet<string> AnimalList { get; set; }
        public string? ChosenAnimal { get; set; }
        public string? ChosenAnimalKey { get; set; }

        public WildEggExporter(WildEgg wildEgg)
        {
            ID = wildEgg.ID;
            Spawned = wildEgg.Spawned;
            Name = wildEgg.Name;
            IsMagic = wildEgg.IsMagic;
            IsLarge = wildEgg.IsLarge;
            GameLocations = wildEgg.GameLocations;
            Location = wildEgg.Location;
            AnimalList = new HashSet<string>(wildEgg.AnimalList.Select(a => a.Name));
            ChosenAnimal = wildEgg.ChosenAnimal?.Name;
            ChosenAnimalKey = wildEgg.ChosenAnimalKey;
        }
    }
}
