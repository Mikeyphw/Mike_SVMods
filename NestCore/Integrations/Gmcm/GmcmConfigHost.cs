using System;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Config;
using StardewModdingAPI;

namespace Mikew.NestCore.Integrations.Gmcm;

/// <summary>
/// Thin wrapper that binds a config source to the NestCore GMCM layout DSL.
/// Use from <c>GameLaunched</c>; if GMCM isn't installed, <see cref="TryCreate"/> returns null.
/// </summary>
public sealed class GmcmConfigHost<TConfig>
    where TConfig : class, new()
{
    private readonly GmcmIntegration _gmcm;
    private readonly Func<TConfig> _getConfig;
    private readonly Action _reset;
    private readonly Action _save;
    private readonly Action? _onSaved;

    private GmcmConfigHost(
        GmcmIntegration gmcm,
        Func<TConfig> getConfig,
        Action reset,
        Action save,
        Action? onSaved)
    {
        _gmcm = gmcm ?? throw new ArgumentNullException(nameof(gmcm));
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        _reset = reset ?? throw new ArgumentNullException(nameof(reset));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _onSaved = onSaved;
    }

    /// <summary>
    /// Try to create a host bound to a <see cref="SmapiConfigProvider{TConfig}"/>.
    /// </summary>
    public static GmcmConfigHost<TConfig>? TryCreate(
        IModHelper helper,
        IManifest manifest,
        SmapiConfigProvider<TConfig> configProvider,
        I18TranslationHelper? translation = null,
        Action? onSaved = null,
        string gmcmModId = "spacechase0.GenericModConfigMenu",
        string gmcmOptionsModId = "jltaylor-us.GMCMOptions")
    {
        if (configProvider is null)
            throw new ArgumentNullException(nameof(configProvider));

        return TryCreate(
            helper: helper,
            manifest: manifest,
            getConfig: configProvider.Get,
            reset: () => configProvider.ResetToDefault(save: true),
            save: configProvider.Save,
            translation: translation,
            onSaved: onSaved,
            gmcmModId: gmcmModId,
            gmcmOptionsModId: gmcmOptionsModId
        );
    }

    /// <summary>
    /// Try to create a host bound to explicit get/reset/save delegates.
    /// Useful for mods that manage config persistence outside <see cref="SmapiConfigProvider{TConfig}"/>.
    /// </summary>
    public static GmcmConfigHost<TConfig>? TryCreate(
        IModHelper helper,
        IManifest manifest,
        Func<TConfig> getConfig,
        Action reset,
        Action save,
        I18TranslationHelper? translation = null,
        Action? onSaved = null,
        string gmcmModId = "spacechase0.GenericModConfigMenu",
        string gmcmOptionsModId = "jltaylor-us.GMCMOptions")
    {
        var gmcm = GmcmIntegration.TryCreate(
            helper: helper,
            manifest: manifest,
            translation: translation,
            gmcmModId: gmcmModId,
            gmcmOptionsModId: gmcmOptionsModId
        );

        return gmcm is null
            ? null
            : new GmcmConfigHost<TConfig>(gmcm, getConfig, reset, save, onSaved);
    }

    /// <summary>
    /// (Re)register and rebuild the GMCM layout using the bound config delegates.
    /// </summary>
    public void RebuildLayout(
        Action<GmcmMenuLayout<TConfig>> define,
        string? fieldIdPrefix = null,
        bool titleScreenOnly = false,
        bool force = true,
        bool attachLocaleHandler = true)
    {
        _gmcm.RebuildLayout(
            getConfig: _getConfig,
            reset: _reset,
            save: () =>
            {
                _save();
                _onSaved?.Invoke();
            },
            define: define,
            fieldIdPrefix: fieldIdPrefix,
            titleScreenOnly: titleScreenOnly,
            force: force,
            attachLocaleHandler: attachLocaleHandler
        );
    }
}
