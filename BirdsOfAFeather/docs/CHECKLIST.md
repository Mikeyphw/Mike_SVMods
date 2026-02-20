# CHECKLIST — BirdsOfAFeather

## Scope
## Phase 0 — Planning docs baseline
- [ ] Create/refresh `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md`.
- [ ] Ensure phases have 3–7 steps and are actionable.
- [ ] Record any cross-subdir dependencies explicitly.
## Phase 1 — Config-bound GMCM host helper
- [ ] Add `GmcmConfigHost<TConfig>` wrapping existing `GmcmIntegration` and layout DSL.
- [ ] Provide `RebuildFrom(SmapiConfigProvider<TConfig> ...)` convenience binding `get/reset/save` with optional `onSaved`.
- [ ] Ensure stable field IDs are defaulted via existing `GmcmFieldIds`.
- [ ] Ensure `SmapiConfigProvider<TConfig>` supports `ResetToDefault()` (replace with new defaults + save).
- [ ] Document usage pattern: wire in `GameLaunched`, no-op if GMCM missing.
## Phase 5 — Dynamic/dictionary editors support
- [ ] Add DSL overloads accepting custom label funcs (name/tooltip) for dynamic elements.
- [ ] Add `GmcmDynamicIds` helper for stable element IDs derived from dynamic keys.
- [ ] Add host rebuild helpers for save-dependent menus (`SaveLoaded` / `ReturnedToTitle`) with next-tick scheduling.
- [ ] Document patterns: default-vs-override dictionary storage (remove key when equals default).
- [ ] (Optional) Add unit tests for ID normalization/determinism.
