using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Mikew.NestCore.Integrations.Gmcm;

/// <summary>
/// Fluent helpers for adding options to the current GMCM page.
/// Includes expression-based bindings, plus auto-generated stable <c>fieldId</c>s.
/// </summary>
public sealed class GmcmPageBuilder<TConfig>
{
    private readonly IGMCMOptionsHelper _menu;
    private readonly Func<TConfig> _getConfig;
    private readonly string _fieldIdPrefix;

    private string _pageId = "root";
    private int _autoIdCounter;

    internal GmcmPageBuilder(IGMCMOptionsHelper menu, Func<TConfig> getConfig, string fieldIdPrefix)
    {
        _menu = menu;
        _getConfig = getConfig;
        _fieldIdPrefix = fieldIdPrefix;
    }

    internal void SetCurrentPage(string pageId)
    {
        _pageId = string.IsNullOrWhiteSpace(pageId) ? "root" : pageId;
        _autoIdCounter = 0;
    }

    public bool SupportsExtraOptions => _menu.SupportsExtraOptions;

    /// <summary>Direct access to GMCM's full upstream API surface (if installed).</summary>
    public IGenericModConfigMenuApi? Raw => _menu.Raw;

    /// <summary>Manifest-bound wrappers for GMCM's obsolete method surface.</summary>
    public IGMCMOptionsHelper.GmcmLegacyFacade Legacy => _menu.Legacy;


    // -----------------------------
    // Page elements
    // -----------------------------

    public void Section(string sectionName, bool tooltip = false)
        => _menu.AddSectionTitle(sectionName, tooltip);

    public void SubHeader(string subHeaderName)
        => _menu.AddSubHeader(subHeaderName);

    public void SubHeaderLabel(Func<string> text)
        => _menu.AddSubHeader(text);


    public void SectionLabel(Func<string> text, Func<string>? tooltip = null)
        => _menu.AddSectionTitle(text, tooltip);

    public void Paragraph(string paragraphName)
        => _menu.AddParagraph(paragraphName);

    public void ParagraphDynamic(Func<string> text, bool styled = false, string? id = null)
    {
        var seed = id ?? $"para_{++_autoIdCounter}";
        var logName = $"{_fieldIdPrefix}.{_pageId}.{GmcmFieldIds.Normalize(seed)}";
        _menu.AddDynamicParagraph(logName, text, isStyledText: styled);
    }

    public void ParagraphGmcmOptionsStatus()
    {
        ParagraphDynamic(
            text: () => _menu.SupportsExtraOptions
                ? "config.paragraph.gmcmoptions_status.enabled.text"
                : "config.paragraph.gmcmoptions_status.disabled.text",
            styled: true,
            id: "gmcmoptions_status"
        );
    }

    public void ImagePicker(
        string elementId,
        Func<string> name,
        Func<string>? tooltip,
        Func<uint> getValue,
        Action<uint> setValue,
        Func<(Func<string?> label, Texture2D sheet, Rectangle? sourceRect)[]> choices,
        Func<uint, string?>? imageTooltipTitle = null,
        Func<uint, string?>? imageTooltipText = null,
        int arrowLocation = (int)IGMCMOptionsAPI.ImageOptionArrowLocation.Top,
        int labelLocation = (int)IGMCMOptionsAPI.ImageOptionLabelLocation.Bottom,
        string? fieldId = null)
    {
        if (!_menu.SupportsExtraOptions)
            return;

        _menu.AddImageOption(
            getValue: getValue,
            setValue: setValue,
            name: name,
            choices: choices,
            tooltip: tooltip,
            imageTooltipTitle: imageTooltipTitle,
            imageTooltipText: imageTooltipText,
            arrowLocation: arrowLocation,
            labelLocation: labelLocation,
            fieldId: fieldId ?? AutoFieldId(elementId)
        );
    }

    public void Link(string pageId, bool tooltip = false)
        => _menu.AddPageLink(pageId, tooltip);

    public void Links(IEnumerable<string> pageIds, bool tooltip = false)
    {
        foreach (var pageId in pageIds)
            Link(pageId, tooltip);
    }

    public void Links(bool tooltip = false, params string[] pageIds)
        => Links((IEnumerable<string>)pageIds, tooltip);


/// <summary>Add a page link with a custom label (label can be translated via i18n keys).</summary>
public void LinkLabel(string pageId, Func<string> text, Func<string>? tooltip = null)
    => _menu.AddPageLink(pageId, text, tooltip);


    /// <summary>
    /// Add a horizontal separator. Prefer the overload with an explicit <paramref name="id"/>
    /// if you need it to be stable across layout changes.
    /// </summary>
    public void Separator(bool simple = false, string? fieldId = null)
        => Separator(id: $"sep_{++_autoIdCounter}", simple: simple, fieldId: fieldId);

    /// <summary>
    /// Add a horizontal separator with a stable id seed.
    /// </summary>
    public void Separator(string id, bool simple = false, string? fieldId = null)
    {
        if (simple)
            _menu.AddSimpleHorizontalSeparator(fieldId);
        else
            _menu.AddHorizontalSeparator(fieldId);
    }

    public void Image(Func<Texture2D> texture, Rectangle? texturePixelArea = null, int scale = Game1.pixelZoom)
        => _menu.AddImage(texture, texturePixelArea, scale);

    public void DynamicParagraph(string logName, string textKeyOrText, bool isStyledText = false)
        => _menu.AddDynamicParagraph(logName, textKeyOrText, isStyledText);

    // -----------------------------
    // Bound options (expression-based)
    // -----------------------------

    public void Bool(Expression<Func<TConfig, bool>> member, string? optionId = null, bool tooltip = false, string? fieldId = null)
        => Bind(member, optionId, (get, set, id) => _menu.AddBoolOption(get, set, id, tooltip, fieldId ?? AutoFieldId(id)));

    public void Int(
        Expression<Func<TConfig, int>> member,
        string? optionId = null,
        bool tooltip = false,
        int? min = null,
        int? max = null,
        int? interval = null,
        Func<int, string>? formatValue = null,
        string? fieldId = null)
        => Bind(member, optionId, (get, set, id) => _menu.AddIntNumberOption(get, set, id, tooltip, min, max, interval, formatValue, fieldId ?? AutoFieldId(id)));

    public void Float(
        Expression<Func<TConfig, float>> member,
        string? optionId = null,
        bool tooltip = false,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldId = null)
        => Bind(member, optionId, (get, set, id) => _menu.AddFloatNumberOption(get, set, id, tooltip, min, max, interval, formatValue, fieldId ?? AutoFieldId(id)));

    public void Text(
        Expression<Func<TConfig, string>> member,
        string? optionId = null,
        bool tooltip = false,
        string[]? allowedValues = null,
        bool formatAllowedValue = false,
        string? fieldId = null)
        => Bind(member, optionId, (get, set, id) => _menu.AddTextOption(get, set, id, tooltip, allowedValues, formatAllowedValue, fieldId ?? AutoFieldId(id)));

    public void Keybind(Expression<Func<TConfig, SButton>> member, string? optionId = null, bool tooltip = false, string? fieldId = null)
        => Bind(member, optionId, (get, set, id) => _menu.AddKeybindOption(get, set, id, tooltip, fieldId ?? AutoFieldId(id)));

    public void KeybindList(Expression<Func<TConfig, KeybindList>> member, string? optionId = null, bool tooltip = false, string? fieldId = null)
        => Bind(member, optionId, (get, set, id) => _menu.AddKeybindListOption(get, set, id, tooltip, fieldId ?? AutoFieldId(id)));


    public void Enum<TEnum>(Expression<Func<TConfig, TEnum>> member, string? optionId = null, bool tooltip = false)
        where TEnum : struct, Enum
        => Bind(member, optionId, (get, set, id) => _menu.AddEnumOption(id, get, set, tooltip));

    public void Color(
        Expression<Func<TConfig, Color>> member,
        string? optionId = null,
        bool tooltip = false,
        bool showAlpha = true,
        uint colorPickerStyle = 0,
        string? fieldId = null,
        Action<SpriteBatch, int, int, Color>? drawSample = null)
        => Bind(member, optionId, (get, set, id) => _menu.AddColorOption(get, set, id, tooltip, showAlpha, colorPickerStyle, fieldId ?? AutoFieldId(id), drawSample));

    public void ColorSwatch(
        Expression<Func<TConfig, Color>> member,
        string? optionId = null,
        bool tooltip = false,
        bool showAlpha = true,
        uint colorPickerStyle = 0,
        string? fieldId = null,
        Action<SpriteBatch, int, int, Color>? drawSample = null)
        => Color(
            member,
            optionId: optionId,
            tooltip: tooltip,
            showAlpha: showAlpha,
            colorPickerStyle: colorPickerStyle,
            fieldId: fieldId,
            drawSample: drawSample ?? _menu.MakeColorSwatchDrawer()
        );


    // -----------------------------
    // Unbound options (manual get/set, still auto fieldIds by default)
    // -----------------------------

    public void Bool(string optionId, Func<bool> getValue, Action<bool> setValue, bool tooltip = false, string? fieldId = null)
        => _menu.AddBoolOption(getValue, setValue, optionId, tooltip, fieldId ?? AutoFieldId(optionId));

    public void Int(
        string optionId,
        Func<int> getValue,
        Action<int> setValue,
        bool tooltip = false,
        int? min = null,
        int? max = null,
        int? interval = null,
        Func<int, string>? formatValue = null,
        string? fieldId = null)
        => _menu.AddIntNumberOption(getValue, setValue, optionId, tooltip, min, max, interval, formatValue, fieldId ?? AutoFieldId(optionId));

    public void Float(
        string optionId,
        Func<float> getValue,
        Action<float> setValue,
        bool tooltip = false,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldId = null)
        => _menu.AddFloatNumberOption(getValue, setValue, optionId, tooltip, min, max, interval, formatValue, fieldId ?? AutoFieldId(optionId));

    public void Text(
        string optionId,
        Func<string> getValue,
        Action<string> setValue,
        bool tooltip = false,
        string[]? allowedValues = null,
        bool formatAllowedValue = false,
        string? fieldId = null)
        => _menu.AddTextOption(getValue, setValue, optionId, tooltip, allowedValues, formatAllowedValue, fieldId ?? AutoFieldId(optionId));

    public void Keybind(string optionId, Func<SButton> getValue, Action<SButton> setValue, bool tooltip = false, string? fieldId = null)
        => _menu.AddKeybindOption(getValue, setValue, optionId, tooltip, fieldId ?? AutoFieldId(optionId));

    public void KeybindList(string optionId, Func<KeybindList> getValue, Action<KeybindList> setValue, bool tooltip = false, string? fieldId = null)
        => _menu.AddKeybindListOption(getValue, setValue, optionId, tooltip, fieldId ?? AutoFieldId(optionId));


    public void Color(
        string optionId,
        Func<Color> getValue,
        Action<Color> setValue,
        bool tooltip = false,
        bool showAlpha = true,
        uint colorPickerStyle = 0,
        string? fieldId = null,
        Action<SpriteBatch, int, int, Color>? drawSample = null)
        => _menu.AddColorOption(getValue, setValue, optionId, tooltip, showAlpha, colorPickerStyle, fieldId ?? AutoFieldId(optionId), drawSample);

    public void ImageOption(
        string optionId,
        Func<uint> getValue,
        Action<uint> setValue,
        Func<uint> getMaxValue,
        Func<int> maxImageHeight,
        Func<int> maxImageWidth,
        Action<uint, SpriteBatch, Vector2> drawImage,
        bool tooltip = false,
        Func<uint, string?>? label = null,
        Func<uint, string?>? imageTooltipTitle = null,
        Func<uint, string?>? imageTooltipText = null,
        int arrowLocation = 0,
        int labelLocation = 0,
        string? fieldId = null)
        => _menu.AddImageOption(getValue, setValue, optionId, getMaxValue, maxImageHeight, maxImageWidth, drawImage,
            tooltip, label, imageTooltipTitle, imageTooltipText, arrowLocation, labelLocation, fieldId ?? AutoFieldId(optionId));


    // -----------------------------
    // Advanced
    // -----------------------------

    public void Complex(
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
        => _menu.AddComplexOption(
            elementId: elementId,
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
            fieldId: fieldId ?? AutoFieldId(elementId)
        );

    public void TitleScreenOnlyForNextOptions(bool titleScreenOnly)
        => _menu.SetTitleScreenOnlyForNextOptions(titleScreenOnly);

    public void OnFieldChanged(Action<string, object> onChange)
        => _menu.OnFieldChanged(onChange);

    public void OpenModMenu()
        => _menu.OpenModMenu();

    public void OpenModMenuAsChildMenu()
        => _menu.OpenModMenuAsChildMenu();

    public bool TryGetCurrentMenu(out IManifest mod, out string page)
        => _menu.TryGetCurrentMenu(out mod, out page);

    // -----------------------------
    // Internals
    // -----------------------------

    public string FieldId(string elementId)
        => AutoFieldId(elementId);

    private string AutoFieldId(string elementId)
        => GmcmFieldIds.Build(_fieldIdPrefix, _pageId, elementId);


    // -----------------------------
    // Unbound options with custom label/tooltip (dynamic/dictionary editors)
    // -----------------------------

    public void BoolLabel(
        string elementId,
        Func<string> name,
        Func<string>? tooltip,
        Func<bool> getValue,
        Action<bool> setValue,
        string? fieldId = null)
        => _menu.AddBoolOption(getValue, setValue, name, tooltip, fieldId ?? AutoFieldId(elementId));

    public void IntLabel(
        string elementId,
        Func<string> name,
        Func<string>? tooltip,
        Func<int> getValue,
        Action<int> setValue,
        int? min = null,
        int? max = null,
        int? interval = null,
        Func<int, string>? formatValue = null,
        string? fieldId = null)
        => _menu.AddIntNumberOption(getValue, setValue, name, tooltip, min, max, interval, formatValue, fieldId ?? AutoFieldId(elementId));

    public void FloatLabel(
        string elementId,
        Func<string> name,
        Func<string>? tooltip,
        Func<float> getValue,
        Action<float> setValue,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldId = null)
        => _menu.AddFloatNumberOption(getValue, setValue, name, tooltip, min, max, interval, formatValue, fieldId ?? AutoFieldId(elementId));

    public void TextLabel(
        string elementId,
        Func<string> name,
        Func<string>? tooltip,
        Func<string> getValue,
        Action<string> setValue,
        string[]? allowedValues = null,
        bool formatAllowedValue = false,
        string? fieldId = null)
        => _menu.AddTextOption(getValue, setValue, name, tooltip, allowedValues, formatAllowedValue, fieldId ?? AutoFieldId(elementId));

    public void TextLabel(
        string elementId,
        Func<string> name,
        Func<string>? tooltip,
        Func<string> getValue,
        Action<string> setValue,
        string[]? allowedValues,
        Func<string, string>? formatValue,
        string? fieldId = null)
        => _menu.AddTextOption(
            getValue: getValue,
            setValue: setValue,
            name: name,
            allowedValues: allowedValues,
            formatValue: formatValue ?? (v => v),
            tooltip: tooltip,
            fieldId: fieldId ?? AutoFieldId(elementId)
        );

    public void EnumLabel<TEnum>(
        string elementId,
        Func<string> name,
        Func<TEnum> getValue,
        Action<TEnum> setValue,
        Func<string>? tooltip = null,
        string? fieldId = null)
        where TEnum : struct, Enum
        => EnumLabel(
            elementId: elementId,
            name: name,
            getValue: getValue,
            setValue: setValue,
            formatAllowedValue: null,
            tooltip: tooltip,
            fieldId: fieldId
        );

    public void EnumLabel<TEnum>(
        string elementId,
        Func<string> name,
        Func<TEnum> getValue,
        Action<TEnum> setValue,
        Func<string, string>? formatAllowedValue,
        Func<string>? tooltip = null,
        string? fieldId = null)
        where TEnum : struct, Enum
    {
        var allowedValues = System.Enum.GetNames(typeof(TEnum));

        _menu.AddTextOption(
            getValue: () => getValue().ToString(),
            setValue: v => setValue((TEnum)System.Enum.Parse(typeof(TEnum), v, ignoreCase: true)),
            name: name,
            allowedValues: allowedValues,
            formatValue: formatAllowedValue,
            tooltip: tooltip,
            fieldId: fieldId ?? AutoFieldId(elementId)
        );
    }
    public void ColorLabel(
        string elementId,
        Func<string> name,
        Func<string>? tooltip,
        Func<Color> getValue,
        Action<Color> setValue,
        bool showAlpha = true,
        uint colorPickerStyle = 0,
        string? fieldId = null,
        Action<SpriteBatch, int, int, Color>? drawSample = null)
        => _menu.AddColorOption(getValue, setValue, name, tooltip, showAlpha, colorPickerStyle, fieldId ?? AutoFieldId(elementId), drawSample);

    public void ColorSwatchLabel(
        string elementId,
        Func<string> name,
        Func<string>? tooltip,
        Func<Color> getValue,
        Action<Color> setValue,
        bool showAlpha = true,
        uint colorPickerStyle = 0,
        string? fieldId = null,
        Action<SpriteBatch, int, int, Color>? drawSample = null)
        => ColorLabel(
            elementId: elementId,
            name: name,
            tooltip: tooltip,
            getValue: getValue,
            setValue: setValue,
            showAlpha: showAlpha,
            colorPickerStyle: colorPickerStyle,
            fieldId: fieldId,
            drawSample: drawSample ?? _menu.MakeColorSwatchDrawer()
        );

    private void Bind<T>(
        Expression<Func<TConfig, T>> member,
        string? optionId,
        Action<Func<T>, Action<T>, string> addOption)
    {
        var binding = ExpressionBinding.Create(member);

        var id = optionId ?? binding.MemberPath;

        Func<T> get = () => binding.Getter(_getConfig());
        Action<T> set = v => binding.Setter(_getConfig(), v);

        addOption(get, set, id);
    }

    private sealed record MemberBinding<T>(Func<TConfig, T> Getter, Action<TConfig, T> Setter, string MemberPath);

    private static class ExpressionBinding
    {
        public static MemberBinding<T> Create<T>(Expression<Func<TConfig, T>> expr)
        {
            var body = StripConvert(expr.Body);

            if (body is not MemberExpression memberExpr)
                throw new ArgumentException("Expression must be a member access (e.g. cfg => cfg.Foo.Bar).", nameof(expr));

            var memberPath = GetMemberPath(memberExpr);

            var getter = expr.Compile();
            var setter = CompileSetter<T>(expr.Parameters[0], memberExpr);

            return new MemberBinding<T>(getter, setter, memberPath);
        }

        private static Expression StripConvert(Expression e)
        {
            while (e is UnaryExpression u && (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked))
                e = u.Operand;
            return e;
        }

        private static string GetMemberPath(MemberExpression memberExpr)
        {
            var parts = new Stack<string>();
            Expression? cur = memberExpr;

            while (cur is MemberExpression m)
            {
                parts.Push(m.Member.Name);
                cur = StripConvert(m.Expression!);
            }

            return string.Join(".", parts);
        }

        private static Action<TConfig, T> CompileSetter<T>(
            ParameterExpression originalParam,
            MemberExpression memberExpr)
        {
            var cfgParam = Expression.Parameter(typeof(TConfig), "cfg");
            var valParam = Expression.Parameter(typeof(T), "val");

            var replaced = new ReplaceParameterVisitor(originalParam, cfgParam).Visit(memberExpr)!;
            replaced = StripConvert(replaced);

            if (replaced is not MemberExpression left)
                throw new ArgumentException("Expression must resolve to a settable member.", nameof(memberExpr));

            var assign = Expression.Assign(left, valParam);
            var lambda = Expression.Lambda<Action<TConfig, T>>(assign, cfgParam, valParam);

            return lambda.Compile();
        }

        private sealed class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;

            public ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _from ? _to : base.VisitParameter(node);
        }
    }
}
