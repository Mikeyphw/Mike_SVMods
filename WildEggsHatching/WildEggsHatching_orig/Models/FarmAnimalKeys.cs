namespace WildEggsHatching.Models
{
    public class FarmAnimalKeys
    {
        public HashSet<string> FarmAnimalKeysSameWeight { get; set; } = new();
        public HashSet<AnimalKeyData> FarmAnimalKeysWeighted { get; set; } = new();
    }
}
