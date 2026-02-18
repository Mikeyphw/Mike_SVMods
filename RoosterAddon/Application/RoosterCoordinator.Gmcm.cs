using System;
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
    private const int RequiredBoafApiVersion = 1;

    private GmcmIntegration? _gmcm;

    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ModEntry.ContentPatcher = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

        HatchApi = Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh")
            ?? LetMeHatchApiAdapter.Wrap(Helper.ModRegistry.GetApi<object>("Mikew.lmh"));

        var boaf = Helper.ModRegistry.GetApi<IBirdsOfAFeatherApi>("Mikew.birdsofafeather");
        if (boaf is null)
            throw new InvalidOperationException("BirdsOfAFeather is required but was not found.");

        if (boaf.ApiVersion < RequiredBoafApiVersion)
            throw new InvalidOperationException($"BirdsOfAFeather API is too old (have {boaf.ApiVersion}, need {RequiredBoafApiVersion}+).");


        if (Config.General.EnableDebug)
            GmcmDescriptorSanityChecker.Run(Monitor, Helper.Translation);

        _gmcm = boaf.RegisterGmcmMenu(
            helper: Helper,
            manifest: ModManifest,
            getConfig: () => Config,
            reset: ResetConfig,
            save: SaveConfig,
            define: DefineGmcmLayout,
            fieldIdPrefix: ModManifest.UniqueID,
            titleScreenOnly: false,
            force: true,
            attachLocaleHandler: true
        );
    }

    public void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
    {
        // GMCM rebuild is handled by BOAF when attachLocaleHandler=true.
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void ResetConfig()
    {
        Config = RoosterAddonConfig.CreateDefault();

        // Treat a GMCM reset as "new config" for catalog seeding.
        _catalogs = _catalogManager.LoadOrCreateAll(Helper, Monitor);
        var changed = _catalogManager.ApplyToConfig(Config, _catalogs, isNewConfig: true, Config.General.CatalogApplyMode, Monitor);
        if (changed)
            ConfigManager.Save(Helper, Config);

        InvalidateDailyPlanCache("config reset");
        _needsRoosterSetup = true;
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void SaveConfig()
    {
        ConfigManager.Save(Helper, Config);

        InvalidateDailyPlanCache("config saved");
        _needsRoosterSetup = true;
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void DefineGmcmLayout(GmcmMenuLayout<RoosterAddonConfig> layout)
    {
        layout.Root(b => GmcmLayoutCompiler.Apply(b, RoosterGmcmDescriptorRegistry.RootGeneral()));
        layout.Root(b => GmcmLayoutCompiler.Apply(b, RoosterGmcmDescriptorRegistry.RootRoostersLinks()));

        foreach (var page in RoosterGmcmDescriptorRegistry.Pages(() => Config, () => _catalogs))
            layout.Page(page.PageId, b => GmcmLayoutCompiler.Apply(b, page.Content));
    }
}
