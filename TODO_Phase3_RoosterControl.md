# Phase 3 RoosterControl Migration - TODO

## Step 1: Update RoosterCoordinator.cs
- [x] 1.1 Add using statements (Mikew.NestCore.Helpers.Logging, alias for ModRoosterControlCategory)
- [x] 1.2 Add ILog field
- [x] 1.3 Initialize _log in constructor
- [x] 1.4 Migrate PrintStatus() Monitor.Log calls
- [x] 1.5 Migrate DumpStateCommand() Monitor.Log calls
- [x] 1.6 Migrate OnNextTick() Monitor.Log calls

## Step 2: Update RoosterCoordinator.CatalogCommands.cs
- [ ] 2.1 Add using statements
- [ ] 2.2 Migrate all Monitor.Log calls to use scoped logging (Commands category)

## Step 3: Update RoosterCoordinator.Validation.cs
- [ ] 3.1 Add using statements
- [ ] 3.2 Migrate validation Monitor.Log calls (General category)

## Step 4: Update RoosterCoordinator.Planning.cs
- [ ] 4.1 Add using statements
- [ ] 4.2 Migrate planning Monitor.Log calls (Planning category)

## Step 5: Update RoosterCoordinator.Gmcm.cs
- [ ] 5.1 Add using statements
- [ ] 5.2 Migrate GMCM Monitor.Log calls (Gmcm category)

## Step 6: Update CatalogManager.cs
- [ ] 6.1 Add using statements
- [ ] 6.2 Migrate Monitor.Log calls (Catalogs category)

## Step 7: Update ConfigManager.cs
- [ ] 7.1 Add using statements
- [ ] 7.2 Migrate Monitor.Log calls (Config/SaveData category)

## Step 8: Update LegacyConfigAdapter.cs
- [ ] 8.1 Add using statements
- [ ] 8.2 Migrate Monitor.Log calls (SaveData category)

## Step 9: Build and Verify
- [ ] 9.1 Build project
- [ ] 9.2 Fix any compilation errors
- [ ] 9.3 Mark Phase 3 complete in TODO_CategoryEnums.md

