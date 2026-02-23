# WildEggsHatching Migration TODO

## Category Mapping
- `Application/Spawning/*`: spawning
- `Application/Progression/*`: progression
- `Application/AnimalNotes/*`: animal-notes
- `Application/DesertTomb/*`: desert-tomb
- `Application/Animals/*`: spawning
- `Application/Facade/*`: general
- `Infrastructure/Commands/*`: commands
- `Infrastructure/ModData/*`: save-data
- `Infrastructure/Diagnostics/*`: verify
- `Infrastructure/DesertTomb/Diagnostics/*`: verify
- `Infrastructure/DesertTomb/Monsters/*`: desert-tomb
- `ModEntry.cs`: general

## File Migration Checklist

### High Priority (8 sites each)
- [x] EggSpawnService.cs (spawning)
- [x] ModFacade.cs (general)

### High Priority (7 sites)
- [x] AnimalNotesOrchestrator.cs (animal-notes)

### Medium Priority (6 sites)
- [ ] ModDataLoader.cs (save-data)
- [ ] WeggConsoleCommands.cs (commands)
- [ ] AnimalNotesClientObserver.cs (animal-notes)

### Medium Priority (5 sites)
- [ ] ChosenEggAssignments.cs (spawning)

### Lower Priority (3-4 sites)
- [ ] DesertTombRuntime.cs (desert-tomb)
- [ ] WeggVerifier.cs (verify)
- [ ] DesertTombMonsterFactory.cs (desert-tomb)
- [ ] DesertTombDiagnostics.cs (verify)
- [ ] SpawnPlanner.cs (spawning)
- [ ] ProgressionService.cs (progression)
- [ ] AnimalNotesStartupValidator.cs (animal-notes)

### Low Priority (1-2 sites)
- [ ] SpawnWeatherHistory.cs (spawning)
- [ ] ModEntry.cs (general)
- [ ] AnimalCatalogBuilder.cs (content)
- [ ] WildEggSpawnOrchestrator.cs (spawning)
- [ ] TombRoleEnforcer.cs (desert-tomb)
- [ ] TombPopulationManager.cs (desert-tomb)
- [ ] DesertTombSpawnExecutor.cs (desert-tomb)

## Migration Pattern
For each `_log.Log(...)` call:
```csharp
// Before:
_log.Log("message", LogLevel.Info);

// After:
if (_log is SmapiLog slog)
    slog.LogScoped(ScopedLogCatalog.ModWe, "category", "message", LogLevel.Info);
else
    _log.Log("message", LogLevel.Info);
```

