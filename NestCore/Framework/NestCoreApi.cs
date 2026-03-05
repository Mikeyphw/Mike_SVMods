using System;
using Mikew.NestCore.Integrations.Gmcm;
using Mikew.NestCore.Integrations.StardewUI;
using Mikew.NestCore.Integrations.StardewUI.Dsl;
using StardewModdingAPI;

namespace Mikew.NestCore.Framework;

public sealed class NestCoreApi : INestCoreApi
{
    public int ApiVersion => 2;

    public GmcmIntegration? TryCreateGmcmIntegration(IModHelper helper, IManifest manifest)
        => GmcmIntegration.TryCreate(helper, manifest);

    public GmcmIntegration? RegisterGmcmMenu<TConfig>(
        IModHelper helper,
        IManifest manifest,
        Func<TConfig> getConfig,
        Action reset,
        Action save,
        Action<GmcmMenuLayout<TConfig>> define,
        string? fieldIdPrefix = null,
        bool titleScreenOnly = false,
        bool force = true,
        bool attachLocaleHandler = true)
    {
        var gmcm = GmcmIntegration.TryCreate(helper, manifest);
        if (gmcm is null)
            return null;

        gmcm.RebuildLayout(
            getConfig: getConfig,
            reset: reset,
            save: save,
            define: define,
            fieldIdPrefix: fieldIdPrefix ?? manifest.UniqueID,
            titleScreenOnly: titleScreenOnly,
            force: force,
            attachLocaleHandler: attachLocaleHandler
        );

        return gmcm;
    }

public StardewUiIntegration? TryCreateStardewUiIntegration(IModHelper helper, IManifest manifest)
        => StardewUiIntegration.TryCreate(helper, manifest);

    public StardewUiHost? TryCreateStardewUiHost(IModHelper helper, IManifest manifest)
        => StardewUiHost.TryCreate(helper, manifest);

}
