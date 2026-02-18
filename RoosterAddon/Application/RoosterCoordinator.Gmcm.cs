using System;
using System.Linq;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using RoosterAddon.Application.Gmcm;
using RoosterAddon.Application.Gmcm.Descriptors;
using RoosterAddon.Framework.Config;
using RoosterAddon.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;

using ILetMeHatchApi = Mikew.BirdsOfAFeather.Framework.ILetMeHatchApi;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private const int BoafApiMinVersion = 1;

    private GmcmIntegration? _gmcmIntegration;

    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ModEntry.ContentPatcher = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

        HatchApi = Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh")
            ?? LetMeHatchApiAdapter.Wrap(Helper.ModRegistry.GetApi<object>("Mikew.lmh"));

        RegisterGmcm(force: true);
    }

    public void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
    {
        // BOAF can rebuild GMCM on locale change when registered with attachLocaleHandler=true.
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void RegisterGmcm(bool force)
    {
        var api = Helper.ModRegistry.GetApi<IBirdsOfAFeatherApi>("Mikew.birdsofafeather");
        if (api is null || api.ApiVersion < BoafApiMinVersion)
        {
            throw new InvalidOperationException(
                "BirdsOfAFeather is required but the public API is missing or too old. "
                + "Please install/update BirdsOfAFeather."
            );
        }

        _gmcmIntegration = api.RegisterGmcmMenu(
            helper: Helper,
            manifest: ModManifest,
            getConfig: () => Config,
            reset: ResetConfig,
            save: SaveConfig,
            define: DefineGmcmLayout,
            fieldIdPrefix: ModManifest.UniqueID,
            titleScreenOnly: false,
            force: force,
            attachLocaleHandler: true
        );

        _gmcmRegistered = _gmcmIntegration is not null;
    }

    private void ResetConfig()
    {
        Config = RoosterAddonConfig.CreateDefault();

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
        layout.Root(page =>
        {
            // Page 1: Root -> General (descriptor-driven)
            // Page 2: Root -> Roosters (page links) (descriptor-driven)
            GmcmLayoutCompiler.Apply(
                page,
                RoosterGmcmDescriptorRegistry.RootGeneral(),
                RoosterGmcmDescriptorRegistry.RootRoostersLinks()
            );
        });

        layout.Page(RoosterGmcmPageIds.RoostersWhite, page =>
        {
            GmcmLayoutCompiler.Apply(
                page,
                RoosterGmcmDescriptorRegistry.RoostersWhite(() => GetRoosterSettings(RoosterAddon.Enums.RoosterType.White))
            );
        });
        layout.Page(RoosterGmcmPageIds.RoostersBrown, page =>
        {
            GmcmLayoutCompiler.Apply(
                page,
                RoosterGmcmDescriptorRegistry.RoostersBrown(() => GetRoosterSettings(RoosterAddon.Enums.RoosterType.Brown))
            );
        });
        layout.Page(RoosterGmcmPageIds.RoostersBlue, page =>
        {
            GmcmLayoutCompiler.Apply(
                page,
                RoosterGmcmDescriptorRegistry.RoostersBlue(() => GetRoosterSettings(RoosterAddon.Enums.RoosterType.Blue))
            );
        });
        layout.Page(RoosterGmcmPageIds.RoostersVoid, page => BuildRoosterPage(page, RoosterAddon.Enums.RoosterType.Void));
        layout.Page(RoosterGmcmPageIds.RoostersGold, page => BuildRoosterPage(page, RoosterAddon.Enums.RoosterType.Gold));
    }

    private RoosterSettings GetRoosterSettings(RoosterAddon.Enums.RoosterType type)
    {
        if (!Config.Roosters.TryGetValue(type, out var s))
        {
            s = new RoosterSettings();
            Config.Roosters[type] = s;
        }

        EnsureEntryCount(s.ProduceEntries, 4);
        EnsureEntryCount(s.DeluxeProduceEntries, 4);

        return s;
    }

    private static void EnsureEntryCount(System.Collections.Generic.List<ProduceEntry> entries, int count)
    {
        while (entries.Count < count)
            entries.Add(new ProduceEntry());
    }

    private void BuildRoosterPage(GmcmPageBuilder<RoosterAddonConfig> page, RoosterAddon.Enums.RoosterType type)
    {
        page.Section("rooster.availability");
        page.Bool(
            optionId: "rooster.buyable",
            getValue: () => GetRoosterSettings(type).Buyable,
            setValue: v => GetRoosterSettings(type).Buyable = v,
            tooltip: true
        );
        page.Int(
            optionId: "rooster.price",
            getValue: () => GetRoosterSettings(type).Price,
            setValue: v => GetRoosterSettings(type).Price = ClampInt(v, 0, 100000),
            tooltip: true,
            min: 0,
            max: 100000,
            interval: 100
        );
        page.Text(
            optionId: "rooster.buildingRequired",
            getValue: () => GetRoosterSettings(type).BuildingRequired.ToString(),
            setValue: v => GetRoosterSettings(type).BuildingRequired = ParseEnum(v, RequiredBuilding.BigCoop),
            tooltip: true,
            allowedValues: Enum.GetNames(typeof(RequiredBuilding)),
            formatAllowedValue: true
        );

        page.Section("rooster.behavior");
        page.Bool(
            optionId: "rooster.enableRoosterHatching",
            getValue: () => GetRoosterSettings(type).EnableRoosterHatching,
            setValue: v => GetRoosterSettings(type).EnableRoosterHatching = v,
            tooltip: true
        );
        page.Bool(
            optionId: "rooster.editAllFemales",
            getValue: () => GetRoosterSettings(type).EditAllFemales,
            setValue: v => GetRoosterSettings(type).EditAllFemales = v,
            tooltip: true
        );
        page.Int(
            optionId: "rooster.matureDays",
            getValue: () => GetRoosterSettings(type).MatureDays,
            setValue: v => GetRoosterSettings(type).MatureDays = ClampInt(v, 0, 999),
            tooltip: true,
            min: 0,
            max: 999,
            interval: 1
        );
        page.Int(
            optionId: "rooster.produceDays",
            getValue: () => GetRoosterSettings(type).ProduceDays,
            setValue: v => GetRoosterSettings(type).ProduceDays = ClampInt(v, 1, 999),
            tooltip: true,
            min: 1,
            max: 999,
            interval: 1
        );

        page.Section("rooster.produce");
        page.Text(
            optionId: "rooster.produceMode",
            getValue: () => GetRoosterSettings(type).ProduceOption.ToString(),
            setValue: v => GetRoosterSettings(type).ProduceOption = ParseEnum(v, ProduceMode.CustomizeBelow),
            tooltip: true,
            allowedValues: Enum.GetNames(typeof(ProduceMode)),
            formatAllowedValue: true
        );

        for (var i = 0; i < 4; i++)
        {
            var idx = i;
            var n = idx + 1;

            page.Text(
                optionId: $"rooster.produce.entry{n}.choice",
                getValue: () => GetRoosterSettings(type).ProduceEntries[idx].Choice ?? string.Empty,
                setValue: v => GetRoosterSettings(type).ProduceEntries[idx].Choice = v ?? string.Empty,
                tooltip: true
            );
            page.Text(
                optionId: $"rooster.produce.entry{n}.customName",
                getValue: () => GetRoosterSettings(type).ProduceEntries[idx].CustomItemName ?? "Custom",
                setValue: v => GetRoosterSettings(type).ProduceEntries[idx].CustomItemName = v ?? "Custom",
                tooltip: true
            );
            page.Int(
                optionId: $"rooster.produce.entry{n}.probability",
                getValue: () => GetRoosterSettings(type).ProduceEntries[idx].Probability,
                setValue: v => GetRoosterSettings(type).ProduceEntries[idx].Probability = ClampInt(v, 0, 100),
                tooltip: true,
                min: 0,
                max: 100,
                interval: 1
            );
        }

        page.Section("rooster.deluxe");
        page.Text(
            optionId: "rooster.deluxeMode",
            getValue: () => GetRoosterSettings(type).DeluxeProduceOption.ToString(),
            setValue: v => GetRoosterSettings(type).DeluxeProduceOption = ParseEnum(v, DeluxeProduceMode.CustomizeBelow),
            tooltip: true,
            allowedValues: Enum.GetNames(typeof(DeluxeProduceMode)),
            formatAllowedValue: true
        );

        for (var i = 0; i < 4; i++)
        {
            var idx = i;
            var n = idx + 1;

            page.Text(
                optionId: $"rooster.deluxe.entry{n}.choice",
                getValue: () => GetRoosterSettings(type).DeluxeProduceEntries[idx].Choice ?? string.Empty,
                setValue: v => GetRoosterSettings(type).DeluxeProduceEntries[idx].Choice = v ?? string.Empty,
                tooltip: true
            );
            page.Text(
                optionId: $"rooster.deluxe.entry{n}.customName",
                getValue: () => GetRoosterSettings(type).DeluxeProduceEntries[idx].CustomItemName ?? "Custom",
                setValue: v => GetRoosterSettings(type).DeluxeProduceEntries[idx].CustomItemName = v ?? "Custom",
                tooltip: true
            );
            page.Int(
                optionId: $"rooster.deluxe.entry{n}.probability",
                getValue: () => GetRoosterSettings(type).DeluxeProduceEntries[idx].Probability,
                setValue: v => GetRoosterSettings(type).DeluxeProduceEntries[idx].Probability = ClampInt(v, 0, 100),
                tooltip: true,
                min: 0,
                max: 100,
                interval: 1
            );
        }

        if (type == RoosterAddon.Enums.RoosterType.Blue)
        {
            page.Section("rooster.blue");
            page.Bool(
                optionId: "rooster.bypassBlueConditions",
                getValue: () => GetRoosterSettings(type).BypassBlueConditions,
                setValue: v => GetRoosterSettings(type).BypassBlueConditions = v,
                tooltip: true
            );
            page.Text(
                optionId: "rooster.blueEggChoice",
                getValue: () => GetRoosterSettings(type).BlueEggChoice ?? string.Empty,
                setValue: v => GetRoosterSettings(type).BlueEggChoice = v ?? string.Empty,
                tooltip: true
            );
        }
    }

    private static int ClampInt(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
    private static float ClampFloat(float value, float min, float max) => Math.Max(min, Math.Min(max, value));

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;
}
