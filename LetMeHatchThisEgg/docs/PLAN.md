# LetMeHatchThisEgg PLAN

BOAF-first: use BOAF for plumbing (routing/registration/batching/logging/config/save-data/constants). No local duplicates.

## Phase 0 — Baseline + Guardrails ✅
1. ✅ Fail-fast if BOAF missing; assert no local BOAF copies.
2. ✅ Centralize core context creation (`CoreContext`).

## Phase 1 — Coordinator skeleton ✅
1. ✅ Add `LmhCoordinator` to own mod lifecycle wiring.
2. ✅ Move wiring out of `ModEntry` into coordinator.

## Phase 2 — Content routing + loaders/editors ✅
1. ✅ Document inventory + freeze rules (`Docs/phase2_content_routing_inventory.md`).
2. ✅ Add mod-specific loaders (`Infrastructure/Content/Loaders/*`).
3. ✅ Add mod-specific editors (`Infrastructure/Content/Editors/*`).
4. ✅ Centralize registrations in `LetMeHatchContentPipeline`.
5. ✅ Coordinator delegates `AssetRequested` handling to pipeline (no direct asset logic in event handler).

**Completion note (2026-02-19):** replaced LMH-local plumbing with BOAF coordinator + BOAF config/logging/constants.

## Phase 3 — Layered architecture (Core/Application/Infrastructure) ✅
Inspired by WildEggsHatching: **Core** = pure domain, **Application** = orchestrators/use-cases, **Infrastructure** = SMAPI/Game adapters.

1. ✅ (3.1) Extract pure rule helpers into Core (no SMAPI/Game1 types).
   - Clone-id formatting/parsing: `Core/Cloning/CloneIdService`
   - Egg selection rules: `Core/Breeding/EggSelectionService`
2. ✅ (3.2) Add Application orchestrators/use-cases as stable entrypoints for workflows used by runtime + editors/loaders.
   - `Application/UseCases/CloneIdUseCase`
   - `Application/UseCases/EggSelectionUseCase`
   - `Application/LmhApplicationServices` composed in coordinator and available in `ContentContext`
3. ✅ (3.3) Introduce Infrastructure “game gateways” (interfaces + SMAPI/Game1 implementations) so Core/Application don’t touch SMAPI/Game1.
   - `Application/Gateways/*` interfaces (GameData, World, Clock, Assets)
   - `Infrastructure/Gateways/*` SMAPI/Game1 adapters
   - composed in coordinator and exposed via `LmhApplicationServices.Gateways`
4. ✅ (3.4) Rewire editors/loaders to call **Application** (and gateways) instead of `GameAssetManager`/`Game1`.
   - Editors now call `context.App.*Edits` interfaces
   - Loaders now call `context.App.AssetLoads`
5. ✅ (3.5) Add `Reports/PreBuildValidation.md` checks:
   - Added `Reports/PreBuildValidation.md` + `Tools/Validate-Core.ps1` + `Tools/validate-core.sh`
   - Completion note (3.5): scripts validate Core purity + prevent BOAF duplicates (2026-02-19)
   - Core has no SMAPI/Stardew references
   - no BOAF-local duplicates reappear

## Phase 4 — Invalidation + scheduling ✅
1. ✅ Use BOAF `AssetInvalidationBatcher` + `NextTickScheduler` for all game-content invalidations. (2026-02-19)
2. ✅ Centralize invalidate-all behavior in the asset pipeline (no manual path loops in commands/coordinator). (2026-02-19)
3. ✅ Remove direct `GameContent.InvalidateCache` remnants in `Managers/*` and route invalidation through BOAF batcher only. (2026-02-19)

## Phase 5 — Save-data + config polish (N/A)
1. ✅ N/A — mod does not persist state beyond SMAPI runtime; no migration needed.
2. ✅ N/A — legacy compatibility shims not required/used.

**Completion note (2026-02-19):** Phase 5 marked N/A per project scope (no persisted state).

## Phase 6 — Tests
1. ✅ Add testable Core services + unit tests under `Tests/` (separate zip rules).
2. Validate editor/service behavior with fixtures.

**Completion note (20260219-12):** Added xUnit tests for CloneIdService and EggSelectionService.


## Phase 7 — Packaging + release
1. Verify no local duplicates remain.
2. Ensure full mod packaging and versioning.


