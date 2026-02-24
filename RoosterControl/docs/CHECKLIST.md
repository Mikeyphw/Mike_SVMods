# CHECKLIST — RoosterControl

## Scope
## Phase 0 — Keep docs in sync
- [x] Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
- [x] Keep all debug/diagnostics options on GMCM page id `debug` only.
- [x] Keep option IDs stable forever (translation keys and field IDs depend on this).
## Phase 4 — Baseline GMCM menu via NestCore host (delegate binding)
- [x] Wire `GmcmConfigHost` in `GameLaunched` using delegate binding (`get/reset/save`) to preserve schema handling.
- [x] Build pages: `root`, `compat`, `probabilities`, per-rooster pages (`rooster.white` etc), `females`, `debug`.
- [x] Bind scalar settings and per-rooster scalar settings with stable `optionId`s.
- [x] Keep produce-table editing minimal or deferred; keep debug toggles on `debug`.
- [x] Add i18n keys under `config.*`; use `onSaved` only if runtime refresh is needed.
## Phase 8A — FemalesOptions dynamic assignment editor
- [x] Add save-dependent page `females` populated only when world is ready.
- [x] Enumerate females from the mod’s own planning/assignment service (source of truth).
- [x] Render one dropdown per female key selecting assignment (Off/White/Brown/Blue/Void/Gold).
- [x] Persist into `FemalesOptions` dictionary using stable element IDs.
- [x] Rebuild on `SaveLoaded` and when dynamic sources change.
## Phase 8B — ProduceEntries/DeluxeProduceEntries editor (bounded)
- [x] For each rooster type page, normalize lists to exactly 4 entries before building UI.
- [x] Render each slot as: choice dropdown + custom item name + probability (0–100).
- [x] Keep allowed-values curated to avoid huge lists.
- [x] Validate and normalize before save.
- [x] Preserve config-file editing as an escape hatch.
