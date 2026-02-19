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
- [ ] Phase 4: Asset edit boundaries + services
  - [x] `Data/FarmAnimals` editor only mutates asset data (no reloads, no invalidation, no state dumps).
  - [x] Daily hatching/female assignment/update flow runs outside `AssetRequested` (day start pipeline).
  - [x] Location-change refresh (when enabled) runs outside `AssetRequested` (next-tick pipeline).
  - [ ] Continue isolating any remaining coordinator leakage inside asset editors/services.
- [ ] Phase 5: Daily plan pipeline + decision layer cleanup.
- [ ] Phase 6: Legacy purge + service hardening.
- [ ] Phase 7: Tests (xUnit) + deterministic harness.
- [ ] Phase 8: JSON catalogs (all knobs).
- [ ] Phase 8.1: Catalog tooling + validation/reload/export.
- [ ] Phase 9: Descriptor-driven GMCM via BOAF public API.
- [ ] Phase 10: CI build gate + release hardening.
