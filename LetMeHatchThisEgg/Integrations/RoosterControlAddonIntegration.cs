using LetMeHatchThisEgg.Models;
using Mikew.BirdsOfAFeather.Helpers;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;

using static LetMeHatchThisEgg.ModEntry; namespace LetMeHatchThisEgg.Integrations
{
    public class RoosterControlAddonIntegration
    {
        public static Dictionary<string, BreedingDataFemale> CreateBreedingData()
        {
            var whiteChicken = GetFemaleBreedingDataGroup();
            var brownChicken = GetFemaleBreedingDataGroup();
            var blueChicken = GetFemaleBreedingDataGroup();
            var voidChicken = GetFemaleBreedingDataGroup();
            var goldChicken = GetFemaleBreedingDataGroup();
            var transRooster = GetFemaleBreedingDataGroup();

            if (ModIdEvaluator.Evaluate("Meara.Roosters"))
            {
                AddMales(
                    whiteChicken,
                    "cockwhite",
                    new()
                    {
                        { ItemType.Normal, AnimalProducts.WhiteEgg },
                        { ItemType.Large, AnimalProducts.LargeWhiteEgg },
                        { ItemType.FertilizedEgg, CustomItems.FertilizedWhiteEgg },
                    }
                );
                AddMales(
                    brownChicken,
                    "cockbrown",
                    new()
                    {
                        { ItemType.Normal, AnimalProducts.BrownEgg },
                        { ItemType.Large, AnimalProducts.LargeBrownEgg },
                        { ItemType.FertilizedEgg, CustomItems.FertilizedBrownEgg },
                    }
                );
                AddMales(
                    blueChicken,
                    "cockblue",
                    new()
                    {
                        { ItemType.Normal, CustomItems.MearaBlueEgg },
                        { ItemType.Large, CustomItems.MearaLargeBlueEgg },
                        { ItemType.FertilizedEgg, CustomItems.FertilizedBlueEgg },
                    }
                );
                AddMales(
                    voidChicken,
                    "cockvoid",
                    new()
                    {
                        { ItemType.Normal, AnimalProducts.VoidEgg },
                        { ItemType.Large, CustomItems.MearaLargeVoidEgg },
                        { ItemType.FertilizedEgg, CustomItems.FertilizedVoidEgg },
                    }
                );
                AddMales(
                    goldChicken,
                    "cockgold",
                    new()
                    {
                        { ItemType.Normal, AnimalProducts.GoldenEgg },
                        { ItemType.Large, CustomItems.MearaLargeGoldEgg },
                        { ItemType.FertilizedEgg, CustomItems.FertilizedGoldEgg },
                    }
                );
                AddMales(
                    transRooster,
                    "cockwhite",
                    new()
                    {
                        { ItemType.Normal, "" },
                        { ItemType.Large, "" },
                        { ItemType.FertilizedEgg, "" },
                    }
                );
                AddMales(
                    transRooster,
                    "cockbrown",
                    new()
                    {
                        { ItemType.Normal, "" },
                        { ItemType.Large, "" },
                        { ItemType.FertilizedEgg, "" },
                    }
                );
            }

            if (ModIdEvaluator.Evaluate("Blaklyte.CP.Rooster"))
            {
                AddMales(
                    whiteChicken,
                    "White Rooster",
                    new()
                    {
                        { ItemType.Normal, AnimalProducts.WhiteEgg },
                        { ItemType.Large, AnimalProducts.LargeWhiteEgg },
                        { ItemType.FertilizedEgg, "" },
                    }
                );
                AddMales(
                    brownChicken,
                    "Rooster",
                    new()
                    {
                        { ItemType.Normal, AnimalProducts.BrownEgg },
                        { ItemType.Large, AnimalProducts.LargeBrownEgg },
                        { ItemType.FertilizedEgg, "" },
                    }
                );
                AddMales(
                    blueChicken,
                    "Blue Rooster",
                    new()
                    {
                        { ItemType.Normal, "" },
                        { ItemType.Large, "" },
                        { ItemType.FertilizedEgg, "" },
                    }
                );
                AddMales(
                    voidChicken,
                    "Cockatrice",
                    new()
                    {
                        { ItemType.Normal, "" },
                        { ItemType.Large, "" },
                        { ItemType.FertilizedEgg, "" },
                    }
                );
                AddMales(
                    transRooster,
                    "White Rooster",
                    new()
                    {
                        { ItemType.Normal, "" },
                        { ItemType.Large, "" },
                        { ItemType.FertilizedEgg, "" },
                    }
                );
                AddMales(
                    transRooster,
                    "Rooster",
                    new()
                    {
                        { ItemType.Normal, "" },
                        { ItemType.Large, "" },
                        { ItemType.FertilizedEgg, "" },
                    }
                );
            }

            var data = new Dictionary<string, BreedingDataFemale>()
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
            };

            if (ModIdEvaluator.Evaluate("Blaklyte.CP.Rooster"))
            {
                data["Trans Rooster"] =  transRooster ;
            }

            return data;
        }

        private static BreedingDataFemale GetFemaleBreedingDataGroup()
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
                Males = new(),
            };
        }

        private static void AddMales(
            BreedingDataFemale group,
            string maleName,
            Dictionary<ItemType, string?> eggIds
        )
        {
            group.Males[maleName] = new()
            {
                AdjustableMaleChances = true,
                BoostByMale = 0.1f,
                Enabled = false,
                MaleChance = 0.4f,
                EggsIDs = eggIds,
            };
        }
    }
}
