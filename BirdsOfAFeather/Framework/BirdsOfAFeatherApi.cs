using System;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Framework;

public sealed class BirdsOfAFeatherApi : IBirdsOfAFeatherApi
{
    public int ApiVersion => 1;

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

}
