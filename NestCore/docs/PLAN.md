# PLAN — GMCM Host + Dynamic Options (NestCore)

## Scope
- Subdir: `NestCore`
- Convention: all diagnostics/logging toggles belong on a dedicated GMCM page id `debug` (mods), never mixed into normal pages.

## Status
- Phase 0: complete
- Phase 1: complete
- Phase 5: complete
- Phase 6: complete

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
