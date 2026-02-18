using System;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using RoosterAddon.Framework.Config;
using RoosterAddon.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;

using ILetMeHatchApi = Mikew.BirdsOfAFeather.Framework.ILetMeHatchApi;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private IBirdsOfAFeatherApi? _boafApi;

    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ModEntry.ContentPatcher = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

        HatchApi = Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh")
            ?? LetMeHatchApiAdapter.Wrap(Helper.ModRegistry.GetApi<object>("Mikew.lmh"));

        _boafApi = Helper.ModRegistry.GetApi<IBirdsOfAFeatherApi>("Mikew.birdsofafeather");
        if (_boafApi is null || _boafApi.ApiVersion < 1)
        {
            Monitor.Log("BirdsOfAFeather API not found or too old; GMCM menu won't be registered.", LogLevel.Warn);
            return;
        }

        _boafApi.RegisterGmcmMenu(
            helper: Helper,
            manifest: ModManifest,
            getConfig: () => Config,
            reset: ResetConfig,
            save: SaveConfig,
            define: DefineGmcmLayout,
            force: true,
            attachLocaleHandler: true
        );
    }

    public void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
    {
        // BOAF auto-rebuilds GMCM on locale change; we still refresh assets that may depend on language.
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void ResetConfig()
    {
        Config = RoosterAddonConfig.CreateDefault();

        // Treat as a "new" config so catalogs seed initial values.
        _catalogManager.ApplyToConfig(Config, _catalogs, isNewConfig: true, Monitor);

        RoosterHelper?.UpdateConfig(Config);
        ConfigManager.Save(Helper, Config);

        InvalidateDailyPlanCache("config reset");
        _needsRoosterSetup = true;
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void SaveConfig()
    {
        RoosterHelper?.UpdateConfig(Config);
        ConfigManager.Save(Helper, Config);

        InvalidateDailyPlanCache("config saved");
        _needsRoosterSetup = true;
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void DefineGmcmLayout(GmcmMenuLayout<RoosterAddonConfig> layout)
    {
        layout.Root(root =>
        {
            root.Section("general");

            root.Bool(c => c.General.EnableDebug, optionId: "general.enable_debug");
            root.Enum(c => c.General.UpdateRate, optionId: "general.update_rate");
            root.Int(c => c.General.ChosenPercentageDailyLuckBonus, optionId: "general.daily_luck_bonus", min: 0, max: 100, interval: 1);
            root.Enum(c => c.General.HarvestType, optionId: "general.harvest_type");

            root.Bool(c => c.General.EnableStuckInside, optionId: "general.enable_stuck_inside");
            root.Bool(c => c.General.BetterTrufflesCompat, optionId: "general.better_truffles_compat");

            root.Enum(c => c.General.HatchingMode, optionId: "general.hatching_mode");

            root.Bool(c => c.General.EnableEggProbability, optionId: "general.enable_egg_probability");
            root.Int(c => c.General.EggProbability, optionId: "general.egg_probability", min: 0, max: 100, interval: 1);

            root.Bool(c => c.General.EnableLargeEggProbability, optionId: "general.enable_large_egg_probability");
            root.Int(c => c.General.LargeEggProbability, optionId: "general.large_egg_probability", min: 0, max: 100, interval: 1);

            root.Int(c => c.General.FertilizedEggProbability, optionId: "general.fertilized_egg_probability", min: 0, max: 100, interval: 1);
            root.Int(c => c.General.RoosterToChickenRatio, optionId: "general.rooster_to_chicken_ratio", min: 0, max: 100, interval: 1);

            root.Float(c => c.General.BoostByRooster, optionId: "general.boost_by_rooster", min: 0f, max: 10f, interval: 0.01f);

            root.Enum(c => c.General.CatalogApplyMode, optionId: "general.catalog_apply_mode");
        });

        layout.Root(root =>
        {
            root.Section("roosters");
            root.Link("roosters_white");
            root.Link("roosters_brown");
            root.Link("roosters_blue");
            root.Link("roosters_void");
            root.Link("roosters_gold");
        });

        layout.Page("roosters_white", p => BuildRoosterPage(p, RoosterAddon.Enums.RoosterType.White));
        layout.Page("roosters_brown", p => BuildRoosterPage(p, RoosterAddon.Enums.RoosterType.Brown));
        layout.Page("roosters_blue", p => BuildRoosterPage(p, RoosterAddon.Enums.RoosterType.Blue));
        layout.Page("roosters_void", p => BuildRoosterPage(p, RoosterAddon.Enums.RoosterType.Void));
        layout.Page("roosters_gold", p => BuildRoosterPage(p, RoosterAddon.Enums.RoosterType.Gold));
    }

    private void BuildRoosterPage(GmcmPageBuilder<RoosterAddonConfig> page, RoosterAddon.Enums.RoosterType type)
    {
        if (!Config.Roosters.TryGetValue(type, out var s))
        {
            s = new RoosterSettings();
            Config.Roosters[type] = s;
        }

        page.Section("availability");
        page.Bool("rooster.buyable", () => s.Buyable, v => s.Buyable = v);
        page.Int("rooster.price", () => s.Price, v => s.Price = v, min: 0, max: 100000, interval: 100);
        page.Text(
            "rooster.required_building",
            () => s.BuildingRequired.ToString(),
            v => s.BuildingRequired = ParseEnum(v, RequiredBuilding.BigCoop),
            allowedValues: Enum.GetNames(typeof(RequiredBuilding)),
            formatAllowedValue: true
        );

        page.Section("behavior");
        page.Bool("rooster.enable_rooster_hatching", () => s.EnableRoosterHatching, v => s.EnableRoosterHatching = v);
        page.Bool("rooster.edit_all_females", () => s.EditAllFemales, v => s.EditAllFemales = v);
        page.Int("rooster.mature_days", () => s.MatureDays, v => s.MatureDays = v, min: 0, max: 999, interval: 1);
        page.Int("rooster.produce_days", () => s.ProduceDays, v => s.ProduceDays = v, min: 1, max: 999, interval: 1);

        page.Section("produce");
        page.Text(
            "rooster.produce_mode",
            () => s.ProduceOption.ToString(),
            v => s.ProduceOption = ParseEnum(v, ProduceMode.CustomizeBelow),
            allowedValues: Enum.GetNames(typeof(ProduceMode)),
            formatAllowedValue: true
        );

        for (var i = 0; i < s.ProduceEntries.Count; i++)
        {
            var idx = i;
            var n = i + 1;

            page.Text($"produce.normal.entry{n}.choice", () => s.ProduceEntries[idx].Choice ?? string.Empty, v => s.ProduceEntries[idx].Choice = v ?? string.Empty);
            page.Text($"produce.normal.entry{n}.custom_name", () => s.ProduceEntries[idx].CustomItemName ?? string.Empty, v => s.ProduceEntries[idx].CustomItemName = v ?? string.Empty);
            page.Int($"produce.normal.entry{n}.probability", () => s.ProduceEntries[idx].Probability, v => s.ProduceEntries[idx].Probability = v, min: 0, max: 100, interval: 1);
        }

        page.Section("deluxe_produce");
        page.Text(
            "rooster.deluxe_produce_mode",
            () => s.DeluxeProduceOption.ToString(),
            v => s.DeluxeProduceOption = ParseEnum(v, DeluxeProduceMode.CustomizeBelow),
            allowedValues: Enum.GetNames(typeof(DeluxeProduceMode)),
            formatAllowedValue: true
        );

        for (var i = 0; i < s.DeluxeProduceEntries.Count; i++)
        {
            var idx = i;
            var n = i + 1;

            page.Text($"produce.deluxe.entry{n}.choice", () => s.DeluxeProduceEntries[idx].Choice ?? string.Empty, v => s.DeluxeProduceEntries[idx].Choice = v ?? string.Empty);
            page.Text($"produce.deluxe.entry{n}.custom_name", () => s.DeluxeProduceEntries[idx].CustomItemName ?? string.Empty, v => s.DeluxeProduceEntries[idx].CustomItemName = v ?? string.Empty);
            page.Int($"produce.deluxe.entry{n}.probability", () => s.DeluxeProduceEntries[idx].Probability, v => s.DeluxeProduceEntries[idx].Probability = v, min: 0, max: 100, interval: 1);
        }

        page.Section("blue_only");
        page.Bool("rooster.bypass_blue_conditions", () => s.BypassBlueConditions, v => s.BypassBlueConditions = v);
        page.Text("rooster.blue_egg_choice", () => s.BlueEggChoice ?? string.Empty, v => s.BlueEggChoice = v ?? string.Empty);
    }

    private static T ParseEnum<T>(string value, T fallback) where T : struct, Enum
        => Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
}

