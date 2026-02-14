# WildEggs Main Refactor Plan

## TL;DR
This plan defines a clean `ModEntry + Coordinator` architecture for the main WildEggs system (excluding DesertTomb).

Constraints enforced:
- Zero gameplay logic in `ModEntry`
- No `Game1` usage in domain code
- Save/load handled through repository only
- Mine level and luck injected via spawn context
- EventBus-driven progression/actions
- Strict infrastructure boundary for SMAPI and game side effects

---

## Current Agreement Snapshot (`refactor`)

This plan is a **target architecture**. Current implementation under `refactor` is a partial migration and maps as follows:

- Target `Application/` -> current `refactor/Coordination/`
- Target `Domain/` -> current `refactor/Core/`, `refactor/Spawning/`, `refactor/Progression/`, `refactor/Tracking/`, `refactor/Notes/`
- Target `Infrastructure/Save` -> current `refactor/State/PlayerStateRepository.cs` (still compatibility-oriented, not final DTO versioning)

Implemented in `refactor` now:
- Event bus and domain events
- Spawn core services (`SpawnChanceCalculator`, `EggSpawnService`, `EggPlacementService`)
- Progression core (`PhaseProgressionService`, unlock rules objects, `UnlockService`)
- Tracking and notes services
- Coordinator wiring (`refactor/Coordination/WildEggCoordinator.cs`)

Not yet aligned with final target:
- No dedicated `SmapiEventRouter` boundary yet
- Coordinator still registers SMAPI events directly
- Domain/progression still has legacy game/static dependencies in some places
- Save repository shape/versioning is not finalized (`ModSaveDataV1/V2` not introduced)
- `ModEntry.cs` still owns significant legacy orchestration

Agreement rule for this document:
- Treat section “Final Target Structure” as destination.
- Treat this snapshot as current state baseline for migration sequencing.

---

## Final Target Structure

```text
WildEggsHatching/
│
├── ModEntry.cs
│
├── Application/
│   └── WildEggCoordinator.cs
│
├── Domain/
│   ├── Spawning/
│   │   ├── EggSpawnService.cs
│   │   ├── SpawnContext.cs
│   │   ├── ISpawnModifier.cs
│   │   ├── MineLevelModifier.cs
│   │   └── LuckModifier.cs
│   │
│   ├── Progression/
│   │   ├── ProgressionService.cs
│   │   ├── PhaseDefinition.cs
│   │   └── IPhaseCondition.cs
│   │
│   ├── Animals/
│   │   ├── AnimalDefinition.cs
│   │   └── AnimalUnlockService.cs
│   │
│   └── Events/
│       ├── IGameEvent.cs
│       ├── EventBus.cs
│       ├── DayStartedEvent.cs
│       ├── EggSpawnedEvent.cs
│       └── AnimalUnlockedEvent.cs
│
├── Infrastructure/
│   ├── Save/
│   │   ├── ModSaveData.cs
│   │   ├── ModSaveDataV1.cs
│   │   ├── ModSaveDataV2.cs
│   │   └── SaveDataRepository.cs
│   │
│   ├── Game/
│   │   ├── GameStateReader.cs
│   │   ├── GameObjectPlacer.cs
│   │   └── GameMailService.cs
│   │
│   └── Smapi/
│       ├── SmapiEventRouter.cs
│       └── SmapiContentEditor.cs
```

---

## Layer Responsibilities

### `ModEntry.cs`
- Build infrastructure instances and coordinator.
- Wire SMAPI events to coordinator entry points only.
- Dispose/unregister events on cleanup.
- Must not contain progression/spawn/mail rules.

### `Application/WildEggCoordinator.cs`
- Own workflow orchestration (`OnSaveLoaded`, `OnDayStarted`, `OnDayEnding`, `OnWarped`).
- Build domain `SpawnContext` using `GameStateReader`.
- Persist save via repository at consistent checkpoints.
- Subscribe to domain events and map them to infrastructure actions.

### `Domain/*`
- Pure gameplay logic and event publication.
- No references to `Game1`, `IModHelper`, SMAPI event args, or content APIs.
- Input comes from coordinator/context DTOs only.

### `Infrastructure/*`
- Only place that touches SMAPI and `Game1`.
- Includes save adapters, object placement, quest/mail mutation, content editing.
- Converts domain intents/events into concrete game operations.

---

## Detailed Design

## 1) Clean `ModEntry` Template
`ModEntry` should be intentionally boring and stable.

```csharp
public sealed class ModEntry : Mod
{
    private WildEggCoordinator _coordinator = null!;
    private SmapiEventRouter _eventRouter = null!;

    public override void Entry(IModHelper helper)
    {
        // Infrastructure
        var saveRepository = new SaveDataRepository(helper);
        var gameReader = new GameStateReader();
        var objectPlacer = new GameObjectPlacer();
        var mailService = new GameMailService();
        var contentEditor = new SmapiContentEditor();

        // Domain event system
        var eventBus = new EventBus();

        // Application coordinator
        _coordinator = new WildEggCoordinator(
            eventBus,
            saveRepository,
            gameReader,
            objectPlacer,
            mailService,
            contentEditor
        );

        // SMAPI event routing
        _eventRouter = new SmapiEventRouter(helper.Events, _coordinator);
        _eventRouter.Register();
    }
}
```

Rules:
- No spawn chance calculations in `ModEntry`.
- No direct calls to `Game1.player.mailReceived` from `ModEntry`.
- No save key usage outside repository.

## 2) Coordinator Contract
Coordinator API should match SMAPI lifecycle needs:
- `OnGameLaunched()`
- `OnSaveLoaded()`
- `OnDayStarted()`
- `OnDayEnding()`
- `OnWarped(locationName, playerTile)`
- `OnAssetRequested(assetName, editorContext)`
- `Cleanup()`

Coordinator internals:
- Build context from `GameStateReader`.
- Call domain services.
- Execute side effects through infrastructure ports only.
- Save once per operation boundary (end of day, unlock event, hatch event).

## 3) EventBus Policy
EventBus is used for domain-to-application decoupling:
- Domain publishes: `EggSpawnedEvent`, `AnimalUnlockedEvent`, `PhaseAdvancedEvent`.
- Coordinator subscribes and dispatches to infrastructure actions.
- Avoid direct domain-to-infrastructure calls.

Minimum EventBus methods:
- `Subscribe<T>(Action<T> handler)`
- `Unsubscribe<T>(Action<T> handler)`
- `Publish<T>(T evt)`

## 4) Spawn Context and Modifiers
Mine level and luck are passed as data, not read from `Game1`.

`SpawnContext` must include:
- `MineLevel`
- `DailyLuck`
- `DaysPlayed`
- `CurrentProgressionStep`
- `LocationKey`
- `CandidateTiles`

Spawn pipeline:
1. Start with base chance.
2. Apply ordered modifiers (`ISpawnModifier` list).
3. Clamp chance.
4. Roll result.
5. Publish `EggSpawnedEvent` if successful.

Modifier examples:
- `MineLevelModifier`
- `LuckModifier`
- `ProgressionStepModifier`
- `EggTypeModifier`

## 5) Save Layer and Compatibility
Repository is the only persistence gateway.

Requirements:
- Preserve existing key `wildEggs-animalData`.
- Read old `ModData` shape and map to new `ModSaveDataV2`.
- Write `V2`; optionally dual-write compatibility payload during transition.
- Validate null/missing fields with safe defaults.

Suggested repository surface:
- `ModSaveData Load()`
- `void Save(ModSaveData data)`
- `void MigrateIfNeeded(ModSaveData loaded)`

## 6) Game Infrastructure Ports
Use small, explicit interfaces:
- `IGameStateReader`: mine level, day, luck, location snapshots, quest/mail status.
- `IGameObjectPlacer`: place egg/object at tile, verify tile validity.
- `IGameMailService`: queue/send/remove mail, mark received flags.
- `IContentEditor`: edit assets in `AssetRequested` with scoped handlers.

Implementation classes (SMAPI/Game1 aware):
- `GameStateReader`
- `GameObjectPlacer`
- `GameMailService`
- `SmapiContentEditor`

## 7) SMAPI Event Routing
`SmapiEventRouter` owns all `+=` and `-=`:
- Registers once.
- Routes events to coordinator methods.
- Unregisters in `Cleanup`.

Target routed events:
- `GameLoop.GameLaunched`
- `GameLoop.SaveLoaded`
- `GameLoop.DayStarted`
- `GameLoop.DayEnding`
- `Player.Warped`
- `Content.AssetRequested`
- `GameLoop.Saving` (if needed for checkpoint save)

No other class should subscribe directly unless explicitly approved.

## 8) Asset Editing Boundary
For `objects`, `machines`, `mail`, `quests`, `special orders`:
- Keep edit logic in infrastructure editor classes.
- Coordinator chooses *when* to invalidate/request edits.
- Domain decides *what* should be unlocked or available.

Do not encode progression conditions directly in `AssetRequested` handlers.

## 9) Migration Phases

### Phase 1: Introduce New Boundaries
- Add `Application`, `Domain`, `Infrastructure` folders.
- Add `WildEggCoordinator`.
- Add `SmapiEventRouter` and route existing events through it.
- Keep old managers temporarily behind coordinator calls.

### Phase 2: Save Migration
- Implement `SaveDataRepository` with old-key compatibility.
- Map from current `ModData` to new save DTO.
- Move all direct `WriteSaveData` calls into repository.

### Phase 3: Spawn Isolation
- Add `SpawnContext`, `ISpawnModifier`, `MineLevelModifier`, `LuckModifier`.
- Replace `Game1` reads in spawn logic with context values.
- Keep placement in `GameObjectPlacer`.

### Phase 4: Progression Isolation
- Move unlock checks into `ProgressionService` + `IPhaseCondition`.
- Publish events for unlock transitions.
- Move quest/mail writes into `GameMailService`.

### Phase 5: Asset/Content Isolation
- Move asset mutation logic out of `ModEntry` into `SmapiContentEditor`.
- Keep data flow: domain decision -> coordinator trigger -> content edit.

### Phase 6: Remove Legacy Coupling
- Delete direct `ProgressionManager.ModSaveData` access paths.
- Remove old manager responsibilities after parity verification.
- Reduce `ModEntry` to construction + router registration only.

### Phase Status vs `refactor` (Updated - 2024-01)
- Phase 1: ✅ DONE (SmapiEventRouter created, coordinator updated with DI)
- Phase 2: ✅ DONE (ModSaveDataV1/V2 created, repository updated)
- Phase 3: ✅ DONE (Basic - Seeded RNG implemented, Game1.random removed from spawn)
- Phase 4: ⏳ PARTIAL (PhaseProgressionService needs full 6-step conditions, quest tracking incomplete)
- Phase 5: ⏳ PARTIAL (SmapiContentEditor skeleton created, editors not yet ported)
- Phase 6: ❌ NOT STARTED (ModEntry not refactored, legacy removal pending)

## 10) Definition of Done
- `ModEntry.cs` has no gameplay rules.
- Domain assembly/files have no `Game1`, `IModHelper`, or SMAPI event args.
- Repository is sole save read/write path.
- Mine level/luck only enter domain via `SpawnContext`.
- Event subscriptions are centralized and cleaned up deterministically.
- Behavior parity confirmed for:
  - progression step advancement
  - egg spawning decisions and placement
  - mail/quest/special-order effects
  - asset edits for objects/machines/mail/quests

## 11) Forbidden Patterns
- Domain reading from `Game1`.
- Scattered save writes in random services.
- `ModEntry` handling progression or spawn logic.
- Progression logic mutating mail directly.
- Spawn chance code pulling mine level/luck from globals.

---

## Immediate Next Implementation Slice
1. Add `SmapiEventRouter` and delegate current event handlers to coordinator.
2. Add `SaveDataRepository` as single persistence point (existing key compatible).
3. Introduce `SpawnContext` creation in coordinator using `GameStateReader`.
4. Move one full flow end-to-end (`DayStarted -> TrySpawn -> Place -> Save`) through new layers before migrating the rest.
