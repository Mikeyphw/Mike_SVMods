# CHECKLIST — WildEggsHatching

## Scope
## Phase 0 — Keep docs in sync
- [ ] Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
- [ ] Keep all debug/diagnostics options on GMCM page id `debug` only.
- [ ] Keep option IDs stable forever (translation keys and field IDs depend on this).
## Phase 2 — Baseline GMCM menu via BOAF host + DSL
- [ ] Wire `GmcmConfigHost` in `GameLaunched`; if GMCM missing, return.
- [ ] Build pages: `root` (General), `desertTomb`, `items`, `hatching`, `prices`, `animalNotes`, `debug`.
- [ ] Bind options using stable `optionId`s; ensure debug-only fields live on `debug`.
- [ ] Add i18n keys for pages and options under `config.*`.
- [ ] If config changes require refresh, use `onSaved` to invalidate caches.
## Phase 6 — Dynamic per-animal progression mode editor
- [ ] Add save-dependent page `animalProgression` populated only when world is ready.
- [ ] Enumerate animal keys from `Data/FarmAnimals` (and any mod-specific anchors, if applicable).
- [ ] Render a toggle per animal using BOAF dynamic-label overloads with stable element IDs.
- [ ] Store overrides in `AnimalProgressionMode` dictionary using default-vs-override removal (clean config).
- [ ] Rebuild menu on `SaveLoaded` and when relevant assets change.
