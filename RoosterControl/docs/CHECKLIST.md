# CHECKLIST — RoosterControl

## Scope
## Phase 0 — Keep docs in sync
- [x] Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
- [x] Keep all debug/diagnostics options on GMCM page id `debug` only.
- [x] Keep option IDs stable forever (translation keys and field IDs depend on this).
- [x] Gate RoosterControl dev-only code behind `RCTRL_DEV` (built via `dotnet build -c Dev`).
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


## Rooster Produce System
### Phase 1 — Stability + determinism baseline
- [x] Parse all chance/probability values with `CultureInfo.InvariantCulture` + `NumberStyles`.
- [x] Make egg ID removals case-insensitive (`OrdinalIgnoreCase`).
- [x] Make chosen produce deterministic per day (seeded RNG, no shared `Random`).
- [x] Add dev-gated console command to dump seed inputs + chosen results.

### Phase 2 — Determinism everywhere (no call-order sensitivity)
- [x] Centralize seed derivation (`RoosterSeedKey`).
- [x] Remove shared/static RNG usage from RoosterControl produce paths.
- [x] Make `RandomGroupProduceResolver` support deterministic resolution via base seed.
- [x] Ensure deluxe produce selection is deterministic and uses the same seed model.
- [x] Add dev-only warnings for suspicious random-group keys (rate-limited).


### Phase 3 — Validation + semantics cleanup (weights vs percent)
- [x] Clarify weights vs percent semantics (UI labels + validation rules).
- [x] Validate random-group references (missing keys, cycles) at catalog load.
- [x] Canonicalize produce entries on save (trim, stable ordering, normalized weights).


### Phase 4 — Pipeline consolidation (asset editor owns mutations)
- [x] Move all `Data/FarmAnimals` mutations into the asset editor pipeline.
- [x] Make coordinator compute plans; editor applies the edits.
- [x] Batch invalidations via NestCore next-tick helpers.
- [x] Move debug dump / diagnostics to post-apply (next tick) only.
- [x] Add dev-gated pipeline summary console command (`ra_debug_pipeline`).
### Phase 5 — Debug command suite + JSON dumps
- [x] Upgrade `ra_debug_produce` with `--full` (candidates + weights + random-group trace) and `--json` (single-line stable JSON via records).
- [x] Add deterministic trace path in `RandomGroupProduceResolver` for random-group expansion.
- [x] Add `ra_debug_plan` (daily decisions + apply/skip reasons), support `--json`.
- [x] Add `ra_debug_diff` (previous vs current decisions diff), support `--full` and `--json`.
- [x] Ensure outputs are copy/paste friendly and stable ordered.

### Phase 6 — Persist daily decisions snapshot (save state)
- [x] Extend per-save state schema to store previous/current DailyFarmAnimalsDecisions snapshots.
- [x] Load saved snapshots on SaveLoaded and surface them to debug commands.
- [x] Persist snapshots on Saving.
- [x] SchemaVersion migration 1 → 2.
