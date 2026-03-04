# NestCore GMCM Builder (wrapper + layout DSL)

This folder provides a small layer on top of **Generic Mod Config Menu (GMCM)** (and optional **GMCMOptions**) that makes menus:
- **declarative** (no more "current page" footguns),
- **strongly-bound** (expression-based bindings instead of repetitive `get/set` lambdas),
- **stable** (auto-generated deterministic `fieldId`s for options).

> This code lives **inside NestCore** right now, but it’s designed to be copy/pasted into other mods later.

---

## Contents

- `GmcmIntegration`
  - discovers GMCM + GMCMOptions safely
  - registers/unregisters/rebuilds
  - auto-rebuilds on **locale change** (because `I18TranslationHelper` caches strings)
- `GmcmConfigHost<TConfig>`
  - binds a config source (e.g. `SmapiConfigProvider<TConfig>`) to the layout DSL
  - recommended entrypoint for mods
- `GmcmIntegrationExtensions.RebuildLayout`
  - entrypoint that turns a declarative layout into actual GMCM calls
- `GmcmMenuLayout<TConfig>`
  - stores the layout:
    - `Root(...)` (main page)
    - `Page(pageId, ...)` (subpages)
- `GmcmPageBuilder<TConfig>`
  - fluent builder for content + options
  - expression bindings like `page.Bool(cfg => cfg.VerboseLogging)`
  - **auto stable field IDs**
- `GmcmFieldIds`
  - deterministic ID generator + normalization rules

---

## Quick start

### Recommended: use `GmcmConfigHost<TConfig>` (config-bound, minimal boilerplate)

```csharp
private SmapiConfigProvider<CoreModConfig>? _config;
private GmcmConfigHost<CoreModConfig>? _gmcmHost;

private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    _config ??= new SmapiConfigProvider<CoreModConfig>(Helper, Monitor);

    _gmcmHost = GmcmConfigHost<CoreModConfig>.TryCreate(
        helper: Helper,
        manifest: ModManifest,
        configProvider: _config,
        translation: Context.Translation
    );

    if (_gmcmHost is null)
        return;

    _gmcmHost.RebuildLayout(layout =>
    {
        layout.Root(root =>
        {
            root.PageLink("debug");
        });

        layout.Page("debug", debug =>
        {
            debug.Bool(cfg => cfg.VerboseLogging, optionId: "VerboseLogging", tooltip: true);
            debug.Bool(cfg => cfg.TestingLogging, optionId: "TestingLogging", tooltip: true);
        });
    });
}
```

> Convention: keep diagnostics/logging toggles behind a dedicated page id `debug`.

### Advanced: use `GmcmIntegration` directly

```csharp
private GmcmIntegration? _gmcm;

private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    _gmcm = GmcmIntegration.TryCreate(
        helper: Helper,
        manifest: ModManifest,
        translation: Context.Translation
    );

    if (_gmcm is null)
        return;

    _gmcm.RebuildLayout(
        getConfig: () => ModCfg,
        reset: () => ModCfg = new CoreModConfig(),
        save: () => Helper.WriteConfig(ModCfg),
        define: layout =>
        {
            layout.Root(root =>
            {
                root.PageLink("debug");
            });

            layout.Page("debug", debug =>
            {
                debug.Bool(cfg => cfg.VerboseLogging, optionId: "VerboseLogging", tooltip: true);
                debug.Bool(cfg => cfg.TestingLogging, optionId: "TestingLogging", tooltip: true);
            });
        }
    );
}
```

That’s it.

---

## Translation key conventions

This builder sits on top of your existing `IGMCMOptionsHelper`, so it follows its key conventions.

### Options

When you register an option with id `"ExtraDebugInfo"`:

- label: `config.ExtraDebugInfo.name`
- tooltip: `config.ExtraDebugInfo.description`

### Pages

When you define `layout.Page("advanced", ...)`:

- page title: `config.page.advanced.name`
- page link label: **defaults to the same key** (`config.page.advanced.name`)
  - back-compat fallback: `config.pagelink.advanced.name`
- page link tooltip:
  - preferred: `config.pagelink.advanced.description`
  - fallback (optional): `config.page.advanced.description`

### Sections + paragraphs

- `root.Section("general")` uses:
  - `config.section.general.text`
  - `config.section.general.description` (tooltip)
- `root.Paragraph("help")` uses:
  - `config.paragraph.help.text`

---

## Field IDs (stable + deterministic)

GMCM supports `fieldId` per option.

By default, if you don't pass a `fieldId`, GMCM generates a **random** id.
That makes it painful to:
- react to changes with GMCM’s `OnFieldChanged`,
- locate/search fields by ID,
- debug config menus.

This builder auto-generates a stable `fieldId` for every option + separator.

### Field ID format

```
<prefix>.gmcm.<pageId>.<elementId>
```

Where:

- `prefix` defaults to `ModManifest.UniqueID` (overrideable).
- `pageId` is `"root"` or your `layout.Page(...)` id.
- `elementId` is:
  - the `optionId` (preferred), OR
  - the expression member path (e.g. `General.EnableDebug`) if you omit `optionId`.

Examples:

- `YourName.NestCore.gmcm.root.ExtraDebugInfo`
- `YourName.NestCore.gmcm.advanced.General.EnableDebug`

### Normalization rules

- Unsafe characters are replaced with `_`.
- Repeated `_` are collapsed.
- IDs longer than ~160 chars are truncated with a stable SHA1 suffix.

### Best practice

**Always pass a stable `optionId`** for UI-facing options.

Even if your config property is `VerboseLogging`,
you might want the UI option to be `"ExtraDebugInfo"` for translation/back-compat.
Passing an explicit `optionId` makes `fieldId` stable even if you rename config properties.

---

## Building pages + links

### Typical structure

- Put global “navigation” + high-level toggles on the root page.
- Put specialized settings on subpages.

```csharp
_gmcm.RebuildLayout(
    getConfig: () => Config,
    reset: () => Config = ModConfig.Default(),
    save: () => Helper.WriteConfig(Config),
    define: layout =>
    {
        layout.Root(root =>
        {
            root.Section("general");
            root.Bool(cfg => cfg.Enabled, optionId: "Enabled", tooltip: true);

            root.Separator("root_sep_1");
            root.Link("advanced", tooltip: true);
        });

        layout.Page("advanced", page =>
        {
            page.Section("debug");
            page.Bool(cfg => cfg.VerboseLogging, optionId: "VerboseLogging", tooltip: true);
            page.Int(cfg => cfg.DebugLevel, optionId: "DebugLevel", min: 0, max: 5, interval: 1, tooltip: true);
        });
    }
);
```

---

## Dynamic pages (generated at runtime)

This is useful when you have a list/dictionary of per-item settings.

```csharp
layout.Root(root =>
{
    root.Section("items");
    foreach (var itemId in Config.ItemRules.Keys)
        root.Link($"item.{itemId}");
});

foreach (var (itemId, rule) in Config.ItemRules)
{
    layout.Page(
        pageId: $"item.{itemId}",
        title: () => GetDisplayName(itemId),  // can be translated
        build: page =>
        {
            page.Bool(
                member: _ => rule.Enabled,
                optionId: $"ItemRules.{itemId}.Enabled",
                tooltip: true
            );

            page.Float(
                member: _ => rule.Weight,
                optionId: $"ItemRules.{itemId}.Weight",
                min: 0f,
                max: 10f,
                interval: 0.25f,
                tooltip: true
            );
        }
    );
}
```

Notes:
- The expression binding expects a member access on `TConfig`.
- For dynamic objects not stored as real properties on the config root, use the **unbound** overloads:
  - `page.Bool(optionId, getValue, setValue, ...)`
  - `page.Float(optionId, getValue, setValue, ...)`

This still gives you stable IDs and translation keys.

---

## Advanced option types (GMCMOptions)

If **GMCMOptions** is installed, `IGMCMOptionsHelper` can add extra controls like color pickers and image pickers.
The builder supports those via:

- `page.Color(...)`
- `page.ImageOption(...)`

Example (color):

```csharp
layout.Page("visual", page =>
{
    page.Color(cfg => cfg.HoverColor, optionId: "HoverColor", tooltip: true, showAlpha: true);
});
```

---

## Separator stability

`Separator()` without an `id` will generate an ID based on build order on that page:

```csharp
page.Separator(); // gets something like "...sep_1"
```

That is **deterministic for a specific build**, but not stable if you insert controls above it later.

Prefer:

```csharp
page.Separator("my_separator_id");
```

---

## Troubleshooting

### GMCM not installed
`TryCreate(...)` returns `null`, so the mod continues without a config UI.

### Missing translations
If `I18TranslationHelper` can’t find a key, your helper will usually show a missing-key placeholder.
Fix by adding the appropriate `config.*` keys.

### Locale changes don’t update GMCM strings
This wrapper subscribes to SMAPI `LocaleChanged` and:
- clears the translation cache,
- rebuilds the menu.

---

## Pattern library (copy/paste)

### One-page menu

```csharp
layout.Root(root =>
{
    root.Bool(cfg => cfg.Enabled, optionId: "Enabled", tooltip: true);
    root.Int(cfg => cfg.Amount, optionId: "Amount", min: 0, max: 100, interval: 1, tooltip: true);
    root.Text(cfg => cfg.Mode, optionId: "Mode", allowedValues: new[] { "Off", "On", "Auto" }, formatAllowedValue: true);
});
```

### Multi-page menu with navigation

```csharp
layout.Root(root =>
{
    root.Link("general");
    root.Link("advanced");
});

layout.Page("general", page =>
{
    page.Section("general");
    page.Bool(cfg => cfg.Enabled, optionId: "Enabled", tooltip: true);
});

layout.Page("advanced", page =>
{
    page.Section("advanced");
    page.Bool(cfg => cfg.Debug, optionId: "Debug", tooltip: true);
});
```

---

## Notes on future expansion

This wrapper currently targets the subset of GMCM methods exposed in
`Mikew.NestCore.Framework.IGenericModConfigMenuAPI`.

If you later expand that interface to include GMCM’s `OnFieldChanged` hook,
the auto-generated field IDs from this builder are already designed for it.

---

## API reference (builder)

### `GmcmIntegration`
- `TryCreate(...) -> GmcmIntegration?`
- `Rebuild(...)`
- `FieldIdPrefix`

### `GmcmIntegrationExtensions`
- `RebuildLayout(getConfig, reset, save, define, fieldIdPrefix?, titleScreenOnly?, force?, attachLocaleHandler?)`

### `GmcmMenuLayout<TConfig>`
- `Root(Action<GmcmPageBuilder<TConfig>> build)`
- `Page(string pageId, Action<GmcmPageBuilder<TConfig>> build, bool printId = false)`
- `Page(string pageId, Func<string> title, Action<GmcmPageBuilder<TConfig>> build, bool printId = false)`

### `GmcmPageBuilder<TConfig>`
- page content: `Section`, `Paragraph`, `Link`, `Separator`, `Image`, `DynamicParagraph`
- options (bound): `Bool`, `Int`, `Float`, `Text`, `Enum`, `Color`
- options (unbound): `Bool`, `Int`, `Float`, `Text`, `Color`, `ImageOption`


## Dynamic/dictionary editors

Use the `*Label` overloads on `GmcmPageBuilder<TConfig>` when you need to render options for dynamic keys (e.g. dictionaries) without adding i18n keys for every entry.

Example patterns:
- `b.EnumLabel(...)` for per-key enum selection.
- Use `GmcmDynamicIds.ElementId(prefix, key)` to build stable element IDs.

## GMCM Options (jltaylor-us.GMCMOptions)

NestCore will automatically detect **GMCM Options** (Jamie Taylor) and expose its complex widgets through `GmcmPageBuilder`:
- `Color(...)` (color picker)
- `ImageOption(...)` (image picker)
- `Separator(...)` (horizontal separator)
- `DynamicParagraph(...)` (dynamic/styled paragraphs)

If GMCM Options isn't installed, these calls are **silently ignored** (they won't render, but the menu still works).

GMCM Options UniqueID: `jltaylor-us.GMCMOptions`.



## GMCM Options helpers (optional)

- `page.Separator(...)`: uses GMCM Options horizontal separator when available; ignored if GMCM Options isn't installed.
- `page.ParagraphDynamic(...)`: uses GMCM Options dynamic paragraph when available; falls back to native `AddParagraph`.
- `page.ColorSwatch(...)`: same as `Color(...)` but auto-uses `MakeColorSwatchDrawer()` when GMCM Options is available.
- `page.ColorSwatchLabel(...)`: dynamic-label version for dictionary/derived values; falls back to native GMCM color option if GMCM Options isn't installed.


---

## Save-dependent menus (next-tick rebuild helpers)

Some menus build dynamic options from save/game state. To avoid querying world state directly inside SMAPI event handlers,
NestCore can rebuild your GMCM layout on the **next update tick** when the save is loaded and/or when returning to title.

```csharp
// In GameLaunched:
_gmcmHost.RebuildLayoutSaveAware(
    define: DefineLayout,
    force: true,
    rebuildOnSaveLoaded: true,
    rebuildOnReturnedToTitle: true
);
```

If your menu depends on something that is initialized asynchronously after `SaveLoaded`, provide a readiness predicate and a max tick budget:

```csharp
_gmcmHost.RebuildLayoutSaveAware(
    define: DefineLayout,
    force: true,
    rebuildOnSaveLoaded: true,
    rebuildOnReturnedToTitle: false,
    saveLoadedReady: () => MyService.Current is not null,
    saveLoadedMaxTicks: 120
);
```

---

## Default + overrides dictionary pattern (remove key when equals default)

For per-item/per-animal configuration, prefer storing only overrides:

- missing key => use default
- key present => use overridden value
- when a value is changed back to default => **remove the key**

NestCore provides <c>OverrideDictionaries</c> for this:

```csharp
using Mikew.NestCore.Helpers.Config;

public sealed class ModConfig
{
    public Dictionary<string, bool> FeatureEnabledOverrides { get; set; } = new();
}

private const bool FeatureDefaultEnabled = true;

private static bool IsEnabled(ModConfig cfg, string key)
    => OverrideDictionaries.GetValue(cfg.FeatureEnabledOverrides, key, FeatureDefaultEnabled);

private static void SetEnabled(ModConfig cfg, string key, bool enabled)
    => OverrideDictionaries.SetOverride(cfg.FeatureEnabledOverrides, key, enabled, FeatureDefaultEnabled);
```

This keeps configs smaller and avoids "redundant overrides" drift over time.


---

## Raw GMCM API

The builder DSL covers the full GMCM API surface.

When you need a method that isn't wrapped with a higher-level helper, you can use:

- `page.Raw` → the full upstream `IGenericModConfigMenuApi` (direct calls).
- `page.Legacy` → manifest-bound wrappers for the obsolete GMCM surface (no need to pass `IManifest`).

### Stable field IDs

NestCore generates stable `fieldId`s as:

`{ModUniqueId}.gmcm.{pageId}.{elementId}` (normalized, hash-shortened if needed).

All bound options (`Bool/Int/Float/Text/...`) and DSL helpers default to stable IDs.
For raw calls, prefer passing `fieldId: page.FieldId("myElement")` (see below) or reuse the DSL helpers.

### Complex option (custom rendering)

```csharp
page.Complex(
    elementId: "my_widget",
    name: () => I18n.Get("config.my_widget.name"),
    tooltip: () => I18n.Get("config.my_widget.description"),
    draw: (b, pos) =>
    {
        // draw at pos.X/pos.Y
    },
    height: () => 64
);

// optional: react to changes (requires stable fieldId for options you care about)
page.OnFieldChanged((fieldId, value) =>
{
    if (fieldId.EndsWith(".my_widget"))
    {
        // ...
    }
});
```

### Obsolete GMCM methods (if you need them)

```csharp
#pragma warning disable CS0618
page.Legacy.RegisterSimpleOption(
    optionName: "Legacy bool",
    optionDesc: "Old API example",
    optionGet: () => true,
    optionSet: _ => { }
);
#pragma warning restore CS0618
```
