using System.Collections.Generic;
using RoosterAddon.Application.Gmcm.Descriptors;
using RoosterAddon.Framework.Config;

namespace RoosterAddon.Application.Gmcm.Descriptors;

/// <summary>Descriptor registry for GMCM layout (Phase 9, page-by-page migration).</summary>
internal static class RoosterGmcmDescriptorRegistry
{
    public static IReadOnlyList<IGmcmElementDescriptor<RoosterAddonConfig>> RootGeneral()
        => new IGmcmElementDescriptor<RoosterAddonConfig>[]
        {
            new GmcmSectionDescriptor<RoosterAddonConfig>("general"),

            new GmcmOptionDescriptors.Bool<RoosterAddonConfig>(c => c.General.EnableDebug, "general.enable_debug"),
            new GmcmOptionDescriptors.EnumOption<RoosterAddonConfig, UpdateRateMode>(c => c.General.UpdateRate, "general.update_rate"),
            new GmcmOptionDescriptors.Int<RoosterAddonConfig>(c => c.General.ChosenPercentageDailyLuckBonus, "general.daily_luck_bonus", Min: 0, Max: 100, Interval: 1),
            new GmcmOptionDescriptors.EnumOption<RoosterAddonConfig, HarvestType>(c => c.General.HarvestType, "general.harvest_type"),

            new GmcmOptionDescriptors.Bool<RoosterAddonConfig>(c => c.General.EnableStuckInside, "general.enable_stuck_inside"),
            new GmcmOptionDescriptors.Bool<RoosterAddonConfig>(c => c.General.BetterTrufflesCompat, "general.better_truffles_compat"),

            new GmcmOptionDescriptors.EnumOption<RoosterAddonConfig, HatchingMode>(c => c.General.HatchingMode, "general.hatching_mode"),

            new GmcmOptionDescriptors.Bool<RoosterAddonConfig>(c => c.General.EnableEggProbability, "general.enable_egg_probability"),
            new GmcmOptionDescriptors.Int<RoosterAddonConfig>(c => c.General.EggProbability, "general.egg_probability", Min: 0, Max: 100, Interval: 1),

            new GmcmOptionDescriptors.Bool<RoosterAddonConfig>(c => c.General.EnableLargeEggProbability, "general.enable_large_egg_probability"),
            new GmcmOptionDescriptors.Int<RoosterAddonConfig>(c => c.General.LargeEggProbability, "general.large_egg_probability", Min: 0, Max: 100, Interval: 1),

            new GmcmOptionDescriptors.Int<RoosterAddonConfig>(c => c.General.FertilizedEggProbability, "general.fertilized_egg_probability", Min: 0, Max: 100, Interval: 1),
            new GmcmOptionDescriptors.Int<RoosterAddonConfig>(c => c.General.RoosterToChickenRatio, "general.rooster_to_chicken_ratio", Min: 0, Max: 100, Interval: 1),

            new GmcmOptionDescriptors.Float<RoosterAddonConfig>(c => c.General.BoostByRooster, "general.boost_by_rooster", Min: 0f, Max: 10f, Interval: 0.01f),

            new GmcmOptionDescriptors.EnumOption<RoosterAddonConfig, CatalogApplyMode>(c => c.General.CatalogApplyMode, "general.catalog_apply_mode"),
        };
}
