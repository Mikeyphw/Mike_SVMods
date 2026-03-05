// FILE: NestCore/Integrations/StardewUI/Dsl/StardewUiHost.cs
// Main entry point for StardewUI DSL - provides GMCM-like experience for StardewUI.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley.Menus;

namespace Mikew.NestCore.Integrations.StardewUI.Dsl;

/// <summary>
/// Host for StardewUI DSL providing GMCM-like experience.
/// </summary>
public sealed class StardewUiHost
{
    private readonly StardewUiIntegration _integration;
    private readonly StardewUiLayout _layout = new();
    private bool _assetsPreloaded;

    private StardewUiHost(StardewUiIntegration integration)
    {
        _integration = integration ?? throw new ArgumentNullException(nameof(integration));
    }

    /// <summary>
    /// The underlying StardewUI integration.
    /// </summary>
    public StardewUiIntegration Integration => _integration;

    /// <summary>
    /// Tries to create a StardewUI host for the given mod.
    /// Returns null if StardewUI isn't installed.
    /// </summary>
    public static StardewUiHost? TryCreate(
        IModHelper helper,
        IManifest manifest,
        string stardewUiModId = StardewUiIntegration.DefaultStardewUiModId)
    {
        var integration = StardewUiIntegration.TryCreate(helper, manifest, stardewUiModId);
        return integration is null ? null : new StardewUiHost(integration);
    }

    /// <summary>
    /// Registers assets and optionally preloads them.
    /// </summary>
    /// <param name="options">Asset registration options.</param>
    /// <param name="preloadAssetsOnce">Whether to preload assets after registration.</param>
    /// <param name="preloadModels">Model types to preload for reflection.</param>
    public void RegisterAssets(StardewUiAssetsOptions? options = null, bool preloadAssetsOnce = false, params Type[] preloadModels)
    {
        _integration.RegisterAssets(options);

        if (preloadAssetsOnce && !_assetsPreloaded)
        {
            _integration.PreloadAssetsOnce();
            _assetsPreloaded = true;
        }

        if (preloadModels.Length > 0)
        {
            _integration.PreloadModels(preloadModels);
        }
    }

    /// <summary>
    /// Rebuilds the layout with the given definition.
    /// </summary>
    /// <param name="define">Action to define the layout.</param>
    /// <param name="assets">Optional asset options.</param>
    /// <param name="preloadAssetsOnce">Whether to preload assets.</param>
    /// <param name="preloadModels">Models to preload.</param>
    public void RebuildLayout(
        Action<StardewUiLayout> define,
        StardewUiAssetsOptions? assets = null,
        bool preloadAssetsOnce = false,
        params Type[] preloadModels)
    {
        // Register assets first
        RegisterAssets(assets, preloadAssetsOnce, preloadModels);

        // Build the layout definitions
        define(_layout);
    }

    /// <summary>
    /// Creates a menu from a registered view.
    /// </summary>
    /// <param name="menuId">The menu ID as defined in the layout.</param>
    /// <returns>The created menu, or null if not found.</returns>
    public IClickableMenu? CreateMenu(string menuId)
    {
        if (!_layout.TryGetMenu(menuId, out var builderAction))
            return null;
        if (builderAction is null)
            return null;

        var builder = new StardewUiMenuBuilder();
        builderAction(builder);

        if (builder.ViewName is null)
            throw new InvalidOperationException($"Menu '{menuId}' must have a view specified.");

        var menu = _integration.CreateMenu(builder.ViewName, builder.ContextProvider?.Invoke());

        // Apply controller configuration if needed
        // Note: CreateMenu returns IClickableMenu, but we could use CreateMenuController for more control

        return menu;
    }

    /// <summary>
    /// Creates a menu controller from a registered view.
    /// </summary>
    /// <param name="menuId">The menu ID as defined in the layout.</param>
    /// <returns>The created controller, or null if not found.</returns>
    public IMenuController? CreateMenuController(string menuId)
    {
        if (!_layout.TryGetMenu(menuId, out var builderAction))
            return null;
        if (builderAction is null)
            return null;

        var builder = new StardewUiMenuBuilder();
        builderAction(builder);

        if (builder.ViewName is null)
            throw new InvalidOperationException($"Menu '{menuId}' must have a view specified.");

        var controller = _integration.CreateMenuController(builder.ViewName, builder.ContextProvider?.Invoke());

        // Apply controller configuration
        if (builder.ControllerConfig is not null)
        {
            builder.ControllerConfig(controller);
        }

        // Wire up events
        if (builder.OnClosingCallback is not null)
        {
            controller.Closing += builder.OnClosingCallback;
        }

        if (builder.OnClosedCallback is not null)
        {
            controller.Closed += builder.OnClosedCallback;
        }

        return controller;
    }

    /// <summary>
    /// Creates a HUD drawable from a registered view.
    /// </summary>
    /// <param name="hudId">The HUD ID as defined in the layout.</param>
    /// <returns>The created drawable, or null if not found.</returns>
    public IViewDrawable? CreateHud(string hudId)
    {
        if (!_layout.TryGetHud(hudId, out var builderAction))
            return null;
        if (builderAction is null)
            return null;

        var builder = new StardewUiHudBuilder();
        builderAction(builder);

        if (builder.ViewName is null)
            throw new InvalidOperationException($"HUD '{hudId}' must have a view specified.");

        var drawable = _integration.CreateHudDrawable(
            builder.ViewName,
            builder.ContextProvider?.Invoke(),
            builder.MaxSizeProvider?.Invoke()
        );

        return drawable;
    }

    /// <summary>
    /// Creates a HUD host for managing HUD drawables with event wiring.
    /// </summary>
    /// <returns>A new HUD host instance.</returns>
    /// <remarks>
    /// The caller is responsible for disposing the returned host when no longer needed.
    /// </remarks>
    public StardewUiHudHost CreateHudHost()
    {
        // This will be created with a reference to the helper from the integration
        // We need to get the helper from the integration's manifest or find another way
        // For now, we'll create a simpler approach - the host needs access to the helper
        // Let's check how to get it from StardewUiIntegration
        throw new NotImplementedException("CreateHudHost requires helper access. Use CreateHudHost(IModHelper) instead.");
    }

    /// <summary>
    /// Creates a HUD host for managing HUD drawables with event wiring.
    /// </summary>
    /// <param name="helper">SMAPI helper for event wiring.</param>
    /// <returns>A new HUD host instance.</returns>
    /// <remarks>
    /// The caller is responsible for disposing the returned host when no longer needed.
    /// </remarks>
    public StardewUiHudHost CreateHudHost(IModHelper helper)
    {
        return new StardewUiHudHost(helper, this);
    }

    /// <summary>
    /// Creates a menu host for managing menu controllers with lifecycle management.
    /// </summary>
    /// <returns>A new menu host instance.</returns>
    /// <remarks>
    /// The caller is responsible for disposing the returned host when no longer needed.
    /// </remarks>
    public StardewUiMenuHost CreateMenuHost()
    {
        return new StardewUiMenuHost(this);
    }

#if WEGG_DEV
    /// <summary>
    /// Enables hot reloading. Dev-only: do not ship in Release builds.
    /// </summary>
    public void EnableHotReloading(string? sourceDirectory = null)
        => _integration.EnableHotReloading(sourceDirectory);

    /// <summary>
    /// Dev-only convenience wrapper that attempts to auto-detect a project directory for source-sync hot reload.
    /// </summary>
    public void EnableHotReloadingWithSourceSync([CallerFilePath] string? callerFilePath = null)
        => _integration.EnableHotReloadingWithSourceSync(callerFilePath);
#endif
}
