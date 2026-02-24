# TODO: Migrate to Category Enums

## Phase 1: NestCore Infrastructure (COMPLETE)
- [x] 1. Update ScopedLogCatalog.cs - Add enum types for each mod's categories
- [x] 2. Update SmapiLog.cs - Add enum overloads for LogScoped
- [x] 3. Update Logger.cs - Add enum overloads for LogScoped

## Phase 2: EggsInWild Migration (COMPLETE)
- [x] 4. Update all call sites to use enum categories
- [x] Categories: progression, spawning, animal-notes, desert-tomb, content, verify, commands, save-data, integrations, general

## Phase 3: RoosterControl Migration
- [ ] 5. Replace Monitor.Log() with scoped logging
- [ ] 6. Update all call sites to use enum categories
- [ ] Categories: catalogs, hatching, planning, farmanimals, assets, gmcm, commands, save-data, integrations, general

## Phase 4: LetMeHatch Migration (COMPLETE)
- [x] 7. Update _log.Log() calls to scoped logging
- [x] 8. Update all call sites to use enum categories
- [x] Categories: cloning, breeding, assets, content-pipeline, gmcm, commands, integrations, runtime, general

## Phase 5: NestCore Usage (COMPLETE)
- [x] 9. Update NestCore's own logging to use enums
- [x] Categories: gmcm, config, content, save-data, integrations, runtime, general

