using static LetMeHatch.EnumsAndStrings.Enums;

using static LetMeHatch.ModEntry;

namespace LetMeHatch.Models
{
    public class BreedingDataFemale
    {
        public bool ApplyDailyLuckBoost { get; set; } = true;
        public bool AdjustableEggChances { get; set; } = true;
        public Dictionary<ItemType, float> EggChances { get; set; } = new();
        public Dictionary<string, BreedingDataMale> Males { get; set; } = new();
        public ClonedAnimalInfo CloneConfig { get; set; } = new();

        public BreedingDataFemale()
        {
            EggChances = new()
            {
                { ItemType.Normal, 0.4f },
                { ItemType.Large, 0.6f },
                { ItemType.FertilizedEgg, 0.6f },
            };
        }
    }
}
