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
