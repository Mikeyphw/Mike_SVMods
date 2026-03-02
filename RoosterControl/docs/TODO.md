# RoosterControl TODO

## Next: Phase 9B (cycling + apply)
- Decide rooster provider interaction:
  - enable/disable multiple providers (union), or single active provider for roosters.
- Implement `ra_compat cycle-*` commands.
- Apply egg selections to:
  - daily plan decisions
  - fertilized egg cloning mapping
- Apply rooster provider keys/roles to:
  - coop scanning
  - breeding assignments

## After: Phase 10 (late apply)
- Resolve FarmAnimalRef egg sources against final `Data/FarmAnimals` (post-CP).
- Ensure our edits run at a safe late priority.

## After: Phase 11 (fertilized egg synthesis)
- If chosen provider has no fertilized eggs, clone fertilized variants into `Data/Objects`.

## After: Phase 12 (GMCM UI)
- Add compat selection UI + validation.
