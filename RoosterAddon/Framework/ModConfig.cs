using System.Collections.Generic;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Helpers;
using StardewModdingAPI;
using StardewValley;

namespace RoosterAddon.Framework
{
    public class ModConfig
    {
        public enum RoosterProduceOptions
        {
            Produce_1,
            Produce_2,
            Produce_3,
            Produce_4,
            DeluxeProduce_1,
            DeluxeProduce_2,
            DeluxeProduce_3,
            DeluxeProduce_4,
            Produce_1_Prob,
            Produce_2_Prob,
            Produce_3_Prob,
            Produce_4_Prob,
            DeluxeProduce_1_Prob,
            DeluxeProduce_2_Prob,
            DeluxeProduce_3_Prob,
            DeluxeProduce_4_Prob,
            AllowedValues
        }

        public Dictionary<OptionEntries, Dictionary<OptionDetails, string>> Options { get; set; }
        private Dictionary<
            OptionEntries,
            Dictionary<OptionDetails, string>
        > WhiteRoosterOptions { get; set; }
        private Dictionary<
            OptionEntries,
            Dictionary<OptionDetails, string>
        > BrownRoosterOptions { get; set; }
        private Dictionary<
            OptionEntries,
            Dictionary<OptionDetails, string>
        > BlueRoosterOptions { get; set; }
        private Dictionary<
            OptionEntries,
            Dictionary<OptionDetails, string>
        > VoidRoosterOptions { get; set; }
        private Dictionary<
            OptionEntries,
            Dictionary<OptionDetails, string>
        > GoldRoosterOptions { get; set; }
        public Dictionary<
            RoosterType,
            Dictionary<OptionEntries, Dictionary<OptionDetails, string>>
        > RoosterOptions { get; set; }
        public Dictionary<string, string> FemalesOptions { get; set; } =
            new();
        public int steps = 5;
        public ModConfig()
        {
            Options = CreateGeneralOptions();
            WhiteRoosterOptions = CreateRoosterOptions(RoosterType.White);
            BrownRoosterOptions = CreateRoosterOptions(RoosterType.Brown);
            BlueRoosterOptions = CreateRoosterOptions(RoosterType.Blue);
            GoldRoosterOptions = CreateRoosterOptions(RoosterType.Gold);
            VoidRoosterOptions = CreateRoosterOptions(RoosterType.Void);

            VoidRoosterOptions[OptionEntries.Buyable][OptionDetails.Value] = OptionValues.False;
            GoldRoosterOptions[OptionEntries.Buyable][OptionDetails.Value] = OptionValues.False;

            BlueRoosterOptions[OptionEntries.BlueEggChoice] = new Dictionary<OptionDetails, string>
            {
                { OptionDetails.Value, ItemIds.CustomItems.MearaBlueEgg },
                { OptionDetails.Type, GMCMTypes.Text },
                {
                    OptionDetails.AllowedValues,
                    $"{ItemIds.CustomItems.MearaBlueEgg},{ItemIds.CustomItems.Six480BlueEgg}"
                }
            };
            BlueRoosterOptions[OptionEntries.BypassBlueConditions] = new Dictionary<
                OptionDetails,
                string
            >
            {
                { OptionDetails.Value, OptionValues.False },
                { OptionDetails.Type, GMCMTypes.Bool },
            };

            RoosterOptions = new Dictionary<
                RoosterType,
                Dictionary<OptionEntries, Dictionary<OptionDetails, string>>
            >
            {
                { RoosterType.Void, VoidRoosterOptions },
                { RoosterType.White, WhiteRoosterOptions },
                { RoosterType.Brown, BrownRoosterOptions },
                { RoosterType.Blue, BlueRoosterOptions },
                { RoosterType.Gold, GoldRoosterOptions },
            };
        }

        private Dictionary<OptionEntries, Dictionary<OptionDetails, string>> CreateGeneralOptions()
        {
            return new Dictionary<OptionEntries, Dictionary<OptionDetails, string>>
            {
                {
                    OptionEntries.UpdateRate,
                    CreateOptionDetails(
                        defaultValue: OptionValues.OnDayStart,
                        valueType: GMCMTypes.Text,
                        allowedValues: $"{OptionValues.OnDayStart}, {OptionValues.OnLocationChange}"
                    )
                },
                {
                    OptionEntries.ChosenPercentage_DailyLuckBonus,
                    CreateOptionDetails(
                        defaultValue: "10",
                        valueType: GMCMTypes.Number,
                        min: "0",
                        max: "100",
                        interval: "5"
                    )
                },
                {
                    OptionEntries.HatchingMode,
                    CreateOptionDetails(
                        defaultValue: OptionValues.Original,
                        valueType: GMCMTypes.Text,
                        allowedValues: $"{OptionValues.Original}, {OptionValues.OnlyFertilizedEggs}"
                    )
                },
                {
                    OptionEntries.HarvestType,
                    CreateOptionDetails(
                        defaultValue: OptionValues.DigUp,
                        valueType: GMCMTypes.Text,
                        allowedValues: $"{OptionValues.DigUp}, {OptionValues.DropOvernight}"
                    )
                },
                {
                    OptionEntries.BetterTrufflesCompat,
                    CreateOptionDetails(defaultValue: OptionValues.True, valueType: GMCMTypes.Bool)
                },
                {
                    OptionEntries.EnableStuckInside,
                    CreateOptionDetails(defaultValue: OptionValues.True, valueType: GMCMTypes.Bool)
                },
                {
                    OptionEntries.EnableEggProbability,
                    CreateOptionDetails(defaultValue: OptionValues.True, valueType: GMCMTypes.Bool)
                },
                {
                    OptionEntries.EnableDebug,
                    CreateOptionDetails(defaultValue: OptionValues.True, valueType: GMCMTypes.Bool)
                },
                {
                    OptionEntries.EggProbability,
                    CreateOptionDetails(
                        defaultValue: "20",
                        valueType: GMCMTypes.Number,
                        min: "0",
                        max: "100",
                        interval: "5"
                    )
                },
                {
                    OptionEntries.EnableLargeEggProbability,
                    CreateOptionDetails(defaultValue: OptionValues.True, valueType: GMCMTypes.Bool)
                },
                {
                    OptionEntries.LargeEggProbability,
                    CreateOptionDetails(
                        defaultValue: "40",
                        valueType: GMCMTypes.Number,
                        min: "0",
                        max: "100",
                        interval: "5"
                    )
                },
                {
                    OptionEntries.FertilizedEggProbability,
                    CreateOptionDetails(
                        defaultValue: "60",
                        valueType: GMCMTypes.Number,
                        min: "0",
                        max: "100",
                        interval: "5"
                    )
                },
                {
                    OptionEntries.RoosterToChickenRatio,
                    CreateOptionDetails(
                        defaultValue: "50",
                        valueType: GMCMTypes.Number,
                        min: "0",
                        max: "100",
                        interval: "5"
                    )
                },
                {
                    OptionEntries.BoostByRooster,
                    CreateOptionDetails(
                        defaultValue: "0.1",
                        valueType: GMCMTypes.Number,
                        min: "0.0",
                        max: "10.0",
                        interval: "0.1"
                    )
                }
            };
        }

        private Dictionary<OptionEntries, Dictionary<OptionDetails, string>> CreateRoosterOptions(
            RoosterType roosterType
        )
        {
            var RoosterDic = new Dictionary<OptionEntries, Dictionary<OptionDetails, string>>
            {
                {
                    OptionEntries.BuildingRequired,
                    CreateOptionDetails(
                        defaultValue: OptionValues.BigCoop,
                        valueType: GMCMTypes.Text,
                        allowedValues: $"{OptionValues.Coop}, {OptionValues.BigCoop}, {OptionValues.DeluxeCoop}"
                    )
                },
                {
                    OptionEntries.Buyable,
                    CreateOptionDetails(defaultValue: OptionValues.True, valueType: GMCMTypes.Bool)
                },
                {
                    OptionEntries.Price,
                    CreateOptionDetails(
                        defaultValue: "2400",
                        valueType: GMCMTypes.Number,
                        min: "0",
                        max: "50000",
                        interval: "150"
                    )
                },
                {
                    OptionEntries.EnableRoosterHatching,
                    CreateOptionDetails(defaultValue: OptionValues.True, valueType: GMCMTypes.Bool)
                },
                {
                    OptionEntries.EditAllFemales,
                    CreateOptionDetails(defaultValue: OptionValues.True, valueType: GMCMTypes.Bool)
                },
                {
                    OptionEntries.MatureDays,
                    CreateOptionDetails(
                        defaultValue: "4",
                        valueType: GMCMTypes.Number,
                        min: "1",
                        max: "14",
                        interval: "1"
                    )
                },
                {
                    OptionEntries.ProduceDays,
                    CreateOptionDetails(
                        defaultValue: "1",
                        valueType: GMCMTypes.Number,
                        min: "1",
                        max: "14",
                        interval: "1"
                    )
                },
                {
                    OptionEntries.ProduceOption,
                    CreateOptionDetails(
                        defaultValue: OptionValues.CustomizeBelow,
                        valueType: GMCMTypes.Text,
                        allowedValues: $"{OptionValues.KeepOriginalProduce}, {OptionValues.CustomizeBelow}"
                    )
                },
                {
                    OptionEntries.DeluxeProduceOption,
                    CreateOptionDetails(
                        defaultValue: OptionValues.CustomizeBelow,
                        valueType: GMCMTypes.Text,
                        allowedValues: $"{OptionValues.Off}, {OptionValues.KeepOriginalProduce}, {OptionValues.CustomizeBelow}"
                    )
                },
            };
            SetUpRoosterProduceOptions(roosterType, ref RoosterDic);
            return RoosterDic;
        }

        private Dictionary<OptionDetails, string> CreateOptionDetails(
            string defaultValue,
            string valueType,
            string? allowedValues = null,
            string? min = null,
            string? max = null,
            string? interval = null
        )
        {
            var optionDetails = new Dictionary<OptionDetails, string>
            {
                { OptionDetails.Value, defaultValue },
                { OptionDetails.Type, valueType }
            };
            if (allowedValues != null)
            {
                optionDetails.Add(OptionDetails.AllowedValues, allowedValues);
            }
            if (min != null)
            {
                optionDetails.Add(OptionDetails.Min, min);
            }
            if (max != null)
            {
                optionDetails.Add(OptionDetails.Max, max);
            }
            if (interval != null)
            {
                optionDetails.Add(OptionDetails.Interval, interval);
            }
            return optionDetails;
        }

        private Dictionary<
            RoosterType,
            Dictionary<RoosterProduceOptions, string>
        > BuildProducesDictionaries()
        {
            return new Dictionary<RoosterType, Dictionary<RoosterProduceOptions, string>>
            {
                {
                    RoosterType.White,
                    new Dictionary<RoosterProduceOptions, string>
                    {
                        { RoosterProduceOptions.Produce_1, ItemIds.MonsterLoot.BugMeat },
                        { RoosterProduceOptions.Produce_2, ItemIds.Baits.Bait },
                        { RoosterProduceOptions.Produce_3, ItemIds.Crops.MixedSeeds },
                        { RoosterProduceOptions.Produce_4, ItemIds.MonsterLoot.BugMeat },
                        { RoosterProduceOptions.DeluxeProduce_1, ItemIds.Baits.WildBait },
                        { RoosterProduceOptions.DeluxeProduce_2, ItemIds.Minerals.Geode },
                        { RoosterProduceOptions.DeluxeProduce_3, ItemIds.Baits.DeluxeBait },
                        { RoosterProduceOptions.DeluxeProduce_4, ItemIds.Baits.WildBait },
                        { RoosterProduceOptions.Produce_1_Prob, "50" },
                        { RoosterProduceOptions.Produce_2_Prob, "30" },
                        { RoosterProduceOptions.Produce_3_Prob, "20" },
                        { RoosterProduceOptions.Produce_4_Prob, "0" },
                        { RoosterProduceOptions.DeluxeProduce_1_Prob, "50" },
                        { RoosterProduceOptions.DeluxeProduce_2_Prob, "30" },
                        { RoosterProduceOptions.DeluxeProduce_3_Prob, "20" },
                        { RoosterProduceOptions.DeluxeProduce_4_Prob, "0" },
                        {
                            RoosterProduceOptions.AllowedValues,
                            $"{ItemIds.AnimalProducts.WhiteEgg},{ItemIds.AnimalProducts.LargeWhiteEgg},{ItemIds.RandomProduces.RandomSizeWhiteEgg},{ItemIds.MonsterLoot.BugMeat},{ItemIds.Baits.Bait},{ItemIds.Baits.WildBait},{ItemIds.Baits.DeluxeBait},{ItemIds.Crops.MixedSeeds},{ItemIds.Minerals.Geode}"
                        }
                    }
                },
                {
                    RoosterType.Brown,
                    new Dictionary<RoosterProduceOptions, string>
                    {
                        { RoosterProduceOptions.Produce_1, ItemIds.MonsterLoot.BugMeat },
                        { RoosterProduceOptions.Produce_2, ItemIds.Baits.Bait },
                        { RoosterProduceOptions.Produce_3, ItemIds.Crops.MixedFlowerSeeds },
                        { RoosterProduceOptions.Produce_4, ItemIds.MonsterLoot.BugMeat },
                        { RoosterProduceOptions.DeluxeProduce_1, ItemIds.Baits.WildBait },
                        { RoosterProduceOptions.DeluxeProduce_2, ItemIds.Minerals.Geode },
                        { RoosterProduceOptions.DeluxeProduce_3, ItemIds.Baits.DeluxeBait },
                        { RoosterProduceOptions.DeluxeProduce_4, ItemIds.Baits.WildBait },
                        { RoosterProduceOptions.Produce_1_Prob, "50" },
                        { RoosterProduceOptions.Produce_2_Prob, "30" },
                        { RoosterProduceOptions.Produce_3_Prob, "20" },
                        { RoosterProduceOptions.Produce_4_Prob, "0" },
                        { RoosterProduceOptions.DeluxeProduce_1_Prob, "50" },
                        { RoosterProduceOptions.DeluxeProduce_2_Prob, "30" },
                        { RoosterProduceOptions.DeluxeProduce_3_Prob, "20" },
                        { RoosterProduceOptions.DeluxeProduce_4_Prob, "0" },
                        {
                            RoosterProduceOptions.AllowedValues,
                            $"{ItemIds.AnimalProducts.BrownEgg},{ItemIds.AnimalProducts.LargeBrownEgg},{ItemIds.RandomProduces.RandomSizeBrownEgg},{ItemIds.MonsterLoot.BugMeat},{ItemIds.Baits.Bait},{ItemIds.Baits.WildBait},{ItemIds.Baits.DeluxeBait},{ItemIds.Crops.MixedFlowerSeeds},{ItemIds.Minerals.Geode}"
                        }
                    }
                },
                {
                    RoosterType.Blue,
                    new Dictionary<RoosterProduceOptions, string>
                    {
                        { RoosterProduceOptions.Produce_1, ItemIds.Baits.MagicBait },
                        { RoosterProduceOptions.Produce_2, ItemIds.Fruits.CrystalFruit },
                        { RoosterProduceOptions.Produce_3, ItemIds.Baits.MagicBait },
                        { RoosterProduceOptions.Produce_4, ItemIds.Baits.MagicBait },
                        { RoosterProduceOptions.DeluxeProduce_1, ItemIds.Fruits.AncientFruit },
                        { RoosterProduceOptions.DeluxeProduce_2, ItemIds.Miscellaneous.MysteryBox },
                        { RoosterProduceOptions.DeluxeProduce_3, ItemIds.Fruits.AncientFruit },
                        { RoosterProduceOptions.DeluxeProduce_4, ItemIds.Fruits.AncientFruit },
                        { RoosterProduceOptions.Produce_1_Prob, "60" },
                        { RoosterProduceOptions.Produce_2_Prob, "40" },
                        { RoosterProduceOptions.Produce_3_Prob, "0" },
                        { RoosterProduceOptions.Produce_4_Prob, "0" },
                        { RoosterProduceOptions.DeluxeProduce_1_Prob, "60" },
                        { RoosterProduceOptions.DeluxeProduce_2_Prob, "40" },
                        { RoosterProduceOptions.DeluxeProduce_3_Prob, "0" },
                        { RoosterProduceOptions.DeluxeProduce_4_Prob, "0" },
                        {
                            RoosterProduceOptions.AllowedValues,
                            $"{ItemIds.CustomItems.MearaBlueEgg},{ItemIds.CustomItems.MearaLargeBlueEgg},{ItemIds.RandomProduces.RandomSizeBlueEgg},{ItemIds.Baits.MagicBait},{ItemIds.Fruits.CrystalFruit},{ItemIds.Fruits.AncientFruit},{ItemIds.Miscellaneous.MysteryBox},{ItemIds.Minerals.FrozenGeode}"
                        }
                    }
                },
                {
                    RoosterType.Void,
                    new Dictionary<RoosterProduceOptions, string>
                    {
                        { RoosterProduceOptions.Produce_1, ItemIds.MonsterLoot.VoidEssence },
                        { RoosterProduceOptions.Produce_2, ItemIds.Minerals.ThunderEgg },
                        { RoosterProduceOptions.Produce_3, ItemIds.MonsterLoot.VoidEssence },
                        { RoosterProduceOptions.Produce_4, ItemIds.MonsterLoot.VoidEssence },
                        { RoosterProduceOptions.DeluxeProduce_1, ItemIds.MonsterLoot.MonsterMusk },
                        {
                            RoosterProduceOptions.DeluxeProduce_2,
                            ItemIds.Miscellaneous.VoidGhostPendant
                        },
                        { RoosterProduceOptions.DeluxeProduce_3, ItemIds.MonsterLoot.MonsterMusk },
                        { RoosterProduceOptions.DeluxeProduce_4, ItemIds.MonsterLoot.MonsterMusk },
                        { RoosterProduceOptions.Produce_1_Prob, "60" },
                        { RoosterProduceOptions.Produce_2_Prob, "40" },
                        { RoosterProduceOptions.Produce_3_Prob, "0" },
                        { RoosterProduceOptions.Produce_4_Prob, "0" },
                        { RoosterProduceOptions.DeluxeProduce_1_Prob, "60" },
                        { RoosterProduceOptions.DeluxeProduce_2_Prob, "40" },
                        { RoosterProduceOptions.DeluxeProduce_3_Prob, "0" },
                        { RoosterProduceOptions.DeluxeProduce_4_Prob, "0" },
                        {
                            RoosterProduceOptions.AllowedValues,
                            $"{ItemIds.AnimalProducts.VoidEgg},{ItemIds.CustomItems.MearaLargeVoidEgg},{ItemIds.RandomProduces.RandomSizeVoidEgg},{ItemIds.MonsterLoot.VoidEssence},{ItemIds.Minerals.ThunderEgg},{ItemIds.MonsterLoot.MonsterMusk},{ItemIds.Miscellaneous.VoidGhostPendant},{ItemIds.Artifacts.RainTotem},{ItemIds.MonsterLoot.BatWing},{ItemIds.Miscellaneous.SpiritTorch},{ItemIds.Minerals.MagmaGeode}"
                        }
                    }
                },
                {
                    RoosterType.Gold,
                    new Dictionary<RoosterProduceOptions, string>
                    {
                        { RoosterProduceOptions.Produce_1, ItemIds.RandomProduces.RandomGems },
                        { RoosterProduceOptions.Produce_2, ItemIds.RandomProduces.RandomMinerals },
                        { RoosterProduceOptions.Produce_3, ItemIds.RandomProduces.RandomGems },
                        { RoosterProduceOptions.Produce_4, ItemIds.RandomProduces.RandomGems },
                        { RoosterProduceOptions.DeluxeProduce_1, ItemIds.Artifacts.GoldenMask },
                        { RoosterProduceOptions.DeluxeProduce_2, ItemIds.Artifacts.TreasureChest },
                        { RoosterProduceOptions.DeluxeProduce_3, ItemIds.Artifacts.GoldenMask },
                        { RoosterProduceOptions.DeluxeProduce_4, ItemIds.Artifacts.GoldenMask },
                        { RoosterProduceOptions.Produce_1_Prob, "60" },
                        { RoosterProduceOptions.Produce_2_Prob, "40" },
                        { RoosterProduceOptions.Produce_3_Prob, "0" },
                        { RoosterProduceOptions.Produce_4_Prob, "0" },
                        { RoosterProduceOptions.DeluxeProduce_1_Prob, "60" },
                        { RoosterProduceOptions.DeluxeProduce_2_Prob, "40" },
                        { RoosterProduceOptions.DeluxeProduce_3_Prob, "0" },
                        { RoosterProduceOptions.DeluxeProduce_4_Prob, "0" },
                        {
                            RoosterProduceOptions.AllowedValues,
                            $"{ItemIds.AnimalProducts.GoldenEgg},{ItemIds.CustomItems.MearaLargeGoldEgg},{ItemIds.RandomProduces.RandomSizeGoldenEgg},{ItemIds.AnimalProducts.GoldenAnimalCracker},{ItemIds.RandomProduces.RandomGems},{ItemIds.RandomProduces.RandomMinerals},{ItemIds.Artifacts.TreasureChest},{ItemIds.Artifacts.ArtifactTrove},{ItemIds.Artifacts.RingofYoba},{ItemIds.Artifacts.GoldenMask},{ItemIds.Artifacts.GoldenRelic},{ItemIds.Miscellaneous.GoldenBobber},{ItemIds.Miscellaneous.GoldenMysteryBox},{ItemIds.Fruits.GoldenCoconut},{ItemIds.Fruits.GoldenPumpkin},{ItemIds.Minerals.OmniGeode}"
                        }
                    }
                }
            };
        }

        public string GetProduceOptionDefaultValue(
            RoosterType roosterType,
            Dictionary<RoosterType, Dictionary<RoosterProduceOptions, string>> Produces,
            OptionEntries optionEntry
        )
        {
            var optionEntryName = GetEnumName(optionEntry);
            if (optionEntryName.Contains("_Prob"))
            {
                if (optionEntryName.Contains('1'))
                {
                    return "60";
                }
                else if (optionEntryName.Contains('2'))
                {
                    return "40";
                }
                else
                {
                    return "0";
                }
            }
            else if (optionEntryName.Contains("Custom"))
            {
                return "Custom";
            }
            else
            {
                var OptionToRoosterProd = (RoosterProduceOptions)
                    Enum.Parse(typeof(RoosterProduceOptions), optionEntryName, true);
                return Produces[roosterType][OptionToRoosterProd];
            }
        }

        public string? GetProduceOptionIntegers(
            Dictionary<RoosterType, Dictionary<RoosterProduceOptions, string>> Produces,
            OptionEntries optionEntry,
            OptionDetails integer
        )
        {
            var optionEntryName = GetEnumName(optionEntry);
            if (optionEntryName.Contains("Prob"))
            {
                return integer switch
                {
                    OptionDetails.Min => "0",
                    OptionDetails.Max => "100",
                    OptionDetails.Interval => "5",
                    _ => "0",
                };
            }
            else
            {
                return null;
            }
        }

        public string? GetProduceOptionAllowedValues(
            RoosterType roosterType,
            Dictionary<RoosterType, Dictionary<RoosterProduceOptions, string>> Produces,
            OptionEntries optionEntry
        )
        {
            var optionEntryName = GetEnumName(optionEntry);
            if (optionEntryName.Contains("Prob") || optionEntryName.Contains("Custom"))
            {
                return null;
            }
            else
            {
                return Produces[roosterType][RoosterProduceOptions.AllowedValues];
            }
        }

        public string GetProduceOptionType(OptionEntries optionEntry)
        {
            var optionEntryName = GetEnumName(optionEntry);
            if (optionEntryName.Contains("Prob"))
            {
                return GMCMTypes.Number;
            }
            else
            {
                return GMCMTypes.Text;
            }
        }

        public void SetUpRoosterProduceOptions(
            RoosterType roosterType,
            ref Dictionary<OptionEntries, Dictionary<OptionDetails, string>> roosterData
        )
        {
            var ProduceOptionList = new List<OptionEntries>
            {
                OptionEntries.Produce_1,
                OptionEntries.Produce_1_Prob,
                OptionEntries.CustomProduce_1,
                OptionEntries.Produce_2,
                OptionEntries.Produce_2_Prob,
                OptionEntries.CustomProduce_2,
                OptionEntries.Produce_3,
                OptionEntries.Produce_3_Prob,
                OptionEntries.CustomProduce_3,
                OptionEntries.Produce_4,
                OptionEntries.Produce_4_Prob,
                OptionEntries.CustomProduce_4,
                OptionEntries.DeluxeProduce_1,
                OptionEntries.DeluxeProduce_1_Prob,
                OptionEntries.CustomDeluxeProduce_1,
                OptionEntries.DeluxeProduce_2,
                OptionEntries.DeluxeProduce_2_Prob,
                OptionEntries.CustomDeluxeProduce_2,
                OptionEntries.DeluxeProduce_3,
                OptionEntries.DeluxeProduce_3_Prob,
                OptionEntries.CustomDeluxeProduce_3,
                OptionEntries.DeluxeProduce_4,
                OptionEntries.DeluxeProduce_4_Prob,
                OptionEntries.CustomDeluxeProduce_4,
            };
            var Produces = BuildProducesDictionaries();
            foreach (var optionEntry in ProduceOptionList)
            {
                roosterData[optionEntry] = CreateOptionDetails(
                    defaultValue: GetProduceOptionDefaultValue(roosterType, Produces, optionEntry),
                    valueType: GetProduceOptionType(optionEntry),
                    allowedValues: GetProduceOptionAllowedValues(
                        roosterType,
                        Produces,
                        optionEntry
                    ),
                    min: GetProduceOptionIntegers(Produces, optionEntry, OptionDetails.Min),
                    max: GetProduceOptionIntegers(Produces, optionEntry, OptionDetails.Max),
                    interval: GetProduceOptionIntegers(
                        Produces,
                        optionEntry,
                        OptionDetails.Interval
                    )
                );
            }
        }

        public string[] GetAllowedValues(OptionEntries optionEntry, RoosterType? roosterType = null)
        {
            if (
                roosterType.HasValue
                && !RoosterOptions[roosterType.Value]
                    [optionEntry]
                    .ContainsKey(OptionDetails.AllowedValues)
            )
            {
                return null!;
            }
            if (
                roosterType == null
                && !Options[optionEntry].ContainsKey(OptionDetails.AllowedValues)
            )
            {
                return null!;
            }
            string[] possibleValues = roosterType.HasValue
                ? RoosterOptions[roosterType.Value]
                    [optionEntry][OptionDetails.AllowedValues]
                    .Split(',', StringSplitOptions.TrimEntries)
                : Options[optionEntry]
                    [OptionDetails.AllowedValues]
                    .Split(',', StringSplitOptions.TrimEntries);
            if (possibleValues.Length == 0)
            {
                return Array.Empty<string>();
            }
            return possibleValues.ToArray();
        }

        public static string GetEnumName<TEnum>(TEnum enumValue)
            where TEnum : Enum
        {
            return enumValue.ToString();
        }
    }

    public sealed class AnimalExclusion
    {
        public List<string> AnimalExclusions { get; set; }
        public List<string> AnimalExclusionsModID { get; set; }

        public AnimalExclusion()
        {
            AnimalExclusions = new()
            {
                "Domestic Guinea Fowl",
                "White Legbar",
                "Blue Parrot",
                "White Parrot",
                "Red Parrot",
                "Void Parrot",
                "Echidna",
                "Birch Echidna",
                "Knuckles the Echidna",
                "Oak Echidna",
                "Warm Echidna",
                "Lady Amherst's Pheasant",
                "ExampleAnimalKey1",
                "ExampleAnimalKey2",
                "ExampleAnimalKey3",
                "OR",
                "ExampleAnimalKey1, ExampleAnimalKey2, ExampleAnimalKey3"
            };
            AnimalExclusionsModID = new() { "ModIDExample" };
        }

      }
        public sealed class RoosterFemales
        {
            public Dictionary<string,string> Females { get; set; }

            public RoosterFemales()
            {
                Females = new();
            }
        }
    
}
