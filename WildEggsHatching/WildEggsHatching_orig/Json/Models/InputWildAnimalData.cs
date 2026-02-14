using Mikew.BirdsOfAFeather.Helpers;
using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.Models
{
    public class InputWildAnimalData
    {
        public bool IsMagic { get; set; } = false;
        public bool LargeEgg {get; set;} = false;
        public int Price { get; set; } = 0;
        public Dictionary<LocationType, bool> LocationTypes { get; set;}
        public Dictionary<string, FarmAnimalKeys> ExtraModAnimalKeys { get; set; } = new();
        public InputWildAnimalData(){
            LocationTypes = EnumExtensions.GetValues<LocationType>().ToDictionary(x=>x,y=>false);
        }
        public WildAnimalData GetWildAnimalData(string name)
        {
            return new WildAnimalData(name, Price, IsMagic, LargeEgg ,ExtraModAnimalKeys, LocationTypes);
        }
    }
}
