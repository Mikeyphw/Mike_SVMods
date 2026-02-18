using System;
using RoosterAddon.Application.Gmcm;
using RoosterAddon.Framework.Config;

namespace RoosterAddon.Application.Gmcm.Descriptors;

internal static class RoosterGmcmDescriptorRegistry
{
    public static GmcmSectionDescriptor RootGeneral()
        => new(
            SectionId: "general",
            Tooltip: false,
            Entries: new IGmcmEntryDescriptor[]
            {
                new GmcmBoolOptionDescriptor(
                    OptionId: "general.enableDebug",
                    Member: c => c.General.EnableDebug,
                    Tooltip: true),

                new GmcmEnumOptionDescriptor<UpdateRateMode>(
                    OptionId: "general.updateRate",
                    Member: c => c.General.UpdateRate,
                    Tooltip: true),

                new GmcmIntOptionDescriptor(
                    OptionId: "general.chosenPercentageDailyLuckBonus",
                    Member: c => c.General.ChosenPercentageDailyLuckBonus,
                    Min: 0,
                    Max: 100,
                    Interval: 1,
                    Tooltip: true),

                new GmcmEnumOptionDescriptor<HarvestType>(
                    OptionId: "general.harvestType",
                    Member: c => c.General.HarvestType,
                    Tooltip: true),

                new GmcmBoolOptionDescriptor(
                    OptionId: "general.enableStuckInside",
                    Member: c => c.General.EnableStuckInside,
                    Tooltip: true),

                new GmcmBoolOptionDescriptor(
                    OptionId: "general.betterTrufflesCompat",
                    Member: c => c.General.BetterTrufflesCompat,
                    Tooltip: true),

                new GmcmEnumOptionDescriptor<HatchingMode>(
                    OptionId: "general.hatchingMode",
                    Member: c => c.General.HatchingMode,
                    Tooltip: true),

                new GmcmBoolOptionDescriptor(
                    OptionId: "general.enableEggProbability",
                    Member: c => c.General.EnableEggProbability,
                    Tooltip: true),

                new GmcmIntOptionDescriptor(
                    OptionId: "general.eggProbability",
                    Member: c => c.General.EggProbability,
                    Min: 0,
                    Max: 100,
                    Interval: 1,
                    Tooltip: true),

                new GmcmBoolOptionDescriptor(
                    OptionId: "general.enableLargeEggProbability",
                    Member: c => c.General.EnableLargeEggProbability,
                    Tooltip: true),

                new GmcmIntOptionDescriptor(
                    OptionId: "general.largeEggProbability",
                    Member: c => c.General.LargeEggProbability,
                    Min: 0,
                    Max: 100,
                    Interval: 1,
                    Tooltip: true),

                new GmcmIntOptionDescriptor(
                    OptionId: "general.fertilizedEggProbability",
                    Member: c => c.General.FertilizedEggProbability,
                    Min: 0,
                    Max: 100,
                    Interval: 1,
                    Tooltip: true),

                new GmcmIntOptionDescriptor(
                    OptionId: "general.roosterToChickenRatio",
                    Member: c => c.General.RoosterToChickenRatio,
                    Min: 0,
                    Max: 100,
                    Interval: 1,
                    Tooltip: true),

                new GmcmFloatOptionDescriptor(
                    OptionId: "general.boostByRooster",
                    Member: c => c.General.BoostByRooster,
                    Min: 0f,
                    Max: 10f,
                    Interval: 0.01f,
                    Tooltip: true),
            });

    public static GmcmSectionDescriptor RootRoostersLinks()
        => new(
            SectionId: "roosters",
            Tooltip: false,
            Entries: new IGmcmEntryDescriptor[]
            {
                new GmcmPageLinkDescriptor(RoosterGmcmPageIds.RoostersWhite),
                new GmcmPageLinkDescriptor(RoosterGmcmPageIds.RoostersBrown),
                new GmcmPageLinkDescriptor(RoosterGmcmPageIds.RoostersBlue),
                new GmcmPageLinkDescriptor(RoosterGmcmPageIds.RoostersVoid),
                new GmcmPageLinkDescriptor(RoosterGmcmPageIds.RoostersGold),
            });

    public static GmcmSectionDescriptor[] RoostersWhite(Func<RoosterSettings> getSettings)
        => BuildRoosterPage(getSettings, includeBlue: false);

    public static GmcmSectionDescriptor[] RoostersBrown(Func<RoosterSettings> getSettings)
        => BuildRoosterPage(getSettings, includeBlue: false);

    public static GmcmSectionDescriptor[] RoostersBlue(Func<RoosterSettings> getSettings)
        => BuildRoosterPage(getSettings, includeBlue: true);

private static GmcmSectionDescriptor[] BuildRoosterPage(Func<RoosterSettings> getSettings, bool includeBlue)
    {
        var availability = new GmcmSectionDescriptor(
            SectionId: "rooster.availability",
            Tooltip: false,
            Entries: new IGmcmEntryDescriptor[]
            {
                new GmcmBoolFuncOptionDescriptor(
                    OptionId: "rooster.buyable",
                    GetValue: () => getSettings().Buyable,
                    SetValue: v => getSettings().Buyable = v,
                    Tooltip: true),

                new GmcmIntFuncOptionDescriptor(
                    OptionId: "rooster.price",
                    GetValue: () => getSettings().Price,
                    SetValue: v => getSettings().Price = ClampInt(v, 0, 100000),
                    Min: 0,
                    Max: 100000,
                    Interval: 100,
                    Tooltip: true),

                new GmcmTextFuncOptionDescriptor(
                    OptionId: "rooster.buildingRequired",
                    GetValue: () => getSettings().BuildingRequired.ToString(),
                    SetValue: v => getSettings().BuildingRequired = ParseEnum(v, RequiredBuilding.BigCoop),
                    AllowedValues: System.Enum.GetNames(typeof(RequiredBuilding)),
                    FormatAllowedValue: true,
                    Tooltip: true),
            });

        var behavior = new GmcmSectionDescriptor(
            SectionId: "rooster.behavior",
            Tooltip: false,
            Entries: new IGmcmEntryDescriptor[]
            {
                new GmcmBoolFuncOptionDescriptor(
                    OptionId: "rooster.enableRoosterHatching",
                    GetValue: () => getSettings().EnableRoosterHatching,
                    SetValue: v => getSettings().EnableRoosterHatching = v,
                    Tooltip: true),

                new GmcmBoolFuncOptionDescriptor(
                    OptionId: "rooster.editAllFemales",
                    GetValue: () => getSettings().EditAllFemales,
                    SetValue: v => getSettings().EditAllFemales = v,
                    Tooltip: true),

                new GmcmIntFuncOptionDescriptor(
                    OptionId: "rooster.matureDays",
                    GetValue: () => getSettings().MatureDays,
                    SetValue: v => getSettings().MatureDays = ClampInt(v, 0, 999),
                    Min: 0,
                    Max: 999,
                    Interval: 1,
                    Tooltip: true),

                new GmcmIntFuncOptionDescriptor(
                    OptionId: "rooster.produceDays",
                    GetValue: () => getSettings().ProduceDays,
                    SetValue: v => getSettings().ProduceDays = ClampInt(v, 1, 999),
                    Min: 1,
                    Max: 999,
                    Interval: 1,
                    Tooltip: true),
            });

        var produceEntries = new System.Collections.Generic.List<IGmcmEntryDescriptor>
        {
            new GmcmTextFuncOptionDescriptor(
                OptionId: "rooster.produceMode",
                GetValue: () => getSettings().ProduceOption.ToString(),
                SetValue: v => getSettings().ProduceOption = ParseEnum(v, ProduceMode.CustomizeBelow),
                AllowedValues: System.Enum.GetNames(typeof(ProduceMode)),
                FormatAllowedValue: true,
                Tooltip: true),
        };

        for (var i = 0; i < 4; i++)
        {
            var idx = i;
            var n = idx + 1;

            produceEntries.Add(
                new GmcmTextFuncOptionDescriptor(
                    OptionId: $"rooster.produce.entry{n}.choice",
                    GetValue: () => getSettings().ProduceEntries[idx].Choice,
                    SetValue: v => getSettings().ProduceEntries[idx].Choice = v,
                    Tooltip: true));

            produceEntries.Add(
                new GmcmTextFuncOptionDescriptor(
                    OptionId: $"rooster.produce.entry{n}.customName",
                    GetValue: () => getSettings().ProduceEntries[idx].CustomItemName,
                    SetValue: v => getSettings().ProduceEntries[idx].CustomItemName = v,
                    Tooltip: true));

            produceEntries.Add(
                new GmcmIntFuncOptionDescriptor(
                    OptionId: $"rooster.produce.entry{n}.probability",
                    GetValue: () => getSettings().ProduceEntries[idx].Probability,
                    SetValue: v => getSettings().ProduceEntries[idx].Probability = ClampInt(v, 0, 100),
                    Min: 0,
                    Max: 100,
                    Interval: 1,
                    Tooltip: true));
        }

        var produce = new GmcmSectionDescriptor(
            SectionId: "rooster.produce",
            Tooltip: false,
            Entries: produceEntries);

        var deluxeEntries = new System.Collections.Generic.List<IGmcmEntryDescriptor>
        {
            new GmcmTextFuncOptionDescriptor(
                OptionId: "rooster.deluxeMode",
                GetValue: () => getSettings().DeluxeProduceOption.ToString(),
                SetValue: v => getSettings().DeluxeProduceOption = ParseEnum(v, DeluxeProduceMode.CustomizeBelow),
                AllowedValues: System.Enum.GetNames(typeof(DeluxeProduceMode)),
                FormatAllowedValue: true,
                Tooltip: true),
        };

        for (var i = 0; i < 4; i++)
        {
            var idx = i;
            var n = idx + 1;

            deluxeEntries.Add(
                new GmcmTextFuncOptionDescriptor(
                    OptionId: $"rooster.deluxe.entry{n}.choice",
                    GetValue: () => getSettings().DeluxeProduceEntries[idx].Choice,
                    SetValue: v => getSettings().DeluxeProduceEntries[idx].Choice = v,
                    Tooltip: true));

            deluxeEntries.Add(
                new GmcmTextFuncOptionDescriptor(
                    OptionId: $"rooster.deluxe.entry{n}.customName",
                    GetValue: () => getSettings().DeluxeProduceEntries[idx].CustomItemName,
                    SetValue: v => getSettings().DeluxeProduceEntries[idx].CustomItemName = v,
                    Tooltip: true));

            deluxeEntries.Add(
                new GmcmIntFuncOptionDescriptor(
                    OptionId: $"rooster.deluxe.entry{n}.probability",
                    GetValue: () => getSettings().DeluxeProduceEntries[idx].Probability,
                    SetValue: v => getSettings().DeluxeProduceEntries[idx].Probability = ClampInt(v, 0, 100),
                    Min: 0,
                    Max: 100,
                    Interval: 1,
                    Tooltip: true));
        }

        var deluxe = new GmcmSectionDescriptor(
            SectionId: "rooster.deluxe",
            Tooltip: false,
            Entries: deluxeEntries);

        if (!includeBlue)
            return new[] { availability, behavior, produce, deluxe };

        var blue = new GmcmSectionDescriptor(
            SectionId: "rooster.blue",
            Tooltip: false,
            Entries: new IGmcmEntryDescriptor[]
            {
                new GmcmBoolFuncOptionDescriptor(
                    OptionId: "rooster.bypassBlueConditions",
                    GetValue: () => getSettings().BypassBlueConditions,
                    SetValue: v => getSettings().BypassBlueConditions = v,
                    Tooltip: true),

                new GmcmTextFuncOptionDescriptor(
                    OptionId: "rooster.blueEggChoice",
                    GetValue: () => getSettings().BlueEggChoice ?? string.Empty,
                    SetValue: v => getSettings().BlueEggChoice = v,
                    Tooltip: true),
            });

        return new[] { availability, behavior, produce, deluxe, blue };
    }

    private static int ClampInt(int value, int min, int max) => System.Math.Max(min, System.Math.Min(max, value));

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct, System.Enum
        => System.Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;

}
