# RoosterAddon Refactor Checklist

This tracks the WildEggsHatching-style refactor plan and the shipped stabilization fixes.

## Phase 0 — Baseline + guardrails
- [x] Add `roosteraddon_dump_state` console command.
- [x] Add parity checklist doc.
- [ ] Capture a “before” dump and commit it as a baseline reference.
- [ ] Capture an “after each phase” dump and compare.

## Phase 1 — Stop-the-bleeding fixes
- [x] GMCM registers once (no `UpdateTicked` setup loop).
- [x] Fix probability math (`0%` truly means `0%`).
- [x] Remove file I/O from asset edits (`Data/FarmAnimals` / `Data/Objects` edit path).
- [x] Remove `InvalidateCache("Data/Objects")` from inside asset edits.
- [x] Optimize BetterTruffles tagging (no nested full scan).

## Phase 2 — Thin ModEntry
- [ ] Event router + coordinator.
- [ ] External mod adapters.

## Phase 3 — Typed config + migration
- [ ] `ModConfigV2` with validation + migration from legacy.

## Phase 4 — Content pipeline editors
- [ ] `SmapiContentCoordinator`
- [ ] `FarmAnimalsEditor`
- [ ] `ObjectsEditor`
- [ ] `TexturesEditor`

## Phase 5 — Core services + strategies
- [ ] Strategy-based hatching service.
- [ ] Luck adjustment service.

## Phase 6 — Deterministic daily plan + save data
- [ ] Daily plan computed once/day, persisted.

## Phase 7 — Data-driven catalogs
- [ ] Move rooster defs + produce groups + egg rules to JSON.

## Phase 8 — GMCM descriptor UI
- [ ] Descriptor-driven pages/sections.

## Phase 9 — Tests + cleanup
- [ ] Unit tests for core logic.
- [ ] Remove legacy paths.
