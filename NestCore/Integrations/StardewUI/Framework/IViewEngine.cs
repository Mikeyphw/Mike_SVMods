using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace StardewUI.Framework;

/// <summary>
/// Public API for StardewUI, abstracting away all implementation details of views and trees.
/// </summary>
public interface IViewEngine
{
    /// <summary>
    /// Creates an <see cref="IViewDrawable"/> from the StarML stored in a game asset, as provided by a mod via SMAPI or
    /// Content Patcher.
    /// </summary>
    /// <param name="assetName">The name of the StarML view asset in the content pipeline, e.g. Mods/MyMod/Views/MyView.</param>
    IViewDrawable CreateDrawableFromAsset(string assetName);

    /// <summary>
    /// Creates an <see cref="IViewDrawable"/> from arbitrary markup.
    /// </summary>
    /// <remarks>
    /// Warning: Ad-hoc drawables created this way cannot be cached, nor patched by other mods.
    /// Most mods should not use this API except for testing/experimentation.
    /// </remarks>
    /// <param name="markup">The markup in StarML format.</param>
    IViewDrawable CreateDrawableFromMarkup(string markup);

    /// <summary>
    /// Creates a menu from the StarML stored in a game asset, as provided by a mod via SMAPI or Content Patcher, and
    /// returns a controller for customizing the menu's behavior.
    /// </summary>
    /// <remarks>
    /// The menu is not automatically shown; to show it, set <see cref="Game1.activeClickableMenu"/> or equivalent.
    /// </remarks>
    /// <param name="assetName">The name of the StarML view asset in the content pipeline, e.g. Mods/MyMod/Views/MyView.</param>
    /// <param name="context">
    /// The context, or "model", for the menu's view.
    /// Note: The type must implement <see cref="INotifyPropertyChanged"/> in order for any changes to this data
    /// to be automatically reflected in the UI.
    /// </param>
    IMenuController CreateMenuControllerFromAsset(string assetName, object? context = null);

    /// <summary>
    /// Creates a menu from arbitrary markup and returns a controller for customizing the menu's behavior.
    /// </summary>
    /// <remarks>
    /// Warning: Ad-hoc menus created this way cannot be cached, nor patched by other mods.
    /// Most mods should not use this API except for testing/experimentation.
    /// </remarks>
    /// <param name="markup">The markup in StarML format.</param>
    /// <param name="context">
    /// The context, or "model", for the menu's view.
    /// Note: The type must implement <see cref="INotifyPropertyChanged"/> in order for any changes to this data
    /// to be automatically reflected in the UI.
    /// </param>
    IMenuController CreateMenuControllerFromMarkup(string markup, object? context = null);

    /// <summary>
    /// Creates a menu from the StarML stored in a game asset, as provided by a mod via SMAPI or Content Patcher.
    /// </summary>
    /// <remarks>
    /// Does not make the menu active. To show it, set <see cref="Game1.activeClickableMenu"/> or equivalent.
    /// </remarks>
    /// <param name="assetName">The name of the StarML view asset in the content pipeline, e.g. Mods/MyMod/Views/MyView.</param>
    /// <param name="context">
    /// The context, or "model", for the menu's view.
    /// Note: The type must implement <see cref="INotifyPropertyChanged"/> in order for any changes to this data
    /// to be automatically reflected in the UI.
    /// </param>
    IClickableMenu CreateMenuFromAsset(string assetName, object? context = null);

    /// <summary>
    /// Creates a menu from arbitrary markup.
    /// </summary>
    /// <remarks>
    /// Warning: Ad-hoc menus created this way cannot be cached, nor patched by other mods.
    /// Most mods should not use this API except for testing/experimentation.
    /// </remarks>
    /// <param name="markup">The markup in StarML format.</param>
    /// <param name="context">
    /// The context, or "model", for the menu's view.
    /// Note: The type must implement <see cref="INotifyPropertyChanged"/> in order for any changes to this data
    /// to be automatically reflected in the UI.
    /// </param>
    IClickableMenu CreateMenuFromMarkup(string markup, object? context = null);

    /// <summary>
    /// Starts monitoring this mod's directory for changes to assets managed by any of the <c>Register*</c> methods.
    /// </summary>
    /// <remarks>
    /// If <paramref name="sourceDirectory"/> is specified and points to a directory with the same asset structure as the
    /// deployed mod directory, then an additional sync is set up such that files modified in the source directory while
    /// the game is running will be copied to the active mod directory and subsequently reloaded.
    /// </remarks>
    /// <param name="sourceDirectory">Optional source directory to watch and sync changes from.</param>
    void EnableHotReloading(string? sourceDirectory = null);

    /// <summary>
    /// Begins preloading assets found in this mod's registered asset directories.
    /// </summary>
    /// <remarks>
    /// Must be called after asset registration and must not be called more than once per mod.
    /// </remarks>
    void PreloadAssets();

    /// <summary>
    /// Declares that the specified context types will be used in one or more subsequent APIs and instructs the framework
    /// to begin inspecting those types and optimizing for later use.
    /// </summary>
    /// <param name="types">The types that the mod expects to use as context.</param>
    void PreloadModels(params Type[] types);

    /// <summary>
    /// Registers a mod directory to be searched for special-purpose mod data, i.e. that is not either views or sprites.
    /// </summary>
    /// <param name="assetPrefix">
    /// The prefix for all asset names, excluding the category which is deduced from the file extension.
    /// For example, given a value of Mods/MyMod, a file named foo.buttonspritemap.json would be referenced in views as
    /// @Mods/MyMod/ButtonSpriteMaps/Foo.
    /// </param>
    /// <param name="modDirectory">The physical directory relative to the mod directory.</param>
    void RegisterCustomData(string assetPrefix, string modDirectory);

    /// <summary>
    /// Registers a mod directory to be searched for sprite (and corresponding texture/sprite sheet data) assets.
    /// </summary>
    /// <param name="assetPrefix">The prefix for all asset names, e.g. Mods/MyMod/Sprites.</param>
    /// <param name="modDirectory">The physical directory relative to the mod directory.</param>
    void RegisterSprites(string assetPrefix, string modDirectory);

    /// <summary>
    /// Registers a mod directory to be searched for view (StarML) assets. Uses the <c>.sml</c> extension.
    /// </summary>
    /// <param name="assetPrefix">The prefix for all asset names, e.g. Mods/MyMod/Views.</param>
    /// <param name="modDirectory">The physical directory relative to the mod directory.</param>
    void RegisterViews(string assetPrefix, string modDirectory);
}

/// <summary>
/// Provides methods to update and draw a simple, non-interactive UI component, such as a HUD widget.
/// </summary>
public interface IViewDrawable : IDisposable
{
    /// <summary>The current size required for the content.</summary>
    Vector2 ActualSize { get; }

    /// <summary>
    /// The binding context or "model" for the view.
    /// The type must implement <see cref="INotifyPropertyChanged"/> for automatic UI updates.
    /// </summary>
    object? Context { get; set; }

    /// <summary>
    /// The maximum size allowed for this content. If null, content may use the entire viewport.
    /// </summary>
    Vector2? MaxSize { get; set; }

    /// <summary>Draws the current contents.</summary>
    /// <param name="b">Target sprite batch.</param>
    /// <param name="position">Top-left corner position.</param>
    void Draw(SpriteBatch b, Vector2 position);
}

/// <summary>
/// Wrapper for a mod-managed <see cref="IClickableMenu"/> allowing customization of menu-level behavior.
/// </summary>
public interface IMenuController : IDisposable
{
    /// <summary>Event raised after the menu has been closed.</summary>
    event Action Closed;

    /// <summary>Event raised when the menu is about to close.</summary>
    event Action Closing;

    /// <summary>Gets or sets a function that returns whether or not the menu can be closed.</summary>
    Func<bool>? CanClose { get; set; }

    /// <summary>
    /// Gets or sets an action that replaces the default menu-close behavior.
    /// </summary>
    Action? CloseAction { get; set; }

    /// <summary>Offset from the menu view's top-right edge to draw the close button.</summary>
    Vector2 CloseButtonOffset { get; set; }

    /// <summary>Whether to automatically close the menu on mouse clicks outside bounds.</summary>
    bool CloseOnOutsideClick { get; set; }

    /// <summary>Sound to play when closing the menu.</summary>
    string CloseSound { get; set; }

    /// <summary>How much the menu should dim the underlying screen.</summary>
    float DimmingAmount { get; set; }

    /// <summary>The created menu instance.</summary>
    IClickableMenu Menu { get; }

    /// <summary>
    /// Gets or sets a function that returns the top-left menu position.
    /// </summary>
    Func<Vector2>? PositionSelector { get; set; }

    /// <summary>Removes any cursor attachment previously set by <see cref="SetCursorAttachment"/>.</summary>
    void ClearCursorAttachment();

    /// <summary>Closes the menu following standard rules.</summary>
    void Close();

    /// <summary>
    /// Configures the menu to display a close button on the upper-right side.
    /// </summary>
    void EnableCloseButton(Texture2D? texture = null, Rectangle? sourceRect = null, float scale = 4f);

    /// <summary>Begins displaying a cursor attachment sprite that follows the mouse cursor.</summary>
    void SetCursorAttachment(
        Texture2D texture,
        Rectangle? sourceRect = null,
        Point? size = null,
        Point? offset = null,
        Color? tint = null);

    /// <summary>Configures the menu's gutters (areas of the viewport the menu should not occupy).</summary>
    void SetGutters(int left, int top, int right = -1, int bottom = -1);
}

/// <summary>Extensions for the <see cref="IViewEngine"/> interface.</summary>
internal static class ViewEngineExtensions
{
    /// <summary>
    /// Starts monitoring the mod directory for changes to managed assets and attempts to set up a source sync from the
    /// project directory to the deployed mod directory.
    /// </summary>
    public static void EnableHotReloadingWithSourceSync(
        this IViewEngine viewEngine,
        [CallerFilePath] string? callerFilePath = null)
    {
        viewEngine.EnableHotReloading(FindProjectDirectory(callerFilePath));
    }

    private static string? FindProjectDirectory(string? sourceFilePath)
    {
        if (string.IsNullOrEmpty(sourceFilePath))
            return null;

        for (var dir = Directory.GetParent(sourceFilePath); dir is not null; dir = dir.Parent)
        {
            if (dir.EnumerateFiles("*.csproj").Any())
                return dir.FullName;
        }

        return null;
    }
}
