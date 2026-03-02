# TODO — EggsInWild (WildAnimals)

## Now (next implementation work)
- [ ] Phase 1: scalar overrides + normalization + apply during load.
- [ ] Phase 2: GMCM per-animal editor for scalar fields.
- [ ] Phase 3: dev commands for status/set_enabled/dump_effective.
- [ ] Phase 4: per-animal diagnostics display (read-only sources + effective ids).

## Later / optional
- [ ] GMCM editing for complex collections (e.g. `ExtraModAnimalKeys`).
- [ ] UI enhancements that would require dynamic menu rebuilds (search/filter) — likely not worth it.
- [ ] Additional validation rules (range checks, richer error reporting).

## Notes
- Keep changes inside `EggsInWild` and prefer NestCore helpers for GMCM plumbing.
- Avoid fragile syntax edits in `WeggCommandDispatcher.cs` (braces/strings/switch expressions).
