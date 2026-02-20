# PLAN — GMCM Host + Dynamic Options (BOAF)

## Scope
- Subdir: `BirdsOfAFeather`
- Convention: all diagnostics/logging toggles belong on a dedicated GMCM page id `debug` (mods), never mixed into normal pages.

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

## Phase 5 — Dynamic/dictionary editors support
1. Add DSL overloads accepting custom label funcs (name/tooltip) for dynamic elements.
2. Add `GmcmDynamicIds` helper for stable element IDs derived from dynamic keys.
3. Add host rebuild helpers for save-dependent menus (`SaveLoaded` / `ReturnedToTitle`) with next-tick scheduling.
4. Document patterns: default-vs-override dictionary storage (remove key when equals default).
5. (Optional) Add unit tests for ID normalization/determinism.
