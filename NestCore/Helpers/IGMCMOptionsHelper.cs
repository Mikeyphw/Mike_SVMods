using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.NestCore.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using static Mikew.NestCore.Framework.IGMCMOptionsAPI;

namespace Mikew.NestCore.Helpers;

/// <summary>
/// Thin convenience wrapper around GMCM (and GMCM Options) which:
/// - standardizes i18n keys under <c>config.*</c>
/// - returns translated strings (not raw keys)
/// - provides small ergonomic overloads used across mods.
/// </summary>
public sealed class IGMCMOptionsHelper
{
    private readonly IGMCMOptionsAPI? _extraOptionsApi;
    private readonly IGenericModConfigMenuApi? _gmcm;
    private readonly I18TranslationHelper _i18n;
    private readonly IManifest _manifest;

    public IGMCMOptionsHelper(
        IGMCMOptionsAPI? extraOptionsAPI,
        IGenericModConfigMenuApi? configMenuAPI,
        I18TranslationHelper translationHelper,
        IManifest manifest)
    {
        _extraOptionsApi = extraOptionsAPI;
        _gmcm = configMenuAPI;
        _i18n = translationHelper;
        _manifest = manifest;

        Legacy = new GmcmLegacyFacade(_gmcm, _manifest);
    }


    public bool SupportsExtraOptions => _extraOptionsApi is not null;

    /// <summary>Direct access to GMCM's full upstream API surface, if GMCM is installed.</summary>
    public IGenericModConfigMenuApi? Raw => _gmcm;

    /// <summary>
    /// Manifest-bound wrappers for GMCM's obsolete API surface.
    /// Keeps all legacy methods reachable without having to pass <see cref="IManifest"/>.
    /// </summary>
    public GmcmLegacyFacade Legacy { get; }

    /// <summary>Direct access to the raw obsolete GMCM interface (you must pass <see cref="IManifest"/> yourself).</summary>
    public IGenericModConfigMenuApiWithObsoleteMethods? LegacyRaw => _gmcm;


    private static string Key(params string?[] parts)
        => string.Join(".", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    private string TranslateOrFallback(string key, string fallback)
        => _i18n.TryGet(key, out var t) ? t : fallback;

    private string TranslateIfKey(string keyOrText)
        => _i18n.TryGet(keyOrText, out var t) ? t : keyOrText;

    
    private static string StripXmlLikeMarkup(string text)
        => string.IsNullOrWhiteSpace(text) ? string.Empty : Regex.Replace(text, "<[^>]+>", string.Empty);

private Func<string> GetName(string optionName)
        => () => TranslateOrFallback(Key("config", optionName, "name"), optionName);

    private Func<string> GetPageTitle(string pageId)
        => () =>
        {
            var pageKey = Key("config", "page", pageId, "name");
            if (_i18n.TryGet(pageKey, out var pageTitle) && !string.IsNullOrWhiteSpace(pageTitle))
                return pageTitle;

            // Back-compat: older menus used config.pagelink.<id>.name for link labels.
            var linkKey = Key("config", "pagelink", pageId, "name");
            if (_i18n.TryGet(linkKey, out var linkTitle) && !string.IsNullOrWhiteSpace(linkTitle))
                return linkTitle;

            return pageId;
        };

    private Func<string>? GetPageLinkTooltip(string pageId)
    {
        var linkDescKey = Key("config", "pagelink", pageId, "description");
        if (_i18n.TryGet(linkDescKey, out var linkDesc) && !string.IsNullOrWhiteSpace(linkDesc))
            return () => _i18n.GetString(linkDescKey);

        var pageDescKey = Key("config", "page", pageId, "description");
        if (_i18n.TryGet(pageDescKey, out var pageDesc) && !string.IsNullOrWhiteSpace(pageDesc))
            return () => _i18n.GetString(pageDescKey);

        return null;
    }


    private Func<string> GetText(string optionName, string? type = null)
        => () => _i18n.GetString(Key("config", type, optionName, "text"));

    private Func<string>? GetDescription(string? optionName)
        => optionName is null ? null : () => _i18n.GetString(Key("config", optionName, "description"));

    private Func<string>? GetDescription(string? optionName, string? type = null)
        => optionName is null ? null : () => _i18n.GetString(Key("config", type, optionName, "description"));

    public void Register(Action reset, Action save, bool titleScreenOnly = false)
        => _gmcm?.Register(_manifest, reset, save, titleScreenOnly);

    public void RegisterImage(string texPath, Rectangle? texRect = null, int scale = 4)
    {
#pragma warning disable CS0618 // Back-compat wrapper intentionally uses GMCM obsolete API.
        _gmcm?.RegisterImage(_manifest, texPath, texRect, scale);
#pragma warning restore CS0618
    }

    public void AddBoolOption(
        Func<bool> getValue,
        Action<bool> setValue,
        string optionName,
        bool tooltip = false,
        string? fieldId = null)
    {
        _gmcm?.AddBoolOption(
            _manifest,
            getValue,
            setValue,
            name: GetName(optionName),
            tooltip: tooltip ? GetDescription(optionName) : null,
            fieldId
        );
    }

    public void AddBoolOption(
        Func<bool> getValue,
        Action<bool> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string? fieldId = null)
    {
        _gmcm?.AddBoolOption(
            _manifest,
            getValue,
            setValue,
            name: name,
            tooltip: tooltip,
            fieldId
        );
    }

    public void AddFloatNumberOption(
        Func<float> getValue,
        Action<float> setValue,
        string optionName,
        bool tooltip = false,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldId = null)
    {
        _gmcm?.AddNumberOption(
            _manifest,
            getValue,
            setValue,
            name: GetName(optionName),
            tooltip: tooltip ? GetDescription(optionName) : null,
            min,
            max,
            interval,
            formatValue,
            fieldId
        );
    }

    public void AddFloatNumberOption(
        Func<float> getValue,
        Action<float> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldId = null)
    {
        _gmcm?.AddNumberOption(
            _manifest,
            getValue,
            setValue,
            name: name,
            tooltip: tooltip,
            min,
            max,
            interval,
            formatValue,
            fieldId
        );
    }

    public void AddIntNumberOption(
        Func<int> getValue,
        Action<int> setValue,
        string optionName,
        bool tooltip = false,
        int? min = null,
        int? max = null,
        int? interval = null,
        Func<int, string>? formatValue = null,
        string? fieldId = null)
    {
        _gmcm?.AddNumberOption(
            _manifest,
            getValue,
            setValue,
            name: GetName(optionName),
            tooltip: tooltip ? GetDescription(optionName) : null,
            min,
            max,
            interval,
            formatValue,
            fieldId
        );
    }

    public void AddIntNumberOption(
        Func<int> getValue,
        Action<int> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        int? min = null,
        int? max = null,
        int? interval = null,
        Func<int, string>? formatValue = null,
        string? fieldId = null)
    {
        _gmcm?.AddNumberOption(
            _manifest,
            getValue,
            setValue,
            name: name,
            tooltip: tooltip,
            min,
            max,
            interval,
            formatValue,
            fieldId
        );
    }

    public void AddTextOption(
        Func<string> getValue,
        Action<string> setValue,
        string optionName,
        bool tooltip = false,
        string[]? allowedValues = null,
        bool formatAllowedValue = false,
        string? fieldId = null)
    {
        _gmcm?.AddTextOption(
            _manifest,
            getValue,
            setValue,
            name: GetName(optionName),
            tooltip: tooltip ? GetDescription(optionName) : null,
            allowedValues,
            formatAllowedValue
                ? v => TranslateOrFallback(Key("config", optionName, "allowed", v), v)
                : null,
            fieldId
        );
    }

    public void AddTextOption(
        Func<string> getValue,
        Action<string> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string[]? allowedValues = null,
        bool formatAllowedValue = false,
        string? fieldId = null)
    {
        _gmcm?.AddTextOption(
            _manifest,
            getValue,
            setValue,
            name: name,
            tooltip: tooltip,
            allowedValues,
            formatAllowedValue ? TranslateIfKey : null,
            fieldId
        );
    }

    public void AddTextOption(
        Func<string> getValue,
        Action<string> setValue,
        Func<string> name,
        string[]? allowedValues,
        Func<string, string>? formatValue,
        Func<string>? tooltip = null,
        string? fieldId = null)
    {
        _gmcm?.AddTextOption(
            _manifest,
            getValue,
            setValue,
            name: name,
            tooltip: tooltip,
            allowedValues,
            formatValue,
            fieldId
        );
    }

    
    // -----------------------------
    // GMCM advanced / full API surface
    // -----------------------------

    public void AddKeybindOption(
        Func<SButton> getValue,
        Action<SButton> setValue,
        string optionName,
        bool tooltip = false,
        string? fieldId = null)
    {
        _gmcm?.AddKeybind(
            _manifest,
            getValue,
            setValue,
            name: GetName(optionName),
            tooltip: tooltip ? GetDescription(optionName) : null,
            fieldId
        );
    }

    public void AddKeybindOption(
        Func<SButton> getValue,
        Action<SButton> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string? fieldId = null)
        => _gmcm?.AddKeybind(_manifest, getValue, setValue, name, tooltip, fieldId);

    public void AddKeybindListOption(
        Func<KeybindList> getValue,
        Action<KeybindList> setValue,
        string optionName,
        bool tooltip = false,
        string? fieldId = null)
    {
        _gmcm?.AddKeybindList(
            _manifest,
            getValue,
            setValue,
            name: GetName(optionName),
            tooltip: tooltip ? GetDescription(optionName) : null,
            fieldId
        );
    }

    public void AddKeybindListOption(
        Func<KeybindList> getValue,
        Action<KeybindList> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string? fieldId = null)
        => _gmcm?.AddKeybindList(_manifest, getValue, setValue, name, tooltip, fieldId);

    public void AddComplexOption(
        string elementId,
        Func<string> name,
        Action<SpriteBatch, Vector2> draw,
        Func<string>? tooltip = null,
        Action? beforeMenuOpened = null,
        Action? beforeSave = null,
        Action? afterSave = null,
        Action? beforeReset = null,
        Action? afterReset = null,
        Action? beforeMenuClosed = null,
        Func<int>? height = null,
        string? fieldId = null)
    {
        _gmcm?.AddComplexOption(
            _manifest,
            name: name,
            draw: draw,
            tooltip: tooltip,
            beforeMenuOpened: beforeMenuOpened,
            beforeSave: beforeSave,
            afterSave: afterSave,
            beforeReset: beforeReset,
            afterReset: afterReset,
            beforeMenuClosed: beforeMenuClosed,
            height: height,
            fieldId: fieldId ?? elementId
        );
    }

    public void SetTitleScreenOnlyForNextOptions(bool titleScreenOnly)
        => _gmcm?.SetTitleScreenOnlyForNextOptions(_manifest, titleScreenOnly);

    public void OnFieldChanged(Action<string, object> onChange)
        => _gmcm?.OnFieldChanged(_manifest, onChange);

    public void OpenModMenu()
        => _gmcm?.OpenModMenu(_manifest);

    public void OpenModMenuAsChildMenu()
        => _gmcm?.OpenModMenuAsChildMenu(_manifest);

    public bool TryGetCurrentMenu(out IManifest mod, out string page)
    {
        if (_gmcm is null)
        {
            mod = null!;
            page = null!;
            return false;
        }

        return _gmcm.TryGetCurrentMenu(out mod, out page);
    }

public void AddSectionTitle(string sectionName, bool tooltip = false)
    {
        _gmcm?.AddSectionTitle(
            _manifest,
            text: GetText(sectionName, "section"),
            tooltip: tooltip ? GetDescription(sectionName, "section") : null
        );
    }

    public void AddSectionTitle(Func<string> text, Func<string>? tooltip = null)
        => _gmcm?.AddSectionTitle(_manifest, text, tooltip);

    public void AddSubHeader(string subHeaderName)
        => _gmcm?.AddSubHeader(_manifest, GetText(subHeaderName, "subheader"));

    public void AddSubHeader(Func<string> text)
        => _gmcm?.AddSubHeader(_manifest, text);


    public void AddParagraph(string paragraphName)
        => _gmcm?.AddParagraph(_manifest, GetText(paragraphName, "paragraph"));

    public void AddParagraph(Func<string> text)
        => _gmcm?.AddParagraph(_manifest, text);

    public void AddPage(string pageTitle, bool printID = false)
    {
        _gmcm?.AddPage(_manifest, pageTitle, GetPageTitle(pageTitle));
        if (printID)
            _gmcm?.AddParagraph(_manifest, () => string.Format(_i18n.GetString("config.paragraph.page_id.text"), pageTitle));
    }

    public void AddPage(string pageId, Func<string>? pageTitle = null, bool printID = false)
    {
        _gmcm?.AddPage(_manifest, pageId, pageTitle ?? GetPageTitle(pageId));
        if (printID)
            _gmcm?.AddParagraph(_manifest, () => $"Page ID: {pageId}");
    }

    public void AddPageLink(string pageName, bool tooltip = false)
    {
        _gmcm?.AddPageLink(
            _manifest,
            pageName,
            GetPageTitle(pageName),
            tooltip ? GetPageLinkTooltip(pageName) : null
        );
    }

    public void AddPageLink(string pageId, Func<string> text, Func<string>? tooltip = null)
        => _gmcm?.AddPageLink(_manifest, pageId, text, tooltip);

    public void AddImage(Func<Texture2D> texture, Rectangle? texturePixelArea = null, int scale = Game1.pixelZoom)
        => _gmcm?.AddImage(_manifest, texture, texturePixelArea, scale);

    public void AddImageOption(
        Func<uint> getValue,
        Action<uint> setValue,
        Func<string> name,
        Func<(Func<string?> label, Texture2D sheet, Rectangle? sourceRect)[]> choices,
        Func<string>? tooltip = null,
        Func<uint, string?>? imageTooltipTitle = null,
        Func<uint, string?>? imageTooltipText = null,
        int arrowLocation = (int)ImageOptionArrowLocation.Top,
        int labelLocation = (int)ImageOptionLabelLocation.Bottom,
        string? fieldId = null)
    {
        if (_extraOptionsApi is null)
            return;

        _extraOptionsApi.AddImageOption(
            mod: _manifest,
            getValue: getValue,
            setValue: setValue,
            name: name,
            choices: choices,
            tooltip: tooltip,
            imageTooltipTitle: imageTooltipTitle,
            imageTooltipText: imageTooltipText,
            arrowLocation: arrowLocation,
            labelLocation: labelLocation,
            fieldId: fieldId
        );
    }

    public void Unregister(IManifest mod)
        => _gmcm?.Unregister(mod);

    public void AddEnumOption<TEnum>(string optionName, Func<TEnum> getValue, Action<TEnum> setValue, bool tooltip = false)
        where TEnum : Enum
    {
        var allowedValues = Enum.GetNames(typeof(TEnum));

        AddTextOption(
            getValue: () => getValue().ToString(),
            setValue: v => setValue((TEnum)Enum.Parse(typeof(TEnum), v)),
            optionName: optionName,
            tooltip: tooltip,
            allowedValues: allowedValues,
            formatAllowedValue: true
        );
    }

    public void AddEnumOption<TEnum>(Func<string> name, Func<TEnum> getValue, Action<TEnum> setValue, Func<string>? tooltip = null)
        where TEnum : Enum
    {
        var allowedValues = Enum.GetNames(typeof(TEnum));

        AddTextOption(
            getValue: () => getValue().ToString(),
            setValue: v => setValue((TEnum)Enum.Parse(typeof(TEnum), v)),
            name: name,
            tooltip: tooltip,
            allowedValues: allowedValues,
            formatAllowedValue: true
        );
    }

    public void AddColorOption(
        Func<Color> getValue,
        Action<Color> setValue,
        string optionName,
        bool tooltip = false,
        bool showAlpha = true,
        uint colorPickerStyle = 0,
        string? fieldId = null,
        Action<SpriteBatch, int, int, Color>? drawSample = null)
    {
        if (_extraOptionsApi is not null)
        {
            _extraOptionsApi.AddColorOption(
                _manifest,
                getValue,
                setValue,
                name: GetName(optionName),
                tooltip: tooltip ? GetDescription(optionName) : null,
                showAlpha,
                colorPickerStyle,
                fieldId,
                drawSample
            );
            return;
        }

        // Base GMCM API doesn't provide color pickers; no-op when GMCM Options API isn't available.
    }


public void AddColorOption(
        Func<Color> getValue,
        Action<Color> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        bool showAlpha = true,
        uint colorPickerStyle = 0,
        string? fieldId = null,
        Action<SpriteBatch, int, int, Color>? drawSample = null)
    {
        if (_extraOptionsApi is not null)
        {
            _extraOptionsApi.AddColorOption(
                _manifest,
                getValue,
                setValue,
                name,
                tooltip,
                showAlpha,
                colorPickerStyle,
                fieldId,
                drawSample
            );
            return;
        }

        // Base GMCM API doesn't provide color pickers; no-op when GMCM Options API isn't available.
    }



/// <summary>
    /// Create a draw function suitable for the <c>drawSample</c> parameter of <see cref="AddColorOption"/>.
    /// Returns null if GMCM Options isn't installed.
    /// </summary>
    public Action<SpriteBatch, int, int, Color>? MakeColorSwatchDrawer(
        Action<SpriteBatch, int, int>? drawBackground = null,
        Action<SpriteBatch, int, int, Color>? drawForeground = null)
        => _extraOptionsApi?.MakeColorSwatchDrawer(drawBackground, drawForeground);

public void AddImageOption(
        Func<uint> getValue,
        Action<uint> setValue,
        string optionName,
        Func<uint> getMaxValue,
        Func<int> maxImageHeight,
        Func<int> maxImageWidth,
        Action<uint, SpriteBatch, Vector2> drawImage,
        bool tooltip = false,
        Func<uint, string?>? label = null,
        Func<uint, string?>? imageTooltipTitle = null,
        Func<uint, string?>? imageTooltipText = null,
        int arrowLocation = (int)ImageOptionArrowLocation.Top,
        int labelLocation = (int)ImageOptionLabelLocation.Top,
        string? fieldId = null)
    {
        _extraOptionsApi?.AddImageOption(
            _manifest,
            getValue,
            setValue,
            name: GetName(optionName),
            tooltip: tooltip ? GetDescription(optionName) : null,
            getMaxValue: getMaxValue,
            maxImageHeight: maxImageHeight,
            maxImageWidth: maxImageWidth,
            drawImage: drawImage,
            label: label,
            imageTooltipTitle: imageTooltipTitle,
            imageTooltipText: imageTooltipText,
            arrowLocation: arrowLocation,
            labelLocation: labelLocation,
            fieldId: fieldId
        );
    }

    public void AddImageOption(
        Func<uint> getValue,
        Action<uint> setValue,
        Func<string> name,
        Func<string>? tooltip,
        Func<uint> getMaxValue,
        Func<int> maxImageHeight,
        Func<int> maxImageWidth,
        Action<uint, SpriteBatch, Vector2> drawImage,
        Func<uint, string?>? label = null,
        Func<uint, string?>? imageTooltipTitle = null,
        Func<uint, string?>? imageTooltipText = null,
        int arrowLocation = (int)ImageOptionArrowLocation.Top,
        int labelLocation = (int)ImageOptionLabelLocation.Top,
        string? fieldId = null)
    {
        _extraOptionsApi?.AddImageOption(
            _manifest,
            getValue,
            setValue,
            name: name,
            getMaxValue: getMaxValue,
            maxImageHeight: maxImageHeight,
            maxImageWidth: maxImageWidth,
            drawImage: drawImage,
            tooltip: tooltip,
            label: label,
            imageTooltipTitle: imageTooltipTitle,
            imageTooltipText: imageTooltipText,
            arrowLocation: arrowLocation,
            labelLocation: labelLocation,
            fieldId: fieldId
        );
    }

    public void AddHorizontalSeparator(string? fieldId = null)
        => _extraOptionsApi?.AddHorizontalSeparator(_manifest);

    public void AddSimpleHorizontalSeparator(string? fieldId = null)
        => _extraOptionsApi?.AddSimpleHorizontalSeparator(_manifest);
public void AddDynamicParagraph(string logName, string textOrKey, bool isStyledText)
        => AddDynamicParagraph(logName, () => textOrKey, isStyledText);

    public void AddDynamicParagraph(string logName, Func<string> text, bool isStyledText)
    {
        if (_extraOptionsApi is not null)
        {
            _extraOptionsApi.AddDynamicParagraph(_manifest, logName, () => TranslateIfKey(text()), isStyledText);
            return;
        }

        _gmcm?.AddParagraph(
            _manifest,
            () =>
            {
                var raw = TranslateIfKey(text());
                return isStyledText ? StripXmlLikeMarkup(raw) : raw;
            }
        );
    }

    /// <summary>Manifest-bound wrappers for GMCM's obsolete API surface.</summary>
    public sealed class GmcmLegacyFacade
    {
        private readonly IGenericModConfigMenuApiWithObsoleteMethods? _legacy;
        private readonly IManifest _manifest;

        internal GmcmLegacyFacade(IGenericModConfigMenuApiWithObsoleteMethods? legacy, IManifest manifest)
        {
            _legacy = legacy;
            _manifest = manifest;
        }

        [Obsolete("Use IGenericModConfigMenuApi.AddComplexOption with more callback arguments instead.")]
        public void AddComplexOption(Func<string> name, Func<string> tooltip, Action<SpriteBatch, Vector2> draw, Action saveChanges, Func<int>? height = null, string? fieldId = null)
            => _legacy?.AddComplexOption(_manifest, name, tooltip, draw, saveChanges, height, fieldId);

        [Obsolete("Use IGenericModConfigMenuApi.AddComplexOption with more callback arguments instead.")]
        public void AddComplexOption(Func<string> name, Action<SpriteBatch, Vector2> draw, Func<string>? tooltip = null, Action? beforeSave = null, Action? afterSave = null, Action? beforeReset = null, Action? afterReset = null, Func<int>? height = null, string? fieldId = null)
            => _legacy?.AddComplexOption(_manifest, name, draw, tooltip, beforeSave, afterSave, beforeReset, afterReset, height, fieldId);

        [Obsolete("Use IGenericModConfigMenuApi.AddNumberOption with the formatValue callback instead.")]
        public void AddNumberOption(Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, string? fieldId = null)
            => _legacy?.AddNumberOption(_manifest, getValue, setValue, name, tooltip, min, max, interval, fieldId);

        [Obsolete("Use IGenericModConfigMenuApi.AddNumberOption with the formatValue callback instead.")]
        public void AddNumberOption(Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, string? fieldId = null)
            => _legacy?.AddNumberOption(_manifest, getValue, setValue, name, tooltip, min, max, interval, fieldId);

        [Obsolete("Use IGenericModConfigMenuApi.AddTextOption with the formatAllowedValue argument instead.")]
        public void AddTextOption(Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, string? fieldId = null)
            => _legacy?.AddTextOption(_manifest, getValue, setValue, name, tooltip, allowedValues, fieldId);

        [Obsolete("Use IGenericModConfigMenuApi.Register instead.")]
        public void RegisterModConfig(Action revertToDefault, Action saveToFile)
            => _legacy?.RegisterModConfig(_manifest, revertToDefault, saveToFile);

        [Obsolete("Use IGenericModConfigMenuApi.Unregister instead.")]
        public void UnregisterModConfig()
            => _legacy?.UnregisterModConfig(_manifest);

        [Obsolete("Use IGenericModConfigMenuApi.Register or SetTitleScreenOnlyForNextOptions instead.")]
        public void SetDefaultIngameOptinValue(bool optedIn)
            => _legacy?.SetDefaultIngameOptinValue(_manifest, optedIn);

        [Obsolete("Use IGenericModConfigMenuApi.AddPage instead.")]
        public void StartNewPage(string pageName)
            => _legacy?.StartNewPage(_manifest, pageName);

        [Obsolete("Use IGenericModConfigMenuApi.AddPage instead.")]
        public void OverridePageDisplayName(string pageName, string displayName)
            => _legacy?.OverridePageDisplayName(_manifest, pageName, displayName);

        [Obsolete("Use IGenericModConfigMenuApi.AddSectionTitle instead.")]
        public void RegisterLabel(string labelName, string labelDesc)
            => _legacy?.RegisterLabel(_manifest, labelName, labelDesc);

        [Obsolete("Use IGenericModConfigMenuApi.AddParagraph instead.")]
        public void RegisterParagraph(string paragraph)
            => _legacy?.RegisterParagraph(_manifest, paragraph);

        [Obsolete("Use IGenericModConfigMenuApi.AddPageLink instead.")]
        public void RegisterPageLabel(string labelName, string labelDesc, string newPage)
            => _legacy?.RegisterPageLabel(_manifest, labelName, labelDesc, newPage);

        [Obsolete("Use IGenericModConfigMenuApi.AddImage instead.")]
        public void RegisterImage(string texPath, Rectangle? texRect = null, int scale = 4)
            => _legacy?.RegisterImage(_manifest, texPath, texRect, scale);

        [Obsolete("Use IGenericModConfigMenuApi.AddBoolOption instead.")]
        public void RegisterSimpleOption(string optionName, string optionDesc, Func<bool> optionGet, Action<bool> optionSet)
            => _legacy?.RegisterSimpleOption(_manifest, optionName, optionDesc, optionGet, optionSet);

        [Obsolete("Use IGenericModConfigMenuApi.AddNumberOption instead.")]
        public void RegisterSimpleOption(string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet)
            => _legacy?.RegisterSimpleOption(_manifest, optionName, optionDesc, optionGet, optionSet);

        [Obsolete("Use IGenericModConfigMenuApi.AddNumberOption instead.")]
        public void RegisterSimpleOption(string optionName, string optionDesc, Func<float> optionGet, Action<float> optionSet)
            => _legacy?.RegisterSimpleOption(_manifest, optionName, optionDesc, optionGet, optionSet);

        [Obsolete("Use IGenericModConfigMenuApi.AddTextOption instead.")]
        public void RegisterSimpleOption(string optionName, string optionDesc, Func<string> optionGet, Action<string> optionSet)
            => _legacy?.RegisterSimpleOption(_manifest, optionName, optionDesc, optionGet, optionSet);

        [Obsolete("Use IGenericModConfigMenuApi.AddKeybind instead.")]
        public void RegisterSimpleOption(string optionName, string optionDesc, Func<SButton> optionGet, Action<SButton> optionSet)
            => _legacy?.RegisterSimpleOption(_manifest, optionName, optionDesc, optionGet, optionSet);

        [Obsolete("Use IGenericModConfigMenuApi.AddKeybindList instead.")]
        public void RegisterSimpleOption(string optionName, string optionDesc, Func<KeybindList> optionGet, Action<KeybindList> optionSet)
            => _legacy?.RegisterSimpleOption(_manifest, optionName, optionDesc, optionGet, optionSet);

        [Obsolete("Use IGenericModConfigMenuApi.AddNumberOption instead.")]
        public void RegisterClampedOption(string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet, int min, int max)
            => _legacy?.RegisterClampedOption(_manifest, optionName, optionDesc, optionGet, optionSet, min, max);

        [Obsolete("Use IGenericModConfigMenuApi.AddNumberOption instead.")]
        public void RegisterClampedOption(string optionName, string optionDesc, Func<float> optionGet, Action<float> optionSet, float min, float max)
            => _legacy?.RegisterClampedOption(_manifest, optionName, optionDesc, optionGet, optionSet, min, max);

        [Obsolete("Use IGenericModConfigMenuApi.AddNumberOption instead.")]
        public void RegisterClampedOption(string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet, int min, int max, int interval)
            => _legacy?.RegisterClampedOption(_manifest, optionName, optionDesc, optionGet, optionSet, min, max, interval);

        [Obsolete("Use IGenericModConfigMenuApi.AddNumberOption instead.")]
        public void RegisterClampedOption(string optionName, string optionDesc, Func<float> optionGet, Action<float> optionSet, float min, float max, float interval)
            => _legacy?.RegisterClampedOption(_manifest, optionName, optionDesc, optionGet, optionSet, min, max, interval);

        [Obsolete("Use IGenericModConfigMenuApi.AddTextOption instead.")]
        public void RegisterChoiceOption(string optionName, string optionDesc, Func<string> optionGet, Action<string> optionSet, string[] choices)
            => _legacy?.RegisterChoiceOption(_manifest, optionName, optionDesc, optionGet, optionSet, choices);

        [Obsolete("Use IGenericModConfigMenuApi.AddComplexOption instead.")]
        public void RegisterComplexOption(string optionName, string optionDesc, Func<Vector2, object, object> widgetUpdate, Func<SpriteBatch, Vector2, object, object> widgetDraw, Action<object> onSave)
            => _legacy?.RegisterComplexOption(_manifest, optionName, optionDesc, widgetUpdate, widgetDraw, onSave);

        [Obsolete("Use IGenericModConfigMenuApi.OnFieldChanged instead.")]
        public void SubscribeToChange(Action<string, bool> changeHandler)
            => _legacy?.SubscribeToChange(_manifest, changeHandler);

        [Obsolete("Use IGenericModConfigMenuApi.OnFieldChanged instead.")]
        public void SubscribeToChange(Action<string, int> changeHandler)
            => _legacy?.SubscribeToChange(_manifest, changeHandler);

        [Obsolete("Use IGenericModConfigMenuApi.OnFieldChanged instead.")]
        public void SubscribeToChange(Action<string, float> changeHandler)
            => _legacy?.SubscribeToChange(_manifest, changeHandler);

        [Obsolete("Use IGenericModConfigMenuApi.OnFieldChanged instead.")]
        public void SubscribeToChange(Action<string, string> changeHandler)
            => _legacy?.SubscribeToChange(_manifest, changeHandler);
    }

}
