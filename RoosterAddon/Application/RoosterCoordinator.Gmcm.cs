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

    private IBirdsOfAFeatherApi? _boaf;
    private GmcmIntegration? _gmcm;
    private bool _gmcmRebuildQueued;

    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ModEntry.ContentPatcher = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

        HatchApi = Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh")
            ?? LetMeHatchApiAdapter.Wrap(Helper.ModRegistry.GetApi<object>("Mikew.lmh"));

        _boaf = Helper.ModRegistry.GetApi<IBirdsOfAFeatherApi>("Mikew.birdsofafeather")
            ?? throw new InvalidOperationException("BirdsOfAFeather is required but was not found.");

        if (_boaf.ApiVersion < RequiredBoafApiVersion)
            throw new InvalidOperationException($"BirdsOfAFeather API is too old (have {_boaf.ApiVersion}, need {RequiredBoafApiVersion}+).");

        if (Config.General.EnableDebug)
            GmcmDescriptorSanityChecker.Run(Monitor, Helper.Translation);

        _gmcm = RegisterOrRebuildGmcm(force: true);
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

        RequestGmcmRebuild();
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

        foreach (var page in RoosterGmcmDescriptorRegistry.Pages(() => Config, () => _catalogs, RequestGmcmRebuild))
            layout.Page(page.PageId, b => GmcmLayoutCompiler.Apply(b, page.Content));
    }

    private void RequestGmcmRebuild()
    {
        if (_boaf is null)
            return;

        if (_gmcmRebuildQueued)
            return;

        _gmcmRebuildQueued = true;
        QueueNextTickAction(() =>
        {
            _gmcmRebuildQueued = false;
            _gmcm = RegisterOrRebuildGmcm(force: true);
        });
    }

    private GmcmIntegration RegisterOrRebuildGmcm(bool force)
    {
        if (_boaf is null)
            throw new InvalidOperationException("BirdsOfAFeather API not available.");

        return _boaf.RegisterGmcmMenu(
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
    }
}
