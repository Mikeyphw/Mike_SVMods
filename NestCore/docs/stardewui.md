# StardewUI integration (NestCore)

> **Recommendation**: For new mods, use the **StardewUI DSL** (Host pattern) for a GMCM-like experience. See [`STARDEWUI_BUILDER.md`](STARDEWUI_BUILDER.md) for the complete guide.

NestCore provides two ways to use StardewUI:
1. **DSL Host** (Recommended) - GMCM-like experience with layout definitions
2. **Direct Integration** - Lower-level API access

---

## Quick Start: DSL Host (Recommended)

```csharp
private StardewUiHost? _uiHost;

private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    var nest = Helper.ModRegistry.GetApi<INestCoreApi>("Mikew.NestCore");
    _uiHost = nest?.TryCreateStardewUiHost(Helper, ModManifest);

    if (_uiHost is null)
        return; // StardewUI not installed

    // Define layout once
    _uiHost.RebuildLayout(define: layout =>
    {
        layout.Menu("config", menu => menu
            .View("config")
            .Context(() => new ConfigViewModel()));
    });
}

// Open a menu
private void OpenConfig()
{
    var controller = _uiHost?.CreateMenuController("config");
    if (controller is not null)
        Game1.activeClickableMenu = controller.Menu;
}
```

For detailed DSL documentation, see [`STARDEWUI_BUILDER.md`](STARDEWUI_BUILDER.md).

---

## Direct Integration (Legacy)

The following sections describe the lower-level integration API.

- Discover StardewUI safely (`null` when not installed)
- Register view/sprite/custom-data directories by convention
- Create menus and HUD drawables from StarML views

## Quick start (consumer mod)

```csharp
private StardewUiIntegration? _ui;

private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    var nest = Helper.ModRegistry.GetApi<INestCoreApi>("Mikew.NestCore");
    _ui = nest?.TryCreateStardewUiIntegration(Helper, ModManifest);

    if (_ui is null)
        return;

    _ui.RegisterAssets(); // defaults to assets/views + assets/sprites
    _ui.PreloadAssetsOnce(); // optional
}
```

### View conventions

With default `RegisterAssets()`:

- views: `assets/views/*.sml` => `Mods/{YourUniqueId}/Views/{Name}`
- sprites: `assets/sprites/*` => `Mods/{YourUniqueId}/Sprites/{Name}`

Example: `assets/views/config.sml` can be opened via:

```csharp
Game1.activeClickableMenu = _ui.CreateMenu("config", new ConfigViewModel());
```

## Viewmodels: ObservableObject + AsyncRelayCommand

Use `ObservableObject` for automatic binding updates.

```csharp
internal sealed class ConfigViewModel : ObservableObject
{
    private string _message = "Hello";

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public AsyncRelayCommand Refresh { get; }

    public ConfigViewModel()
    {
        Refresh = new AsyncRelayCommand(async () =>
        {
            await Task.Delay(250);
            Message = $"Updated at {DateTime.Now:T}";
        });
    }
}
```

### Minimal StarML snippet

```xml
<lane orientation="vertical" padding="16" item-spacing="8">
  <label text={Message} />
  <button text="Refresh"
          enabled={!Refresh.IsExecuting}
          click=|Refresh.Execute()| />
</lane>
```

## HUD overlay example (create/draw/dispose)

```csharp
private IViewDrawable? _hud;
private HudOverlayViewModel? _hudVm;

private void EnableHud()
{
    if (_ui is null || _hud is not null)
        return;

    _hudVm = new HudOverlayViewModel();
    _hud = _ui.CreateHudDrawable("hud_overlay", _hudVm);
    Helper.Events.Display.RenderedHud += OnRenderedHud;
    Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
}

private void DisableHud()
{
    Helper.Events.Display.RenderedHud -= OnRenderedHud;
    Helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle;

    _hud?.Dispose();
    _hud = null;
    _hudVm = null;
}

private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    => _hud?.Draw(e.SpriteBatch, new Vector2(16, 16));

private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    => DisableHud();
```

## Dev-only: hot reload gating (WEGG_DEV)

NestCore gates hot reload helpers behind `WEGG_DEV`, matching the pattern in EggsInWild.

- Build **NestCore** (and your mod) with configuration **Dev** to enable:
  - `StardewUiIntegration.EnableHotReloading(...)`
  - `StardewUiIntegration.EnableHotReloadingWithSourceSync(...)`

In Release builds those methods are not available.
