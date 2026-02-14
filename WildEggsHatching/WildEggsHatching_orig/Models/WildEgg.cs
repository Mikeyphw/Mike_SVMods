using WildEggsHatching.Managers;
using static WildEggsHatching.EnumsAndStrings.Enums;
using LocationType = WildEggsHatching.EnumsAndStrings.Enums.LocationType;

namespace WildEggsHatching.Models
{
    public class WildEgg
    {
        internal string ID { get; set; }
        internal string Name { get; set; }
        internal bool IsMagic { get; set; }
        internal bool IsLarge { get; set; }
        internal bool Spawned { get; set; }

        //internal ChosenAnimal? ChosenAnimalData { get; set; }
        internal HashSet<string> GameLocations { get; set; }
        internal LocationType Location { get; set; }
        internal HashSet<WildAnimalData> AnimalList { get; set; }
        internal WildEggManager Manager { get; set; }
        internal WildAnimalData? ChosenAnimal { get; set; }
        internal string? ChosenAnimalKey { get; set; }

        internal WildEgg(
            LocationType locationType,
            string eggID,
            bool isMagic,
            bool isLarge,
            WildListManager wildListManager
        )
        {
            ID = eggID;
            Spawned = false;
            IsMagic = isMagic;
            IsLarge = isLarge;
            Location = locationType;
            Name = GetName(this);
            AnimalList = new();
            GameLocations = new();
            Manager = new WildEggManager(wildListManager, this);
        }

        internal bool IsValid => AnimalList != null && AnimalList.Count > 0;

        public override string ToString()
        {
            // Get the names of the animals in the AnimalList
            var animalNames = AnimalList.Select(animal => animal.Name).ToList();

            // Get the names of the game locations

            // Format the output string
            return $"{Name} (ID: {ID})"
                + $"\nAnimals: {string.Join(", ", animalNames)},"
                + $"\nGame Locations: {string.Join(", ", GameLocations)}"
                + $"\nChosen Animal: {Manager.GetChosenAnimal(false)};"
                + $"\nChosen Animal Key: {Manager.GetChosenAnimal(true)};";
        }

        public static string GetName(WildEgg egg)
        {
            // Determine the title based on IsMagic and IsLarge
            string title = (egg.IsMagic, egg.IsLarge) switch
            {
                (false, false) => "Wild Egg",
                (false, true) => "Large Wild Egg",
                (true, false) => "Magic Wild Egg",
                (true, true) => "Large Magic Wild Egg",
            };
            return $"{egg.Location} {title}";
        }

        public static string GetName(string eggID)
        {
            // Extract the prefix and location from the eggID

            string prefix = "Mikew.wildeggshatching_";

            string? location = eggID.StartsWith(prefix)
                ? eggID.Substring(prefix.Length, eggID.IndexOf("Egg") - prefix.Length)
                : null;

            // Determine if the egg is magic

            bool isMagic = eggID.Contains('M');

            // Determine if the egg is large

            bool isLarge = eggID.Contains('L');

            // Construct the egg name based on the parsed information

            string eggType = isMagic ? "Magic " : "Normal ";

            string size = isLarge ? "Large " : "Normal ";

            // Return the constructed egg name

            return $"{location} {size}{eggType}";
        }

        public static EggType GetType(string eggID)
        {
            // Extract the prefix and location from the eggID

            string prefix = "Mikew.wildeggshatching_";

            string? location = eggID.StartsWith(prefix)
                ? eggID.Substring(prefix.Length, eggID.IndexOf("Egg") - prefix.Length)
                : null;

            // Determine if the egg is magic

            bool isMagic = eggID.Contains('M');

            // Determine if the egg is large

            bool isLarge = eggID.Contains('L');

            return (isMagic, isLarge) switch
            {
                (false, false) => EggType.wildEgg,
                (false, true) => EggType.largeWildEgg,
                (true, false) => EggType.magicWildEgg,
                (true, true) => EggType.largeMagicWildEgg,
            };
        }
    }
}
