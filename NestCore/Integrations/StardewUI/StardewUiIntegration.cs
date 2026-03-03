// FILE: NestCore/Integrations/StardewUI/StardewUiIntegration.cs
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley.Menus;

namespace Mikew.NestCore.Integrations.StardewUI;

/// <summary>
/// Facade over StardewUI's <see cref="IViewEngine"/>:
/// - safe API discovery;
/// - convention-based asset registration (views/sprites/custom data);
/// - convenience helpers for creating menus/HUD drawables by short view name.
/// </summary>
public sealed class StardewUiIntegration
{
    public const string DefaultStardewUiModId = "focustense.StardewUI";

    private readonly IManifest _manifest;
    private bool _assetsPreloaded;

    private StardewUiIntegration(IManifest manifest, IViewEngine engine)
    {
        _manifest = manifest;
        Engine = engine;

        ViewsPrefix = $"Mods/{manifest.UniqueID}/Views";
        SpritesPrefix = $"Mods/{manifest.UniqueID}/Sprites";
        CustomDataPrefix = $"Mods/{manifest.UniqueID}";
    }

    /// <summary>Underlying StardewUI engine API.</summary>
    public IViewEngine Engine { get; }

    /// <summary>Resolved view asset-name prefix, e.g. Mods/MyMod/Views.</summary>
    public string ViewsPrefix { get; private set; }

    /// <summary>Resolved sprite asset-name prefix, e.g. Mods/MyMod/Sprites.</summary>
    public string SpritesPrefix { get; private set; }

    /// <summary>Resolved custom data asset-name prefix, e.g. Mods/MyMod.</summary>
    public string CustomDataPrefix { get; private set; }

    /// <summary>
    /// Try to create a StardewUI integration for this mod. Returns null if StardewUI isn't installed.
    /// </summary>
    public static StardewUiIntegration? TryCreate(
        IModHelper helper,
        IManifest manifest,
        string stardewUiModId = DefaultStardewUiModId)
    {
        var engine = helper.ModRegistry.GetApi<IViewEngine>(stardewUiModId);
        return engine is null ? null : new StardewUiIntegration(manifest, engine);
    }

    /// <summary>
    /// Registers view/sprite/custom-data directories using conventions.
    /// Safe to call multiple times; last call wins.
    /// </summary>
    public void RegisterAssets(StardewUiAssetsOptions? options = null)
    {
        options ??= new StardewUiAssetsOptions();

        ViewsPrefix = options.ViewsPrefix ?? $"Mods/{_manifest.UniqueID}/Views";
        SpritesPrefix = options.SpritesPrefix ?? $"Mods/{_manifest.UniqueID}/Sprites";
        CustomDataPrefix = options.CustomDataPrefix ?? $"Mods/{_manifest.UniqueID}";

        Engine.RegisterViews(ViewsPrefix, options.ViewsDir);
        Engine.RegisterSprites(SpritesPrefix, options.SpritesDir);

        if (!string.IsNullOrWhiteSpace(options.CustomDataDir))
            Engine.RegisterCustomData(CustomDataPrefix, options.CustomDataDir!);
    }

#if WEGG_DEV
    /// <summary>
    /// Enables StardewUI hot reloading. Dev-only: do not ship in Release builds.
    /// </summary>
    public void EnableHotReloading(string? sourceDirectory = null)
        => Engine.EnableHotReloading(sourceDirectory);

    /// <summary>
    /// Dev-only convenience wrapper that attempts to auto-detect a project directory for source-sync hot reload.
    /// </summary>
    public void EnableHotReloadingWithSourceSync([CallerFilePath] string? callerFilePath = null)
        => Engine.EnableHotReloading(FindProjectDirectory(callerFilePath));

    private static string? FindProjectDirectory(string? sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            return null;

        for (var dir = Directory.GetParent(sourceFilePath); dir is not null; dir = dir.Parent)
        {
            foreach (var _ in dir.EnumerateFiles("*.csproj"))
                return dir.FullName;
        }

        return null;
    }
#endif

    /// <summary>
    /// Begins preloading assets in the background.
    /// Must not be called more than once per mod.
    /// </summary>
    public void PreloadAssetsOnce()
    {
        if (_assetsPreloaded)
            return;

        Engine.PreloadAssets();
        _assetsPreloaded = true;
    }

    /// <summary>Preloads reflection metadata for model types used as binding contexts.</summary>
    public void PreloadModels(params Type[] types)
        => Engine.PreloadModels(types);

    /// <summary>Builds a fully-qualified view asset name from a short view name.</summary>
    public string GetViewAssetName(string viewName)
        => QualifyAsset(ViewsPrefix, viewName, ".sml");

    /// <summary>Creates a Stardew menu from a registered view asset (short view name).</summary>
    public IClickableMenu CreateMenu(string viewName, object? context = null)
        => Engine.CreateMenuFromAsset(GetViewAssetName(viewName), context);

    /// <summary>Creates a menu controller from a registered view asset (short view name).</summary>
    public IMenuController CreateMenuController(string viewName, object? context = null)
        => Engine.CreateMenuControllerFromAsset(GetViewAssetName(viewName), context);

    /// <summary>
    /// Creates a HUD drawable from a registered view asset (short view name).
    /// Caller owns disposal.
    /// </summary>
    public IViewDrawable CreateHudDrawable(string viewName, object? context = null, Vector2? maxSize = null)
    {
        var drawable = Engine.CreateDrawableFromAsset(GetViewAssetName(viewName));
        drawable.Context = context;
        drawable.MaxSize = maxSize;
        return drawable;
    }

    private static string QualifyAsset(string prefix, string name, string stripExtension)
    {
        var normalized = (name ?? string.Empty).Replace('\', '/').TrimStart('/');
        if (normalized.EndsWith(stripExtension, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^stripExtension.Length];

        return normalized.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"{prefix}/{normalized}";
    }
}
