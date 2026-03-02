# RoosterControl TODO

## Next: Phase 10 (late apply + validation)
- Late apply strategy:
  - Resolve `FarmAnimalRef` egg sources against final `Data/FarmAnimals` after all CP patches.
  - Ensure RoosterControl edits run at a safe late priority.
- Optional: add `ra_compat validate` (read-only) to report invalid `FarmAnimalRef` targets/indexes.

## After: Phase 11 (fertilized egg synthesis)
- If chosen provider has no fertilized eggs, clone fertilized variants into `Data/Objects`.
- Prefer native fertilized eggs if provided.

## After: Phase 12 (GMCM UI)
- Add compat selection UI + validation messaging.
