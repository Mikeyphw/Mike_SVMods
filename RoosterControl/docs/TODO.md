# TODO — RoosterControl

- [x] Ensure GMCM option IDs are stable and documented.
- [x] Verify all debug/diagnostic/logging toggles are only on the `debug` page.
- [x] Add/verify i18n keys for all pages/options (`config.*`).
- [x] Decide v1 scope for produce tables (minimal vs full 4-slot editor).
- [x] Ensure dynamic female list is stable across rebuilds (consistent ordering).
- [x] Switch RoosterControl dev symbol from `MIKE_DEV` to `RCTRL_DEV`.



## Rooster Produce System
- [x] Phase 3: weight/percent semantics cleanup + validation warnings.
- [x] Phase 3: validate random-group references (missing/cycles) on catalog load.
- [x] Phase 4: consolidate FarmAnimals mutations into the asset editor pipeline (steps 1–2).

- [x] Phase 4: batch invalidations via NestCore next-tick helpers.
- [x] Phase 4: add dev-gated pipeline summary console command (`ra_debug_pipeline`).

- [x] Phase 5: upgrade `ra_debug_produce` with `--full` + `--json` and random-group expansion trace.
- [x] Phase 5: add `ra_debug_plan` (apply/skip reasons) with `--json`.
- [x] Phase 5: add `ra_debug_diff` (prev vs current diff) with `--full` + `--json`.

- [x] Phase 6: persist daily decisions snapshot in save state (diff across sessions).

## Phase 7 — Produce system improvements (follow-ups)
- Consider bounded/LRU caching for resolver caches if they grow too large.
- Expand anomaly reporting to cover missing/empty random groups and unresolved variable keys.
- Add property/regression tests for seeding invariants and cache correctness.
