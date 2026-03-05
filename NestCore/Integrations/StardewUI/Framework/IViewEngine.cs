// FILE: NestCore/Integrations/StardewUI/Framework/IViewEngine.cs
// This is a vendored API surface from StardewUI (focustense) so NestCore and dependent mods can compile
// without referencing StardewUI's assemblies directly.
//
// Source: focustense/StardewUI (MIT)
// https://raw.githubusercontent.com/focustense/StardewUI/refs/heads/dev/Framework/IViewEngine.cs

using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace StardewUI.Framework;

/// <summary>
/// Public API for StardewUI, abstracting away implementation details of views and trees.
/// </summary>
public interface IViewEngine
{
    /// <summary>
    /// Creates a drawable from a registered view asset.
    /// </summary>
    IViewDrawable CreateDrawableFromAsset(string assetName);

    /// <summary>
    /// Creates a drawable from inline StarML markup.
    /// </summary>
    IViewDrawable CreateDrawableFromMarkup(string markup);

    /// <summary>
    /// Creates a menu controller from a registered view asset.
    /// </summary>
    IMenuController CreateMenuControllerFromAsset(string assetName, object? context = null);

    /// <summary>
    /// Creates a menu controller from inline StarML markup.
    /// </summary>
    IMenuController CreateMenuControllerFromMarkup(string markup, object? context = null);

    /// <summary>
    /// Creates a clickable menu from a registered view asset.
    /// </summary>
    IClickableMenu CreateMenuFromAsset(string assetName, object? context = null);

    /// <summary>
    /// Creates a clickable menu from inline StarML markup.
    /// </summary>
    IClickableMenu CreateMenuFromMarkup(string markup, object? context = null);

    /// <summary>
    /// Enables hot reloading of views. Call once on mod initialization.
    /// </summary>
    /// <param name="sourceDirectory">Optional source directory to watch for changes. If null, only monitors compiled assets.</param>
    void EnableHotReloading(string? sourceDirectory = null);

    /// <summary>
    /// Begins preloading assets in the background. Must not be called more than once.
    /// </summary>
    void PreloadAssets();

    /// <summary>
    /// Preloads reflection metadata for model types used as binding contexts.
    /// </summary>
    /// <param name="types">Model types to preload.</param>
    void PreloadModels(params Type[] types);

    /// <summary>
    /// Registers a directory containing custom data JSON files.
    /// </summary>
    /// <param name="assetPrefix">The asset name prefix (e.g., Mods/MyMod).</param>
    /// <param name="modDirectory">Path to the mod's directory containing the data files.</param>
    void RegisterCustomData(string assetPrefix, string modDirectory);

    /// <summary>
    /// Registers a directory containing sprite images.
    /// </summary>
    /// <param name="assetPrefix">The asset name prefix (e.g., Mods/MyMod/Sprites).</param>
    /// <param name="modDirectory">Path to the mod's directory containing sprite files.</param>
    void RegisterSprites(string assetPrefix, string modDirectory);

    /// <summary>
    /// Registers a directory containing StarML view files.
    /// </summary>
    /// <param name="assetPrefix">The asset name prefix (e.g., Mods/MyMod/Views).</param>
    /// <param name="modDirectory">Path to the mod's directory containing view files.</param>
    void RegisterViews(string assetPrefix, string modDirectory);
}

/// <summary>
/// Provides methods to update and draw a simple, non-interactive UI component (e.g., a HUD widget).
/// </summary>
public interface IViewDrawable : IDisposable
{
    /// <summary>
    /// Gets the actual rendered size of this drawable after layout has been calculated.
    /// </summary>
    Vector2 ActualSize { get; }

    /// <summary>
    /// Gets or sets the binding context for data binding expressions.
    /// </summary>
    object? Context { get; set; }

    /// <summary>
    /// Gets or sets the maximum size constraint for this drawable.
    /// </summary>
    Vector2? MaxSize { get; set; }

    /// <summary>
    /// Draws this component to the screen.
    /// </summary>
    /// <param name="b">The sprite batch to draw to.</param>
    /// <param name="position">The position to draw at.</param>
    void Draw(SpriteBatch b, Vector2 position);
}

/// <summary>
/// Controller for a mod-managed menu with lifecycle management and UI customization.
/// </summary>
public interface IMenuController : IDisposable
{
    /// <summary>
    /// Raised when the menu has been closed and disposed.
    /// </summary>
    event Action? Closed;

    /// <summary>
    /// Raised when the menu is about to close.
    /// </summary>
    event Action? Closing;

    /// <summary>
    /// Gets whether the menu can be closed by the player (e.g., via ESC key or close button).
    /// </summary>
    bool CanClose { get; }

    /// <summary>
    /// Gets or sets the action to invoke when the menu is closed.
    /// </summary>
    Action? CloseAction { get; set; }

    /// <summary>
    /// Gets or sets the offset for the close button position.
    /// </summary>
    Vector2 CloseButtonOffset { get; set; }

    /// <summary>
    /// Gets or sets whether clicking outside the menu should close it.
    /// </summary>
    bool CloseOnOutsideClick { get; set; }

    /// <summary>
    /// Gets or sets the sound to play when the menu closes.
    /// </summary>
    string? CloseSound { get; set; }

    /// <summary>
    /// Gets or sets the dimming amount (0.0 to 1.0) for the background overlay.
    /// </summary>
    float DimmingAmount { get; set; }

    /// <summary>
    /// Gets the underlying clickable menu instance.
    /// </summary>
    IClickableMenu Menu { get; }

    /// <summary>
    /// Gets or sets the position selector for the menu.
    /// </summary>
    Func<Vector2>? PositionSelector { get; set; }

    /// <summary>
    /// Clears any cursor attachment set by the menu.
    /// </summary>
    void ClearCursorAttachment();

    /// <summary>
    /// Closes the menu.
    /// </summary>
    void Close();

    /// <summary>
    /// Enables the default close button on the menu.
    /// </summary>
    void EnableCloseButton();

    /// <summary>
    /// Attaches the cursor to a specific element within the menu.
    /// </summary>
    /// <param name="componentId">The ID of the component to attach to.</param>
    void SetCursorAttachment(string componentId);

    /// <summary>
    /// Sets the gutters (padding) around the menu content.
    /// </summary>
    /// <param name="left">Left gutter width.</param>
    /// <param name="top">Top gutter height.</param>
    /// <param name="right">Right gutter width.</param>
    /// <param name="bottom">Bottom gutter height.</param>
    void SetGutters(int left, int top, int right, int bottom);
}

/// <summary>
/// Extension methods for IViewEngine.
/// </summary>
public static class ViewEngineExtensions
{
    /// <summary>
    /// Enables hot reloading with automatic source directory detection.
    /// </summary>
    /// <remarks>
    /// This is a convenience method that searches up from the caller's file path
    /// to find a project directory (.csproj file) to use as the source directory.
    /// </remarks>
    /// <param name="engine">The view engine instance.</param>
    /// <param name="callerFilePath">The file path of the caller (automatically supplied by compiler).</param>
    public static void EnableHotReloadingWithSourceSync(
        this IViewEngine engine,
        [CallerFilePath] string? callerFilePath = null)
    {
        if (engine is null)
            throw new ArgumentNullException(nameof(engine));

        if (string.IsNullOrWhiteSpace(callerFilePath))
        {
            // No source directory available, enable basic hot reload
            engine.EnableHotReloading(null);
            return;
        }

        // Search up for project directory
        var directory = Path.GetDirectoryName(callerFilePath);
        while (directory is not null)
        {
            if (Directory.EnumerateFiles(directory, "*.csproj").Any())
            {
                engine.EnableHotReloading(directory);
                return;
            }

            var parent = Directory.GetParent(directory);
            if (parent is null)
                break;
            directory = parent.FullName;
        }

        // Fallback to basic hot reload if no project found
        engine.EnableHotReloading(null);
    }
}

