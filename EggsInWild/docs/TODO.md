# TODO — EggsInWild (WildAnimals)

## Next (after Phases 1–5)
- [ ] GMCM editing for complex collections:
  - [x] `LocationTypes` editor UI (override schema+loader merge is done).
  - [x] `ExtraModAnimalKeys` editor UI (override schema+loader merge is done).
- [ ] Phase 8: guard against accidental config churn (only save config when normalization changes).
- [ ] Index-page navigation improvements that fit GMCM static-page limits (no live search).
- [ ] Add richer validation/range checks (price bounds, empty farm-animal list warnings surfaced in GMCM).

## Notes
- Keep changes inside `EggsInWild` and prefer NestCore helpers for GMCM plumbing.
- Avoid fragile syntax edits in `WeggCommandDispatcher.cs` (braces/strings/switch expressions).
