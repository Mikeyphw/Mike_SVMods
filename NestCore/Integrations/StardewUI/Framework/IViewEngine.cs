// FILE: NestCore/Integrations/StardewUI/Framework/IViewEngine.cs
// This is a small, vendored API surface so NestCore and dependent mods can compile
// without referencing StardewUI's assemblies directly.
//
// Source (for method names/signatures): focustense/StardewUI (MIT)
// https://raw.githubusercontent.com/focustense/StardewUI/master/Framework/IViewEngine.cs

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace StardewUI.Framework;

/// <summary>
/// Public API for StardewUI, abstracting away implementation details of views and trees.
/// </summary>
public interface IViewEngine
{
    IViewDrawable CreateDrawableFromAsset(string assetName);

    IViewDrawable CreateDrawableFromMarkup(string markup);

    IMenuController CreateMenuControllerFromAsset(string assetName, object? context = null);

    IMenuController CreateMenuControllerFromMarkup(string markup, object? context = null);

    IClickableMenu CreateMenuFromAsset(string assetName, object? context = null);

    IClickableMenu CreateMenuFromMarkup(string markup, object? context = null);

    void EnableHotReloading(string? sourceDirectory = null);

    void PreloadAssets();

    void PreloadModels(params Type[] types);

    void RegisterCustomData(string assetPrefix, string modDirectory);

    void RegisterSprites(string assetPrefix, string modDirectory);

    void RegisterViews(string assetPrefix, string modDirectory);
}

/// <summary>
/// Provides methods to update and draw a simple, non-interactive UI component (e.g., a HUD widget).
/// </summary>
public interface IViewDrawable : IDisposable
{
    Vector2 ActualSize { get; }

    object? Context { get; set; }

    Vector2? MaxSize { get; set; }

    void Draw(SpriteBatch b, Vector2 position);
}

/// <summary>
/// Minimal controller wrapper for a mod-managed menu.
/// </summary>
/// <remarks>
/// StardewUI exposes additional controller knobs; NestCore keeps this minimal and consumers that need the
/// full surface should vendor the official StardewUI interface file into their own mod.
/// </remarks>
public interface IMenuController : IDisposable
{
    IClickableMenu Menu { get; }

    void Close();
}
