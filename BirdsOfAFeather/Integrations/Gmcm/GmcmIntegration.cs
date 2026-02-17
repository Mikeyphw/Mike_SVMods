using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather.Integrations.Gmcm;

/// <summary>
/// Facade over Generic Mod Config Menu (GMCM) and optional GMCMOptions:
/// - safe API discovery;
/// - central register/unregister/rebuild;
/// - optional auto-refresh on locale change (clears cached translations).
/// </summary>
public sealed class GmcmIntegration
{
    private readonly IModHelper _helper;
    private readonly IManifest _manifest;
    private readonly I18TranslationHelper _i18n;

    private bool _registered;
    private bool _localeHandlerAttached;

    private Action? _lastReset;
    private Action? _lastSave;
    private Action<IGMCMOptionsHelper>? _lastBuild;
    private bool _lastTitleScreenOnly;

    /// <summary>The unified menu helper (GMCM + optional GMCMOptions).</summary>
    public IGMCMOptionsHelper Menu { get; }

    /// <summary>Stable field-id prefix for this mod.</summary>
    public string FieldIdPrefix => _manifest.UniqueID;

    /// <summary>Whether GMCM is installed and API was found.</summary>
    public bool IsAvailable { get; }

    private GmcmIntegration(
        IModHelper helper,
        IManifest manifest,
        I18TranslationHelper i18n,
        IGenericModConfigMenuAPI gmcmApi,
        IGMCMOptionsAPI? gmcmOptionsApi)
    {
        _helper = helper;
        _manifest = manifest;
        _i18n = i18n;

        IsAvailable = true;
        Menu = new IGMCMOptionsHelper(
            extraOptionsAPI: gmcmOptionsApi,
            configMenuAPI: gmcmApi,
            translationHelper: i18n,
            manifest: manifest
        );
    }

    /// <summary>
    /// Try to create a GMCM integration for this mod. Returns null if GMCM isn't installed.
    /// </summary>
    public static GmcmIntegration? TryCreate(
        IModHelper helper,
        IManifest manifest,
        I18TranslationHelper? translation = null,
        string gmcmModId = "spacechase0.GenericModConfigMenu",
        string gmcmOptionsModId = "jltaylor-us.GMCMOptions")
    {
        var gmcm = helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>(gmcmModId);
        if (gmcm is null)
            return null;

        var extra = helper.ModRegistry.GetApi<IGMCMOptionsAPI>(gmcmOptionsModId);
        var i18n = translation ?? new I18TranslationHelper(helper);

        return new GmcmIntegration(helper, manifest, i18n, gmcm, extra);
    }

    /// <summary>
    /// (Re)register the config menu and rebuild all options.
    /// </summary>
    public void Rebuild(
        Action reset,
        Action save,
        Action<IGMCMOptionsHelper> build,
        bool titleScreenOnly = false,
        bool force = true,
        bool attachLocaleHandler = true)
    {
        if (!IsAvailable)
            return;

        _lastReset = reset;
        _lastSave = save;
        _lastBuild = build;
        _lastTitleScreenOnly = titleScreenOnly;

        if (_registered && force)
            Menu.Unregister(_manifest);

        if (!_registered || force)
        {
            Menu.Register(reset, save, titleScreenOnly);
            _registered = true;
        }

        build(Menu);

        if (attachLocaleHandler)
            AttachLocaleChangedHandler();
    }

    /// <summary>
    /// Subscribe once to locale changes to clear cached translations + rebuild the menu.
    /// </summary>
    public void AttachLocaleChangedHandler()
    {
        if (_localeHandlerAttached)
            return;

        _helper.Events.GameLoop.LocaleChanged += OnLocaleChanged;
        _localeHandlerAttached = true;
    }

    private void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
    {
        _i18n.ClearCache();

        if (_lastReset is null || _lastSave is null || _lastBuild is null)
            return;

        Rebuild(
            reset: _lastReset,
            save: _lastSave,
            build: _lastBuild,
            titleScreenOnly: _lastTitleScreenOnly,
            force: true,
            attachLocaleHandler: true
        );
    }
}
