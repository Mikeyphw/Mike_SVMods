# PLAN — GMCM Host + Dynamic Options (NestCore)

## Scope
- Subdir: `NestCore`
- Convention: all diagnostics/logging toggles belong on a dedicated GMCM page id `debug` (mods), never mixed into normal pages.

## Status
- Phase 0: complete
- Phase 1: complete
- Phase 5: complete
- Phase 6: complete
- Phase 7: complete
- Phase 8: complete
- Phase 18A: complete
- Phase 18D: complete
- Phase 18E: complete
- R3: **complete**

## Phase 0 — Planning docs baseline
1. Create/refresh `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md`.
2. Ensure phases have 3–7 steps and are actionable.
3. Record any cross-subdir dependencies explicitly.

## Phase 1 — Config-bound GMCM host helper
1. Add `GmcmConfigHost<TConfig>` wrapping existing `GmcmIntegration` and layout DSL.
2. Provide `RebuildFrom(SmapiConfigProvider<TConfig> ...)` convenience binding `get/reset/save` with optional `onSaved`.
3. Ensure stable field IDs are defaulted via existing `GmcmFieldIds`.
4. Ensure `SmapiConfigProvider<TConfig>` supports `ResetToDefault()` (replace with new defaults + save).
5. Document usage pattern: wire in `GameLaunched`, no-op if GMCM missing.
6. Align page link labels to `config.page.<id>.name` by default (fallback to `config.pagelink.<id>.name`).

## Phase 5 — Dynamic/dictionary editors support
1. Add DSL overloads accepting custom label funcs (name/tooltip) for dynamic elements.
2. Add `GmcmDynamicIds` helper for stable element IDs derived from dynamic keys.
3. Add host rebuild helpers for save-dependent menus (`SaveLoaded` / `ReturnedToTitle`) with next-tick scheduling.
4. Document patterns: default-vs-override dictionary storage (remove key when equals default).
5. (Optional) Add unit tests for ID normalization/determinism.
6. Integrate GMCM Options complex widgets (color picker, image picker, separator, dynamic paragraph) and keep them no-op if the mod isn't installed.

## Phase 6 — StardewUI integration facade
1. Vendor StardewUI framework API interfaces (IViewEngine/IViewDrawable/IMenuController) under `Integrations/StardewUI/Framework`.
2. Add `StardewUiIntegration` facade: safe API discovery + view/sprite/custom-data registration + menu/drawable helpers.
3. Add `StardewUiAssetsOptions` record for convention-driven registration.
4. Expose `TryCreateStardewUiIntegration(...)` on `INestCoreApi` and implement in `NestCoreApi` (bump ApiVersion).
5. Implement a source-sync hot reload helper (`EnableHotReloadingWithSourceSync`) on the facade.
6. Update `docs/*` checklist/todo to reflect completion and add follow-ups.

## Phase 7 — UI viewmodel base helpers
1. Add `ObservableObject` (INotifyPropertyChanged) under `Helpers/UI`.
2. Add `RelayCommand` (ICommand) under `Helpers/UI` for reusable enable/disable logic.
3. Add async command variants (`AsyncRelayCommand`/`AsyncRelayCommand<T>`) with built-in "is executing" gating.
4. Add `docs/stardewui.md` with a consumer-facing example (GameLaunched acquisition + menu open + sample viewmodel + StarML snippet).
5. Extend `docs/stardewui.md` with a HUD overlay example (create/draw/dispose `IViewDrawable`).
6. Document intended use for StardewUI binding contexts and event handlers (method-first, optionally routing into commands).
7. Verify `NestCore` builds (net6.0) with implicit usings + nullable enabled.

## Phase 8 — GMCM batching helpers

Goal: add small GMCM DSL helpers that reduce repeated page-link/page-registration code without changing behavior.

Steps:
1. Add `GmcmPageBuilder<TConfig>.Links(...)` overloads for batched page-link registration.
2. Add `GmcmMenuLayout<TConfig>.Pages(...)` overloads for batched page registration.
3. Keep existing `Root(...)`, `Page(...)`, and `Build()` behavior unchanged.
4. Document the new helpers in `GMCM_BUILDER.md` with one generated-subpages example.
5. Update `docs/CHECKLIST.md` and `docs/TODO.md` to track the new helper surface.
6. Verify `NestCore` builds cleanly.

Acceptance:
* `Links(...)` exists and delegates to existing `Link(...)`.
* `Pages(...)` exists and delegates to existing `Page(...)`.
* Existing GMCM call sites do not require changes.
* `GMCM_BUILDER.md` includes helper usage notes and example.

## Phase 18A — GMCM action option

Goal: add a first-class one-shot action control to the NestCore GMCM DSL (no fake bool buttons).

Steps:
1. Add `GmcmPageBuilder<TConfig>.Action(...)` using `optionId` + callback.
2. Back it with a dedicated NestCore helper registration path (non-persistent action semantics).
3. Keep existing field-id generation and i18n conventions intact.
4. Document `Action(...)` in `GMCM_BUILDER.md` with simple and reset/remove/apply examples.
5. Verify `NestCore`, `LetMeHatch`, `EggsInWild`, `RoosterControl`, and solution builds remain green.

## Phase 18D — Repo GMCM convention alignment

1. Add a repo convention section to `GMCM_BUILDER.md` with the 10 standardized GMCM rules.
2. Record the convention as established in NestCore planning/checklist/todo docs.
3. Mark downstream mod docs as aligned (`LetMeHatch`, `EggsInWild`).
4. Keep production behavior unchanged (docs-only alignment in this phase).

## Phase 18E — Final GMCM standardization validation

1. Re-scan mods for fake-bool action patterns and replace/remove remaining cases where safe.
2. Re-scan for large inline GMCM layouts in `ModEntry`/coordinators and keep orchestration in `<Mod>GmcmMenu`.
3. Record final architecture state across NestCore/LetMeHatch/EggsInWild docs.
4. Run full build gate and finalize.

---

# R3 — StardewUI DSL (Complete)

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
- [x] Add `StardewUiLayout.Clear()` and call it from `StardewUiHost.RebuildLayout(...)`.
- [x] Make `StardewUiMenuHost.OpenAsActiveMenu(...)` replace owned handles deterministically.
- [x] Document the replace-only rebuild/open guarantees.
- [x] Build gate: verify `NestCore` and `EggsInWild` builds.
