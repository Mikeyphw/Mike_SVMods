using System;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.NestCore.Integrations.Gmcm;

/// <summary>
/// Thin wrapper that binds a config source to the NestCore GMCM layout DSL.
/// Use from <c>GameLaunched</c>; if GMCM isn't installed, <see cref="TryCreate"/> returns null.
/// </summary>
public sealed class GmcmConfigHost<TConfig>
    where TConfig : class, new()
{
    private readonly IModHelper _helper;
    private readonly GmcmIntegration _gmcm;
    private readonly Func<TConfig> _getConfig;
    private readonly Action _reset;
    private readonly Action _save;
    private readonly Action? _onSaved;

    private LayoutSpec? _lastLayout;
    private bool _autoRebuildSubscribed;
    private bool _rebuildScheduled;

    private bool _rebuildOnSaveLoaded = true;
    private bool _rebuildOnReturnedToTitle = true;
    private Func<bool>? _saveLoadedReady;
    private int _saveLoadedMaxTicks = 1;

    private sealed record LayoutSpec(
        Action<GmcmMenuLayout<TConfig>> Define,
        string? FieldIdPrefix,
        bool TitleScreenOnly,
        bool Force,
        bool AttachLocaleHandler);

    private GmcmConfigHost(
        IModHelper helper,
        GmcmIntegration gmcm,
        Func<TConfig> getConfig,
        Action reset,
        Action save,
        Action? onSaved)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
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
            : new GmcmConfigHost<TConfig>(helper, gmcm, getConfig, reset, save, onSaved);
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

    /// <summary>
    /// Enable automatic next-tick GMCM rebuilds when the save is loaded and/or when returning to title.
    /// Uses the most recent layout provided to <see cref="RebuildLayoutSaveAware"/> or <see cref="RebuildLayoutSaveAware"/>.
    /// </summary>
    /// <remarks>
    /// Why: some menus build dynamic options based on save data which isn't always safe to query inside SMAPI event handlers.
    /// </remarks>
    public void EnableAutoRebuildOnSaveEvents(
        bool rebuildOnSaveLoaded = true,
        bool rebuildOnReturnedToTitle = true,
        Func<bool>? saveLoadedReady = null,
        int saveLoadedMaxTicks = 1)
    {
        _rebuildOnSaveLoaded = rebuildOnSaveLoaded;
        _rebuildOnReturnedToTitle = rebuildOnReturnedToTitle;
        _saveLoadedReady = saveLoadedReady;
        _saveLoadedMaxTicks = Math.Max(1, saveLoadedMaxTicks);

        if (_autoRebuildSubscribed)
            return;

        _helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        _helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        _autoRebuildSubscribed = true;
    }

    /// <summary>
    /// Rebuild the menu now, remember the layout definition, and (optionally) auto-rebuild on save load / return to title.
    /// </summary>
    public void RebuildLayoutSaveAware(
        Action<GmcmMenuLayout<TConfig>> define,
        string? fieldIdPrefix = null,
        bool titleScreenOnly = false,
        bool force = true,
        bool attachLocaleHandler = true,
        bool rebuildOnSaveLoaded = true,
        bool rebuildOnReturnedToTitle = true,
        Func<bool>? saveLoadedReady = null,
        int saveLoadedMaxTicks = 1)
    {
        if (define is null)
            throw new ArgumentNullException(nameof(define));

        _lastLayout = new LayoutSpec(
            Define: define,
            FieldIdPrefix: fieldIdPrefix,
            TitleScreenOnly: titleScreenOnly,
            Force: force,
            AttachLocaleHandler: attachLocaleHandler
        );

        RebuildLayout(
            define: define,
            fieldIdPrefix: fieldIdPrefix,
            titleScreenOnly: titleScreenOnly,
            force: force,
            attachLocaleHandler: attachLocaleHandler
        );

        EnableAutoRebuildOnSaveEvents(
            rebuildOnSaveLoaded: rebuildOnSaveLoaded,
            rebuildOnReturnedToTitle: rebuildOnReturnedToTitle,
            saveLoadedReady: saveLoadedReady,
            saveLoadedMaxTicks: saveLoadedMaxTicks
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!_rebuildOnSaveLoaded)
            return;

        ScheduleRebuild(ready: _saveLoadedReady, maxTicks: _saveLoadedMaxTicks);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        if (!_rebuildOnReturnedToTitle)
            return;

        ScheduleRebuild(ready: null, maxTicks: 1);
    }

    private void ScheduleRebuild(Func<bool>? ready, int maxTicks)
    {
        if (_rebuildScheduled)
            return;

        if (_lastLayout is null)
            return;

        _rebuildScheduled = true;

        var tries = 0;
        EventHandler<UpdateTickedEventArgs>? onTick = null;
        onTick = (_, __) =>
        {
            tries++;
            if (ready is not null && !ready() && tries < maxTicks)
                return;

            _helper.Events.GameLoop.UpdateTicked -= onTick;
            _rebuildScheduled = false;

            var spec = _lastLayout;
            if (spec is null)
                return;

            RebuildLayout(
                define: spec.Define,
                fieldIdPrefix: spec.FieldIdPrefix,
                titleScreenOnly: spec.TitleScreenOnly,
                force: true,
                attachLocaleHandler: spec.AttachLocaleHandler
            );
        };

        _helper.Events.GameLoop.UpdateTicked += onTick;
    }
}
