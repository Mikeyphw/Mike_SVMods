# PLAN — GMCM Menu (RoosterControl)

Status: Phase 8B produce tables + rooster produce system Phase 6 implemented.

## Scope
- Subdir: `RoosterControl`
- Dependency: NestCore Phase 1 (host helper). Dynamic editor work depends on NestCore Phase 5.

## Phase 0 — Keep docs in sync
1. Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
2. Keep all debug/diagnostics options on GMCM page id `debug` only.
3. Keep option IDs stable forever (translation keys and field IDs depend on this).
4. Gate RoosterControl dev-only code behind `RCTRL_DEV` (built via `dotnet build -c Dev`).


## Phase 4 — Baseline GMCM menu via NestCore host (delegate binding)
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


## Rooster Produce System

### Phase 1 — Stability + determinism baseline
1. Parse all chance/probability values with `CultureInfo.InvariantCulture` + `NumberStyles`.
2. Make egg ID removals case-insensitive (`OrdinalIgnoreCase`).
3. Make chosen produce deterministic per day (seeded RNG, no shared `Random`).
4. Add dev-gated console command to dump seed inputs + chosen results.

### Phase 2 — Determinism everywhere (no call-order sensitivity)
1. Centralize seed derivation (`RoosterSeedKey`).
2. Remove shared/static RNG usage from RoosterControl produce paths.
3. Make `RandomGroupProduceResolver` support deterministic resolution via base seed.
4. Ensure deluxe produce selection is deterministic and uses the same seed model.
5. Add dev-only warnings for suspicious random-group keys (rate-limited).

### Phase 3 — Validation + semantics cleanup (weights vs percent)
1. Clarify weights vs percent semantics (UI labels + validation rules).
2. Validate random-group references (missing keys, cycles) at catalog load.
3. Canonicalize produce entries on save (trim, stable ordering, normalized weights).

### Phase 4 — Pipeline consolidation (asset editor owns mutations)
1. Move all `Data/FarmAnimals` mutations into the asset editor pipeline.
2. Make coordinator compute plans; editor applies the edits.
3. Batch invalidations via NestCore next-tick helpers.
4. Move debug dump / diagnostics to post-apply (next tick) only.


### Phase 5 — Debug command suite + JSON dumps
1. Upgrade `ra_debug_produce` with `--full` (candidates + weights + random-group trace) and `--json` (single-line stable JSON via records).
2. Add deterministic trace path in `RandomGroupProduceResolver` for random-group expansion.
3. Add `ra_debug_plan` (daily decisions + apply/skip reasons), support `--json`.
4. Add `ra_debug_diff` (previous vs current decisions diff), support `--full` and `--json`.
5. Ensure outputs are copy/paste friendly and stable ordered.

### Phase 6 — Persist daily decisions snapshot (save state)
1. Extend per-save state schema to store previous/current <c>DailyFarmAnimalsDecisions</c> snapshots.
2. Load saved snapshots on <c>SaveLoaded</c> and expose them to the debug commands (diff across sessions).
3. Update snapshots when computing today’s decisions; persist on <c>Saving</c>.
4. Migrate SchemaVersion=1 → 2 without crashing (null snapshots).
5. Keep snapshots copy/paste friendly (stable ordering for JSON dumps).

## Phase 7 — Produce system improvements (determinism, validation, caching)
1. Centralize deterministic seed inputs via `RoosterSeedPolicy`.
2. Add `--seed <int>` override for `ra_debug_produce` and `ra_debug_plan`.
3. Add reproducibility hash (`reproHash`) to JSON dumps (produce + plan/decisions).
4. Add weight auditing to `--full` outputs (totals + anomalies list).
5. Add deterministic resolver caching (random-group expansion) with cache stats and invalidation hooks.
6. Bound resolver caches using LRU eviction and expose capacity/eviction stats in `--full`/`--json`.
7. Add regression/property tests for determinism invariants and cache behavior.
