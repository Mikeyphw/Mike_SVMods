using LetMeHatchThisEgg.Models;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Integrations
{
    public class VanillaGameIntegration
    {
        public static Dictionary<string, BreedingDataFemale> CreateBreedingData()
        {
            var whiteChicken = GetFemaleBreedingDataGroup(VanillaAnimals.WhiteChicken);
            var brownChicken = GetFemaleBreedingDataGroup(VanillaAnimals.BrownChicken);
            var blueChicken = GetFemaleBreedingDataGroup(VanillaAnimals.BlueChicken);
            var voidChicken = GetFemaleBreedingDataGroup(VanillaAnimals.VoidChicken);
            var goldChicken = GetFemaleBreedingDataGroup(VanillaAnimals.GoldenChicken);
            var duck = GetFemaleBreedingDataGroup(VanillaAnimals.Duck);
            var ostrich = GetFemaleBreedingDataGroup(VanillaAnimals.Ostrich);
            var dinosaur = GetFemaleBreedingDataGroup(VanillaAnimals.Dinosaur);

            return new Dictionary<string, BreedingDataFemale>()
            {
                {
                    VanillaAnimals.WhiteChicken,
                    whiteChicken
                },
                {
                    VanillaAnimals.BrownChicken,
                    brownChicken
                },
                {
                    VanillaAnimals.BlueChicken,
                    blueChicken
                },
                {
                    VanillaAnimals.VoidChicken,
                    voidChicken
                },
                {
                    VanillaAnimals.GoldenChicken,
                    goldChicken
                },
                {
                    VanillaAnimals.Ostrich,
                    ostrich
                },
                {
                    VanillaAnimals.Dinosaur,
                    dinosaur
                },
                {
                    VanillaAnimals.Duck,
                    duck
                },
            };
        }

        private static BreedingDataFemale GetFemaleBreedingDataGroup(string animalKey)
        {
            return new BreedingDataFemale
            {
                AdjustableEggChances = true,
                ApplyDailyLuckBoost = true,
                EggChances = new()
                {
                    { ItemType.Normal, 0.4f },
                    { ItemType.Large, 0.6f },
                    { ItemType.FertilizedEgg, 0.4f },
                },
                Males = new() { { animalKey, GetMaleData() } },
            };
        }

        private static BreedingDataMale GetMaleData()
        {
            return new()
            {
                AdjustableMaleChances = true,
                BoostByMale = 0.1f,
                Enabled = true,
                MaleChance = 0.4f,
                EggsIDs = new()
                {
                    { ItemType.Normal, "" },
                    { ItemType.Large, "" },
                    { ItemType.FertilizedEgg, "" },
                },
            };
        }
    }
}