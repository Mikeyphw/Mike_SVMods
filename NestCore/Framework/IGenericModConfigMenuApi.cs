#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Mikew.NestCore.Framework;

/// <summary>
/// The API which lets other mods add a config UI through Generic Mod Config Menu.
/// Mirrored from the upstream interface so NestCore can consume the full GMCM surface.
/// </summary>
/// <remarks>
/// Source of truth:
/// https://github.com/spacechase0/StardewValleyMods/blob/develop/framework/GenericModConfigMenu/IGenericModConfigMenuApi.cs
/// </remarks>
public interface IGenericModConfigMenuApi : IGenericModConfigMenuApiWithObsoleteMethods
{
    /****
    ** Must be called first
    ****/

    /// <summary>Register a mod whose config can be edited through the UI.</summary>
    /// <param name="mod">The mod's manifest.</param>
    /// <param name="reset">Reset the mod's config to its default values.</param>
    /// <param name="save">Save the mod's current config to the <c>config.json</c> file.</param>
    /// <param name="titleScreenOnly">Whether the options can only be edited from the title screen.</param>
    /// <remarks>Each mod can only be registered once, unless it's deleted via <see cref="Unregister"/> before calling this again.</remarks>
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

    /****
    ** Basic options
    ****/

    /// <summary>Add a section title at the current position in the form.</summary>
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

    /// <summary>Add a subheader at the current position in the form.</summary>
    /// <remarks>Larger than paragraph, smaller than title.</remarks>
    void AddSubHeader(IManifest mod, Func<string> text);

    /// <summary>Add a paragraph of text at the current position in the form.</summary>
    void AddParagraph(IManifest mod, Func<string> text);

    /// <summary>Add an image at the current position in the form.</summary>
    void AddImage(IManifest mod, Func<Texture2D> texture, Rectangle? texturePixelArea = null, int scale = Game1.pixelZoom);

    /// <summary>Add a boolean option at the current position in the form.</summary>
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

    /// <summary>Add an integer option at the current position in the form.</summary>
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);

    /// <summary>Add a float option at the current position in the form.</summary>
    void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);

    /// <summary>Add a string option at the current position in the form.</summary>
    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);

    /// <summary>Add a keybind at the current position in the form.</summary>
    void AddKeybind(IManifest mod, Func<SButton> getValue, Action<SButton> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

    /// <summary>Add a keybind list at the current position in the form.</summary>
    void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

    /****
    ** Multi-page management
    ****/

    /// <summary>Start a new page in the mod's config UI, or switch to that page if it already exists.</summary>
    void AddPage(IManifest mod, string pageId, Func<string>? pageTitle = null);

    /// <summary>Add a link to a page added via <see cref="AddPage"/> at the current position in the form.</summary>
    void AddPageLink(IManifest mod, string pageId, Func<string> text, Func<string>? tooltip = null);

    /****
    ** Advanced
    ****/

    /// <summary>Add an option at the current position in the form using custom rendering logic.</summary>
    void AddComplexOption(
        IManifest mod,
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
        string? fieldId = null);

    /// <summary>Set whether the options registered after this point can only be edited from the title screen.</summary>
    void SetTitleScreenOnlyForNextOptions(IManifest mod, bool titleScreenOnly);

    /// <summary>Register a method to notify when any option registered by this mod is edited through the config UI.</summary>
    void OnFieldChanged(IManifest mod, Action<string, object> onChange);

    /// <summary>Open the config UI for a specific mod.</summary>
    void OpenModMenu(IManifest mod);

    /// <summary>Open the config UI for a specific mod, as a child menu if there is an existing menu.</summary>
    void OpenModMenuAsChildMenu(IManifest mod);

    /// <summary>Get the currently-displayed mod config menu, if any.</summary>
    bool TryGetCurrentMenu(out IManifest mod, out string page);

    /// <summary>Remove a mod from the config UI and delete all its options and pages.</summary>
    void Unregister(IManifest mod);
}

/// <summary>
/// Back-compat alias used across this repo. Prefer <see cref="IGenericModConfigMenuApi"/> in new code.
/// </summary>
[Obsolete("Use IGenericModConfigMenuApi instead.")]
public interface IGenericModConfigMenuAPI : IGenericModConfigMenuApi
{
}

/// <summary>
/// The upstream obsolete methods surface, mirrored for compatibility.
/// </summary>
/// <remarks>
/// Source of truth:
/// https://github.com/spacechase0/StardewValleyMods/blob/develop/framework/GenericModConfigMenu/Framework/IGenericModConfigMenuApiWithObsoleteMethods.cs
/// </remarks>
public interface IGenericModConfigMenuApiWithObsoleteMethods
{
    // Obsolete since 1.8.0
    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddComplexOption) + " with more callback arguments instead.")]
    void AddComplexOption(
        IManifest mod,
        Func<string> name,
        Func<string> tooltip,
        Action<SpriteBatch, Vector2> draw,
        Action saveChanges,
        Func<int>? height = null,
        string? fieldId = null);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddNumberOption) + " with the formatValue callback instead.")]
    void AddNumberOption(
        IManifest mod,
        Func<int> getValue,
        Action<int> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        int? min = null,
        int? max = null,
        int? interval = null,
        string? fieldId = null);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddNumberOption) + " with the formatValue callback instead.")]
    void AddNumberOption(
        IManifest mod,
        Func<float> getValue,
        Action<float> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        float? min = null,
        float? max = null,
        float? interval = null,
        string? fieldId = null);

    // Obsolete since 1.7.0
    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddComplexOption) + " with more callback arguments instead.")]
    void AddComplexOption(
        IManifest mod,
        Func<string> name,
        Action<SpriteBatch, Vector2> draw,
        Func<string>? tooltip = null,
        Action? beforeSave = null,
        Action? afterSave = null,
        Action? beforeReset = null,
        Action? afterReset = null,
        Func<int>? height = null,
        string? fieldId = null);

    // obsolete since 1.6.0
    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddTextOption) + " with the formatAllowedValue argument instead.")]
    void AddTextOption(
        IManifest mod,
        Func<string> getValue,
        Action<string> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string[]? allowedValues = null,
        string? fieldId = null);

    // obsolete since 1.5.0
    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.Register) + " instead.")]
    void RegisterModConfig(IManifest mod, Action revertToDefault, Action saveToFile);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.Unregister) + " instead.")]
    void UnregisterModConfig(IManifest mod);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.Register) + " or " + nameof(IGenericModConfigMenuApi.SetTitleScreenOnlyForNextOptions) + " instead.")]
    void SetDefaultIngameOptinValue(IManifest mod, bool optedIn);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddPage) + " instead.")]
    void StartNewPage(IManifest mod, string pageName);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddPage) + " instead.")]
    void OverridePageDisplayName(IManifest mod, string pageName, string displayName);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddSectionTitle) + " instead.")]
    void RegisterLabel(IManifest mod, string labelName, string labelDesc);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddParagraph) + " instead.")]
    void RegisterParagraph(IManifest mod, string paragraph);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddPageLink) + " instead.")]
    void RegisterPageLabel(IManifest mod, string labelName, string labelDesc, string newPage);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddImage) + " instead.")]
    void RegisterImage(IManifest mod, string texPath, Rectangle? texRect = null, int scale = 4);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddBoolOption) + " instead.")]
    void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<bool> optionGet, Action<bool> optionSet);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddNumberOption) + " instead.")]
    void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddNumberOption) + " instead.")]
    void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<float> optionGet, Action<float> optionSet);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddTextOption) + " instead.")]
    void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<string> optionGet, Action<string> optionSet);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddKeybind) + " instead.")]
    void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<SButton> optionGet, Action<SButton> optionSet);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddKeybindList) + " instead.")]
    void RegisterSimpleOption(IManifest mod, string optionName, string optionDesc, Func<KeybindList> optionGet, Action<KeybindList> optionSet);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddNumberOption) + " instead.")]
    void RegisterClampedOption(IManifest mod, string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet, int min, int max);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddNumberOption) + " instead.")]
    void RegisterClampedOption(IManifest mod, string optionName, string optionDesc, Func<float> optionGet, Action<float> optionSet, float min, float max);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddNumberOption) + " instead.")]
    void RegisterClampedOption(IManifest mod, string optionName, string optionDesc, Func<int> optionGet, Action<int> optionSet, int min, int max, int interval);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddNumberOption) + " instead.")]
    void RegisterClampedOption(IManifest mod, string optionName, string optionDesc, Func<float> optionGet, Action<float> optionSet, float min, float max, float interval);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddTextOption) + " instead.")]
    void RegisterChoiceOption(IManifest mod, string optionName, string optionDesc, Func<string> optionGet, Action<string> optionSet, string[] choices);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.AddComplexOption) + " instead.")]
    void RegisterComplexOption(
        IManifest mod,
        string optionName,
        string optionDesc,
        Func<Vector2, object, object> widgetUpdate,
        Func<SpriteBatch, Vector2, object, object> widgetDraw,
        Action<object> onSave);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.OnFieldChanged) + " instead.")]
    void SubscribeToChange(IManifest mod, Action<string, bool> changeHandler);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.OnFieldChanged) + " instead.")]
    void SubscribeToChange(IManifest mod, Action<string, int> changeHandler);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.OnFieldChanged) + " instead.")]
    void SubscribeToChange(IManifest mod, Action<string, float> changeHandler);

    [Obsolete("Use " + nameof(IGenericModConfigMenuApi.OnFieldChanged) + " instead.")]
    void SubscribeToChange(IManifest mod, Action<string, string> changeHandler);
}
