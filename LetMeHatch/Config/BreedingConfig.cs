using LetMeHatch.Models;
using static LetMeHatch.EnumsAndStrings.Enums;

using static LetMeHatch.ModEntry;

namespace LetMeHatch.Config
{
    public class BreedingConfig
    {
        public bool ApplyDailyLuckBoost { get; set; }
        public Dictionary<string, float> MaleChance { get; set; }
        public Dictionary<string, float> BoostByMale { get; set; }
        public Dictionary<string, bool> Enabled { get; set; }
        public Dictionary<ItemType, float> EggChances { get; set; }
        public ClonedAnimalInfo MaleConfig { get; set; }

        public BreedingConfig()
        {
            MaleChance = new();
            BoostByMale = new();
            Enabled = new();
            ApplyDailyLuckBoost = true;
            EggChances = new()
            {
                { ItemType.Normal, 0.4f },
                { ItemType.Large, 0.6f },
                { ItemType.FertilizedEgg, 0.6f },
            };
            MaleConfig = new();
        }

        public BreedingConfig(BreedingDataFemale breedingDataFemale)
        {
            MaleChance = new();
            BoostByMale = new();
            Enabled = new();
            MaleConfig = breedingDataFemale.CloneConfig;
            ApplyDailyLuckBoost = breedingDataFemale.ApplyDailyLuckBoost;
            EggChances = new(breedingDataFemale.EggChances);
            foreach (var (maleKey, maleData) in breedingDataFemale.Males)
            {
                MaleChance[maleKey] = maleData.MaleChance;
                BoostByMale[maleKey] = maleData.BoostByMale;
                Enabled[maleKey] = maleData.Enabled;
            }
        }
    }
}
