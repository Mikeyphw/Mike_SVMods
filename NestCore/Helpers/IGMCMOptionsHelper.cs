using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.NestCore.Framework;
using StardewModdingAPI;
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
    private readonly IGenericModConfigMenuAPI? _gmcm;
    private readonly I18TranslationHelper _i18n;
    private readonly IManifest _manifest;

    public IGMCMOptionsHelper(
        IGMCMOptionsAPI? extraOptionsAPI,
        IGenericModConfigMenuAPI? configMenuAPI,
        I18TranslationHelper translationHelper,
        IManifest manifest)
    {
        _extraOptionsApi = extraOptionsAPI;
        _gmcm = configMenuAPI;
        _i18n = translationHelper;
        _manifest = manifest;
    }


    public bool SupportsExtraOptions => _extraOptionsApi is not null;

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
        => _gmcm?.RegisterImage(_manifest, texPath, texRect, scale);

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
}
