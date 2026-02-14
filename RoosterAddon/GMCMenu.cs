using Microsoft.VisualBasic;
using Microsoft.Xna.Framework.Graphics;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework;
using Mikew.BirdsOfAFeather.Framework;
using RoosterAddon.Helpers;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon
{
    /// <summary>The mod entry point.</summary>
    ///
    internal sealed partial class ModEntry : Mod
    {
        private void GMCMSetup(ref IGenericModConfigMenuAPI configMenu)
        {
            var OptionsOrder = new List<OptionEntries>
            {
                OptionEntries.EnableDebug,
                OptionEntries.UpdateRate,
                OptionEntries.ChosenPercentage_DailyLuckBonus,
                OptionEntries.HarvestType,
                OptionEntries.EnableStuckInside,
                OptionEntries.BetterTrufflesCompat,
                OptionEntries.HatchingMode,
                OptionEntries.EnableEggProbability,
                OptionEntries.EggProbability,
                OptionEntries.EnableLargeEggProbability,
                OptionEntries.LargeEggProbability,
                OptionEntries.FertilizedEggProbability,
                OptionEntries.RoosterToChickenRatio,
                OptionEntries.BoostByRooster,
                //Per Rooster
                OptionEntries.MatureDays,
                OptionEntries.ProduceDays,
                OptionEntries.BuildingRequired,
                OptionEntries.Buyable,
                OptionEntries.Price,
                OptionEntries.BypassBlueConditions,
                OptionEntries.EditAllFemales,
                OptionEntries.EnableRoosterHatching
            };
            if (HasMod("6480.blueegg", Helper))
            {
                OptionsOrder.Insert(18, OptionEntries.BlueEggChoice);
            }
            configMenu.AddImage(
                mod: this.ModManifest,
                texture: () =>
                    Helper.ModContent.Load<Texture2D>(
                        ModTextures[Textures.AllRoosters][TextureDetails.MoldFolderPath]
                    ),
                scale: 1
            );/*
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Steps",
                getValue: () => ModCfg.steps,
                setValue: value => ModCfg.steps = value,
                min: 1,
                max: 20,
                interval: 1
            );*/
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => Helper.Translation.Get("config.section.general.name")
            );
            AddOptions(OptionsOrder);
            if (ModCfg.FemalesOptions.Count > 0 && GetHatchApiCloneEggsOption())
            {
                configMenu.AddPageLink(
                    mod: this.ModManifest,
                    pageId: $"Females",
                    text: () => Helper.Translation.Get("config.pagelink.females.name")
                );
            }
            foreach (
                var rooster in ModCfg.RoosterOptions.Select(x => x.Key).OrderBy(rooster => rooster)
            )
            {
                configMenu.AddPageLink(
                    mod: this.ModManifest,
                    pageId: ModConfig.GetEnumName(rooster),
                    text: () =>
                        Helper.Translation.Get(
                            $"config.pagelink.{ModConfig.GetEnumName(rooster)}.name"
                        )
                );
            }
            AddFemaleOptions();
            foreach (var rooster in ModCfg.RoosterOptions.Select(x => x.Key))
            {
                configMenu.AddPage(mod: this.ModManifest, pageId: ModConfig.GetEnumName(rooster));
                configMenu.AddImage(
                    mod: this.ModManifest,
                    texture: GetRoosterTexture(rooster),
                    scale: 1
                );
                configMenu.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () =>
                        Helper.Translation.Get(
                            $"config.section.{ModConfig.GetEnumName(rooster)}.name"
                        )
                );
                AddOptions(OptionsOrder, rooster);
                configMenu.AddPageLink(
                    mod: this.ModManifest,
                    pageId: ModConfig.GetEnumName(rooster) + "Produces",
                    text: () => Helper.Translation.Get("config.pagelink.Produces.name")
                );
            }

            var OptionsOrderProduces = new List<OptionEntries>
            {
                OptionEntries.ProduceOption,
                OptionEntries.Produce_1,
                OptionEntries.CustomProduce_1,
                OptionEntries.Produce_1_Prob,
                OptionEntries.Produce_2,
                OptionEntries.CustomProduce_2,
                OptionEntries.Produce_2_Prob,
                OptionEntries.Produce_3,
                OptionEntries.CustomProduce_3,
                OptionEntries.Produce_3_Prob,
                OptionEntries.Produce_4,
                OptionEntries.CustomProduce_4,
                OptionEntries.Produce_4_Prob,
                OptionEntries.DeluxeProduceOption,
                OptionEntries.DeluxeProduce_1,
                OptionEntries.CustomDeluxeProduce_1,
                OptionEntries.DeluxeProduce_1_Prob,
                OptionEntries.DeluxeProduce_2,
                OptionEntries.CustomDeluxeProduce_2,
                OptionEntries.DeluxeProduce_2_Prob,
                OptionEntries.DeluxeProduce_3,
                OptionEntries.CustomDeluxeProduce_3,
                OptionEntries.DeluxeProduce_3_Prob,
                OptionEntries.DeluxeProduce_4,
                OptionEntries.CustomDeluxeProduce_4,
                OptionEntries.DeluxeProduce_4_Prob,
            };
            var OnlyProduceOptions = OptionsOrderProduces
                .Where(p => !ModConfig.GetEnumName(p).Contains("Deluxe"))
                .ToList();
            var DeluxeOptions = OptionsOrderProduces
                .Where(p => ModConfig.GetEnumName(p).Contains("Deluxe"))
                .ToList();
            foreach (var rooster in ModCfg.RoosterOptions.Select(x => x.Key))
            {
                configMenu.AddPage(
                    mod: this.ModManifest,
                    pageId: ModConfig.GetEnumName(rooster) + "Produces"
                );
                configMenu.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => Helper.Translation.Get("config.section.Produces.name")
                );
                AddOptions(OnlyProduceOptions, rooster);
                configMenu.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => Helper.Translation.Get("config.section.DeluxeProduces.name")
                );
                configMenu.AddParagraph(
                    mod: this.ModManifest,
                    text: () => Helper.Translation.Get("config.section.DeluxeProduces.description")
                );
                AddOptions(DeluxeOptions, rooster);
            }
        }

        private void AddOptions(
            List<OptionEntries> optionEntriesOrder,
            RoosterType? roosterType = null
        )
        {
            foreach (
                var optionEntries in optionEntriesOrder.Where(x =>
                    (
                        roosterType.HasValue
                            ? ModCfg.RoosterOptions[roosterType.Value]
                            : ModCfg.Options
                    ).ContainsKey(x)
                )
            )
            {
                string Type = roosterType.HasValue
                    ? ModCfg.RoosterOptions[roosterType.Value][optionEntries][OptionDetails.Type]
                    : ModCfg.Options[optionEntries][OptionDetails.Type];
                var _name = Helper.Translation.Get(
                    "config." + Type + "." + ModConfig.GetEnumName(optionEntries) + ".name"
                );
                var _description = Helper.Translation.Get(
                    "config." + Type + "." + ModConfig.GetEnumName(optionEntries) + ".description"
                );

                switch (Type)
                {
                    case GMCMTypes.Bool:
                        configMenu.AddBoolOption(
                            mod: this.ModManifest,
                            getValue: () =>
                                Convert.ToBoolean(
                                    (
                                        roosterType.HasValue
                                            ? ModCfg.RoosterOptions[roosterType.Value]
                                            : ModCfg.Options
                                    )[optionEntries][OptionDetails.Value]
                                ),
                            setValue: value =>
                                (
                                    roosterType.HasValue
                                        ? ModCfg.RoosterOptions[roosterType.Value]
                                        : ModCfg.Options
                                )[optionEntries][OptionDetails.Value] = value.ToString(),
                            name: () => _name,
                            tooltip: () => _description
                        );
                        break;
                    case GMCMTypes.Text:
                        var allowedVallues = ModCfg.GetAllowedValues(
                            optionEntries,
                            roosterType.HasValue ? roosterType.Value : null
                        );
                        configMenu.AddTextOption(
                            mod: this.ModManifest,
                            getValue: () =>
                                (
                                    roosterType.HasValue
                                        ? ModCfg.RoosterOptions[roosterType.Value]
                                        : ModCfg.Options
                                )[optionEntries][OptionDetails.Value],
                            setValue: value =>
                                (
                                    roosterType.HasValue
                                        ? ModCfg.RoosterOptions[roosterType.Value]
                                        : ModCfg.Options
                                )[optionEntries][OptionDetails.Value] = value,
                            name: () => _name,
                            tooltip: () => _description,
                            allowedValues: allowedVallues != null ? allowedVallues : null!,
                            formatAllowedValue: value =>
                                Helper.Translation.Get("config.value." + value)
                        );
                        break;
                    case GMCMTypes.Number:
                        var OptionEntryName = ModConfig.GetEnumName(optionEntries);
                        var Format =
                            OptionEntryName.Contains("Prob")
                            || OptionEntryName.Contains("Boost")
                            || OptionEntryName.Contains("Daily");
                        if (optionEntries == OptionEntries.BoostByRooster)
                        {
                            configMenu.AddNumberOption(
                                mod: this.ModManifest,
                                getValue: () =>
                                    float.Parse(
                                        ModCfg.Options[OptionEntries.BoostByRooster][
                                            OptionDetails.Value
                                        ]
                                    ),
                                setValue: value =>
                                    ModCfg.Options[OptionEntries.BoostByRooster][
                                        OptionDetails.Value
                                    ] = value.ToString(),
                                name: () => _name,
                                tooltip: () => _description,
                                min: float.Parse(
                                    ModCfg.Options[OptionEntries.BoostByRooster][OptionDetails.Min]
                                ),
                                max: float.Parse(
                                    ModCfg.Options[OptionEntries.BoostByRooster][OptionDetails.Max]
                                ),
                                interval: float.Parse(
                                    ModCfg.Options[OptionEntries.BoostByRooster][
                                        OptionDetails.Interval
                                    ]
                                ),
                                formatValue: Format ? value => $"{value}%" : null!
                            );
                        }
                        else
                        {
                            configMenu.AddNumberOption(
                                mod: this.ModManifest,
                                getValue: () =>
                                    Int32.Parse(
                                        (
                                            roosterType.HasValue
                                                ? ModCfg.RoosterOptions[roosterType.Value]
                                                : ModCfg.Options
                                        )[optionEntries][OptionDetails.Value]
                                    ),
                                setValue: value =>
                                    (
                                        roosterType.HasValue
                                            ? ModCfg.RoosterOptions[roosterType.Value]
                                            : ModCfg.Options
                                    )[optionEntries][OptionDetails.Value] = value.ToString(),
                                name: () => _name,
                                tooltip: () => _description,
                                min: Convert.ToInt32(
                                    (
                                        roosterType.HasValue
                                            ? ModCfg.RoosterOptions[roosterType.Value]
                                            : ModCfg.Options
                                    )[optionEntries][OptionDetails.Min]
                                ),
                                max: Convert.ToInt32(
                                    (
                                        roosterType.HasValue
                                            ? ModCfg.RoosterOptions[roosterType.Value]
                                            : ModCfg.Options
                                    )[optionEntries][OptionDetails.Max]
                                ),
                                interval: Convert.ToInt32(
                                    (
                                        roosterType.HasValue
                                            ? ModCfg.RoosterOptions[roosterType.Value]
                                            : ModCfg.Options
                                    )[optionEntries][OptionDetails.Interval]
                                ),
                                formatValue: Format ? value => $"{value}%" : null!
                            );
                        }
                        break;
                    default:
                        break;
                }
                if (optionEntries == OptionEntries.HatchingMode)
                {
                    configMenu.AddSectionTitle(
                        mod: this.ModManifest,
                        text: () => Helper.Translation.Get("config.section.originalhatching.name")
                    );
                    configMenu.AddParagraph(
                        mod: this.ModManifest,
                        text: () =>
                            Helper.Translation.Get("config.section.originalhatching.description")
                    );
                }
                if (optionEntries == OptionEntries.LargeEggProbability)
                {
                    configMenu.AddSectionTitle(
                        mod: this.ModManifest,
                        text: () => Helper.Translation.Get("config.section.fertilizedhatching.name")
                    );
                    configMenu.AddParagraph(
                        mod: this.ModManifest,
                        text: () =>
                            Helper.Translation.Get("config.section.fertilizedhatching.description")
                    );
                }
            }
        }

        private Func<Texture2D> GetRoosterTexture(RoosterType roosterType)
        {
            var RoosterTypeToTexture = new Dictionary<RoosterType, Func<Texture2D>>
            {
                {
                    RoosterType.Brown,
                    () =>
                        Helper.ModContent.Load<Texture2D>(
                            ModTextures[Textures.BrownRooster][TextureDetails.MoldFolderPath]
                        )
                },
                {
                    RoosterType.White,
                    () =>
                        Helper.ModContent.Load<Texture2D>(
                            ModTextures[Textures.WhiteRooster][TextureDetails.MoldFolderPath]
                        )
                },
                {
                    RoosterType.Blue,
                    () =>
                        Helper.ModContent.Load<Texture2D>(
                            ModTextures[Textures.BlueRooster][TextureDetails.MoldFolderPath]
                        )
                },
                {
                    RoosterType.Gold,
                    () =>
                        Helper.ModContent.Load<Texture2D>(
                            ModTextures[Textures.GoldRooster][TextureDetails.MoldFolderPath]
                        )
                },
                {
                    RoosterType.Void,
                    () =>
                        Helper.ModContent.Load<Texture2D>(
                            ModTextures[Textures.VoidRooster][TextureDetails.MoldFolderPath]
                        )
                }
            };
            return RoosterTypeToTexture[roosterType];
        }

        private void AddFemaleOptions()
        {
            if (_roosterFemales.Females.Count > 0 && GetHatchApiCloneEggsOption())
            {
                configMenu.AddPage(
                    mod: this.ModManifest,
                    pageId: $"Females",
                    pageTitle: () => Helper.Translation.Get("config.pagelink.females.name")
                );
                foreach (var female in _roosterFemales.Females.Keys)
                {
                    configMenu.AddTextOption(
                        mod: this.ModManifest,
                        getValue: () => ModCfg.FemalesOptions[female],
                        setValue: value => ModCfg.FemalesOptions[female] = value,
                        name: () => female,
                        allowedValues: new string[]
                        {
                            OptionValues.Off,
                            OptionValues.WhiteRooster,
                            OptionValues.BrownRooster,
                            OptionValues.BlueRooster,
                            OptionValues.VoidRooster,
                            OptionValues.GoldRooster,
                        },
                        formatAllowedValue: value =>
                            $"{Helper.Translation.Get($"config.value.{value}")}"
                    );
                }
            }
        }
    }
}
