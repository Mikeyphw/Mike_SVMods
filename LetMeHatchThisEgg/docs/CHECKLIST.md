# LetMeHatchThisEgg CHECKLIST

## Phase 0 — Baseline + Guardrails ✅
- [x] Fail-fast if BOAF missing; assert no local BOAF copies.
- [x] Centralize core context creation (`CoreContext`).

## Phase 1 — Coordinator skeleton ✅
- [x] Add `LmhCoordinator` to own mod lifecycle wiring.
- [x] Move wiring out of `ModEntry` into coordinator.

## Phase 2 — Content routing + loaders/editors ✅
- [x] Document inventory + freeze rules (`Docs/phase2_content_routing_inventory.md`).
- [x] Add mod-specific loaders (`Infrastructure/Content/Loaders/*`).
- [x] Add mod-specific editors (`Infrastructure/Content/Editors/*`).
- [x] Centralize registrations in `LetMeHatchContentPipeline`.
- [x] Coordinator delegates `AssetRequested` to pipeline.

## Phase 3 — Layered architecture (Core/Application/Infrastructure) ✅
- [x] Extract pure rules into `Core/` (no SMAPI/Game1).
- [x] Add Application use-cases/orchestrators as editor/loader entrypoints.
- [x] Add Infrastructure gateways (WEH-style adapters).
- [x] Rewire editors/loaders to call Application (and gateways) instead of `GameAssetManager`.
- [x] Add `Reports/PreBuildValidation` (Core purity + BOAF-duplicate checks).

## Phase 4 — Invalidation + scheduling ✅
- [x] Use BOAF `AssetInvalidationBatcher` + next-tick scheduling for invalidations.
- [x] Centralize invalidate-all behavior in the asset pipeline.
- [x] Remove direct `GameContent.InvalidateCache` remnants in `Managers/*`.

## Phase 5 — Save-data + config polish (N/A)
- [x] N/A — mod does not persist state beyond runtime.
- [x] N/A — no legacy compatibility shims required.

## Phase 6 — Tests
- [x] Add testable Core services + unit tests under `Tests/` (separate Tests zip rules).
- [ ] Validate editor/service behavior with fixtures.

## Phase 7 — Packaging + release
- [ ] Verify no local duplicates remain.
- [ ] Ensure packaging + versioning + smoke tests.

