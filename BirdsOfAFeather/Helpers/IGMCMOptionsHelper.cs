using System.Threading;
using Mikew.BirdsOfAFeather.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.BirdsOfAFeather.Framework;
using StardewModdingAPI;
using StardewValley;
using static Mikew.BirdsOfAFeather.Framework.IGMCMOptionsAPI;

namespace Mikew.BirdsOfAFeather.Helpers
{
    public class IGMCMOptionsHelper
    {
        private readonly IGMCMOptionsAPI? ExtraOptionsAPI;
        private readonly IGenericModConfigMenuAPI? ConfigMenuAPI;
        private readonly I18TranslationHelper _translationHelper;
        private readonly IManifest ModManifest;

        public IGMCMOptionsHelper(
            IGMCMOptionsAPI? extraOptionsAPI,
            IGenericModConfigMenuAPI? configMenuAPI,
            I18TranslationHelper translationHelper,
            IManifest manifest
        )
        {
            _translationHelper = translationHelper;
            ExtraOptionsAPI = extraOptionsAPI;
            ConfigMenuAPI = configMenuAPI;
            ModManifest = manifest;
        }

                private Func<string> GetName(string optionName)
        {
            return () => _translationHelper.GetString($"config.{optionName}.name");
        }

                private Func<string> GetPageElement(string optionName, string type = "page")
        {
            return () => _translationHelper.GetString($"config.{type}.{optionName}.name");
        }

                private Func<string> GetText(string optionName, string? type = null)
        {
            return () =>
            {
                var prefix = string.IsNullOrWhiteSpace(type) ? string.Empty : type + ".";
                return _translationHelper.GetString($"config.{prefix}{optionName}.text");
            };
        }

                private Func<string>? GetDescription(string? optionName)
        {
            if (optionName is null)
            {
                return null;
            }

            return () => _translationHelper.GetString($"config.{optionName}.description");
        }
            return () => ($"config.{optionName}.description");
        }

                private Func<string>? GetDescription(string? optionName, string? type = null)
        {
            if (optionName is null)
            {
                return null;
            }

            return () =>
            {
                var prefix = string.IsNullOrWhiteSpace(type) ? string.Empty : type + ".";
                return _translationHelper.GetString($"config.{prefix}{optionName}.description");
            };
        }
            return () => ($"config.{$"{type}." ?? ""}{optionName}.description");
        }

        public void Register(Action reset, Action save, bool titleScreenOnly = false)
        {
            ConfigMenuAPI?.Register(ModManifest, reset, save, titleScreenOnly);
        }

        public void RegisterImage(string texPath, Rectangle? texRect = null, int scale = 4)
        {
            ConfigMenuAPI?.RegisterImage(ModManifest, texPath, texRect, scale);
        }

        public void AddBoolOption(
            Func<bool> getValue,
            Action<bool> setValue,
            string optionName,
            bool tooltip = false,
            string? fieldId = null
        )
        {
            ConfigMenuAPI?.AddBoolOption(
                ModManifest,
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
            string? fieldId = null
        )
        {
            ConfigMenuAPI?.AddBoolOption(
                ModManifest,
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
            string? fieldId = null
        )
        {
            ConfigMenuAPI?.AddNumberOption(
                ModManifest,
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
            string? fieldId = null
        )
        {
            ConfigMenuAPI?.AddNumberOption(
                ModManifest,
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
            string? fieldId = null
        )
        {
            ConfigMenuAPI?.AddNumberOption(
                ModManifest,
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
            string? fieldId = null
        )
        {
            ConfigMenuAPI?.AddNumberOption(
                ModManifest,
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
            string? fieldId = null
        )
        {
            ConfigMenuAPI?.AddTextOption(
                ModManifest,
                getValue,
                setValue,
                name: GetName(optionName),
                tooltip: tooltip ? GetDescription(optionName) : null,
                allowedValues,
                allowedValues != null && formatAllowedValue
                    ? value => _translationHelper.Get($"config.value.{value}")
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
            string? fieldId = null
        )
        {
            ConfigMenuAPI?.AddTextOption(
                ModManifest,
                getValue,
                setValue,
                name: name,
                tooltip: tooltip,
                allowedValues,
                allowedValues != null && formatAllowedValue
                    ? value => _translationHelper.Get($"config.value.{value}")
                    : null,
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
            string? fieldId = null
        )
        {
            ConfigMenuAPI?.AddTextOption(
                ModManifest,
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
            ConfigMenuAPI?.AddSectionTitle(
                ModManifest,
                GetText(sectionName, "section"),
                tooltip ? GetDescription(sectionName, "section") : null
            );
        }

        public void AddSectionTitle(Func<string> text, Func<string>? tooltip = null)
        {
            ConfigMenuAPI?.AddSectionTitle(ModManifest, text, tooltip);
        }

        public void AddParagraph(string paragraphName)
        {
            ConfigMenuAPI?.AddParagraph(ModManifest, GetText(paragraphName, "paragraph"));
        }

        public void AddParagraph(Func<string> text)
        {
            ConfigMenuAPI?.AddParagraph(ModManifest, text);
        }

        public void AddPage(string pageTitle, bool printID = false)
        {
            ConfigMenuAPI?.AddPage(ModManifest, pageTitle, GetPageElement(pageTitle));
            if (printID)
            {
                ConfigMenuAPI?.AddParagraph(ModManifest, () => $"Page ID: {pageTitle}");
            }
        }

        public void AddPage(string pageId, Func<string>? pageTitle = null, bool printID = false)
        {
            ConfigMenuAPI?.AddPage(ModManifest, pageId, pageTitle);
            if (printID)
            {
                ConfigMenuAPI?.AddParagraph(ModManifest, () => $"Page ID: {pageId}");
            }
        }

        public void AddPageLink(string pageName, bool tooltip = false)
        {
            ConfigMenuAPI?.AddPageLink(
                ModManifest,
                pageName,
                GetPageElement(pageName, "pagelink"),
                tooltip ? GetDescription(pageName, "pagelink") : null
            );
        }

        public void AddPageLink(string pageId, Func<string> text, Func<string>? tooltip = null)
        {
            ConfigMenuAPI?.AddPageLink(ModManifest, pageId, text, tooltip);
        }

        public void AddImage(
            Func<Texture2D> texture,
            Rectangle? texturePixelArea = null,
            int scale = Game1.pixelZoom
        )
        {
            ConfigMenuAPI?.AddImage(ModManifest, texture, texturePixelArea, scale);
        }

        public void Unregister(IManifest mod)
        {
            ConfigMenuAPI?.Unregister(mod);
        }

        public void AddEnumOption<TEnum>(
            string optionName,
            Func<TEnum> getValue,
            Action<TEnum> setValue,
            bool tooltip = false
        )
            where TEnum : Enum
        {
            var allowedValues = Enum.GetNames(typeof(TEnum));

            AddTextOption(
                optionName: optionName,
                tooltip: tooltip,
                getValue: () => getValue().ToString(),
                setValue: value => setValue((TEnum)Enum.Parse(typeof(TEnum), value)),
                allowedValues: allowedValues,
                formatAllowedValue: true
            );
        }

        public void AddEnumOption<TEnum>(
            Func<string> name,
            Func<TEnum> getValue,
            Action<TEnum> setValue,
            Func<string>? tooltip = null
        )
            where TEnum : Enum
        {
            var allowedValues = Enum.GetNames(typeof(TEnum));

            AddTextOption(
                name: name,
                tooltip: tooltip,
                getValue: () => getValue().ToString(),
                setValue: value => setValue((TEnum)Enum.Parse(typeof(TEnum), value)),
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
            string? fieldId = null
        )
        {
            ExtraOptionsAPI?.AddColorOption(
                ModManifest,
                getValue,
                setValue,
                name: GetName(optionName),
                tooltip: tooltip ? GetDescription(optionName) : null,
                showAlpha,
                colorPickerStyle,
                fieldId
            );
        }

        public void AddColorOption(
            Func<Color> getValue,
            Action<Color> setValue,
            Func<string> name,
            Func<string>? tooltip = null,
            bool showAlpha = true,
            uint colorPickerStyle = 0,
            string? fieldId = null
        )
        {
            ExtraOptionsAPI?.AddColorOption(
                ModManifest,
                getValue,
                setValue,
                name,
                tooltip,
                showAlpha,
                colorPickerStyle,
                fieldId
            );
        }

        public void AddImageOption(
            Func<uint> getValue,
            Action<uint> setValue,
            string optionName,
            Func<uint> getMaxValue,
            Func<int> maxImageHeight,
            Func<int> maxImageWidth,
            Action<uint, SpriteBatch, Vector2> drawImage,
            bool tooltip = false,
            Func<uint, String?>? label = null,
            Func<uint, String?>? imageTooltipTitle = null,
            Func<uint, String?>? imageTooltipText = null,
            int arrowLocation = (int)ImageOptionArrowLocation.Top,
            int labelLocation = (int)ImageOptionLabelLocation.Top,
            string? fieldId = null
        )
        {
            ExtraOptionsAPI?.AddImageOption(
                ModManifest,
                getValue: getValue,
                setValue: setValue,
                getMaxValue: getMaxValue,
                maxImageHeight: maxImageHeight,
                maxImageWidth: maxImageWidth,
                drawImage: drawImage,
                label: label,
                imageTooltipTitle: imageTooltipTitle,
                imageTooltipText: imageTooltipText,
                arrowLocation: arrowLocation,
                labelLocation: labelLocation,
                fieldId: fieldId,
                name: GetName(optionName),
                tooltip: tooltip ? GetDescription(optionName) : null
            );
        }

        public void AddImageOption(
            Func<uint> getValue,
            Action<uint> setValue,
            string optionName,
            Func<(Func<String?> label, Texture2D sheet, Rectangle? sourceRect)[]> choices,
            bool tooltip = false,
            Func<uint, String?>? imageTooltipTitle = null,
            Func<uint, String?>? imageTooltipText = null,
            int arrowLocation = (int)ImageOptionArrowLocation.Top,
            int labelLocation = (int)ImageOptionLabelLocation.Top,
            string? fieldId = null
        )
        {
            ExtraOptionsAPI?.AddImageOption(
                mod: ModManifest,
                getValue: getValue,
                setValue: setValue,
                choices: choices,
                imageTooltipTitle: imageTooltipTitle,
                imageTooltipText: imageTooltipText,
                arrowLocation: arrowLocation,
                labelLocation: labelLocation,
                fieldId: fieldId,
                name: GetName(optionName),
                tooltip: tooltip ? GetDescription(optionName) : null
            );
        }

        public void AddHorizontalSeparator(
            Func<double>? getWidthFraction = null,
            int height = 3,
            int padAbove = 0,
            int padBelow = 0,
            int alignment = (int)HorizontalAlignment.Center,
            Func<Color>? getColor = null,
            Func<Color>? getShadowColor = null
        )
        {
            ExtraOptionsAPI?.AddHorizontalSeparator(
                ModManifest,
                getWidthFraction,
                height,
                padAbove,
                padBelow,
                alignment,
                getColor,
                getShadowColor
            );
        }

        public void AddSimpleHorizontalSeparator(
            double widthFraction = 0.85,
            int height = 3,
            int padAbove = 0,
            int padBelow = 0,
            int alignment = (int)HorizontalAlignment.Center,
            Color? color = null,
            Color? shadowColor = null
        )
        {
            ExtraOptionsAPI?.AddSimpleHorizontalSeparator(
                ModManifest,
                widthFraction,
                height,
                padAbove,
                padBelow,
                alignment,
                color,
                shadowColor
            );
        }

        public void AddDynamicParagraph(string logName, string text, bool isStyledText)
        {
            ExtraOptionsAPI?.AddDynamicParagraph(
                ModManifest,
                logName,
                GetText(text, "paragraph"),
                isStyledText
            );
        }
    }
}
