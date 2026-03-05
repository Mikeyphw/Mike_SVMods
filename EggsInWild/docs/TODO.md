# TODO — EggsInWild (WildAnimals)

## Next (after Phase 8)
- [ ] Phase 7 UX improvements:
  - [ ] Bulk add/update/remove workflows that fit GMCM static-page limits.
    - [x] Bulk stage + preview + apply (Paste: `modId | csvKeys`; `*` to reset/remove).
    - [x] Bulk apply quick-fix toggles (trim/dedupe/drop unknown; also applied to per-block edits).
    - [x] Bulk preview shows before/after and what quick-fixes will change (trim/dedupe/drop unknown).
    - [x] Bulk preview includes planned action summary (set/clear/no-op) + clearer per-modId actions.
    - [x] WildAnimals index shows group counts and flags staged bulk entries (with warn count).
    - [x] Clear staged bulk input toggle.
    - [x] Export current effective blocks as `modId | csvKeys` for round-trip editing.
    - [x] Copy exported effective blocks into Bulk stage (overwrite/append).
  - [x] Surface richer validation warnings in GMCM (unknown modIds, empty/duplicate keys, keys not present in game data when available).
    - [x] ExtraModAnimalKeys: GMCM shows inline validation warnings (unloaded modIds, blank/whitespace/dupes, unknown FarmAnimalKeys when Data/FarmAnimals is available).
  - [x] Dev: `wegg wildanimals dump_effective` includes validation warnings (unloaded modIds + unknown FarmAnimalKeys when Data/FarmAnimals is available).
- [x] Index-page navigation improvements that fit GMCM static-page limits (no live search).
  - [x] Group index pages: All, A–F, G–L, M–R, S–Z, Other (with counts).
- [x] Keep build/warning budget tight (no obsolete legacy support; avoid config churn).
  - [x] Phase 9E: dedupe bulk planning logic + shared export builder + reuse cached Data/FarmAnimals load.

## Notes
- Keep changes inside `EggsInWild` and prefer NestCore helpers for GMCM plumbing.
- Avoid fragile syntax edits in `WeggCommandDispatcher.cs` (braces/strings/switch expressions).


## Phase 10 — Bulk tooling parity
- [x] 10C: unit tests for bulk parsing + planning (set/clear/no-op).
- [x] 10D: dev commands `export_effective_bulk` + `validate_bulk`.
- [ ] Add dev-command-focused tests for export/validate output formatting.

## Phase 18C — GMCM migration
- [x] Replace pseudo-button fake bool GMCM controls with `Action(...)` on WildAnimals pages.
- [x] Keep `EiwGmcmMenu` as the GMCM orchestrator and preserve save-aware rebuild.
- [x] Remove duplicated Animal Progression page body from legacy `ModEntry.Gmcm.cs`.
- [x] Run full build gate after GMCM consolidation.

## Phase 18D — GMCM convention
- [x] Mark EggsInWild docs aligned with repo-wide GMCM convention.

## Phase 18E — Finalization
- [x] Final grep/review completed: no fake-bool action patterns remain.
- [x] Legacy `ModEntry.Gmcm*` inline layout partials removed; `EiwGmcmMenu + Pages/*` is canonical.
