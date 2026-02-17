using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Core.Abstractions;
using LetMeHatchThisEgg.Infrastructure.Integrations;
using LetMeHatchThisEgg.Infrastructure.Logging;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Runtime;

/// <summary>Manual DI composition root for LMH.</summary>
public static class LmheCompositionRoot
{
    /// <summary>Builds the LMH runtime bundle (context + base services).</summary>
    public static LmheRuntime Create(IModHelper helper, IMonitor monitor, IManifest manifest)
    {
        if (helper is null)
            throw new ArgumentNullException(nameof(helper));
        if (monitor is null)
            throw new ArgumentNullException(nameof(monitor));
        if (manifest is null)
            throw new ArgumentNullException(nameof(manifest));

        var config = helper.ReadConfig<ModConfig>();

        var boafLogger = new Logger(monitor, config);
        var i18n = new I18TranslationHelper(helper);
        var core = BoafCoreContextFactory.Create(helper, monitor, manifest, i18n);

        var lmhLogger = new LmheLogger(boafLogger, manifest.Name);
        var assets = new ModAssetHandler(manifest.UniqueID, monitor);

        IConfigProvider<ModConfig> configProvider = new StaticConfigProvider<ModConfig>(config);
        IAssetRegistry assetRegistry = new SmapiAssetRegistry(manifest.UniqueID, helper);
        IIntegrationService integrationService = new SmapiIntegrationService(helper.ModRegistry);

        var integrations = new LmheIntegrations(
            integrationService.ContentPatcher.Api,
            integrationService.WildEggs.Api,
            integrationService.RoostersAddon.Api
        );

        var context = new LmheContext(core, config, lmhLogger, assets, integrations);

        return new LmheRuntime(context, configProvider, lmhLogger, assetRegistry, integrationService);
    }
}
