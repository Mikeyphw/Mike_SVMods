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
}
