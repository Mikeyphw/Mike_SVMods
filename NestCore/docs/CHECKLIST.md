# CHECKLIST — NestCore

## Scope
## Phase 0 — Planning docs baseline
- [x] Create/refresh `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md`.
- [x] Ensure phases have 3–7 steps and are actionable.
- [x] Record any cross-subdir dependencies explicitly.
## Phase 1 — Config-bound GMCM host helper
- [x] Add `GmcmConfigHost<TConfig>` wrapping existing `GmcmIntegration` and layout DSL.
- [x] Provide `RebuildFrom(SmapiConfigProvider<TConfig> ...)` convenience binding `get/reset/save` with optional `onSaved`.
- [x] Ensure stable field IDs are defaulted via existing `GmcmFieldIds`.
- [x] Ensure `SmapiConfigProvider<TConfig>` supports `ResetToDefault()` (replace with new defaults + save).
- [x] Document usage pattern: wire in `GameLaunched`, no-op if GMCM missing.
- [x] Align page link labels to `config.page.<id>.name` by default (fallback to `config.pagelink.<id>.name`).
## Phase 5 — Dynamic/dictionary editors support
- [x] Add DSL overloads accepting custom label funcs (name/tooltip) for dynamic elements.
- [x] Add `GmcmDynamicIds` helper for stable element IDs derived from dynamic keys.
- [x] Add `EnumLabel` overload supporting custom allowed-value formatting.
- [x] Add host rebuild helpers for save-dependent menus (`SaveLoaded` / `ReturnedToTitle`) with next-tick scheduling.
- [x] Integrate GMCM Options complex widgets (Color/Image/Separator/DynamicParagraph) behind optional API discovery.
- [x] Document patterns: default-vs-override dictionary storage (remove key when equals default).
- [ ] (Optional) Add unit tests for ID normalization/determinism.
## Phase 6 — StardewUI integration facade
- [x] Vendor StardewUI framework API interfaces (IViewEngine/IViewDrawable/IMenuController) under `Integrations/StardewUI/Framework`.
- [x] Add `StardewUiIntegration` facade: safe API discovery + view/sprite/custom-data registration + menu/drawable helpers.
- [x] Add `StardewUiAssetsOptions` record for convention-driven registration.
- [x] Expose `TryCreateStardewUiIntegration(...)` on `INestCoreApi` and implement in `NestCoreApi` (bump ApiVersion).
- [x] Implement a source-sync hot reload helper (`EnableHotReloadingWithSourceSync`) on the facade.
- [x] Update `docs/*` checklist/todo to reflect completion and add follow-ups.
## Phase 7 — UI viewmodel base helpers
- [x] Add `ObservableObject` (INotifyPropertyChanged) under `Helpers/UI`.
- [x] Add `RelayCommand` (ICommand) under `Helpers/UI`.
- [x] Add `AsyncRelayCommand`/`AsyncRelayCommand<T>` with built-in "is executing" gating.
- [x] Extend `docs/stardewui.md` with a HUD overlay example (create/draw/dispose `IViewDrawable`).
- [x] Add `docs/stardewui.md` consumer example (acquisition + menu open + viewmodel + StarML snippet).
- [x] Verify `NestCore` builds (net6.0) with implicit usings + nullable enabled.

## Phase 8 — GMCM batching helpers
- [x] Add `GmcmPageBuilder<TConfig>.Links(...)` overloads for batched page-link registration
- [x] Add `GmcmMenuLayout<TConfig>.Pages(...)` overloads for batched page registration
- [x] Keep existing `Root(...)`, `Page(...)`, and `Build()` behavior unchanged
- [x] Document the new helpers in `GMCM_BUILDER.md`
- [x] Update `docs/TODO.md` and `docs/PLAN.md`
- [x] Verify `NestCore` builds cleanly

## Phase 18A — GMCM action option
- [x] Add a first-class `Action(...)` DSL primitive on `GmcmPageBuilder<TConfig>`.
- [x] Wire action registration through NestCore helper plumbing without persisted fake values.
- [x] Preserve existing i18n + field-id conventions for action controls.
- [x] Document `Action(...)` usage with simple and reset/remove/apply examples.
- [x] Verify build gate (`NestCore`, `LetMeHatch`, `EggsInWild`, `RoosterControl`, solution).

## Phase 18D — Repo GMCM convention alignment
- [x] Add repo convention section to `GMCM_BUILDER.md` with the 10 standardized rules.
- [x] Record convention establishment in NestCore docs.
- [x] Align LetMeHatch/EggsInWild docs to mark convention adoption.
- [x] Verify build gate (`NestCore`, `LetMeHatch`, `EggsInWild`, `RoosterControl`, solution).

## Phase 18E — Final GMCM standardization validation
- [x] Re-scan for pseudo-button/fake-bool action patterns in GMCM code.
- [x] Re-scan for inline GMCM layout bodies in `ModEntry`/coordinator files.
- [x] Confirm final docs reflect NestCore-first GMCM architecture.
- [x] Verify final build gate.

---

# R3 — StardewUI DSL

## Phase NC-SUI-R3-1 — Sync vendored StardewUI framework API (full surface)
- [x] Replace minimal vendored `IViewEngine.cs` with full StardewUI framework API surface
- [x] Ensure `IMenuController` includes all required members (Closed/Closing events, all properties, all methods)
- [x] Include `ViewEngineExtensions.EnableHotReloadingWithSourceSync(...)` helper
- [x] Update PLAN.md, CHECKLIST.md, TODO.md for R3
- [x] Build gate: verify NestCore and solution builds

## Phase NC-SUI-R3-2 — Add StardewUI DSL core (Host + Layout + Builders)
- [x] Create folder `NestCore/Integrations/StardewUI/Dsl/`
- [x] Create `StardewUiHost.cs` with TryCreate, Integration, RebuildLayout, CreateMenu, CreateMenuController, CreateHud
- [x] Create `StardewUiLayout.cs` with Menu/Hud definition methods
- [x] Create `StardewUiMenuBuilder.cs` with View, Context, ConfigureController, OnClosing, OnClosed
- [x] Create `StardewUiHudBuilder.cs` with View, Context, MaxSize
- [x] Update `StardewUiIntegration.cs` with CreateMenuFromMarkup, CreateMenuControllerFromMarkup, CreateHudDrawableFromMarkup
- [x] Update docs for R3
- [x] Build gate: verify NestCore and solution builds

## Phase NC-SUI-R3-3 — Add HUD runtime host (draw + dispose)
- [x] Create `StardewUiHudHandle.cs` with IDisposable, stores Drawable/subscriptions/position provider
- [x] Create `StardewUiHudHost.cs` with Show/Hide/HideAll methods and event wiring
- [x] Add `CreateHudHost()` to `StardewUiHost`
- [x] Update docs
- [x] Build gate: verify NestCore and solution builds

## Phase NC-SUI-R3-4 — Add Menu runtime host (controller customization + lifecycle)
- [x] Create `StardewUiMenuHandle.cs` with IDisposable, owns IMenuController
- [x] Create `StardewUiMenuHost.cs` with OpenAsActiveMenu/CloseActiveIfOwned/DisposeAll
- [x] Wire controller events (Closing/Closed callbacks)
- [x] Add `CreateMenuHost()` to `StardewUiHost`
- [x] Update docs
- [x] Build gate: verify NestCore and solution builds

## Phase NC-SUI-R3-5 — Expose StardewUI DSL via NestCore public API
- [x] Add `TryCreateStardewUiHost(...)` to `INestCoreApi`
- [x] Implement in `NestCoreApi.cs`
- [x] Update docs
- [x] Build gate: verify NestCore and solution builds

## Phase NC-SUI-R3-6 — Documentation + "full-feature parity checklist"
- [x] Create `NestCore/STARDEWUI_BUILDER.md` with Quick start, Asset registration, Menus vs Controllers, HUD drawables, Hot reload
- [x] Update `docs/stardewui.md` to prefer DSL host usage
- [x] Add explicit "Parity checklist" bullet list
- [x] Update PLAN/CHECKLIST/TODO marking R3 complete
- [x] Build gate: verify NestCore and solution builds

## Phase NC-SUI-R3-7 — Deterministic host rebuild/open behavior
- [x] Add `StardewUiLayout.Clear()` and use it from `StardewUiHost.RebuildLayout(...)`.
- [x] Replace owned `StardewUiMenuHost` handles deterministically on repeated opens.
- [x] Document the replace-only rebuild/open guarantees.
- [x] Build gate: verify `NestCore` and `EggsInWild` builds.

## R3 Parity Checklist — Feature Completeness

The following features are available via the StardewUI DSL:

### Core API
- [x] TryCreate for safe discovery (null when StardewUI not installed)
- [x] Asset registration (views, sprites, custom data)
- [x] Convention-based asset discovery

### Menu System
- [x] Menu definition via DSL (StardewUiLayout.Menu)
- [x] CreateMenu for simple modal menus
- [x] CreateMenuController for full-featured menus
- [x] View configuration (view name, context provider)
- [x] Controller configuration (gutters, close button)
- [x] Lifecycle events (OnClosing, OnClosed)

### HUD System
- [x] HUD definition via DSL (StardewUiLayout.Hud)
- [x] CreateHud for basic drawables
- [x] StardewUiHudHost for managed HUD lifecycle
- [x] Show/Hide/HideAll operations
- [x] Dynamic position providers
- [x] Automatic RenderedHud event wiring

### Menu Host
- [x] StardewUiMenuHost for menu orchestration
- [x] OpenAsActiveMenu
- [x] CloseActiveIfOwned
- [x] DisposeAll for cleanup

### Developer Experience
- [x] RebuildLayout for hot configuration
- [x] Asset preloading support
- [x] Model reflection preloading
- [x] Hot reload helpers (WEGG_DEV gated)

### ViewModel Support
- [x] ObservableObject helper
- [x] RelayCommand helper
- [x] AsyncRelayCommand with IsExecuting

### Documentation
- [x] STARDEWUI_BUILDER.md comprehensive guide
- [x] docs/stardewui.md updated with DSL preference
- [x] API reference documentation
