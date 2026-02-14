using System.Collections.Generic;
using StardewModdingAPI;
using WildEggsHatching.EnumsAndStrings;
using WildEggsHatching.Models;
using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.Services
{
    /// <summary>
    /// Class responsible for generating JSON data files for the mod.
    /// Generates LocationPacks.json, SpawnAreas.json, and SpawnParameters.json in ModData folder.
    /// Excludes wildAnimals.json as requested.
    /// </summary>
    public class JsonDataGenerator
    {
        private readonly IModHelper Helper;

        public JsonDataGenerator(IModHelper helper)
        {
            Helper = helper;
        }

        /// <summary>
        /// Generates the LocationPacks data structure with exact values as in the JSON.
        /// </summary>
        /// <returns>Dictionary of LocationPack keyed by mod ID.</returns>
        public Dictionary<string, LocationPack> GenerateLocationPacks()
        {
            var locationPacks = new Dictionary<string, LocationPack>();

            locationPacks["Vanilla"] = new LocationPack
            {
                ModID = "Vanilla",
                NumberOfSpawnedEggs = 0,
                MinimumSpawnsPerDay = 15,
                MaximumSpawnsPerDay = 15,
                Locations = new Dictionary<LocationType, HashSet<string>>
                {
                    {
                        LocationType.Farm,
                        new HashSet<string> { "Farm" }
                    },
                    {
                        LocationType.Forest,
                        new HashSet<string> { "Forest" }
                    },
                    {
                        LocationType.Woods,
                        new HashSet<string> { "Woods" }
                    },
                    {
                        LocationType.Beach,
                        new HashSet<string> { "Beach", "IslandWest" }
                    },
                    {
                        LocationType.Mountain,
                        new HashSet<string> { "Mountain" }
                    },
                    {
                        LocationType.Town,
                        new HashSet<string> { "Town" }
                    },
                    {
                        LocationType.Island,
                        new HashSet<string> { "IslandNorth", "IslandWest" }
                    },
                    {
                        LocationType.IslandBeach,
                        new HashSet<string> { "IslandSouth", "IslandSouthEast" }
                    },
                    {
                        LocationType.DeepIsland,
                        new HashSet<string> { "IslandEast" }
                    },
                    {
                        LocationType.Cliff,
                        new HashSet<string> { "Railroad" }
                    },
                    { LocationType.Magical, new HashSet<string>() },
                    {
                        LocationType.Desert,
                        new HashSet<string> { "Desert" }
                    },
                    {
                        LocationType.Cave,
                        new HashSet<string> { "Mine" }
                    },
                    {
                        LocationType.Swamp,
                        new HashSet<string> { "WitchSwamp" }
                    },
                    {
                        LocationType.BugLand,
                        new HashSet<string> { "BugLand" }
                    },
                    {
                        LocationType.Clearing,
                        new HashSet<string> { "Backwoods", "BusStop" }
                    },
                },
            };

            locationPacks["Rafseazz.RSVCP"] = new LocationPack
            {
                ModID = "Rafseazz.RSVCP",
                NumberOfSpawnedEggs = 0,
                MinimumSpawnsPerDay = 15,
                MaximumSpawnsPerDay = 15,
                Locations = new Dictionary<LocationType, HashSet<string>>
                {
                    {
                        LocationType.Forest,
                        new HashSet<string> { "Custom_Ridgeside_RidgeForest" }
                    },
                    {
                        LocationType.Woods,
                        new HashSet<string>
                        {
                            "Custom_Ridgeside_RidgeFalls",
                            "Custom_Ridgeside_RidgeForest",
                        }
                    },
                    {
                        LocationType.Mountain,
                        new HashSet<string>
                        {
                            "Custom_Ridgeside_RidgePond",
                            "Custom_Ridgeside_Ridge",
                        }
                    },
                    {
                        LocationType.Town,
                        new HashSet<string> { "Custom_Ridgeside_RidgesideVillage" }
                    },
                    {
                        LocationType.Cliff,
                        new HashSet<string>
                        {
                            "Custom_Ridgeside_RSVWestCliff",
                            "Custom_Ridgeside_RSVCliff",
                            "Custom_Ridgeside_SummitFarm",
                        }
                    },
                    {
                        LocationType.Magical,
                        new HashSet<string>
                        {
                            "Custom_Ridgeside_MysticFalls1",
                            "Custom_Ridgeside_MysticFalls2",
                            "Custom_Ridgeside_MysticFalls3",
                            "Custom_Ridgeside_RSVSpiritRealm",
                        }
                    },
                    {
                        LocationType.Clearing,
                        new HashSet<string>
                        {
                            "Custom_Ridgeside_RSVTheHike",
                            "Custom_Ridgeside_RSVRoad",
                            "Custom_Ridgeside_RSVCableCar",
                        }
                    },
                },
            };

            locationPacks["LemurKat.EastScarpe.CP"] = new LocationPack
            {
                ModID = "LemurKat.EastScarpe.CP",
                NumberOfSpawnedEggs = 0,
                MinimumSpawnsPerDay = 15,
                MaximumSpawnsPerDay = 15,
                Locations = new Dictionary<LocationType, HashSet<string>>
                {
                    {
                        LocationType.Forest,
                        new HashSet<string> { "EastScarp_Orchard", "EastScarp_ArkOrchard" }
                    },
                    {
                        LocationType.Woods,
                        new HashSet<string> { "EastScarp_DeepMountains" }
                    },
                    {
                        LocationType.Beach,
                        new HashSet<string> { "EastScarp_Village" }
                    },
                    {
                        LocationType.Mountain,
                        new HashSet<string> { "EastScarp_MineEntrance" }
                    },
                    {
                        LocationType.Town,
                        new HashSet<string> { "EastScarp_Village" }
                    },
                    {
                        LocationType.Clearing,
                        new HashSet<string> { "EastScarp_MeadowFarm" }
                    },
                    {
                        LocationType.Cave,
                        new HashSet<string> { "EastScarp_SmugglerDen", "EastScarp_DeepDark" }
                    },
                },
            };

            locationPacks["SunberryTeam.SBVSMAPI"] = new LocationPack
            {
                ModID = "SunberryTeam.SBVSMAPI",
                NumberOfSpawnedEggs = 0,
                MinimumSpawnsPerDay = 15,
                MaximumSpawnsPerDay = 15,
                Locations = new Dictionary<LocationType, HashSet<string>>
                {
                    {
                        LocationType.Farm,
                        new HashSet<string> { "Custom_SBV_SunberryFarm" }
                    },
                    {
                        LocationType.Forest,
                        new HashSet<string> { "Custom_SBV_SunberryForest" }
                    },
                    {
                        LocationType.Woods,
                        new HashSet<string> { "Custom_SBV_SecretForest1" }
                    },
                    {
                        LocationType.Mountain,
                        new HashSet<string> { "Custom_SBV_SunberryMountain" }
                    },
                    {
                        LocationType.Town,
                        new HashSet<string> { "Custom_SBV_SunberryVillage" }
                    },
                },
            };

            locationPacks["Tarniyar.CP.WalkToDesert"] = new LocationPack
            {
                ModID = "Tarniyar.CP.WalkToDesert",
                NumberOfSpawnedEggs = 0,
                MinimumSpawnsPerDay = 15,
                MaximumSpawnsPerDay = 15,
                Locations = new Dictionary<LocationType, HashSet<string>>
                {
                    {
                        LocationType.Desert,
                        new HashSet<string> { "Tarniyar.CP.WalkToDesert_Ghost_Town" }
                    },
                    {
                        LocationType.Forest,
                        new HashSet<string> { "Tarniyar.CP.WalkToDesert_Meadow" }
                    },
                    {
                        LocationType.Clearing,
                        new HashSet<string>
                        {
                            "Tarniyar.CP.WalkToDesert_Forest_Road",
                            "Tarniyar.CP.WalkToDesert_Winding_Road",
                            "Tarniyar.CP.WalkToDesert_Interchange",
                        }
                    },
                },
            };

            locationPacks["Lumisteria.MtVapius"] = new LocationPack
            {
                ModID = "Lumisteria.MtVapius",
                NumberOfSpawnedEggs = 0,
                MinimumSpawnsPerDay = 15,
                MaximumSpawnsPerDay = 15,
                Locations = new Dictionary<LocationType, HashSet<string>>
                {
                    {
                        LocationType.Woods,
                        new HashSet<string> { "Lumisteria.MtVapius_ForestJunimoClearing" }
                    },
                    {
                        LocationType.Forest,
                        new HashSet<string>
                        {
                            "Lumisteria.MtVapius_Clearing",
                            "Lumisteria.MtVapius_Lodge",
                            "Lumisteria.MtVapius_ForestTrailSinuous",
                            "Lumisteria.MtVapius_ForestTrailWoodland",
                            "Lumisteria.MtVapius_ForestTrailPond",
                            "Lumisteria.MtVapius_ForestTrailLong",
                            "Lumisteria.MtVapius_ForestTrailShortcut",
                        }
                    },
                    {
                        LocationType.Mountain,
                        new HashSet<string>
                        {
                            "Lumisteria.MtVapius_Cliff",
                            "Lumisteria.MtVapius_ForestCrystalLake",
                        }
                    },
                    {
                        LocationType.Clearing,
                        new HashSet<string>
                        {
                            "Lumisteria.MtVapius_Hamlet",
                            "Lumisteria.MtVapius_Orchard",
                            "Lumisteria.MtVapius_Faycombe",
                            "Lumisteria.MtVapius_FaycombeMeadow",
                        }
                    },
                    {
                        LocationType.Cave,
                        new HashSet<string>
                        {
                            "Lumisteria.MtVapius_ForestCaveLong",
                            "Lumisteria.MtVapius_ForestCaveRock",
                            "Lumisteria.MtVapius_ForestCaveHigh",
                            "Lumisteria.MtVapius_ForestCaveLush",
                            "Lumisteria.MtVapius_ForestCaveStar",
                            "Lumisteria.MtVapius_ForestCaveShortcut",
                            "Lumisteria.MtVapius_ForestCaveAmber",
                            "Lumisteria.MtVapius_ForestCaveMoonPond",
                            "Lumisteria.MtVapius_ForestCaveMarsh",
                            "Lumisteria.MtVapius_ForestCaveWinterRift",
                        }
                    },
                },
            };

            locationPacks["dreamy.kickitspot"] = new LocationPack
            {
                ModID = "dreamy.kickitspot",
                NumberOfSpawnedEggs = 0,
                MinimumSpawnsPerDay = 15,
                MaximumSpawnsPerDay = 15,
                Locations = new Dictionary<LocationType, HashSet<string>>
                {
                    {
                        LocationType.Forest,
                        new HashSet<string> { "Custom_CapeForest" }
                    },
                    {
                        LocationType.Beach,
                        new HashSet<string> { "Custom_CapeStardew" }
                    },
                    {
                        LocationType.Clearing,
                        new HashSet<string>
                        {
                            "Custom_CapeBusStop",
                            "Custom_CapeBusStop2",
                            "Custom_AnnettaForest",
                            "Custom_CapeMeadowForestSouth",
                            "Custom_CapeMineCart",
                            "Custom_CapeStardew",
                        }
                    },
                },
            };

            locationPacks["Azathii.ForgottenWoods"] = new LocationPack
            {
                ModID = "Azathii.ForgottenWoods",
                NumberOfSpawnedEggs = 0,
                MinimumSpawnsPerDay = 15,
                MaximumSpawnsPerDay = 15,
                Locations = new Dictionary<LocationType, HashSet<string>>
                {
                    {
                        LocationType.Woods,
                        new HashSet<string>
                        {
                            "Azathii.ForgottenWoods_ForgottenFarm",
                            "Azathii.ForgottenWoods_ForgottenHill",
                            "Azathii.ForgottenWoods_ForgottenPeak",
                            "Azathii.ForgottenWoods_ForgottenShrine",
                            "Azathii.ForgottenWoods_ForgottenExit",
                            "Azathii.ForgottenWoods_ForgottenPicnic",
                            "Azathii.ForgottenWoods_ForgottenCrypt",
                        }
                    },
                },
            };

            return locationPacks;
        }

        /// <summary>
        /// Generates the SpawnAreas data structure with no data as per user request.
        /// </summary>
        /// <returns>Empty dictionary.</returns>
        public Dictionary<string, HashSet<Area>> GenerateSpawnAreas()
        {
            return new Dictionary<string, HashSet<Area>>();
        }

        /// <summary>
        /// Generates the SpawnParameters data with default values as the JSON file does not exist.
        /// </summary>
        /// <returns>SpawnParameters object with default or generated values.</returns>
        /* public SpawnParameters GenerateSpawnParameters()
         {
             return new SpawnParameters
             {
                 MaxSpawningTries = 400,
                 MaxEggsPerMap = 2,
                 BaseRandomChance = 0.15f,
                 SavedMinEggArea = null,
                 SavedMaxEggArea = null,
                 SavedMinEggPack = null,
                 SavedMaxEggPack = null,
                 MaxWorldEggs = 100,
                 UseJsonValues = false,
             };
         }*/
    }
}
