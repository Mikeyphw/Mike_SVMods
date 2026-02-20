# PLAN — GMCM Menu (RoosterAddon)

## Scope
- Subdir: `RoosterAddon`
- Dependency: BOAF Phase 1 (host helper). Dynamic editor work depends on BOAF Phase 5.

## Phase 0 — Keep docs in sync
1. Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
2. Keep all debug/diagnostics options on GMCM page id `debug` only.
3. Keep option IDs stable forever (translation keys and field IDs depend on this).

## Phase 4 — Baseline GMCM menu via BOAF host (delegate binding)
1. Wire `GmcmConfigHost` in `GameLaunched` using delegate binding (`get/reset/save`) to preserve schema handling.
2. Build pages: `root`, `compat`, `probabilities`, per-rooster pages (`rooster.white` etc), `females`, `debug`.
3. Bind scalar settings and per-rooster scalar settings with stable `optionId`s.
4. Keep produce-table editing minimal or deferred; keep debug toggles on `debug`.
5. Add i18n keys under `config.*`; use `onSaved` only if runtime refresh is needed.

## Phase 8A — FemalesOptions dynamic assignment editor
1. Add save-dependent page `females` populated only when world is ready.
2. Enumerate females from the mod’s own planning/assignment service (source of truth).
3. Render one dropdown per female key selecting assignment (Off/White/Brown/Blue/Void/Gold).
4. Persist into `FemalesOptions` dictionary using stable element IDs.
5. Rebuild on `SaveLoaded` and when dynamic sources change.

## Phase 8B — ProduceEntries/DeluxeProduceEntries editor (bounded)
1. For each rooster type page, normalize lists to exactly 4 entries before building UI.
2. Render each slot as: choice dropdown + custom item name + probability (0–100).
3. Keep allowed-values curated to avoid huge lists.
4. Validate and normalize before save.
5. Preserve config-file editing as an escape hatch.
