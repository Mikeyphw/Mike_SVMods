# StardewUI in NestCore

This repo uses **NestCore** as the mandatory framework layer. For StardewUI, NestCore provides a thin facade that:
- safely detects whether StardewUI is installed;
- registers view/sprite/custom-data asset directories by convention; and
- creates menus/HUD drawables from **short view names**.

## 1) Add assets in your mod

Convention (recommended):
- `assets/views/*.sml`
- `assets/sprites/*` (optional, if you use sprites)
- `assets/ui/*` (optional, for custom data)

## 2) Acquire the facade in `GameLaunched`

```csharp
using Mikew.NestCore.Framework;
using Mikew.NestCore.Integrations.StardewUI;

private StardewUiIntegration? ui;

private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    var nestApi = Helper.ModRegistry.GetApi<INestCoreApi>("Mikew.nestcore");
    ui = nestApi?.TryCreateStardewUiIntegration(Helper, ModManifest);

    if (ui is null)
        return; // StardewUI not installed (soft dependency)

    ui.RegisterAssets(new StardewUiAssetsOptions(
        ViewsDir: "assets/views",
        SpritesDir: "assets/sprites",
        CustomDataDir: "assets/ui"));

    ui.PreloadModels(typeof(ConfigMenuViewModel));
    ui.PreloadAssetsOnce(); // optional
}
```

## 3) Open a menu from a view asset

Given `assets/views/ConfigMenu.sml`, open it with the short name `"ConfigMenu"`:

```csharp
using StardewValley;

private void OpenConfigMenu()
{
    if (ui is null)
        return;

    Game1.activeClickableMenu = ui.CreateMenu("ConfigMenu", new ConfigMenuViewModel(CloseConfigMenu));
}

private void CloseConfigMenu() => Game1.exitActiveMenu();
```

## 4) Example viewmodel (binding context)

NestCore provides basic MVVM helpers in `Mikew.NestCore.Helpers.UI`:

```csharp
using Mikew.NestCore.Helpers.UI;

internal sealed class ConfigMenuViewModel : ObservableObject
{
    private bool enabled;

    public bool Enabled
    {
        get => enabled;
        set => SetProperty(ref enabled, value);
    }

    public RelayCommand Close { get; }

    public ConfigMenuViewModel(Action close)
        => Close = new RelayCommand(close);
}
```

## 5) Example StarML view (`assets/views/ConfigMenu.sml`)

StardewUI event bindings typically call methods directly (pipes syntax). If you prefer commands, call them from a method.

```xml
<lane orientation="vertical" padding="16" horizontal-content-alignment="middle">
    <banner text="My Mod" padding="12" />
    <checkbox label-text="Enabled" is-checked={<>Enabled} />

    <button text="Close" click=|OnClose()| />
</lane>
```

With the viewmodel method:

```csharp
public void OnClose() => Close.Execute(null);
```

## 6) HUD overlay example

Create a drawable from a view asset (e.g. `assets/views/HudOverlay.sml`) and draw it in `RenderedHud`:

```csharp
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewUI.Framework;
using StardewValley;

private IViewDrawable? hud;
private readonly Vector2 hudPadding = new(16f, 16f);

private void ToggleHud()
{
    if (ui is null)
        return;

    if (hud is not null)
    {
        hud.Dispose();
        hud = null;
        return;
    }

    hud = ui.CreateHudDrawable("HudOverlay", new HudOverlayViewModel());
}

private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
{
    if (hud is null)
        return;

    var size = hud.ActualSize;
    var x = Game1.uiViewport.Width - size.X - hudPadding.X;
    var y = hudPadding.Y;

    hud.Draw(e.SpriteBatch, new Vector2(x, y));
}

private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
{
    hud?.Dispose();
    hud = null;
}
```

Remember to wire the events (e.g. in `Entry`):

```csharp
Helper.Events.Display.RenderedHud += OnRenderedHud;
Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
```

Minimal StarML (`assets/views/HudOverlay.sml`):

```xml
<panel padding="8">
  <label text="Hello HUD" />
</panel>
```


## Notes

- NestCore qualifies view asset names to `Mods/{UniqueID}/Views/{ViewName}` automatically after registration.
- For HUD overlays, use `ui.CreateHudDrawable(...)` and dispose the drawable when you no longer need it.
