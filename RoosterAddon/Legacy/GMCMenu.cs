using Microsoft.VisualBasic;
using Microsoft.Xna.Framework.Graphics;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework;
using RoosterAddon.Framework.Config;
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
        internal void GMCMSetup(ref IGenericModConfigMenuAPI configMenu)
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
            if (Config.FemalesOptions.Count > 0 && GetHatchApiCloneEggsOption())
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
                var optionEntry in optionEntriesOrder.Where(x =>
                    (
                        roosterType.HasValue
                            ? ModCfg.RoosterOptions[roosterType.Value]
                            : ModCfg.Options
                    ).ContainsKey(x)
                )
            )
            {
                var type = roosterType.HasValue
                    ? ModCfg.RoosterOptions[roosterType.Value][optionEntry][OptionDetails.Type]
                    : ModCfg.Options[optionEntry][OptionDetails.Type];

                var name = Helper.Translation.Get(
                    "config." + type + "." + ModConfig.GetEnumName(optionEntry) + ".name"
                );
                var description = Helper.Translation.Get(
                    "config." + type + "." + ModConfig.GetEnumName(optionEntry) + ".description"
                );

                switch (type)
                {
                    case GMCMTypes.Bool:
                        configMenu.AddBoolOption(
                            mod: this.ModManifest,
                            getValue: () => GetBool(optionEntry, roosterType),
                            setValue: value => SetBool(optionEntry, roosterType, value),
                            name: () => name,
                            tooltip: () => description
                        );
                        break;

                    case GMCMTypes.Text:
                        var allowedValues = ModCfg.GetAllowedValues(
                            optionEntry,
                            roosterType.HasValue ? roosterType.Value : null
                        );

                        configMenu.AddTextOption(
                            mod: this.ModManifest,
                            getValue: () => GetText(optionEntry, roosterType),
                            setValue: value => SetText(optionEntry, roosterType, value),
                            name: () => name,
                            tooltip: () => description,
                            allowedValues: allowedValues != null ? allowedValues : null!,
                            formatAllowedValue: value => Helper.Translation.Get("config.value." + value)
                        );
                        break;

                    case GMCMTypes.Number:
                        var entryName = ModConfig.GetEnumName(optionEntry);
                        var formatPct =
                            entryName.Contains("Prob")
                            || entryName.Contains("Boost")
                            || entryName.Contains("Daily");

                        if (optionEntry == OptionEntries.BoostByRooster)
                        {
                            configMenu.AddNumberOption(
                                mod: this.ModManifest,
                                getValue: () => Config.General.BoostByRooster,
                                setValue: value => Config.General.BoostByRooster = value,
                                name: () => name,
                                tooltip: () => description,
                                min: float.Parse(
                                    ModCfg.Options[OptionEntries.BoostByRooster][OptionDetails.Min]
                                ),
                                max: float.Parse(
                                    ModCfg.Options[OptionEntries.BoostByRooster][OptionDetails.Max]
                                ),
                                interval: float.Parse(
                                    ModCfg.Options[OptionEntries.BoostByRooster][OptionDetails.Interval]
                                ),
                                formatValue: formatPct ? value => $"{value}%" : null!
                            );
                        }
                        else
                        {
                            var range = roosterType.HasValue
                                ? ModCfg.RoosterOptions[roosterType.Value]
                                : ModCfg.Options;

                            configMenu.AddNumberOption(
                                mod: this.ModManifest,
                                getValue: () => GetInt(optionEntry, roosterType),
                                setValue: value => SetInt(optionEntry, roosterType, value),
                                name: () => name,
                                tooltip: () => description,
                                min: Convert.ToInt32(range[optionEntry][OptionDetails.Min]),
                                max: Convert.ToInt32(range[optionEntry][OptionDetails.Max]),
                                interval: Convert.ToInt32(range[optionEntry][OptionDetails.Interval]),
                                formatValue: formatPct ? value => $"{value}%" : null!
                            );
                        }
                        break;
                }

                if (optionEntry == OptionEntries.HatchingMode)
                {
                    configMenu.AddSectionTitle(
                        mod: this.ModManifest,
                        text: () => Helper.Translation.Get("config.section.originalhatching.name")
                    );
                    configMenu.AddParagraph(
                        mod: this.ModManifest,
                        text: () => Helper.Translation.Get("config.section.originalhatching.description")
                    );
                }

                if (optionEntry == OptionEntries.LargeEggProbability)
                {
                    configMenu.AddSectionTitle(
                        mod: this.ModManifest,
                        text: () => Helper.Translation.Get("config.section.fertilizedhatching.name")
                    );
                    configMenu.AddParagraph(
                        mod: this.ModManifest,
                        text: () => Helper.Translation.Get("config.section.fertilizedhatching.description")
                    );
                }
            }
        }

        private RoosterSettings GetRoosterSettings(RoosterType roosterType)
        {
            if (!Config.Roosters.TryGetValue(roosterType, out var settings))
            {
                settings = new RoosterSettings();
                Config.Roosters[roosterType] = settings;
            }

            EnsureProduceList(settings.ProduceEntries);
            EnsureProduceList(settings.DeluxeProduceEntries);

            return settings;
        }

        private static void EnsureProduceList(List<ProduceEntry> list)
        {
            while (list.Count < 4)
                list.Add(new ProduceEntry());
            if (list.Count > 4)
                list.RemoveRange(4, list.Count - 4);
        }

        private bool GetBool(OptionEntries entry, RoosterType? roosterType)
        {
            if (!roosterType.HasValue)
            {
                return entry switch
                {
                    OptionEntries.BetterTrufflesCompat => Config.General.BetterTrufflesCompat,
                    OptionEntries.EnableStuckInside => Config.General.EnableStuckInside,
                    OptionEntries.EnableEggProbability => Config.General.EnableEggProbability,
                    OptionEntries.EnableLargeEggProbability => Config.General.EnableLargeEggProbability,
                    OptionEntries.EnableDebug => Config.General.EnableDebug,
                    _ => false
                };
            }

            var r = GetRoosterSettings(roosterType.Value);
            return entry switch
            {
                OptionEntries.Buyable => r.Buyable,
                OptionEntries.EnableRoosterHatching => r.EnableRoosterHatching,
                OptionEntries.EditAllFemales => r.EditAllFemales,
                OptionEntries.BypassBlueConditions => r.BypassBlueConditions,
                _ => false
            };
        }

        private void SetBool(OptionEntries entry, RoosterType? roosterType, bool value)
        {
            if (!roosterType.HasValue)
            {
                switch (entry)
                {
                    case OptionEntries.BetterTrufflesCompat:
                        Config.General.BetterTrufflesCompat = value;
                        return;
                    case OptionEntries.EnableStuckInside:
                        Config.General.EnableStuckInside = value;
                        return;
                    case OptionEntries.EnableEggProbability:
                        Config.General.EnableEggProbability = value;
                        return;
                    case OptionEntries.EnableLargeEggProbability:
                        Config.General.EnableLargeEggProbability = value;
                        return;
                    case OptionEntries.EnableDebug:
                        Config.General.EnableDebug = value;
                        return;
                }

                return;
            }

            var r = GetRoosterSettings(roosterType.Value);
            switch (entry)
            {
                case OptionEntries.Buyable:
                    r.Buyable = value;
                    return;
                case OptionEntries.EnableRoosterHatching:
                    r.EnableRoosterHatching = value;
                    return;
                case OptionEntries.EditAllFemales:
                    r.EditAllFemales = value;
                    return;
                case OptionEntries.BypassBlueConditions:
                    r.BypassBlueConditions = value;
                    return;
            }
        }

        private int GetInt(OptionEntries entry, RoosterType? roosterType)
        {
            if (!roosterType.HasValue)
            {
                return entry switch
                {
                    OptionEntries.ChosenPercentage_DailyLuckBonus => Config.General.ChosenPercentageDailyLuckBonus,
                    OptionEntries.EggProbability => Config.General.EggProbability,
                    OptionEntries.LargeEggProbability => Config.General.LargeEggProbability,
                    OptionEntries.FertilizedEggProbability => Config.General.FertilizedEggProbability,
                    OptionEntries.RoosterToChickenRatio => Config.General.RoosterToChickenRatio,
                    _ => 0
                };
            }

            var r = GetRoosterSettings(roosterType.Value);

            if (TryGetProduceField(entry, out var isDeluxe, out var index, out var field)
                && field == ProduceField.Probability)
            {
                return isDeluxe ? r.DeluxeProduceEntries[index].Probability : r.ProduceEntries[index].Probability;
            }

            return entry switch
            {
                OptionEntries.Price => r.Price,
                OptionEntries.MatureDays => r.MatureDays,
                OptionEntries.ProduceDays => r.ProduceDays,
                _ => 0
            };
        }

        private void SetInt(OptionEntries entry, RoosterType? roosterType, int value)
        {
            if (!roosterType.HasValue)
            {
                switch (entry)
                {
                    case OptionEntries.ChosenPercentage_DailyLuckBonus:
                        Config.General.ChosenPercentageDailyLuckBonus = value;
                        return;
                    case OptionEntries.EggProbability:
                        Config.General.EggProbability = value;
                        return;
                    case OptionEntries.LargeEggProbability:
                        Config.General.LargeEggProbability = value;
                        return;
                    case OptionEntries.FertilizedEggProbability:
                        Config.General.FertilizedEggProbability = value;
                        return;
                    case OptionEntries.RoosterToChickenRatio:
                        Config.General.RoosterToChickenRatio = value;
                        return;
                }

                return;
            }

            var r = GetRoosterSettings(roosterType.Value);

            if (TryGetProduceField(entry, out var isDeluxe, out var index, out var field)
                && field == ProduceField.Probability)
            {
                if (isDeluxe)
                    r.DeluxeProduceEntries[index].Probability = value;
                else
                    r.ProduceEntries[index].Probability = value;
                return;
            }

            switch (entry)
            {
                case OptionEntries.Price:
                    r.Price = value;
                    return;
                case OptionEntries.MatureDays:
                    r.MatureDays = value;
                    return;
                case OptionEntries.ProduceDays:
                    r.ProduceDays = value;
                    return;
            }
        }

        private string GetText(OptionEntries entry, RoosterType? roosterType)
        {
            if (!roosterType.HasValue)
            {
                return entry switch
                {
                    OptionEntries.UpdateRate => ConfigStringConversions.ToLegacyString(Config.General.UpdateRate),
                    OptionEntries.HatchingMode => ConfigStringConversions.ToLegacyString(Config.General.HatchingMode),
                    OptionEntries.HarvestType => ConfigStringConversions.ToLegacyString(Config.General.HarvestType),
                    _ => string.Empty
                };
            }

            var r = GetRoosterSettings(roosterType.Value);

            if (TryGetProduceField(entry, out var isDeluxe, out var index, out var field))
            {
                return field switch
                {
                    ProduceField.Choice => isDeluxe ? r.DeluxeProduceEntries[index].Choice : r.ProduceEntries[index].Choice,
                    ProduceField.CustomItemName => isDeluxe ? r.DeluxeProduceEntries[index].CustomItemName : r.ProduceEntries[index].CustomItemName,
                    _ => string.Empty
                };
            }

            return entry switch
            {
                OptionEntries.BuildingRequired => ConfigStringConversions.ToLegacyString(r.BuildingRequired),
                OptionEntries.ProduceOption => ConfigStringConversions.ToLegacyString(r.ProduceOption),
                OptionEntries.DeluxeProduceOption => ConfigStringConversions.ToLegacyString(r.DeluxeProduceOption),
                OptionEntries.BlueEggChoice => r.BlueEggChoice,
                _ => string.Empty
            };
        }

        private void SetText(OptionEntries entry, RoosterType? roosterType, string value)
        {
            if (!roosterType.HasValue)
            {
                switch (entry)
                {
                    case OptionEntries.UpdateRate:
                        Config.General.UpdateRate = ConfigStringConversions.ParseUpdateRate(value);
                        return;
                    case OptionEntries.HatchingMode:
                        Config.General.HatchingMode = ConfigStringConversions.ParseHatchingMode(value);
                        return;
                    case OptionEntries.HarvestType:
                        Config.General.HarvestType = ConfigStringConversions.ParseHarvestType(value);
                        return;
                }

                return;
            }

            var r = GetRoosterSettings(roosterType.Value);

            if (TryGetProduceField(entry, out var isDeluxe, out var index, out var field))
            {
                if (isDeluxe)
                {
                    if (field == ProduceField.Choice)
                        r.DeluxeProduceEntries[index].Choice = value;
                    else if (field == ProduceField.CustomItemName)
                        r.DeluxeProduceEntries[index].CustomItemName = value;
                }
                else
                {
                    if (field == ProduceField.Choice)
                        r.ProduceEntries[index].Choice = value;
                    else if (field == ProduceField.CustomItemName)
                        r.ProduceEntries[index].CustomItemName = value;
                }

                return;
            }

            switch (entry)
            {
                case OptionEntries.BuildingRequired:
                    r.BuildingRequired = ConfigStringConversions.ParseRequiredBuilding(value);
                    return;
                case OptionEntries.ProduceOption:
                    r.ProduceOption = ConfigStringConversions.ParseProduceMode(value);
                    return;
                case OptionEntries.DeluxeProduceOption:
                    r.DeluxeProduceOption = ConfigStringConversions.ParseDeluxeProduceMode(value);
                    return;
                case OptionEntries.BlueEggChoice:
                    r.BlueEggChoice = value;
                    return;
            }
        }

        private enum ProduceField
        {
            Choice,
            CustomItemName,
            Probability
        }

        private static bool TryGetProduceField(
            OptionEntries entry,
            out bool isDeluxe,
            out int index,
            out ProduceField field
        )
        {
            var name = ModConfig.GetEnumName(entry);

            isDeluxe = false;
            index = -1;
            field = ProduceField.Choice;

            if (name == ModConfig.GetEnumName(OptionEntries.ProduceOption) ||
                name == ModConfig.GetEnumName(OptionEntries.DeluxeProduceOption))
                return false;

            if (name.StartsWith("CustomDeluxeProduce_", StringComparison.Ordinal))
            {
                isDeluxe = true;
                field = ProduceField.CustomItemName;
                return TryDigit(name, "CustomDeluxeProduce_", out index);
            }

            if (name.StartsWith("CustomProduce_", StringComparison.Ordinal))
            {
                isDeluxe = false;
                field = ProduceField.CustomItemName;
                return TryDigit(name, "CustomProduce_", out index);
            }

            if (name.StartsWith("DeluxeProduce_", StringComparison.Ordinal))
            {
                isDeluxe = true;
                field = name.EndsWith("_Prob", StringComparison.Ordinal) ? ProduceField.Probability : ProduceField.Choice;
                return TryDigit(name, "DeluxeProduce_", out index);
            }

            if (name.StartsWith("Produce_", StringComparison.Ordinal))
            {
                isDeluxe = false;
                field = name.EndsWith("_Prob", StringComparison.Ordinal) ? ProduceField.Probability : ProduceField.Choice;
                return TryDigit(name, "Produce_", out index);
            }

            return false;
        }

        private static bool TryDigit(string name, string prefix, out int index)
        {
            index = -1;

            var rest = name.Substring(prefix.Length);
            if (rest.Length == 0)
                return false;

            var digitChar = rest[0];
            if (digitChar < '1' || digitChar > '4')
                return false;

            index = (digitChar - '1');
            return true;
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
                        getValue: () => Config.FemalesOptions.TryGetValue(female, out var v) ? v : OptionValues.Off,
                        setValue: value => Config.FemalesOptions[female] = value,
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
