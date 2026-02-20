using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley;

namespace Mikew.BirdsOfAFeather.Integrations.Gmcm;

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

    // -----------------------------
    // Page elements
    // -----------------------------

    public void Section(string sectionName, bool tooltip = false)
        => _menu.AddSectionTitle(sectionName, tooltip);

    public void Paragraph(string paragraphName)

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
                ? "<b>GMCM Options detected.</b> Enhanced widgets enabled."
                : "GMCM Options not installed. Enhanced widgets disabled.",
            styled: true,
            id: "gmcmoptions_status"
        );
    }

        => _menu.AddParagraph(paragraphName);

    public void Link(string pageId, bool tooltip = false)
        => _menu.AddPageLink(pageId, tooltip);

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
    // Internals
    // -----------------------------

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
        var allowedValues = Enum.GetNames(typeof(TEnum));

        _menu.AddTextOption(
            getValue: () => getValue().ToString(),
            setValue: v => setValue((TEnum)Enum.Parse(typeof(TEnum), v, ignoreCase: true)),
            name: name,
            allowedValues: allowedValues,
            formatValue: formatAllowedValue,
            tooltip: tooltip,
            fieldId: fieldId ?? AutoFieldId(elementId)
        );
    }
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
            var setter = CompileSetter(expr.Parameters[0], memberExpr, typeof(T));

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
            MemberExpression memberExpr,
            Type valueType)
        {
            var cfgParam = Expression.Parameter(typeof(TConfig), "cfg");
            var valParam = Expression.Parameter(valueType, "val");

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
