using System;
using System.Collections.Generic;
using System.Globalization;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework;

namespace RoosterAddon.Framework.Config;

/// <summary>
/// Converts between the legacy dictionary config (<see cref="ModConfig"/>) and the typed persisted config
/// (<see cref="RoosterAddonConfig"/>).
/// </summary>
public static class LegacyConfigAdapter
{
    public static RoosterAddonConfig FromLegacy(ModConfig legacy)
    {
        var cfg = new RoosterAddonConfig
        {
            SchemaVersion = ConfigSchema.CurrentVersion,
            General = new GeneralSettings
            {
                UpdateRate = ConfigStringConversions.ParseUpdateRate(GetText(legacy, OptionEntries.UpdateRate)),
                ChosenPercentageDailyLuckBonus = GetInt(legacy, OptionEntries.ChosenPercentage_DailyLuckBonus, 10),
                HatchingMode = ConfigStringConversions.ParseHatchingMode(GetText(legacy, OptionEntries.HatchingMode)),
                HarvestType = ConfigStringConversions.ParseHarvestType(GetText(legacy, OptionEntries.HarvestType)),
                BetterTrufflesCompat = GetBool(legacy, OptionEntries.BetterTrufflesCompat, true),
                EnableStuckInside = GetBool(legacy, OptionEntries.EnableStuckInside, true),
                EnableEggProbability = GetBool(legacy, OptionEntries.EnableEggProbability, true),
                EnableLargeEggProbability = GetBool(legacy, OptionEntries.EnableLargeEggProbability, true),
                EnableDebug = GetBool(legacy, OptionEntries.EnableDebug, true),
                EggProbability = GetInt(legacy, OptionEntries.EggProbability, 20),
                LargeEggProbability = GetInt(legacy, OptionEntries.LargeEggProbability, 40),
                FertilizedEggProbability = GetInt(legacy, OptionEntries.FertilizedEggProbability, 60),
                RoosterToChickenRatio = GetInt(legacy, OptionEntries.RoosterToChickenRatio, 50),
                BoostByRooster = GetFloat(legacy, OptionEntries.BoostByRooster, 0.1f),
            },
            FemalesOptions = legacy.FemalesOptions is not null
                ? new Dictionary<string, string>(legacy.FemalesOptions)
                : new Dictionary<string, string>(),
            Roosters = RoosterAddonConfig.CreateDefaultRoosters(),
        };

        foreach (var roosterType in cfg.Roosters.Keys)
        {
            if (!legacy.RoosterOptions.TryGetValue(roosterType, out var legacyRooster))
                continue;

            var r = cfg.Roosters[roosterType];
            r.BuildingRequired = ConfigStringConversions.ParseRequiredBuilding(GetText(legacy, OptionEntries.BuildingRequired, roosterType));
            r.Buyable = GetBool(legacy, OptionEntries.Buyable, true, roosterType);
            r.Price = GetInt(legacy, OptionEntries.Price, 2400, roosterType);
            r.EnableRoosterHatching = GetBool(legacy, OptionEntries.EnableRoosterHatching, true, roosterType);
            r.EditAllFemales = GetBool(legacy, OptionEntries.EditAllFemales, true, roosterType);
            r.MatureDays = GetInt(legacy, OptionEntries.MatureDays, 4, roosterType);
            r.ProduceDays = GetInt(legacy, OptionEntries.ProduceDays, 1, roosterType);
            r.ProduceOption = ConfigStringConversions.ParseProduceMode(GetText(legacy, OptionEntries.ProduceOption, roosterType));
            r.DeluxeProduceOption = ConfigStringConversions.ParseDeluxeProduceMode(GetText(legacy, OptionEntries.DeluxeProduceOption, roosterType));

            if (roosterType == RoosterType.Blue)
            {
                r.BlueEggChoice = GetText(legacy, OptionEntries.BlueEggChoice, roosterType);
                r.BypassBlueConditions = GetBool(legacy, OptionEntries.BypassBlueConditions, false, roosterType);
            }

            ReadProduceList(
                legacyRooster,
                r.ProduceEntries,
                produceChoice: OptionEntries.Produce_1,
                prob: OptionEntries.Produce_1_Prob,
                custom: OptionEntries.CustomProduce_1
            );

            ReadProduceList(
                legacyRooster,
                r.DeluxeProduceEntries,
                produceChoice: OptionEntries.DeluxeProduce_1,
                prob: OptionEntries.DeluxeProduce_1_Prob,
                custom: OptionEntries.CustomDeluxeProduce_1
            );
        }

        return cfg;
    }

    public static ModConfig ToLegacy(RoosterAddonConfig cfg)
    {
        var legacy = new ModConfig();

        SetText(legacy, OptionEntries.UpdateRate, ConfigStringConversions.ToLegacyString(cfg.General.UpdateRate));
        SetText(legacy, OptionEntries.ChosenPercentage_DailyLuckBonus, cfg.General.ChosenPercentageDailyLuckBonus.ToString(CultureInfo.InvariantCulture));
        SetText(legacy, OptionEntries.HatchingMode, ConfigStringConversions.ToLegacyString(cfg.General.HatchingMode));
        SetText(legacy, OptionEntries.HarvestType, ConfigStringConversions.ToLegacyString(cfg.General.HarvestType));

        SetBool(legacy, OptionEntries.BetterTrufflesCompat, cfg.General.BetterTrufflesCompat);
        SetBool(legacy, OptionEntries.EnableStuckInside, cfg.General.EnableStuckInside);
        SetBool(legacy, OptionEntries.EnableEggProbability, cfg.General.EnableEggProbability);
        SetBool(legacy, OptionEntries.EnableLargeEggProbability, cfg.General.EnableLargeEggProbability);
        SetBool(legacy, OptionEntries.EnableDebug, cfg.General.EnableDebug);

        SetText(legacy, OptionEntries.EggProbability, cfg.General.EggProbability.ToString(CultureInfo.InvariantCulture));
        SetText(legacy, OptionEntries.LargeEggProbability, cfg.General.LargeEggProbability.ToString(CultureInfo.InvariantCulture));
        SetText(legacy, OptionEntries.FertilizedEggProbability, cfg.General.FertilizedEggProbability.ToString(CultureInfo.InvariantCulture));
        SetText(legacy, OptionEntries.RoosterToChickenRatio, cfg.General.RoosterToChickenRatio.ToString(CultureInfo.InvariantCulture));

        legacy.Options[OptionEntries.BoostByRooster][OptionDetails.Value] = cfg.General.BoostByRooster.ToString(CultureInfo.InvariantCulture);

        legacy.FemalesOptions = cfg.FemalesOptions is not null
            ? new Dictionary<string, string>(cfg.FemalesOptions)
            : new Dictionary<string, string>();

        foreach (var (roosterType, roosterCfg) in cfg.Roosters)
        {
            if (!legacy.RoosterOptions.TryGetValue(roosterType, out var legacyRooster))
                continue;

            SetText(legacy, OptionEntries.BuildingRequired, ConfigStringConversions.ToLegacyString(roosterCfg.BuildingRequired), roosterType);
            SetBool(legacy, OptionEntries.Buyable, roosterCfg.Buyable, roosterType);
            SetText(legacy, OptionEntries.Price, roosterCfg.Price.ToString(CultureInfo.InvariantCulture), roosterType);
            SetBool(legacy, OptionEntries.EnableRoosterHatching, roosterCfg.EnableRoosterHatching, roosterType);
            SetBool(legacy, OptionEntries.EditAllFemales, roosterCfg.EditAllFemales, roosterType);
            SetText(legacy, OptionEntries.MatureDays, roosterCfg.MatureDays.ToString(CultureInfo.InvariantCulture), roosterType);
            SetText(legacy, OptionEntries.ProduceDays, roosterCfg.ProduceDays.ToString(CultureInfo.InvariantCulture), roosterType);
            SetText(legacy, OptionEntries.ProduceOption, ConfigStringConversions.ToLegacyString(roosterCfg.ProduceOption), roosterType);
            SetText(legacy, OptionEntries.DeluxeProduceOption, ConfigStringConversions.ToLegacyString(roosterCfg.DeluxeProduceOption), roosterType);

            if (roosterType == RoosterType.Blue)
            {
                SetText(legacy, OptionEntries.BlueEggChoice, roosterCfg.BlueEggChoice, roosterType);
                SetBool(legacy, OptionEntries.BypassBlueConditions, roosterCfg.BypassBlueConditions, roosterType);
            }

            WriteProduceList(
                legacyRooster,
                roosterCfg.ProduceEntries,
                produceChoice: OptionEntries.Produce_1,
                prob: OptionEntries.Produce_1_Prob,
                custom: OptionEntries.CustomProduce_1
            );

            WriteProduceList(
                legacyRooster,
                roosterCfg.DeluxeProduceEntries,
                produceChoice: OptionEntries.DeluxeProduce_1,
                prob: OptionEntries.DeluxeProduce_1_Prob,
                custom: OptionEntries.CustomDeluxeProduce_1
            );
        }

        return legacy;
    }

    private static void ReadProduceList(
        Dictionary<OptionEntries, Dictionary<OptionDetails, string>> legacyRooster,
        List<ProduceEntry> target,
        OptionEntries produceChoice,
        OptionEntries prob,
        OptionEntries custom
    )
    {
        EnsureFour(target);

        for (var i = 0; i < 4; i++)
        {
            var choiceKey = (OptionEntries)((int)produceChoice + i);
            var probKey = (OptionEntries)((int)prob + i);
            var customKey = (OptionEntries)((int)custom + i);

            var choice = legacyRooster[choiceKey][OptionDetails.Value];
            target[i].Choice = choice;
            target[i].CustomItemName = legacyRooster[customKey][OptionDetails.Value];
            target[i].Probability = ConfigStringConversions.ParseInt(legacyRooster[probKey][OptionDetails.Value], fallback: target[i].Probability);
        }
    }

    private static void WriteProduceList(
        Dictionary<OptionEntries, Dictionary<OptionDetails, string>> legacyRooster,
        List<ProduceEntry> source,
        OptionEntries produceChoice,
        OptionEntries prob,
        OptionEntries custom
    )
    {
        EnsureFour(source);

        for (var i = 0; i < 4; i++)
        {
            var choiceKey = (OptionEntries)((int)produceChoice + i);
            var probKey = (OptionEntries)((int)prob + i);
            var customKey = (OptionEntries)((int)custom + i);

            legacyRooster[choiceKey][OptionDetails.Value] = source[i].Choice;
            legacyRooster[customKey][OptionDetails.Value] = source[i].CustomItemName;
            legacyRooster[probKey][OptionDetails.Value] = source[i].Probability.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static void EnsureFour(List<ProduceEntry> list)
    {
        while (list.Count < 4)
            list.Add(new ProduceEntry());
        if (list.Count > 4)
            list.RemoveRange(4, list.Count - 4);
    }

    private static string GetText(ModConfig legacy, OptionEntries entry, RoosterType? roosterType = null)
        => roosterType.HasValue
            ? legacy.RoosterOptions[roosterType.Value][entry][OptionDetails.Value]
            : legacy.Options[entry][OptionDetails.Value];

    private static int GetInt(ModConfig legacy, OptionEntries entry, int fallback, RoosterType? roosterType = null)
        => ConfigStringConversions.ParseInt(GetText(legacy, entry, roosterType), fallback);

    private static float GetFloat(ModConfig legacy, OptionEntries entry, float fallback, RoosterType? roosterType = null)
        => ConfigStringConversions.ParseFloat(GetText(legacy, entry, roosterType), fallback);

    private static bool GetBool(ModConfig legacy, OptionEntries entry, bool fallback, RoosterType? roosterType = null)
        => ConfigStringConversions.ParseBool(GetText(legacy, entry, roosterType), fallback);

    private static void SetText(ModConfig legacy, OptionEntries entry, string value, RoosterType? roosterType = null)
    {
        if (roosterType.HasValue)
            legacy.RoosterOptions[roosterType.Value][entry][OptionDetails.Value] = value;
        else
            legacy.Options[entry][OptionDetails.Value] = value;
    }

    private static void SetBool(ModConfig legacy, OptionEntries entry, bool value, RoosterType? roosterType = null)
        => SetText(legacy, entry, value.ToString(), roosterType);
}
