# TODO — EggsInWild (WildAnimals)

## Next (after Phase 8)
- [ ] Phase 7 UX improvements:
  - [ ] Bulk add/update/remove workflows that fit GMCM static-page limits.
    - [x] Bulk stage + preview + apply (Paste: `modId | csvKeys`; `*` to reset/remove).
    - [x] Bulk apply quick-fix toggles (trim/dedupe/drop unknown).
    - [ ] Export current effective blocks as `modId | csvKeys` for round-trip editing.
  - [ ] Surface richer validation warnings in GMCM (unknown modIds, empty/duplicate keys, keys not present in game data when available).
    - [x] ExtraModAnimalKeys: GMCM shows inline validation warnings (unloaded modIds, blank/whitespace/dupes, unknown FarmAnimalKeys when Data/FarmAnimals is available).
  - [x] Dev: `wegg wildanimals dump_effective` includes validation warnings (unloaded modIds + unknown FarmAnimalKeys when Data/FarmAnimals is available).
- [x] Index-page navigation improvements that fit GMCM static-page limits (no live search).
  - [x] Group index pages: All, A–F, G–L, M–R, S–Z, Other.
- [ ] Keep build/warning budget tight (no obsolete legacy support; avoid config churn).

## Notes
- Keep changes inside `EggsInWild` and prefer NestCore helpers for GMCM plumbing.
- Avoid fragile syntax edits in `WeggCommandDispatcher.cs` (braces/strings/switch expressions).
