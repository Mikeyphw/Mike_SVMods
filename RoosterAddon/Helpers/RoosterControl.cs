using System.Reflection;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework;
using Mikew.BirdsOfAFeather.Framework.Random;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Helpers
{
    /// <summary>The mod entry point.</summary>
    ///
    public class RoosterControl
    {
        static readonly Random rnd = new();
        public IModHelper Helper { get; set; }
        public IMonitor Monitor { get; set; }
        private bool DailyLuckAdded { get; set; }
        public LuckyTypes DailyLuck { private get; set; }
        public Dictionary<RoosterType, RoosterData> Roosters { get; set; }
        public ModConfig ModCfg { get; set; }
        public Dictionary<string, Dictionary<EggTypeEntries, string>> FemaleEggs { get; set; }

        public RoosterControl(ref ModConfig _options, IModHelper Helper, IMonitor monitor)
        {
            this.ModCfg = _options;
            this.Helper = Helper;
            this.Monitor = monitor;
            Roosters = new Dictionary<RoosterType, RoosterData>();
            DailyLuck = LuckyTypes.SuperUnlucky;
            DailyLuckAdded = false;
            FemaleEggs = new();
        }

        public static bool UpdatePercentage(int chance)
        {
            if (chance <= 0)
                return false;
            if (chance >= 100)
                return true;

            return rnd.Next(100) < chance;
        }

        public static bool UpdatePercentage(float chance)
        {
            if (chance <= 0)
                return false;
            if (chance >= 100)
                return true;

            return rnd.NextDouble() * 100.0 < chance;
        }

        private Dictionary<
            EggTypeEntries,
            Dictionary<RoosterItemDataEntries, string>
        > SetUpEggsEntries(RoosterType roosterType, string[] eggs, int[] probabilities)
        {
            if (eggs.Length != probabilities.Length)
                throw new ArgumentException(
                    "The length of eggs and probabilities arrays must be equal."
                );
            var RoosterHatching = Convert.ToBoolean(
                ModCfg.RoosterOptions[roosterType][OptionEntries.EnableRoosterHatching][
                    OptionDetails.Value
                ]
            );
            var eggsDictionary =
                new Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>();
            if (
                ModCfg.Options[OptionEntries.HatchingMode][OptionDetails.Value]
                == OptionValues.OnlyFertilizedEggs
            )
            {
                probabilities[0] = 0;
                probabilities[1] = 0;
            }
            for (int i = 0; i < eggs.Length; i++)
            {
                var eggOption = new Dictionary<RoosterItemDataEntries, string>
                {
                    { RoosterItemDataEntries.Name, eggs[i] },
                    {
                        RoosterItemDataEntries.Probability,
                        RoosterHatching ? probabilities[i].ToString() : "0"
                    }
                };
                eggsDictionary.Add((EggTypeEntries)i, eggOption);
            }
            return eggsDictionary;
        }

        private Dictionary<
            RoosterType,
            Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>
        > SetUpEggs(
            Dictionary<
                RoosterType,
                Dictionary<OptionEntries, Dictionary<OptionDetails, string>>
            > options,
            int normalEggProbability,
            int largeEggProbability,
            int fertilizedEggProbability
        )
        {
            var eggProbabilities = new int[]
            {
                normalEggProbability,
                largeEggProbability,
                fertilizedEggProbability
            };
            var roosterEggs = new Dictionary<RoosterType, string[]>
            {
                {
                    RoosterType.White,
                    new string[]
                    {
                        ItemIds.VariableValues[ItemIds.AnimalProducts.WhiteEgg],
                        ItemIds.VariableValues[ItemIds.AnimalProducts.LargeWhiteEgg],
                        ItemIds.VariableValues[ItemIds.CustomItems.FertilizedWhiteEgg]
                    }
                },
                {
                    RoosterType.Brown,
                    new string[]
                    {
                        ItemIds.VariableValues[ItemIds.AnimalProducts.BrownEgg],
                        ItemIds.VariableValues[ItemIds.AnimalProducts.LargeBrownEgg],
                        ItemIds.VariableValues[ItemIds.CustomItems.FertilizedBrownEgg]
                    }
                },
                {
                    RoosterType.Blue,
                    new string[]
                    {
                        ItemIds.VariableValues[ItemIds.CustomItems.MearaBlueEgg],
                        ItemIds.VariableValues[ItemIds.CustomItems.MearaLargeBlueEgg],
                        ItemIds.VariableValues[ItemIds.CustomItems.FertilizedBlueEgg]
                    }
                },
                {
                    RoosterType.Void,
                    new string[]
                    {
                        ItemIds.VariableValues[ItemIds.AnimalProducts.VoidEgg],
                        ItemIds.VariableValues[ItemIds.CustomItems.MearaLargeVoidEgg],
                        ItemIds.VariableValues[ItemIds.CustomItems.FertilizedVoidEgg]
                    }
                },
                {
                    RoosterType.Gold,
                    new string[]
                    {
                        ItemIds.VariableValues[ItemIds.AnimalProducts.GoldenEgg],
                        ItemIds.VariableValues[ItemIds.CustomItems.MearaLargeGoldEgg],
                        ItemIds.VariableValues[ItemIds.CustomItems.FertilizedGoldEgg]
                    }
                }
            };

            var finalEggs =
                new Dictionary<
                    RoosterType,
                    Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>
                >();
            foreach (var roosterType in options.Keys)
            {
                if (!roosterEggs.TryGetValue(roosterType, out var eggs))
                    continue;

                var eggsDictionary = SetUpEggsEntries(roosterType, eggs, eggProbabilities);
                finalEggs.Add(roosterType, eggsDictionary);
            }
            return finalEggs;
        }

        //
        private Dictionary<
            ProduceTypeEntries,
            Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>
        > SetUpProducesEntries(
            Dictionary<OptionEntries, Dictionary<OptionDetails, string>> roosterData
        )
        {
            Dictionary<
                ProduceEntries,
                Dictionary<RoosterItemDataEntries, string>
            > CreateProduceEntry(
                OptionEntries produceOption,
                OptionEntries probabilityOption,
                OptionEntries customProduceOption
            )
            {
                return new Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>
                {
                    {
                        ProduceEntries.First,
                        new Dictionary<RoosterItemDataEntries, string>
                        {
                            {
                                RoosterItemDataEntries.Name,
                                roosterData[produceOption][OptionDetails.Value]
                                == ItemIds.CustomItems.Custom
                                    ? roosterData[customProduceOption][OptionDetails.Value]
                                    : ProduceTranslation(
                                        roosterData[produceOption][OptionDetails.Value]
                                    )
                            },
                            {
                                RoosterItemDataEntries.Probability,
                                roosterData[probabilityOption][OptionDetails.Value]
                            }
                        }
                    },
                    {
                        ProduceEntries.Second,
                        new Dictionary<RoosterItemDataEntries, string>
                        {
                            {
                                RoosterItemDataEntries.Name,
                                roosterData[produceOption + 1][OptionDetails.Value]
                                == ItemIds.CustomItems.Custom
                                    ? roosterData[customProduceOption + 1][OptionDetails.Value]
                                    : ProduceTranslation(
                                        roosterData[produceOption + 1][OptionDetails.Value]
                                    )
                            },
                            {
                                RoosterItemDataEntries.Probability,
                                roosterData[probabilityOption + 1][OptionDetails.Value]
                            }
                        }
                    },
                    {
                        ProduceEntries.Third,
                        new Dictionary<RoosterItemDataEntries, string>
                        {
                            {
                                RoosterItemDataEntries.Name,
                                roosterData[produceOption + 2][OptionDetails.Value]
                                == ItemIds.CustomItems.Custom
                                    ? roosterData[customProduceOption + 2][OptionDetails.Value]
                                    : ProduceTranslation(
                                        roosterData[produceOption + 2][OptionDetails.Value]
                                    )
                            },
                            {
                                RoosterItemDataEntries.Probability,
                                roosterData[probabilityOption + 2][OptionDetails.Value]
                            }
                        }
                    },
                    {
                        ProduceEntries.Fourth,
                        new Dictionary<RoosterItemDataEntries, string>
                        {
                            {
                                RoosterItemDataEntries.Name,
                                roosterData[produceOption + 3][OptionDetails.Value]
                                == ItemIds.CustomItems.Custom
                                    ? roosterData[customProduceOption + 3][OptionDetails.Value]
                                    : ProduceTranslation(
                                        roosterData[produceOption + 3][OptionDetails.Value]
                                    )
                            },
                            {
                                RoosterItemDataEntries.Probability,
                                roosterData[probabilityOption + 3][OptionDetails.Value]
                            }
                        }
                    }
                };
            }

            var produce = CreateProduceEntry(
                OptionEntries.Produce_1,
                OptionEntries.Produce_1_Prob,
                OptionEntries.CustomProduce_1
            );

            var deluxe = CreateProduceEntry(
                OptionEntries.DeluxeProduce_1,
                OptionEntries.DeluxeProduce_1_Prob,
                OptionEntries.CustomDeluxeProduce_1
            );

            return new Dictionary<
                ProduceTypeEntries,
                Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>
            >
            {
                { ProduceTypeEntries.Produce, produce },
                { ProduceTypeEntries.Deluxe, deluxe }
            };
        } //

        public void SetUpRoosters()
        {
            if (ModCfg == null)
            {
                Monitor.Log("DailyLuck or ModCfg is null", LogLevel.Warn);

                return;
            }
            var all_produces = new Dictionary<
                RoosterType,
                Dictionary<
                    ProduceTypeEntries,
                    Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>
                >
            >
            {
                {
                    RoosterType.White,
                    SetUpProducesEntries(ModCfg.RoosterOptions[RoosterType.White])
                },
                {
                    RoosterType.Brown,
                    SetUpProducesEntries(ModCfg.RoosterOptions[RoosterType.Brown])
                },
                { RoosterType.Blue, SetUpProducesEntries(ModCfg.RoosterOptions[RoosterType.Blue]) },
                { RoosterType.Void, SetUpProducesEntries(ModCfg.RoosterOptions[RoosterType.Void]) },
                { RoosterType.Gold, SetUpProducesEntries(ModCfg.RoosterOptions[RoosterType.Gold]) },
            };
            var all_eggs = SetUpEggs(
                new Dictionary<
                    RoosterType,
                    Dictionary<OptionEntries, Dictionary<OptionDetails, string>>
                >
                {
                    { RoosterType.White, ModCfg.RoosterOptions[RoosterType.White] },
                    { RoosterType.Brown, ModCfg.RoosterOptions[RoosterType.Brown] },
                    { RoosterType.Blue, ModCfg.RoosterOptions[RoosterType.Blue] },
                    { RoosterType.Void, ModCfg.RoosterOptions[RoosterType.Void] },
                    { RoosterType.Gold, ModCfg.RoosterOptions[RoosterType.Gold] },
                },
                ApplyAdjustmentsAndUpdatePercentage(
                    Int32.Parse(ModCfg.Options[OptionEntries.EggProbability][OptionDetails.Value])
                ),
                ApplyAdjustmentsAndUpdatePercentage(
                    Int32.Parse(
                        ModCfg.Options[OptionEntries.LargeEggProbability][OptionDetails.Value]
                    )
                ),
                ModCfg.Options[OptionEntries.HatchingMode][OptionDetails.Value]
                == OptionValues.OnlyFertilizedEggs
                    ? ApplyAdjustmentsAndUpdatePercentage(
                        Int32.Parse(
                            ModCfg.Options[OptionEntries.FertilizedEggProbability][
                                OptionDetails.Value
                            ]
                        )
                    )
                    : 0
            );
            var white = SetUpRoosterType(RoosterType.White, all_produces, all_eggs);
            var brown = SetUpRoosterType(RoosterType.Brown, all_produces, all_eggs);
            var blue = SetUpRoosterType(RoosterType.Blue, all_produces, all_eggs);
            var voidd = SetUpRoosterType(RoosterType.Void, all_produces, all_eggs);
            var gold = SetUpRoosterType(RoosterType.Gold, all_produces, all_eggs);
            Roosters = new()
            {
                { RoosterType.White, white },
                { RoosterType.Brown, brown },
                { RoosterType.Blue, blue },
                { RoosterType.Void, voidd },
                { RoosterType.Gold, gold }
            };
            foreach (var roosterType in Roosters)
            {
                var roosterData = roosterType.Value;
                var rng = new SystemRandomProvider(rnd);

var produceTable = new WeightedTable<Dictionary<RoosterItemDataEntries, string>>(rng);
foreach (var item in roosterData.AllProduces[ProduceTypeEntries.Produce])
{
    if (item.Key != ProduceEntries.Chosen)
    {
        _ = int.TryParse(item.Value[RoosterItemDataEntries.Probability], out var weight);
        produceTable.Add(item.Value, weight);
    }
}
roosterData.AllProduces[ProduceTypeEntries.Produce][ProduceEntries.Chosen] =
    produceTable.RollOrDefault(new Dictionary<RoosterItemDataEntries, string>());

var deluxeTable = new WeightedTable<Dictionary<RoosterItemDataEntries, string>>(rng);
foreach (var item in roosterData.AllProduces[ProduceTypeEntries.Deluxe])
{
    if (item.Key != ProduceEntries.Chosen)
    {
        _ = int.TryParse(item.Value[RoosterItemDataEntries.Probability], out var weight);
        deluxeTable.Add(item.Value, weight);
    }
}
roosterData.AllProduces[ProduceTypeEntries.Deluxe][ProduceEntries.Chosen] =
    deluxeTable.RollOrDefault(new Dictionary<RoosterItemDataEntries, string>());
var FertilizedEggProbability = roosterData.Eggs[EggTypeEntries.Fertilized][
                    RoosterItemDataEntries.Probability
                ];
                roosterData.Eggs[EggTypeEntries.Fertilized][RoosterItemDataEntries.Probability] =
                    ApplyAdjustmentsAndUpdatePercentage_RoosterBoost(
                        roosterData.Quantity,
                        float.Parse(FertilizedEggProbability)
                    );
            }
        }

        public static IEnumerable<FarmAnimal> GetRoostersFromCoops()
        {
            return Game1
                .getFarm()
                .buildings.Where(b => IsCoop(b))
                .SelectMany(coop => GetRoostersFromCoop(coop));
        }

        static bool IsCoop(Building b)
        {
            return b.buildingType.Value.ToLower().Contains("coop");
        }

        static IEnumerable<FarmAnimal> GetRoostersFromCoop(Building coop)
        {
            var animalHouse = (AnimalHouse)coop.indoors.Value;
            var cockTypes = new[]
            {
                RoosterKeys.White,
                RoosterKeys.Void,
                RoosterKeys.Blue,
                RoosterKeys.Gold,
                RoosterKeys.Brown
            };
            return animalHouse.Animals.Values.Where(animal =>
                cockTypes.Contains(animal.type.Value)
            );
        }

        public void UpdateRoosterQuantity_All()
        {
            foreach (var roosterType in Roosters.Select(r => r.Value))
            {
                roosterType.Quantity = UpdateRoosterQuantity(roosterType.FarmAnimalName);
            }
        }

        private static int UpdateRoosterQuantity(string farmName)
        {
            bool IsCoop(Building b)
            {
                return b.buildingType.Value.ToLower().Contains("coop");
            }
            int quantityInCoops = Game1
                .getFarm()
                .buildings.Where(b => IsCoop(b))
                .Sum(coop =>
                    ((AnimalHouse)coop.indoors.Value).animals.Values.Count(animal =>
                        animal.type.Value == farmName && animal.isAdult()
                    )
                );

            int quantityOutsideCoops = Game1
                .getFarm()
                .animals.Values.Count(animal =>
                    IsCoop(animal.home) && animal.type.Value == farmName && animal.isAdult()
                );
            var total = quantityInCoops + quantityOutsideCoops;

            return total;
        }

        public int ApplyAdjustmentsAndUpdatePercentage(int originalPercentage)
        {
            if (originalPercentage == 0)
            {
                return 0;
            }
            var ChosenDailyLuckBonus = Int32.Parse(
                ModCfg.Options[OptionEntries.ChosenPercentage_DailyLuckBonus][OptionDetails.Value]
            );
            var luckAdjustment = DailyLuck switch
            {
                LuckyTypes.SuperUnlucky => -2 * ChosenDailyLuckBonus,
                LuckyTypes.Neutral => 0,
                LuckyTypes.Unlucky => -ChosenDailyLuckBonus,
                LuckyTypes.Lucky => ChosenDailyLuckBonus,
                LuckyTypes.SuperLucky => +1.5 * ChosenDailyLuckBonus,
                _ => 0 // Default case (unknown luck type)
            };
            DailyLuckAdded = true;
            return Math.Clamp(originalPercentage + (int)luckAdjustment, 0, 100);
        }

        public string ApplyAdjustmentsAndUpdatePercentage_RoosterBoost(
            int quantity,
            float originalPercentage
        )
        {
            if (
                ModCfg.Options[OptionEntries.HatchingMode][OptionDetails.Value]
                != OptionValues.OnlyFertilizedEggs
            )
            {
                return "0";
            }
            else if (originalPercentage < 0)
            {
                return originalPercentage.ToString();
            }
            else
            {
                var adjusted =
                    quantity > 0
                        ? originalPercentage
                            + quantity
                                * float.Parse(
                                    ModCfg.Options[OptionEntries.BoostByRooster][
                                        OptionDetails.Value
                                    ]
                                )
                        : 0;
                adjusted = Math.Clamp(adjusted, 0, 100);

                return adjusted.ToString();
            }
        }

        internal static string ProduceTranslation(string ProduceName)
        {
            string getNewName(string[] namesList)
            {
                int index = rnd.Next(namesList.Length);

                return ProduceTranslation(namesList[index]);
            }
            if (ProduceName.Contains("Random"))
            {
                return ProduceName switch
                {
                    ItemIds.RandomProduces.RandomColorEgg
                        => getNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.BrownEgg,
                                ItemIds.AnimalProducts.WhiteEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomColorLargeEgg
                        => getNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.LargeBrownEgg,
                                ItemIds.AnimalProducts.LargeWhiteEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeWhiteEgg
                        => getNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.LargeWhiteEgg,
                                ItemIds.AnimalProducts.WhiteEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeBrownEgg
                        => getNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.BrownEgg,
                                ItemIds.AnimalProducts.LargeBrownEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomColorSizeEgg
                        => getNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.BrownEgg,
                                ItemIds.AnimalProducts.WhiteEgg,
                                ItemIds.AnimalProducts.LargeBrownEgg,
                                ItemIds.AnimalProducts.LargeWhiteEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeBlueEgg
                        => getNewName(
                            new string[]
                            {
                                ItemIds.CustomItems.MearaBlueEgg,
                                ItemIds.CustomItems.MearaLargeBlueEgg,
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeVoidEgg
                        => getNewName(
                            new string[]
                            {
                                ItemIds.CustomItems.MearaLargeVoidEgg,
                                ItemIds.AnimalProducts.VoidEgg,
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeGoldenEgg
                        => getNewName(
                            new string[]
                            {
                                ItemIds.CustomItems.MearaLargeGoldEgg,
                                ItemIds.AnimalProducts.GoldenEgg,
                            }
                        ),
                    ItemIds.RandomProduces.RandomGems
                        => getNewName(
                            new string[]
                            {
                                ItemIds.Gemstones.Amethyst,
                                ItemIds.Gemstones.Aquamarine,
                                ItemIds.Gemstones.Emerald,
                                ItemIds.Gemstones.Jade,
                                ItemIds.Gemstones.Ruby,
                                ItemIds.Gemstones.Topaz
                            }
                        ),
                    ItemIds.RandomProduces.RandomMinerals
                        => getNewName(
                            new string[]
                            {
                                ItemIds.Minerals.Aerinite,
                                ItemIds.Minerals.Alamite,
                                ItemIds.Minerals.Baryte,
                                ItemIds.Minerals.Basalt,
                                ItemIds.Minerals.Bixite,
                                ItemIds.Minerals.Calcite,
                                ItemIds.Minerals.Celestine,
                                ItemIds.Minerals.Dolomite,
                                ItemIds.Minerals.EarthCrystal,
                                ItemIds.Minerals.Esperite,
                                ItemIds.Minerals.FairyStone,
                                ItemIds.Minerals.FireOpal,
                                ItemIds.Minerals.FireQuartz,
                                ItemIds.Minerals.FrozenTear,
                                ItemIds.Minerals.Geminite,
                                ItemIds.Minerals.GhostCrystal,
                                ItemIds.Minerals.Granite,
                                ItemIds.Minerals.Helvite,
                                ItemIds.Minerals.Hematite,
                                ItemIds.Minerals.Jagoite,
                                ItemIds.Minerals.Jamborite,
                                ItemIds.Minerals.Kyanite,
                                ItemIds.Minerals.LemonStone,
                                ItemIds.Minerals.Limestone,
                                ItemIds.Minerals.Lunarite,
                                ItemIds.Minerals.Malachite,
                                ItemIds.Minerals.Marble,
                                ItemIds.Minerals.Mudstone,
                                ItemIds.Minerals.Nekoite,
                                ItemIds.Minerals.Neptunite,
                                ItemIds.Minerals.Obsidian,
                                ItemIds.Minerals.OceanStone,
                                ItemIds.Minerals.Opal,
                                ItemIds.Minerals.Orpiment,
                                ItemIds.Minerals.PetrifiedSlime,
                                ItemIds.Minerals.Pyrite,
                                ItemIds.Minerals.Sandstone,
                                ItemIds.Minerals.Slate,
                                ItemIds.Minerals.Soapstone,
                                ItemIds.Minerals.StarShards,
                                ItemIds.Minerals.ThunderEgg,
                                ItemIds.Minerals.TigersEye
                            }
                        ),
                    _ => "",
                    //TODO ADD MISSING RANDOMS
                };
            }
            else
            {
                return ItemIds.VariableValues[ProduceName];
            }
        }

        private RoosterData SetUpRoosterType(
            RoosterType roosterType,
            Dictionary<
                RoosterType,
                Dictionary<
                    ProduceTypeEntries,
                    Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>
                >
            > roosterProduces,
            Dictionary<
                RoosterType,
                Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>
            > roosterEggs
        )
        {
            var farmNames = GetFarmNames();
            var roosterFemale = GetRoosterFemale();

            var roosterData = new RoosterData
            {
                Female = roosterFemale[roosterType],
                FarmAnimalName = farmNames[roosterType],
                ProduceDays = GetIntOptionValue(roosterType, OptionEntries.ProduceDays),
                MatureDays = GetIntOptionValue(roosterType, OptionEntries.MatureDays),
                Price = GetIntOptionValue(roosterType, OptionEntries.Price),
                HarvestType = ModCfg.Options[OptionEntries.HarvestType][OptionDetails.Value],
                BuildingRequired = ModCfg.RoosterOptions[roosterType][
                    OptionEntries.BuildingRequired
                ][OptionDetails.Value],
                Buyable = GetBoolOptionValue(roosterType, OptionEntries.Buyable),
                Eggs = roosterEggs[roosterType],
                ProduceOption = ModCfg.RoosterOptions[roosterType][OptionEntries.ProduceOption][
                    OptionDetails.Value
                ],
                DeluxeProduceOption = ModCfg.RoosterOptions[roosterType][
                    OptionEntries.DeluxeProduceOption
                ][OptionDetails.Value],
                AllProduces = roosterProduces[roosterType],
                Quantity = UpdateRoosterQuantity(farmNames[roosterType])
            };

            return roosterData;
        }

        private static Dictionary<RoosterType, string> GetFarmNames()
        {
            return new Dictionary<RoosterType, string>
            {
                { RoosterType.White, RoosterKeys.White },
                { RoosterType.Brown, RoosterKeys.Brown },
                { RoosterType.Blue, RoosterKeys.Blue },
                { RoosterType.Gold, RoosterKeys.Gold },
                { RoosterType.Void, RoosterKeys.Void }
            };
        }

        private static Dictionary<RoosterType, string> GetRoosterFemale()
        {
            return new Dictionary<RoosterType, string>
            {
                { RoosterType.White, "White Chicken" },
                { RoosterType.Brown, "Brown Chicken" },
                { RoosterType.Blue, "Blue Chicken" },
                { RoosterType.Gold, "Golden Chicken" },
                { RoosterType.Void, "Void Chicken" }
            };
        }

        private int GetIntOptionValue(RoosterType roosterType, OptionEntries option)
        {
            if (
                !int.TryParse(
                    ModCfg.RoosterOptions[roosterType][option][OptionDetails.Value],
                    out int result
                )
            )
            {
                Monitor.Log(
                    $"Invalid integer value for option '{option}' and roostertype '{roosterType}'.",
                    LogLevel.Error
                );
                return 0;
            }
            return result;
        }

        private bool GetBoolOptionValue(RoosterType roosterType, OptionEntries option)
        {
            if (
                bool.TryParse(
                    ModCfg.RoosterOptions[roosterType][option][OptionDetails.Value],
                    out bool result
                )
            )
            {
                return result;
            }
            else
            {
                Monitor.Log(
                    $"Invalid boolean value for option '{option}' and rooster type '{roosterType}'.",
                    LogLevel.Error
                );
                return false;
            }
        }

        public bool CheckIfProduceEgg(
            List<string> eggs,
            IDictionary<string, FarmAnimalData> animalData
        )
        {
            if (!animalData.Select(x => x.Value.ProduceItemIds).Any())
            {
                return false;
            }
            var BothProducesList = animalData
                .SelectMany(x => x.Value.ProduceItemIds)
                .Union(animalData.SelectMany(x => x.Value.DeluxeProduceItemIds))
                .ToList();
            return eggs.Exists(item => BothProducesList.Select(x => x.ItemId).Contains(item));
        }

        public void UpdateFemaleEggs(
            RoosterData rooster,
            IDictionary<string, FarmAnimalData> animalData
        )
        {
            // Create a dictionary to map string keys to EggTypeEntries enum values
            var eggTypeMap = new Dictionary<string, EggTypeEntries>
            {
                { "Normal", EggTypeEntries.Normal },
                { "Large", EggTypeEntries.Large }
            };

            // Iterate over the females associated with the rooster
            foreach (var female in rooster.Females)
            {
                // Check if the animal data dictionary contains an entry for the female
                if (animalData.ContainsKey(female) && animalData[female].CustomFields != null)
                {
                    // Check if the FemaleEggs dictionary already contains an entry for the female
                    if (!FemaleEggs.ContainsKey(female))
                    {
                        // If not, create a new entry
                        FemaleEggs[female] = new Dictionary<EggTypeEntries, string>();
                    }

                    // Iterate over the custom fields of the female's animal data
                    foreach (
                        var (key, value) in animalData[female]
                            .CustomFields.Where(cf => eggTypeMap.ContainsKey(cf.Key))
                    )
                    {
                        // Update the egg data for the female
                        FemaleEggs[female][eggTypeMap[key]] = value;
                    }
                }
            }

            // Update the egg data for the rooster main female
            UpdateRoosterFemaleEggs(rooster);
        }

        private void UpdateRoosterFemaleEggs(RoosterData rooster)
        {
            // Check if the FemaleEggs dictionary already contains an entry for the rooster's female
            if (FemaleEggs.ContainsKey(rooster.Female))
            {
                // If so, update the egg data for the rooster's female
                FemaleEggs[rooster.Female][EggTypeEntries.Normal] = rooster.Eggs[
                    EggTypeEntries.Normal
                ][RoosterItemDataEntries.Name];
                FemaleEggs[rooster.Female][EggTypeEntries.Large] = rooster.Eggs[
                    EggTypeEntries.Large
                ][RoosterItemDataEntries.Name];
            }
            else
            {
                // If not, create a new entry for the rooster's female
                FemaleEggs[rooster.Female] = new Dictionary<EggTypeEntries, string>
                {
                    {
                        EggTypeEntries.Normal,
                        rooster.Eggs[EggTypeEntries.Normal][RoosterItemDataEntries.Name]
                    },
                    {
                        EggTypeEntries.Large,
                        rooster.Eggs[EggTypeEntries.Large][RoosterItemDataEntries.Name]
                    }
                };
            }
        }

        public static RoosterType GetRoosterTypeFromFarmName(string farmName)
        {
            return farmName switch
            {
                RoosterKeys.Blue => RoosterType.Blue,
                RoosterKeys.White => RoosterType.White,
                RoosterKeys.Gold => RoosterType.Gold,
                RoosterKeys.Void => RoosterType.Void,
                RoosterKeys.Brown => RoosterType.Brown,
                _ => RoosterType.White
            };
        }
    }
}
