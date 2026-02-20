# CHECKLIST — LetMeHatchThisEgg

## Scope
## Phase 0 — Keep docs in sync
- [ ] Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
- [ ] Keep all debug/diagnostics options on GMCM page id `debug` only.
- [ ] Keep option IDs stable forever (translation keys and field IDs depend on this).
## Phase 3 — Baseline GMCM menu via BOAF host + DSL
- [x] Remove/retire legacy GMCM UI plumbing in favor of BOAF host + DSL.
- [x] Wire `GmcmConfigHost` in `GameLaunched`; if GMCM missing, return.
- [x] Build pages: `root`, `cloning`, `colors`, `breeding`, `advanced`, `debug`.
- [ ] Bind scalar options with stable `optionId`s; put all logging/show-debug toggles on `debug`.
- [ ] Add i18n keys under `config.*`; use `onSaved` only if cache refresh is needed.
## Phase 7A — Dynamic per-pack/per-animal toggles (low risk)
- [ ] Add page `edits` populated from LMH’s known sources (packs/animal keys).
- [ ] Render one toggle per entry with custom label funcs (friendly name fallback to key).
- [ ] Use default-vs-override dictionary storage (remove key when equals default).
- [ ] Rebuild on `SaveLoaded` so dynamic sources are available.
- [ ] Keep option IDs stable using BOAF dynamic ID helper.
## Phase 7B — Egg color controls (medium risk)
- [ ] Keep scalar toggles on `colors` page (`ChangeEggColors`, `LinkAnimalColorToEgg`).
- [ ] Add dynamic sections per animal for configurable color pickers.
- [ ] Keep UI bounded; avoid rendering huge lists.
- [ ] Use stable element IDs derived from keys.
- [ ] Rebuild on `SaveLoaded`.
## Phase 7C — ItemToClone editor (highest risk; bounded UI)
- [ ] Add page `itemMappings` limited to curated/known animal relation order.
- [ ] Use dropdowns for known choices + a “Custom” fallback text entry.
- [ ] Keep allowed-values lists curated (avoid huge item catalogs).
- [ ] Validate and normalize mapping data before saving.
- [ ] Preserve config-file editing as an escape hatch.
