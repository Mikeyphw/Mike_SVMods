# TODO — NestCore

- [x] Ensure GMCM option IDs are stable and documented.
- [ ] Verify all debug/diagnostic/logging toggles are only on the `debug` page.
- [ ] Add/verify i18n keys for all pages/options (`config.*`).
- [x] Add docs/examples for dynamic dictionary editors with default-vs-override removal.
- [ ] Add optional unit tests for ID normalization if time allows.

- [x] Add GMCM Options section to GMCM_BUILDER.md (Color/Image/Separator/DynamicParagraph).

- [x] Add OverrideDictionaries helper for default+overrides configs.

- [x] (Optional) Add `ObservableObject` (INotifyPropertyChanged) helper for viewmodels.
- [ ] Verify `NestCore` builds (net6.0) locally/CI after adding `ObservableObject`.

- [x] Add StardewUI consumer doc (`docs/stardewui.md`).
- [x] Add `RelayCommand` helper for UI viewmodels.

- [x] Decide whether to expand `StardewUI.Framework.IMenuController` vendored surface (currently minimal).

- [x] Phase 8: Add `GmcmPageBuilder<TConfig>.Links(...)` overloads for batched page-link registration.
- [x] Phase 8: Add `GmcmMenuLayout<TConfig>.Pages(...)` overloads for batched page registration.
- [x] Phase 8: Document batched GMCM page/link helpers in `GMCM_BUILDER.md`.

- [x] Phase 18A: add `GmcmPageBuilder<TConfig>.Action(...)` for one-shot commands (no fake bool toggle pattern).
- [x] Phase 18A: document `Action(...)` in `GMCM_BUILDER.md` with simple + reset/remove/apply examples.
- [x] Phase 18A: run full repo build gate after NestCore action integration.

- [x] Phase 18D: establish repo GMCM convention rules in `GMCM_BUILDER.md`.
- [x] Phase 18D: align NestCore/LetMeHatch/EggsInWild docs to the shared GMCM convention.
- [x] Phase 18D: run full repo build gate after docs alignment.

- [x] Phase 18E: final grep/review for fake-bool action patterns and inline GMCM bodies.
- [x] Phase 18E: final build gate and doc finalization for repo-wide GMCM standardization.

- [ ] Add a tiny consumer mod example (outside NestCore) wired to a real `assets/views/*.sml` view.
- [ ] Consider a SMAPI main-thread scheduler helper for async viewmodels that update UI-bound properties.

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

