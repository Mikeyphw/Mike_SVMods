using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Application.Catalogs;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;

namespace RoosterAddon.Application.Gmcm.Descriptors;

internal static class RoosterGmcmDescriptorRegistry
{
    public static IReadOnlyList<IGmcmDescriptor<RoosterAddonConfig>> RootGeneral()
        => new IGmcmDescriptor<RoosterAddonConfig>[]
        {
            new GmcmSectionDescriptor<RoosterAddonConfig>("root.general", Tooltip: false),

            Opt("general.updateRate", b => b.Enum(c => c.General.UpdateRate, optionId: "general.updateRate", tooltip: true)),
            Opt("general.catalogApplyMode", b => b.Enum(c => c.General.CatalogApplyMode, optionId: "general.catalogApplyMode", tooltip: true)),
            Opt("general.dailyLuckBonusPercent", b => b.Int(c => c.General.ChosenPercentageDailyLuckBonus, optionId: "general.dailyLuckBonusPercent", tooltip: true, min: 0, max: 100, interval: 1)),
            Opt("general.hatchingMode", b => b.Enum(c => c.General.HatchingMode, optionId: "general.hatchingMode", tooltip: true)),
            Opt("general.harvestType", b => b.Enum(c => c.General.HarvestType, optionId: "general.harvestType", tooltip: true)),

            Opt("general.betterTrufflesCompat", b => b.Bool(c => c.General.BetterTrufflesCompat, optionId: "general.betterTrufflesCompat", tooltip: true)),
            Opt("general.enableStuckInside", b => b.Bool(c => c.General.EnableStuckInside, optionId: "general.enableStuckInside", tooltip: true)),

            Opt("general.enableEggProbability", b => b.Bool(c => c.General.EnableEggProbability, optionId: "general.enableEggProbability", tooltip: true)),
            Opt("general.eggProbabilityPercent", b => b.Int(c => c.General.EggProbability, optionId: "general.eggProbabilityPercent", tooltip: true, min: 0, max: 100, interval: 1)),

            Opt("general.enableLargeEggProbability", b => b.Bool(c => c.General.EnableLargeEggProbability, optionId: "general.enableLargeEggProbability", tooltip: true)),
            Opt("general.largeEggProbabilityPercent", b => b.Int(c => c.General.LargeEggProbability, optionId: "general.largeEggProbabilityPercent", tooltip: true, min: 0, max: 100, interval: 1)),

            Opt("general.enableFertilizedEggProbability", b => b.Bool(c => c.General.EnableFertilizedEggProbability, optionId: "general.enableFertilizedEggProbability", tooltip: true)),
            Opt("general.fertilizedEggProbabilityPercent", b => b.Int(c => c.General.FertilizedEggProbability, optionId: "general.fertilizedEggProbabilityPercent", tooltip: true, min: 0, max: 100, interval: 1)),

            Opt("general.roosterToChickenRatioPercent", b => b.Int(c => c.General.RoosterToChickenRatio, optionId: "general.roosterToChickenRatioPercent", tooltip: true, min: 0, max: 100, interval: 1)),
            Opt("general.boostByRooster", b => b.Float(c => c.General.BoostByRooster, optionId: "general.boostByRooster", tooltip: true, min: 0f, max: 1000f, interval: 0.01f)),

            Opt("general.enableDebug", b => b.Bool(c => c.General.EnableDebug, optionId: "general.enableDebug", tooltip: true)),
        };

    public static IReadOnlyList<IGmcmDescriptor<RoosterAddonConfig>> RootRoostersLinks()
        => new IGmcmDescriptor<RoosterAddonConfig>[]
        {
            new GmcmSectionDescriptor<RoosterAddonConfig>("root.roosters", Tooltip: false),
            new GmcmPageLinkDescriptor<RoosterAddonConfig>("roosters_white", Tooltip: true),
            new GmcmPageLinkDescriptor<RoosterAddonConfig>("roosters_brown", Tooltip: true),
            new GmcmPageLinkDescriptor<RoosterAddonConfig>("roosters_blue", Tooltip: true),
            new GmcmPageLinkDescriptor<RoosterAddonConfig>("roosters_void", Tooltip: true),
            new GmcmPageLinkDescriptor<RoosterAddonConfig>("roosters_gold", Tooltip: true),
        };

    public static IReadOnlyList<GmcmPageDescriptor<RoosterAddonConfig>> Pages(Func<RoosterAddonConfig> getConfig, Func<CatalogBundle> getCatalogs)
    {
        if (getConfig is null) throw new ArgumentNullException(nameof(getConfig));

        return new[]
        {
            RoosterPage(getConfig, getCatalogs, RoosterType.White, "roosters_white", "white", includeBlueOnly: false),
            RoosterPage(getConfig, getCatalogs, RoosterType.Brown, "roosters_brown", "brown", includeBlueOnly: false),
            RoosterPage(getConfig, getCatalogs, RoosterType.Blue, "roosters_blue", "blue", includeBlueOnly: true),
            RoosterPage(getConfig, getCatalogs, RoosterType.Void, "roosters_void", "void", includeBlueOnly: false),
            RoosterPage(getConfig, getCatalogs, RoosterType.Gold, "roosters_gold", "gold", includeBlueOnly: false),
        };
    }

    private static GmcmPageDescriptor<RoosterAddonConfig> RoosterPage(
            Func<RoosterAddonConfig> getConfig,
            Func<CatalogBundle> getCatalogs,
            RoosterType type,
        string pageId,
        string colorId,
        bool includeBlueOnly)
    {
        var p = new List<IGmcmDescriptor<RoosterAddonConfig>>();

        p.Add(new GmcmSectionDescriptor<RoosterAddonConfig>("roosters.availability"));

        p.Add(Opt($"roosters.{colorId}.buyable",
            b => b.Bool(
                optionId: $"roosters.{colorId}.buyable",
                getValue: () => GetSettings(getConfig(), type).Buyable,
                setValue: v => GetSettings(getConfig(), type).Buyable = v,
                tooltip: true)));

        p.Add(Opt($"roosters.{colorId}.price",
            b => b.Int(
                optionId: $"roosters.{colorId}.price",
                getValue: () => GetSettings(getConfig(), type).Price,
                setValue: v => GetSettings(getConfig(), type).Price = Clamp(v, 0, 100000),
                tooltip: true,
                min: 0,
                max: 100000,
                interval: 100)));

        p.Add(Opt($"roosters.{colorId}.buildingRequired",
            b => EnumTextOption(
                b,
                optionId: $"roosters.{colorId}.buildingRequired",
                getValue: () => GetSettings(getConfig(), type).BuildingRequired,
                setValue: v => GetSettings(getConfig(), type).BuildingRequired = v,
                tooltip: true)));

        p.Add(new GmcmSectionDescriptor<RoosterAddonConfig>("roosters.behavior"));

        p.Add(Opt($"roosters.{colorId}.enableRoosterHatching",
            b => b.Bool(
                optionId: $"roosters.{colorId}.enableRoosterHatching",
                getValue: () => GetSettings(getConfig(), type).EnableRoosterHatching,
                setValue: v => GetSettings(getConfig(), type).EnableRoosterHatching = v,
                tooltip: true)));

        p.Add(Opt($"roosters.{colorId}.editAllFemales",
            b => b.Bool(
                optionId: $"roosters.{colorId}.editAllFemales",
                getValue: () => GetSettings(getConfig(), type).EditAllFemales,
                setValue: v => GetSettings(getConfig(), type).EditAllFemales = v,
                tooltip: true)));

        p.Add(Opt($"roosters.{colorId}.matureDays",
            b => b.Int(
                optionId: $"roosters.{colorId}.matureDays",
                getValue: () => GetSettings(getConfig(), type).MatureDays,
                setValue: v => GetSettings(getConfig(), type).MatureDays = Clamp(v, 0, 999),
                tooltip: true,
                min: 0,
                max: 999,
                interval: 1)));

        p.Add(Opt($"roosters.{colorId}.produceDays",
            b => b.Int(
                optionId: $"roosters.{colorId}.produceDays",
                getValue: () => GetSettings(getConfig(), type).ProduceDays,
                setValue: v => GetSettings(getConfig(), type).ProduceDays = Clamp(v, 1, 999),
                tooltip: true,
                min: 1,
                max: 999,
                interval: 1)));

        p.Add(new GmcmSectionDescriptor<RoosterAddonConfig>("roosters.produce"));

        p.Add(Opt($"roosters.{colorId}.produceMode",
            b => EnumTextOption(
                b,
                optionId: $"roosters.{colorId}.produceMode",
                getValue: () => GetSettings(getConfig(), type).ProduceOption,
                setValue: v => GetSettings(getConfig(), type).ProduceOption = v,
                tooltip: true)));

        AddProduceEntries(p, getConfig, getCatalogs, type, colorId, deluxe: false);

        p.Add(new GmcmSectionDescriptor<RoosterAddonConfig>("roosters.deluxeProduce", Tooltip: true));

        p.Add(Opt($"roosters.{colorId}.deluxeProduceMode",
            b => EnumTextOption(
                b,
                optionId: $"roosters.{colorId}.deluxeProduceMode",
                getValue: () => GetSettings(getConfig(), type).DeluxeProduceOption,
                setValue: v => GetSettings(getConfig(), type).DeluxeProduceOption = v,
                tooltip: true)));

        AddProduceEntries(p, getConfig, getCatalogs, type, colorId, deluxe: true);

        if (includeBlueOnly)
        {
            p.Add(new GmcmSectionDescriptor<RoosterAddonConfig>("roosters.blueOnly", Tooltip: true));

            p.Add(Opt($"roosters.{colorId}.bypassBlueConditions",
                b => b.Bool(
                    optionId: $"roosters.{colorId}.bypassBlueConditions",
                    getValue: () => GetSettings(getConfig(), type).BypassBlueConditions,
                    setValue: v => GetSettings(getConfig(), type).BypassBlueConditions = v,
                    tooltip: true)));

            p.Add(Opt($"roosters.{colorId}.blueEggChoice",
                b => b.Text(
                    optionId: $"roosters.{colorId}.blueEggChoice",
                    getValue: () => GetSettings(getConfig(), type).BlueEggChoice ?? string.Empty,
                    setValue: v => GetSettings(getConfig(), type).BlueEggChoice = v ?? string.Empty,
                    tooltip: true)));
        }

        return new GmcmPageDescriptor<RoosterAddonConfig>(pageId, p);
    }

    private static void AddProduceEntries(
            List<IGmcmDescriptor<RoosterAddonConfig>> p,
            Func<RoosterAddonConfig> getConfig,
            Func<CatalogBundle> getCatalogs,
            RoosterType type,
        string colorId,
        bool deluxe)
    {
        var listKind = deluxe ? "deluxe" : "produce";
        var allowedChoices = BuildAllowedChoices(getCatalogs(), type, deluxe);
        for (var i = 0; i < 4; i++)
        {
            var idx = i;

            p.Add(Opt($"roosters.{colorId}.{listKind}.{idx + 1}.choice",
                b => b.Text(
                    optionId: $"roosters.{colorId}.{listKind}.{idx + 1}.choice",
                    getValue: () => GetEntry(getConfig(), type, deluxe, idx).Choice ?? string.Empty,
                    setValue: v => GetEntry(getConfig(), type, deluxe, idx).Choice = v ?? string.Empty,
                    tooltip: true,
                    allowedValues: allowedChoices,
                    formatAllowedValue: false)));

            p.Add(Opt($"roosters.{colorId}.{listKind}.{idx + 1}.customItemName",
                b => b.Text(
                    optionId: $"roosters.{colorId}.{listKind}.{idx + 1}.customItemName",
                    getValue: () => GetEntry(getConfig(), type, deluxe, idx).CustomItemName ?? string.Empty,
                    setValue: v => GetEntry(getConfig(), type, deluxe, idx).CustomItemName = v ?? string.Empty,
                    tooltip: true)));

            p.Add(Opt($"roosters.{colorId}.{listKind}.{idx + 1}.probabilityPercent",
                b => b.Int(
                    optionId: $"roosters.{colorId}.{listKind}.{idx + 1}.probabilityPercent",
                    getValue: () => GetEntry(getConfig(), type, deluxe, idx).Probability,
                    setValue: v => GetEntry(getConfig(), type, deluxe, idx).Probability = Clamp(v, 0, 100),
                    tooltip: true,
                    min: 0,
                    max: 100,
                    interval: 1)));
        }
    }

    private static ProduceEntry GetEntry(RoosterAddonConfig config, RoosterType type, bool deluxe, int idx)
    {
        var s = GetSettings(config, type);
        var list = deluxe ? s.DeluxeProduceEntries : s.ProduceEntries;

        list ??= new List<ProduceEntry>();
        while (list.Count < 4)
            list.Add(new ProduceEntry());

        if (deluxe)
            s.DeluxeProduceEntries = list;
        else
            s.ProduceEntries = list;

        return list[idx];
    }

    private static RoosterSettings GetSettings(RoosterAddonConfig config, RoosterType type)
    {
        config.Roosters ??= RoosterAddonConfig.CreateDefaultRoosters();
        if (!config.Roosters.TryGetValue(type, out var s) || s is null)
        {
            s = new RoosterSettings();
            config.Roosters[type] = s;
        }

        s.ProduceEntries ??= ProduceEntry.CreateDefaultFour();
        s.DeluxeProduceEntries ??= ProduceEntry.CreateDefaultFour();

        return s;
    }

    private static int Clamp(int value, int min, int max) => Math.Min(max, Math.Max(min, value));

    private static IGmcmDescriptor<RoosterAddonConfig> Opt(string optionId, Action<Mikew.BirdsOfAFeather.Integrations.Gmcm.GmcmPageBuilder<RoosterAddonConfig>> add)
        => new GmcmOptionDescriptor<RoosterAddonConfig>(optionId, add, Tooltip: true);

    private static void EnumTextOption<TEnum>(
        Mikew.BirdsOfAFeather.Integrations.Gmcm.GmcmPageBuilder<RoosterAddonConfig> b,
        string optionId,
        Func<TEnum> getValue,
        Action<TEnum> setValue,
        bool tooltip)
        where TEnum : struct, Enum
    {
        var allowedValues = Enum.GetNames(typeof(TEnum));
        b.Text(
            optionId: optionId,
            getValue: () => getValue().ToString(),
            setValue: v =>
            {
                if (Enum.TryParse<TEnum>(v, ignoreCase: true, out var parsed))
                    setValue(parsed);
            },
            tooltip: tooltip,
            allowedValues: allowedValues,
            formatAllowedValue: true);
    }
}
