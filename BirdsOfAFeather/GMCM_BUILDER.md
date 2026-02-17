# BirdsOfAFeather GMCM Builder (wrapper + layout DSL)

This folder provides a small layer on top of **Generic Mod Config Menu (GMCM)** (and optional **GMCMOptions**) that makes menus:
- **declarative** (no more "current page" footguns),
- **strongly-bound** (expression-based bindings instead of repetitive `get/set` lambdas),
- **stable** (auto-generated deterministic `fieldId`s for options).

> This code lives **inside BirdsOfAFeather** right now, but it’s designed to be copy/pasted into other mods later.

---

## Contents

- `GmcmIntegration`
  - discovers GMCM + GMCMOptions safely
  - registers/unregisters/rebuilds
  - auto-rebuilds on **locale change** (because `I18TranslationHelper` caches strings)
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

### 1) Create the integration during `GameLaunched`

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
                root.Bool(cfg => cfg.VerboseLogging, optionId: "ExtraDebugInfo", tooltip: true);
                root.Bool(cfg => cfg.TestingLogging, optionId: "Testing", tooltip: true);
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
- page link label: `config.pagelink.advanced.name`
- page link tooltip: `config.pagelink.advanced.description`

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

- `YourName.BirdsOfAFeather.gmcm.root.ExtraDebugInfo`
- `YourName.BirdsOfAFeather.gmcm.advanced.General.EnableDebug`

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
`Mikew.BirdsOfAFeather.Framework.IGenericModConfigMenuAPI`.

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
