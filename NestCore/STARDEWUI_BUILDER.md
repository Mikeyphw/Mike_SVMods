# StardewUI Builder Guide

NestCore provides a GMCM-like DSL experience for creating StardewUI menus and HUDs. This guide covers the DSL host pattern for building StardewUI interfaces.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Asset Registration](#asset-registration)
3. [Menus vs Controllers](#menus-vs-controllers)
4. [HUD Drawables](#hud-drawables)
5. [Hot Reload](#hot-reload)
6. [API Reference](#api-reference)

---

## Quick Start

### 1. Acquire the DSL Host

```csharp
private StardewUiHost? _uiHost;

private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    var nest = Helper.ModRegistry.GetApi<INestCoreApi>("Mikew.NestCore");
    _uiHost = nest?.TryCreateStardewUiHost(Helper, ModManifest);

    if (_uiHost is null)
        return; // StardewUI not installed

    // Build layout and register assets
    _uiHost.RebuildLayout(define: layout =>
    {
        layout.Menu("config", menu => menu
            .View("config")
            .Context(() => new ConfigViewModel()));

        layout.Hud("status", hud => hud
            .View("status")
            .Context(() => new StatusViewModel()));
    });
}
```

### 2. Define Your Views

Place StarML files in `assets/views/`:

```xml<!-- assets/views/config.sml -->
<lane orientation="vertical" padding="16" item-spacing="8">
  <label text="Configuration" />
  <textbox text={Message} />
  <button text="Save" click=|Save.Execute()| />
</lane>
```

### 3. Open a Menu

```csharp
public void OpenConfigMenu()
{
    var menu = _uiHost?.CreateMenuController("config");
    if (menu is null)
        return;

    // Optionally configure before showing
    menu.SetGutters(20);
    
    // Show as active menu
    Game1.activeClickableMenu = menu.Menu;
}
```

---

## Asset Registration

### Convention-Based (Recommended)

```csharp
_uiHost.RebuildLayout(
    define: layout => { /* ... */ },
    assets: new StardewUiAssetsOptions
    {
        ViewsPath = "assets/views",
        SpritesPath = "assets/sprites",
        CustomDataPath = "assets/data"
    }
);
```

### Default Conventions

| Asset Type | Default Path | Registered As |
|------------|--------------|---------------|
| Views | `assets/views/*.sml` | `Mods/{UniqueId}/Views/{Name}` |
| Sprites | `assets/sprites/*` | `Mods/{UniqueId}/Sprites/{Name}` |
| Custom Data | `assets/data/*` | `Mods/{UniqueId}/Data/{Name}` |

### Preloading

```csharp
_uiHost.RebuildLayout(
    define: layout => { /* ... */ },
    preloadAssetsOnce: true  // Preload after registration
);
```

---

## Menus vs Controllers

### When to Use `CreateMenu()`

- Simple modal dialogs
- One-time displays
- When you don't need lifecycle events

```csharp
var menu = _uiHost.CreateMenu("settings");
Game1.activeClickableMenu = menu;
```

### When to Use `CreateMenuController()` (Recommended)

- Full-screen menus with navigation
- Need to handle `Closing`/`Closed` events
- Need to customize controller behavior
- Want programmatic open/close control

```csharp
var controller = _uiHost.CreateMenuController("settings")
    ?? throw new InvalidOperationException("Menu not found");

// Configure before showing
controller.EnableCloseButton();
controller.SetGutters(16);

// Wire lifecycle events
controller.Closing += OnMenuClosing;
controller.Closed += OnMenuClosed;

// Show
Game1.activeClickableMenu = controller.Menu;
```

### Menu Lifecycle

```csharp
private void OnMenuClosing(object? sender, ClosingEventArgs e)
{
    // Called when user tries to close
    // Set e.Cancel = true to prevent closing
}

private void OnMenuClosed(object? sender, ClosedEventArgs e)
{
    // Called after menu is fully closed
    // Good for cleanup
}
```

### Using MenuHost for Complex Scenarios

```csharp
private StardewUiMenuHost? _menuHost;

private void Setup()
{
    _menuHost = _uiHost.CreateMenuHost();
}

private void OpenSettings()
{
    var controller = _uiHost.CreateMenuController("settings");
    _menuHost?.OpenAsActiveMenu(controller);
}

private void Cleanup()
{
    _menuHost?.DisposeAll();
}
```

---

## HUD Drawables

### Basic HUD Usage

```csharp
private StardewUiHudHost? _hudHost;

private void EnableHud()
{
    _hudHost = _uiHost?.CreateHudHost(Helper);
    
    var handle = _hudHost?.Show("status", () => new Vector2(16, 16));
}

private void DisableHud()
{
    _hudHost?.HideAll();
    _hudHost?.Dispose();
}
```

### HUD with Dynamic Position

```csharp
// Position updates every frame
var handle = _hudHost?.Show("minimap", () => GetMinimapPosition());

private Vector2 GetMinimapPosition()
{
    return new Vector2(
        Game1.viewport.Width - 150,
        Game1.viewport.Height - 150
    );
}
```

### HUD Handle

The `StardewUiHudHandle` provides control over individual HUD items:

```csharp
var handle = _hudHost?.Show("status", () => position);

// Update context (re-renders the view)
handle?.UpdateContext(newStatus);

// Temporarily hide without disposing
handle?.Hide();

// Show again
handle?.Show();
```

---

## Hot Reload (Dev-Only)

> **Warning**: Hot reload is only available in Dev builds. These APIs are gated behind `WEGG_DEV` and will not be available in Release builds.

### Source Sync Mode

Automatically watches source files and reloads views:

```csharp
#if WEGG_DEV
_uiHost.EnableHotReloadingWithSourceSync();  // Auto-detects project directory
#endif
```

### Manual Mode

```csharp
#if WEGG_DEV
_uiHost.EnableHotReloading("path/to/views");
#endif
```

### How It Works

1. Enable hot reload during development
2. Edit StarML files in your source directory
3. Changes are detected and views reload automatically
4. Remove in Release builds to keep mod size small

---

## API Reference

### StardewUiHost

| Method | Description |
|--------|-------------|
| `TryCreate()` | Static factory, returns null if StardewUI not installed |
| `RegisterAssets()` | Register assets with optional preloading |
| `RebuildLayout()` | Define menus/HUDs and register assets |
| `CreateMenu()` | Create a simple menu from a view |
| `CreateMenuController()` | Create a controller with lifecycle events |
| `CreateHud()` | Create a HUD drawable |
| `CreateHudHost()` | Create a host for managing HUDs |
| `CreateMenuHost()` | Create a host for managing menus |

`RebuildLayout(...)` replaces the entire stored layout on each call. Re-register every menu and HUD definition you want to keep.

### StardewUiLayout

| Method | Description |
|--------|-------------|
| `Menu(id, define)` | Define a menu with the given ID |
| `Hud(id, define)` | Define a HUD with the given ID |

### StardewUiMenuBuilder

| Method | Description |
|--------|-------------|
| `View(name)` | Set the view name (required) |
| `Context(provider)` | Set the binding context |
| `ConfigureController(action)` | Configure the controller |
| `OnClosing(callback)` | Set closing callback |
| `OnClosed(callback)` | Set closed callback |

### StardewUiHudBuilder

| Method | Description |
|--------|-------------|
| `View(name)` | Set the view name (required) |
| `Context(provider)` | Set the binding context |
| `MaxSize(provider)` | Set maximum size constraint |

### StardewUiHudHost

| Method | Description |
|--------|-------------|
| `Show(id, position)` | Show a HUD by ID |
| `Hide(id)` | Hide a HUD by ID |
| `HideAll()` | Hide all HUDs |
| `Get(id)` | Get a handle |
| `IsShown(id)` | Check if HUD is shown |

### StardewUiMenuHost

| Method | Description |
|--------|-------------|
| `OpenAsActiveMenu(controller)` | Open as active menu |
| `CloseActiveIfOwned()` | Close if we own it |
| `DisposeAll()` | Dispose all managed controllers |

`OpenAsActiveMenu(...)` replaces any owned active instance for the same menu id before opening a new controller.

---

## ViewModel Patterns

### ObservableObject

```csharp
using Mikew.NestCore.Helpers.UI;

internal sealed class ConfigViewModel : ObservableObject
{
    private string _message = "Hello";
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }
}
```

### AsyncRelayCommand

```csharp
public AsyncRelayCommand Save { get; }

public ConfigViewModel()
{
    Save = new AsyncRelayCommand(async () =>
    {
        await SaveConfigAsync();
        Message = "Saved!";
    });
}
```

### StarML Binding

```xml
<lane orientation="vertical">
  <textbox text={Message} />
  <button text="Save" click=|Save.Execute()| 
          enabled={!Save.IsExecuting} />
</lane>
```

---

## Best Practices

1. **Use Controllers for Menus**: Prefer `CreateMenuController()` over `CreateMenu()` for better lifecycle management.

2. **Use MenuHost**: When managing multiple menus, use `CreateMenuHost()` to handle open/close orchestration.

3. **Dispose HUDs**: Always dispose `StardewUiHudHost` when no longer needed (e.g., on mod exit).

4. **Check for Null**: Always check for null when acquiring the host - StardewUI may not be installed.

5. **Preload Assets**: Use `preloadAssetsOnce: true` for frequently opened menus to avoid lag.

6. **Use ViewModels**: Keep presentation logic in viewmodels with `ObservableObject` for clean data binding.

7. **Hot Reload Only Dev**: Never ship with hot reload enabled - it's dev-only.

---

## Migration from Direct Integration

If you're using `StardewUiIntegration` directly, migrate to DSL:

**Before:**
```csharp
var menu = _integration.CreateMenu("config", new ConfigViewModel());
```

**After:**
```csharp
// In layout definition
layout.Menu("config", menu => menu
    .View("config")
    .Context(() => new ConfigViewModel()));

// When opening
var controller = _uiHost.CreateMenuController("config");
```

The DSL provides:
- Type-safe layout definitions
- Centralized menu/HUD registration
- Built-in lifecycle management
- Consistent API across your mod
