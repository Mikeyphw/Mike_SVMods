# EggsInWild

This directory contains the current architecture for EggsInWild.

## Architecture Overview

The refactor follows **clean architecture** principles with clear separation of concerns:

```
refactor/
├── Core/                    # Domain models, events, enums (pure C#, no game dependencies)
│   ├── Models/             # Data classes (PlayerWildState, SpawnContext, etc.)
│   ├── Enums/              # Game enums (ProgressionStep, LuckyTypes)
│   └── Events/              # Domain events (AnimalUnlockedEvent, etc.)
├── Data/                    # Data loading and access
├── State/                   # State persistence (PlayerStateRepository)
├── Progression/             # Progression and unlocking logic
├── Spawning/                # Egg spawning system
├── Tracking/                # Hatch detection
├── Notes/                   # Note spawning and reading
└── Coordination/            # Service wiring and event coordination
```

## Current Status (Agreement Baseline)

This `refactor` directory is a **work-in-progress implementation**, not the final architecture.

Implemented:
- core models/events/enums
- event bus flow
- spawn/progression/tracking/note services
- coordinator wiring
- initial save repository bridge

Not yet finalized:
- SMAPI routing boundary (`SmapiEventRouter`) is not isolated
- `ModEntry` still owns substantial substantial orchestration
- some services still rely on static game APIs
- save DTO versioning/migration (`V1/V2`) is not complete

Use this folder as the implementation baseline when applying:
- `REFACTOR_INTEGRATION_PLAN.md` (main WildEggs boundary migration)
- `DESERT_TOMB_MODULE_PLAN.md` (separate DesertTomb module rewrite)

## Key Changes

### 1. Simplified Multipliers

**OLD:** Scattered across 6+ sources
```csharp
RandomChanceModifiers + DailyLuckModifiers + DifficultyModifierMult + MineMultiplier + BaseDailyLuckModifier
```

**NEW:** 3-layer system in `SpawnChanceCalculator.cs`
```csharp
// Layer 1: Base chance (0.15)
// Layer 2: Egg type modifier (-0.13 for large magic, etc.)
// Layer 3: Small luck influence (-0.02 to +0.02)
```

### 2. Event-Driven Communication

**OLD:** Direct service calls
```csharp
spawnService.SpawnEgg();
unlockService.Unlock(animal);
```

**NEW:** Publish/Subscribe via EventBus
```csharp
eventBus.Publish(new EggSpawnedEvent(...));
// Services subscribe to events they care about
```

### 3. Single Responsibility

| Old Class (Lines) | New Services |
|-------------------|--------------|
| `ProgressionManager.cs` (700+) | `PhaseProgressionService.cs` + `UnlockService.cs` |
| `EggSpawning.cs` (1100+) | `EggSpawnService.cs` + `EggPlacementService.cs` |
| `WildListManager.cs` (300+) | `AnimalCatalog.cs` |

## Core Classes

### PlayerWildState (`Core/Models/PlayerWildState.cs`)
Centralized player progression state:
- Unlocked animals
- Discovered animals  
- Collected notes

### SpawnContext (`Core/Models/SpawnContext.cs`)
All data needed for spawn decisions:
- Base chance
- Egg type
- Daily luck
- Location info
- Area configuration

### SpawnResult (`Core/Models/SpawnResult.cs`)
Immutable result of spawn attempt:
- ShouldSpawn (bool)
- EggId, AnimalId, Location, Tile

### EventBus (`Core/Events/EventBus.cs`)
Simple event dispatcher:
- Subscribe<T>(Action<T>)
- Publish<T>(T)
- Unsubscribe<T>(Action<T>)

## Services

### SpawnChanceCalculator
Calculates spawn chance using:
1. Base chance from config
2. Egg type modifier
3. Daily luck influence

### EggSpawnService
Decides IF and WHAT should spawn:
- Pure logic, no side effects
- Returns SpawnResult

### EggPlacementService
Actually places eggs in the world:
- Only service that modifies Game1
- Publishes EggSpawnedEvent

### PhaseProgressionService
Manages 6-step progression:
- Evaluates unlock conditions
- Advances progression
- Publishes PhaseUnlockedEvent

### UnlockService
Handles animal unlocking:
- Subscribes to AnimalHatchedEvent
- Updates PlayerWildState
- Publishes AnimalUnlockedEvent

### AnimalTrackingService
Watches for hatching:
- Subscribes to update ticks
- Publishes AnimalHatchedEvent

### NoteService
Manages notes:
- Subscribes to progression events
- Spawns appropriate notes
- Tracks collected notes

### WildEggCoordinator
Main wiring class:
- Creates all services
- Injects dependencies
- Registers game events

## Migration Strategy

1. **Phase 1:** Boundary isolation (event router + `ModEntry` slimming) - in progress
2. **Phase 2:** Save migration (versioned DTOs + compatibility mapping) - in progress
3. **Phase 3:** Spawn isolation (remove remaining direct game coupling) - in progress
4. **Phase 4:** Progression isolation (remove temporary static access) - in progress
5. **Phase 5:** Asset/content isolation boundary - not started

## Event Flow Example

```
Day Started
    ↓
WildEggCoordinator.OnDayStarted()
    ↓
PhaseProgressionService.EvaluateProgression()
    ↓
EggSpawnService.TrySpawn(context)
    ↓
EggPlacementService.Place(result)
    ↓
EggSpawnedEvent Published
    ↓
AnimalTrackingService detects hatch
    ↓
AnimalHatchedEvent Published
    ↓
UnlockService unlocks animal
    ↓
AnimalUnlockedEvent Published
    ↓
NoteService may spawn new note
    ↓
PhaseProgressionService may advance step
    ↓
PhaseUnlockedEvent Published
    ↓
MailService sends appropriate mail
```

## Files Reference

| File | Purpose |
|------|---------|
| `Core/Models/PlayerWildState.cs` | Central player state |
| `Core/Models/ProgressionState.cs` | Progression step tracking |
| `Core/Models/SpawnContext.cs` | Spawn decision context |
| `Core/Models/SpawnResult.cs` | Spawn result |
| `Core/Enums/ProgressionStep.cs` | 6-step progression enum |
| `Core/Enums/LuckyTypes.cs` | Daily luck enum |
| `Core/Events/EventBus.cs` | Event dispatcher |
| `Core/Events/*.cs` | Domain events |
| `State/PlayerStateRepository.cs` | Save/load state |
| `Spawning/SpawnChanceCalculator.cs` | Chance math |
| `Spawning/EggSpawnService.cs` | Spawn decisions |
| `Spawning/EggPlacementService.cs` | World placement |
| `Spawning/WeightedTable.cs` | Weighted selection |
| `Progression/PhaseProgressionService.cs` | Progression logic |
| `Progression/UnlockService.cs` | Animal unlocking |
| `Tracking/AnimalTrackingService.cs` | Hatch detection |
| `Notes/NoteService.cs` | Note management |
| `Data/AnimalCatalog.cs` | Animal definitions |
| `Coordination/WildEggCoordinator.cs` | Service wiring |

## Next Steps

1. Isolate SMAPI event registration into a router boundary.
2. Move remaining `ModEntry` orchestration into coordinator/infrastructure adapters.
3. Complete save migration strategy (versioned DTOs and compatibility mapping).
4. Remove transitional manager dependencies after validation checks.
5. Keep DesertTomb migration separate under module plan phases.

## Notes

- DesertTomb is NOT included in this refactor (as requested)
- All classes have XML documentation comments
- EventBus is simple - no reflection or dynamic dispatch
- SpawnChanceCalculator replaces all multiplier chaos with 3 simple layers
- Each service has a single, clear responsibility
