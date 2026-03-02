using System.Collections.Generic;

namespace RoosterControl.Application.Compat;

/// <summary>
/// Built-in compat catalogs used as defaults in all builds.
/// These are also used as the base content for the CP-patchable compat assets.
/// </summary>
public static class CompatDefaults
{
    public const string EggSourcesAssetSuffix = "compat/egg_sources";
    public const string RoosterSourcesAssetSuffix = "compat/rooster_sources";

    public static EggSourcesCatalog CreateEggSourcesCatalog()
        => new()
        {
            Sources = new List<EggSourceDef>
            {
                // Vanilla / "whatever FarmAnimals currently says" sources (supports egg-changing mods).
                new()
                {
                    SourceId = "farmanimal:brown-chicken:egg0",
                    DisplayName = "Brown Chicken (EggItemIds[0])",
                    EggKind = EggKind.BrownEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("Brown Chicken", EggSourceRefList.EggItemIds, 0)
                },
                new()
                {
                    SourceId = "farmanimal:brown-chicken:egg1",
                    DisplayName = "Brown Chicken (EggItemIds[1])",
                    EggKind = EggKind.LargeBrownEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("Brown Chicken", EggSourceRefList.EggItemIds, 1)
                },
                new()
                {
                    SourceId = "farmanimal:white-chicken:egg0",
                    DisplayName = "White Chicken (EggItemIds[0])",
                    EggKind = EggKind.WhiteEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("White Chicken", EggSourceRefList.EggItemIds, 0)
                },
                new()
                {
                    SourceId = "farmanimal:white-chicken:egg1",
                    DisplayName = "White Chicken (EggItemIds[1])",
                    EggKind = EggKind.LargeWhiteEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("White Chicken", EggSourceRefList.EggItemIds, 1)
                },
                new()
                {
                    SourceId = "farmanimal:blue-chicken:egg0",
                    DisplayName = "Blue Chicken (EggItemIds[0])",
                    EggKind = EggKind.BlueEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("Blue Chicken", EggSourceRefList.EggItemIds, 0)
                },
                new()
                {
                    SourceId = "farmanimal:blue-chicken:egg1",
                    DisplayName = "Blue Chicken (EggItemIds[1])",
                    EggKind = EggKind.LargeBlueEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("Blue Chicken", EggSourceRefList.EggItemIds, 1)
                },
                new()
                {
                    SourceId = "farmanimal:void-chicken:egg0",
                    DisplayName = "Void Chicken (EggItemIds[0])",
                    EggKind = EggKind.VoidEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("Void Chicken", EggSourceRefList.EggItemIds, 0)
                },
                new()
                {
                    SourceId = "farmanimal:void-chicken:egg1",
                    DisplayName = "Void Chicken (EggItemIds[1])",
                    EggKind = EggKind.LargeVoidEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("Void Chicken", EggSourceRefList.EggItemIds, 1)
                },
                new()
                {
                    SourceId = "farmanimal:golden-chicken:egg0",
                    DisplayName = "Golden Chicken (EggItemIds[0])",
                    EggKind = EggKind.GoldEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("Golden Chicken", EggSourceRefList.EggItemIds, 0)
                },
                new()
                {
                    SourceId = "farmanimal:golden-chicken:egg1",
                    DisplayName = "Golden Chicken (EggItemIds[1])",
                    EggKind = EggKind.LargeGoldEgg,
                    FarmAnimalRef = new FarmAnimalEggRef("Golden Chicken", EggSourceRefList.EggItemIds, 1)
                },

                // Meara (native eggs + fertilized eggs + large void/gold).
                new()
                {
                    SourceId = "meara:blue-egg",
                    DisplayName = "Meara Roosters: Blue Egg",
                    EggKind = EggKind.BlueEgg,
                    ItemId = "Meara.RoostersBlueEgg"
                },
                new()
                {
                    SourceId = "meara:large-blue-egg",
                    DisplayName = "Meara Roosters: Large Blue Egg",
                    EggKind = EggKind.LargeBlueEgg,
                    ItemId = "Meara.RoostersLargeBlueEgg"
                },
                new()
                {
                    SourceId = "meara:large-void-egg",
                    DisplayName = "Meara Roosters: Large Void Egg",
                    EggKind = EggKind.LargeVoidEgg,
                    ItemId = "Meara.RoostersLargevoidEgg"
                },
                new()
                {
                    SourceId = "meara:large-gold-egg",
                    DisplayName = "Meara Roosters: Large Gold Egg",
                    EggKind = EggKind.LargeGoldEgg,
                    ItemId = "Meara.RoostersLargegoldEgg"
                },
                new()
                {
                    SourceId = "meara:fert-brown",
                    DisplayName = "Meara Roosters: Fertilized Brown Egg",
                    EggKind = EggKind.FertilizedBrownEgg,
                    ItemId = "Meara.Roosters_FertEggBrown"
                },
                new()
                {
                    SourceId = "meara:fert-white",
                    DisplayName = "Meara Roosters: Fertilized White Egg",
                    EggKind = EggKind.FertilizedWhiteEgg,
                    ItemId = "Meara.Roosters_FertEggWhite"
                },
                new()
                {
                    SourceId = "meara:fert-blue",
                    DisplayName = "Meara Roosters: Fertilized Blue Egg",
                    EggKind = EggKind.FertilizedBlueEgg,
                    ItemId = "Meara.Roosters_FertEggBlue"
                },
                new()
                {
                    SourceId = "meara:fert-void",
                    DisplayName = "Meara Roosters: Fertilized Void Egg",
                    EggKind = EggKind.FertilizedVoidEgg,
                    ItemId = "Meara.Roosters_FertEggVoid"
                },
                new()
                {
                    SourceId = "meara:fert-gold",
                    DisplayName = "Meara Roosters: Fertilized Gold Egg",
                    EggKind = EggKind.FertilizedGoldEgg,
                    ItemId = "Meara.Roosters_FertEggGold"
                },

                // Blaklyte (rooster egg; fertilized eggs are generated in a later phase).
                new()
                {
                    SourceId = "blaklyte:rooster-egg",
                    DisplayName = "Blaklyte Roosters: Rooster Egg",
                    EggKind = EggKind.RoosterEgg,
                    ItemId = "Blaklyte.CP.Rooster_rooster_egg"
                }
            }
        };

    public static RoosterSourcesCatalog CreateRoosterSourcesCatalog()
        => new()
        {
            Sources = new List<RoosterSourceDef>
            {
                new()
                {
                    SourceId = "meara",
                    DisplayName = "Meara Roosters",
                    FarmAnimalKeys = new List<RoosterFarmAnimalKeyDef>
                    {
                        new() { Key = "cockbrown", BreedingRole = BreedingRole.Male },
                        new() { Key = "cockwhite", BreedingRole = BreedingRole.Male },
                        new() { Key = "cockblue", BreedingRole = BreedingRole.Male },
                        new() { Key = "cockvoid", BreedingRole = BreedingRole.Male },
                        new() { Key = "cockgold", BreedingRole = BreedingRole.Male }
                    }
                },
                new()
                {
                    SourceId = "blaklyte",
                    DisplayName = "Blaklyte Roosters",
                    RoosterEggItemId = "Blaklyte.CP.Rooster_rooster_egg",
                    FarmAnimalKeys = new List<RoosterFarmAnimalKeyDef>
                    {
                        new() { Key = "Rooster", BreedingRole = BreedingRole.Male },
                        new() { Key = "White Rooster", BreedingRole = BreedingRole.Male },
                        new() { Key = "Blue Rooster", BreedingRole = BreedingRole.Male },
                        new() { Key = "Desert Rooster", BreedingRole = BreedingRole.Male },

                        // Per user preference: treated as non-breeding (egg-layer behavior is inherited).
                        new() { Key = "Trans Rooster", BreedingRole = BreedingRole.None },

                        new() { Key = "Cockatrice", BreedingRole = BreedingRole.Male }
                    }
                }
            }
        };
}
