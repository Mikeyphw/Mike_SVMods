# RoosterAddon Refactor Checklist

This file is a living checklist to keep refactors honest.

## Phase 0: Baseline & guardrails
- [ ] Capture a "known good" save for manual parity checks.
- [x] Add `roosteraddon_dump_state` console command (JSON snapshot).
- [x] Add parity checklist doc.
- [ ] Record baseline state dump files (before/after each phase).

## Phase 1: Stop-the-bleeding fixes
- [x] GMCM registers once on `GameLaunched` (no per-tick registration).
- [x] No file IO inside `AssetRequested` edits.
- [x] No cache invalidation inside asset edits (`InvalidateCache` deferred).
- [x] Fix percentage roll math (0% = never, 100% = always).
- [x] Remove unintended `female.Length` chance factor.
- [x] Optimize BetterTruffles tagging (HashSet + single pass).

## Phase 2: Typed config only (no legacy compatibility)
- [x] Typed config lives under `Framework/Config/*`.
- [x] Remove legacy config artifacts (`Framework/ModConfig.cs`, `Framework/Config/LegacyConfigAdapter.cs`).
- [x] Schema handling is self-healing (backup + upgrade) with no "no migrator" warnings.

## Phase 3: Catalog-driven produce + random groups
- [x] `ProduceCatalog.RandomGroups` added and persisted in `data/catalogs/produce.json`.
- [x] Produce/egg IDs resolved via `IProduceResolver` (`RandomGroupProduceResolver`).
- [x] Remove hardcoded produce randomization (`RoosterControl.ProduceTranslation`).
- [x] Eggs are built from the rooster catalog and resolved into concrete IDs.

## Remaining phases (see ROOSTERADDON_REFACTOR_PLAN.md)
- [x] Phase 4: Asset edit boundaries + services
  - [x] `Data/FarmAnimals` editor only mutates asset data (no reloads, no invalidation, no state dumps).
  - [x] Daily hatching/female assignment/update flow runs outside `AssetRequested` (day start pipeline).
  - [x] Location-change refresh (when enabled) runs outside `AssetRequested` (next-tick pipeline).
  - [x] Continue isolating any remaining coordinator leakage inside asset editors/services.
  - [x] Remove IRoosterAssetEdits interface (editors use narrow constructor deps only).
  - [x] Move BetterTruffles tagging logic into a dedicated service.
- [x] Phase 5: Daily plan pipeline (planner is pure; applier is deterministic).
- [x] Phase 6: Legacy purge + service hardening.
  - [x] Fail-fast validation for catalogs (sanitize + validate + fatal errors).
  - [x] Fix coordinator validation/planning compile breaks and remove dead legacy enums.
- [ ] Phase 7: Tests (xUnit) + deterministic harness.
- [x] Phase 8: JSON catalogs (all knobs).
  - [x] `hatching.json` general section includes all global knobs (percentages, toggles, update rate).
  - [x] `hatching.json` rooster entries include all per-rooster knobs (including `EditAllFemales`).
  - [x] Catalog apply modes (Seed/Repair/Merge/Authoritative) apply/repair these values predictably.
  - [x] Catalog defaults + migrator keep existing installs forward-compatible.

- [x] Phase 8.1: Catalog tooling + validation/reload/export.
  - [x] `ra_catalog validate [--fix]` (per-file OK/MIGRATED/ERROR output).
  - [x] `ra_catalog reload` (apply catalogs to config + invalidate assets).
  - [x] `ra_catalog export [snapshot]` (always writes backups when overwriting).
  - [x] `ra_catalog diff` (dry-run unified diff to file).
  - [x] `ra_catalog mode` (get/set CatalogApplyMode).
- [ ] Phase 9: Descriptor-driven GMCM via BOAF public API.
- [ ] Phase 10: CI build gate + release hardening.
