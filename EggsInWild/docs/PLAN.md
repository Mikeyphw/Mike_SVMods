# PLAN — GMCM Menu (EggsInWild)

## Scope
- Subdir: `EggsInWild`
- Dependency: NestCore Phase 1 (host helper). Dynamic editor work depends on NestCore Phase 5.

## Phase 0 — Keep docs in sync
1. Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
2. Keep all debug/diagnostics options on GMCM page id `debug` only.
3. Keep option IDs stable forever (translation keys and field IDs depend on this).

## Phase 2 — Baseline GMCM menu via NestCore host + DSL
1. Wire `GmcmConfigHost` in `GameLaunched`; if GMCM missing, return.
2. Build pages: `root` (General), `desertTomb`, `items`, `hatching`, `prices`, `animalNotes`, `debug`.
3. Bind options using stable `optionId`s; ensure debug-only fields live on `debug`.
4. Add i18n keys for pages and options under `config.*`.
5. If config changes require refresh, use `onSaved` to invalidate caches.

## Phase 6 — Dynamic per-animal progression mode editor
1. Add save-dependent page `animalProgression` populated only when world is ready.
2. Enumerate animal keys from `Data/FarmAnimals` (and any mod-specific anchors, if applicable).
3. Render a toggle per animal using NestCore dynamic-label overloads with stable element IDs.
4. Store overrides in `AnimalProgressionMode` dictionary using default-vs-override removal (clean config).
5. Rebuild menu on `SaveLoaded` and when relevant assets change.
