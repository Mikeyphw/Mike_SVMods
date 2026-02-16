# RoosterAddon Refactor Checklist (Phase 0+)

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

## Next phases (planned)
- [x] Phase 2: Thin `ModEntry` + coordinator/router (event bodies in coordinator, SaveData store scaffold).
- [ ] Phase 3: Typed config + migration.
- [ ] Phase 4: Content pipeline editors (FarmAnimals/Objects/Textures).
- [ ] Phase 5+: Daily plan + data catalogs + tests.
