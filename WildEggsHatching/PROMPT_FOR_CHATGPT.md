# Prompt for ChatGPT - WildEggsHatching Refactoring Assistance

---

## Context

I have a Stardew Valley mod called "WildEggsHatching" that has two main features:
1. **Wild Eggs** - A system for spawning wild eggs that hatch into various animals
2. **Desert Tomb** - A mineshaft dungeon with custom monsters

I am in the process of refactoring this mod but the work is **incomplete**. I need your help to:
1. Properly complete the WildEggs refactoring
2. Create a detailed plan to refactor the DesertTomb feature

---

## Project Structure

### Original Code (UNCHANGED - DO NOT MODIFY)
**Location:** `/home/mike/Documents/Projects/WildEggsHatching/WildEggsHatching_orig/`

This folder contains the **original, working code** that currently compiles and runs in the game. It includes:
- `WildEggsHatching_orig/ModEntry.cs` - Main mod entry point
- `WildEggsHatching_orig/DesertTomb/` - Desert Tomb feature (TombHandler.cs, LevelGenerator.cs, SpawnRateCalculator.cs, etc.)
- `WildEggsHatching_orig/Services/EggSpawning.cs` - Original egg spawning logic (~1100 lines)
- `WildEggsHatching_orig/Services/NoteSpawning.cs` - Junimo note spawning
- `WildEggsHatching_orig/Managers/WildListManager.cs` - Animal list management
- `WildEggsHatching_orig/Managers/GameAssetManagement/` - Content Patcher editors
- `WildEggsHatching_orig/ModData/WildAnimalsList.json` - ~100 animals data

### Refactor In Progress
**Location:** `/home/mike/Documents/Projects/WildEggsHatching/refactor/`

This folder contains the **incomplete refactoring** work. Current status:

**What's DONE:**
- Basic project structure with separated concerns
- `ModEntry.cs` has no game logic (just forwards events)
- Domain services created (Spawning, Tracking, Progression, etc.)
- Infrastructure layer (IGameStateReader, ISaveDataRepository, etc.)
- Content editor infrastructure

**What's MISSING (CRITICAL):**
1. **AnimalCatalog** (`refactor/Data/AnimalCatalog.cs`) - EMPTY
   - Needs to load ~100 animals from JSON like original `WildListManager.cs` does
   - Needs mod compatibility checking

2. **EggSpawnService** (`refactor/Spawning/EggSpawnService.cs`) - SIMPLIFIED
   - Only does single-location spawning
   - Missing: multi-location spawning, location packs, spawn areas, area conditions (seasons, weather, GameStateQuery)

3. **NoteSpawning** (`refactor/Notes/NoteService.cs`) - PARTIAL
   - Needs junimo note spawning when animals hatch

4. **AnimalTrackingService** (`refactor/Tracking/AnimalTrackingService.cs`) - PARTIAL
   - Missing: hatch detection, tracking animals needing notes

---

## Task 1: How to Complete the WildEggs Refactoring

Please analyze REFACTOR_INCOMPLETE.md and tell me:

1. **What exactly needs to be implemented** to complete the refactor? Be specific about:
   - What methods/classes need to be added to AnimalCatalog
   - What spawning logic needs to be ported from the original EggSpawning.cs (1100+ lines)
   - What tracking logic needs to be ported from NewAnimalMonitoring.cs
   - What note spawning logic needs to be ported from NoteSpawning.cs

2. **What's the proper approach** to integrate the JSON loading (LocationPacks.json, SpawnAreas.json, SpawnParameters.json, WildAnimalsList.json) into the refactored architecture?

3. **Are there any architectural concerns** with the current refactor that might cause issues when porting the complex spawning logic?

---

## Task 2: DesertTomb Refactoring Plan

The DesertTomb feature is in `WildEggsHatching_orig/DesertTomb/` and needs a complete refactor. According to DESERT_TOMB_MODULE_PLAN.md, the plan includes:

### Current Problems in Original DesertTomb Code:
1. Triggering too broad (triggers on any mines warp, re-runs constantly)
2. Duplicate lifecycle calls
3. Hardcoded teleport after generation
4. API mismatch risks
5. Difficulty filtering logic likely inverted
6. Allowed-monsters config ignored
7. Wanderer spawning capped accidentally
8. Hardcoded level type input
9. Hardcoded areas in TombHandler.cs (not data-driven)
10. Uses Game1.random (not deterministic)

### Proposed Architecture:
```
Features/
├── DesertTomb/
│   ├── DesertTombModule.cs
│   ├── Application/
│   │   ├── DesertTombCoordinator.cs
│   │   ├── DesertTombActivationService.cs
│   │   └── DesertTombDiagnosticsService.cs
│   ├── Domain/
│   │   ├── Models/ (TombState, TombRunContext, TombTemplate, SpawnPlan, SpawnBudget)
│   │   ├── Progression/ (TombProgressionService, TombPhase, ITombCondition)
│   │   ├── Generation/ (TemplateResolver, SpawnPlanner)
│   │   └── Spawning/ (MonsterProfile, IMonsterSpawner, MonsterFactory)
│   └── Infrastructure/
│       ├── TombGameBridge.cs
│       ├── TombMailService.cs
│       ├── LevelMutator.cs
│       └── SpawnApplier.cs
```

### Migration Phases:
- Phase 0: Correctness fixes (in original code first)
- Phase 1: Modular boundary (IModule, ModuleContext, ModuleManager)
- Phase 2: Deterministic context (seeded RNG)
- Phase 3: Data templates (move hardcoded areas to JSON)
- Phase 4: Spawn planner rewrite (two-pass allocation)
- Phase 5: Monster factory rewrite (profile-driven)
- Phase 6: Legacy removal

---

## Please Provide:

1. **Detailed step-by-step instructions** on how to complete the WildEggs refactor, specifically:
   - What to add to AnimalCatalog
   - How to port the full spawning logic
   - How to integrate JSON data loading

2. **A comprehensive DesertTomb refactoring plan** that:
   - Addresses all 10+ correctness issues
   - Provides specific code changes needed for each phase
   - Explains how to create the module boundary
   - Details the deterministic generation system
   - Explains the data-driven template approach

---

## Key Files Reference

### For WildEggs Refactor Completion:
- `REFACTOR_INCOMPLETE.md` - Detailed list of what's missing
- `refactor/Data/AnimalCatalog.cs` - Empty catalog
- `refactor/Spawning/EggSpawnService.cs` - Simplified spawner
- `WildEggsHatching_orig/Managers/WildListManager.cs` - Original animal loading
- `WildEggsHatching_orig/Services/EggSpawning.cs` - Original spawning (1100+ lines)
- `WildEggsHatching_orig/Services/NewAnimalMonitoring.cs` - Original tracking
- `WildEggsHatching_orig/Services/NoteSpawning.cs` - Original notes

### For DesertTomb Refactor:
- `refactor/WildEggsRefactorDocs/DESERT_TOMB_MODULE_PLAN.md` - Full plan
- `WildEggsHatching_orig/DesertTomb/Core/TombHandler.cs` - Hardcoded areas (main problem)
- `WildEggsHatching_orig/DesertTomb/Generation/LevelGenerator.cs` - Generation logic
- `WildEggsHatching_orig/DesertTomb/Calculation/SpawnRateCalculator.cs` - Spawn rates

