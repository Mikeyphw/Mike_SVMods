# TODO — EggsInWild (WildAnimals)

## Next (after Phase 8)
- [ ] Phase 7 UX improvements:
  - [ ] Bulk add/update/remove workflows that fit GMCM static-page limits.
  - [ ] Surface richer validation warnings in GMCM (unknown modIds, empty/duplicate keys, keys not present in game data when available).
- [ ] Index-page navigation improvements that fit GMCM static-page limits (no live search).
- [ ] Keep build/warning budget tight (no obsolete legacy support; avoid config churn).

## Notes
- Keep changes inside `EggsInWild` and prefer NestCore helpers for GMCM plumbing.
- Avoid fragile syntax edits in `WeggCommandDispatcher.cs` (braces/strings/switch expressions).
