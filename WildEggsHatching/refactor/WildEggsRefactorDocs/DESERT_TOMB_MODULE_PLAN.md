# DesertTomb Feature Module Plan

## TL;DR
DesertTomb becomes a first-class module with a strict boundary and a deterministic generation/spawn pipeline.

Core outcomes:
- correctness bugs fixed first (before architecture changes)
- no tomb gameplay logic in `ModEntry`
- no `Game1` in tomb domain/services
- deterministic tomb runs (seeded and reproducible)
- data-driven templates (areas/capacity/rules from JSON)
- modular runtime stages with clear ownership
- safe diagnostics and failure isolation

---

## Agreement With Main Refactor

This document is aligned with `REFACTOR_INTEGRATION_PLAN.md` and current `refactor` status:

- WildEggs refactor currently lives in `refactor` and is mid-migration.
- DesertTomb is intentionally treated as a separate feature module, not merged into `refactor` internals.
- Both plans use the same boundary rules:
  - `ModEntry` only forwards lifecycle events
  - domain logic avoids direct `Game1`/SMAPI references
  - infrastructure owns side effects
  - save access goes through repository boundaries

Current-state note:
- DesertTomb runtime is still largely legacy and coupled to `ModEntry`.
- This plan starts with correctness fixes, then modular isolation, then deterministic/data-driven rewrite.

---

## Final Target Structure

```text
WildEggsHatching/
├── ModEntry.cs
├── Core/
│   └── Modules/
│       ├── IModule.cs
│       ├── IModuleMetadata.cs
│       ├── ModuleContext.cs
│       └── ModuleManager.cs
├── Features/
│   ├── WildEggs/
│   │   └── WildEggModule.cs
│   └── DesertTomb/
│       ├── DesertTombModule.cs
│       ├── Application/
│       │   ├── DesertTombCoordinator.cs
│       │   ├── DesertTombActivationService.cs
│       │   └── DesertTombDiagnosticsService.cs
│       ├── Domain/
│       │   ├── Models/
│       │   │   ├── TombState.cs
│       │   │   ├── TombRunContext.cs
│       │   │   ├── TombTemplate.cs
│       │   │   ├── SpawnPlan.cs
│       │   │   └── SpawnBudget.cs
│       │   ├── Progression/
│       │   │   ├── TombProgressionService.cs
│       │   │   ├── TombPhase.cs
│       │   │   └── ITombCondition.cs
│       │   ├── Generation/
│       │   │   ├── TemplateResolver.cs
│       │   │   └── SpawnPlanner.cs
│       │   └── Spawning/
│       │       ├── MonsterProfile.cs
│       │       ├── IMonsterSpawner.cs
│       │       └── MonsterFactory.cs
│       └── Infrastructure/
│           ├── TombGameBridge.cs
│           ├── TombMailService.cs
│           ├── LevelMutator.cs
│           └── SpawnApplier.cs
├── Infrastructure/
│   ├── Save/
│   │   ├── ModSaveData.cs
│   │   └── SaveDataRepository.cs
│   └── Smapi/
│       └── SmapiEventRouter.cs
└── DesertTomb/
    └── Templates/
        ├── templates.schema.json
        ├── level14.json
        ├── level19.json
        ├── level28.json
        ├── level39.json
        └── level77377.json
```

---

## 1) Critical Correctness Fixes (Do First)

These fixes happen before refactor, to stabilize behavior.

1. Triggering too broad and re-runs constantly
- Problem: `ModEntry.cs:611` triggers on any `data/mines*` warp.
- Problem: wrapper recreated every fire (`ModEntry.cs:651`).
- Fix: route activation through `DesertTombActivationService` with strict conditions:
  - progression/quest gating
  - exact location whitelist
  - level whitelist
  - per-level visit idempotency guard

2. Duplicate lifecycle calls
- Problem: duplicate `OnDayStart()` calls (`ModEntry.cs:652`, `ModEntry.cs:653`).
- Fix: single lifecycle entry point per day in module/coordinator.

3. Hardcoded teleport after generation
- Problem: forced teleport (`ModEntry.cs:660`) is brittle.
- Fix: remove from generation pipeline; if needed, apply optional controlled placement via bridge policy.

4. API mismatch risk
- Problem: `SetUpTombHelpers((MineShaft)location)` at `ModEntry.cs:656` does not match current signature in `DesertTomb/Core/DesertTombWrapper.cs:36`.
- Fix: unify signature and make call-site compile-safe via coordinator adapter.

5. Difficulty filtering logic likely inverted
- Problem: `DesertTomb/Core/TombArea.cs:165` removes entries on allowed condition.
- Fix: replace with explicit keep-filter (`Where(isAllowed)`) and add tests.

6. Allowed-monsters config ignored
- Problem: constructor path calls `GetAllowedMonstersForDifficulty()` without config in `DesertTomb/Calculation/SpawnRateCalculator.cs:74`.
- Fix: pass configuration source explicitly and remove hidden fallback except validated default boot path.

7. Wanderer spawning capped accidentally
- Problem: `.Take(2)` in `DesertTomb/Calculation/SpawnRateCalculator.cs:478` truncates types.
- Fix: remove artificial cap; use configured weighted pool.

8. Wanderer counters not updated consistently
- Problem: counters not consistently updated after spawn (`DesertTomb/Calculation/SpawnRateCalculator.cs:497+`).
- Fix: all spawn writes go through one counter update function.

9. Hardcoded level type input
- Problem: `ChooseLevelType(32)` hardcoded (`DesertTomb/Generation/LevelGenerator.cs:37`).
- Fix: derive from `TombRunContext` and template metadata.

---

## 2) Module Contracts and Lifecycle

### Interfaces

```csharp
public interface IModuleMetadata
{
    string Id { get; }
    int Order { get; }
    IReadOnlyCollection<string> Dependencies { get; }
    bool EnabledByDefault { get; }
}

public interface IModule : IDisposable
{
    IModuleMetadata Metadata { get; }
    void Initialize(ModuleContext context);
    void OnGameLaunched();
    void OnSaveLoaded();
    void OnDayStarted();
    void OnDayEnding();
    void OnSaving();
}
```

### `ModuleContext`
- shared references only: helper, monitor, config, save repository, event bus
- no static globals
- no direct module-to-module internals access

### `ModuleManager`
- validates dependency graph
- orders by `Order` and dependencies
- isolates failures per module (catch/log/continue)
- only this manager dispatches lifecycle

---

## 3) Responsibility Split (DesertTomb)

### Application layer
- `DesertTombCoordinator`
  - orchestrates stages
  - enforces idempotency
  - writes diagnostics snapshots
- `DesertTombActivationService`
  - decides whether tomb should activate for current warp/level
  - owns quest/progression/location/level checks

### Domain layer
- pure planning/decision logic
- no `Game1`, no SMAPI
- immutable inputs and outputs

### Infrastructure layer
- `TombGameBridge`: all reads from game state
- `LevelMutator`: applies level changes to mine instance
- `SpawnApplier`: applies spawn plan to game entities
- `TombMailService`: side effects for progression notifications

---

## 4) Immutable Runtime Model (Replace Mutable God Objects)

Use immutable inputs/outputs:

1. `TombRunContext`
- day
- seed
- mine level
- difficulty
- player tile
- location id
- save id (or deterministic salt)

2. `TombTemplate`
- areas
- allowed monsters
- capacities
- area rules
- schema version

3. `SpawnPlan`
- per-area spawn assignments
- final monster list and spawn tiles
- failure reasons summary

4. `SpawnBudget`
- global cap per monster type
- per-area cap per monster type
- total planned count

---

## 5) Explicit Runtime Stages

Runtime pipeline per activation:

1. `ActivationService` (pure decision + reason)
2. `TemplateResolver` (selects and validates template)
3. `LevelMutator` (side effect)
4. `SpawnPlanner` (pure plan generation)
5. `MonsterFactory` (pure profile-based monster descriptors)
6. `SpawnApplier` (side effect)

Rule:
- only `LevelMutator` and `SpawnApplier` can mutate game state
- all other stages remain pure and testable

---

## 6) Determinism Rules

1. Single RNG source per tomb run
- `seed = hash(day, mineLevel, saveId, templateId)`

2. Domain must not use `Game1.random`
- randomness injected via seeded RNG abstraction

3. Reproducibility logs
- log seed
- chosen template id
- spawn budget
- final spawn plan summary

This enables exact replay for bug reports.

---

## 7) Data-Driven Templates and Config Cleanup

1. Move hardcoded areas from `DesertTomb/Core/TombHandler.cs` to JSON templates.
2. Add schema versioning:
- `schemaVersion` field in template root
- migration/validation gates on load
3. Startup validation checks:
- area bounds inside map
- no invalid overlap patterns (unless explicitly allowed)
- duggy areas only on allowed back-tile indexes
- capacity min/max valid
4. Replace scattered defaults with one `TombBalanceConfig` loaded once.
5. Eliminate duplicate source-of-truth for:
- monster caps
- spawn weights
- difficulty multipliers

---

## 8) Spawn Model Improvements

Replace ad-hoc spawn loops with two-pass allocation.

### Pass A: Budget Allocation
- determine per-area target counts from capacity + difficulty + weights
- enforce global and per-area caps through `SpawnBudget`

### Pass B: Placement and Fill
- fill each area from weighted monster pools
- choose valid tiles from area-specific candidate sets
- stop early when area has no viable tiles
- track failure reason codes:
  - no valid tile
  - cap reached
  - factory unavailable
  - constraint mismatch

Rules:
- no implicit fallback-to-mummy unless mummy is valid for that area
- all successful spawns update unified counters via one function

---

## 9) Monster Factory Rebuild

1. Replace giant constructor dictionary with profile registry:
- `MonsterProfile` (type, texture, stats modifiers, drop profile, spawn constraints)
- `IMonsterSpawner` strategies for monster-specific construction

2. Remove stat math from constructors
- apply stat/drops in one post-build pipeline step

3. Inject configured multipliers
- stop reading `SpawnConfiguration.Default` in builders (`DesertTomb/TombMonsters/Base/TombMonsterBuilder.cs:23`)

4. Move special rules to strategy classes
- example: Duggy tile index constraints

---

## 10) Runtime Safety and Diagnostics

1. Idempotency guard per level visit
- generate/spawn once per visit key

2. HUD tracker registration safety
- register once
- avoid repeated `+=` on warp path (`ModEntry.cs:618`)

3. Module-level failure isolation
- one tomb failure does not break WildEggs flow

4. Add `DesertTombDiagnostics` command
- prints:
  - activation reason
  - template id
  - seed
  - spawn attempts/success/failure reasons
  - per-monster planned vs spawned counts

5. Structured debug counters
- areas processed
- attempts by stage
- total spawn failures by reason
- final counts by monster type

---

## 11) Complexity Reduction Rules (Non-Negotiable)

- Ban `ModEntry` tomb logic beyond forwarding lifecycle calls.
- Ban `Game1` usage in tomb domain/services.
- Ban static mutable tomb state.
- Ban duplicate source-of-truth for difficulty/caps/spawn rates.
- Delete dead/commented legacy blocks after each migration phase.

---

## 12) Migration Plan (Ordered)

### Phase 0: Stabilize Current Behavior
1. Apply all correctness fixes listed in section 1.
2. Add minimal regression logs around trigger/generation/spawn.
3. Verify no repeated generation on same visit.

### Phase 1: Modular Boundary
1. Introduce `IModule`, `ModuleContext`, `ModuleManager`.
2. Move tomb entry points to `DesertTombModule` + coordinator.
3. Keep old internals temporarily.

### Phase 2: Deterministic Context
1. Add `TombRunContext` and run-seed generation.
2. Route all random decisions through injected seeded RNG.

### Phase 3: Data Templates
1. Extract hardcoded areas into JSON templates.
2. Add startup validation and schema versioning.
3. Fail fast on invalid template with explicit error.

### Phase 4: Spawn Planner Rewrite
1. Implement `SpawnBudget` + two-pass allocator.
2. Replace ad-hoc loops and implicit fallbacks.
3. Add per-reason failure accounting.

### Phase 5: Monster Factory Rewrite
1. Implement `MonsterProfile` registry and spawner strategies.
2. Move all multipliers to config-driven post-build step.
3. Remove builder dependencies on `SpawnConfiguration.Default`.

### Phase 6: Legacy Removal
1. Remove old `TombHandler` hardcoded area definitions.
2. Remove dead code and commented blocks.
3. Keep diagnostics command and parity tests.

### Phase Status vs Current Code (Updated - 2024-01)
- Phase 0: ⏳ IN MAIN CODE (correctness fixes - NOT in refactor)
- Phase 1: ⏳ NOT STARTED (modular boundary)
- Phase 2: ⏳ NOT STARTED (deterministic context)
- Phase 3: ⏳ NOT STARTED (data templates)
- Phase 4: ⏳ NOT STARTED (spawn planner)
- Phase 5: ⏳ NOT STARTED (monster factory)
- Phase 6: ⏳ NOT STARTED (legacy removal)

## Important: DesertTomb is SEPARATE from refactor

Per PROMPT.md execution order:
- **DesertTomb Phase 0 correctness fixes** → Main code (`/DesertTomb/`)
- **DesertTomb modular rewrite** → New `Features/DesertTomb/` directory
- **WildEggs refactor** → `refactor/` directory

The `refactor` directory contains ONLY WildEggs refactoring. DesertTomb work is tracked separately in this document and will be implemented after WildEggs refactor completes Phase 6.

---

## 13) Definition of Done

- DesertTomb fully isolated as a feature module.
- Trigger path deterministic and idempotent.
- Generation and spawn are reproducible from logged seed/context.
- Template/config are data-driven and validated.
- Monster creation is profile-driven and config-injected.
- Tomb failures are isolated and observable via diagnostics.
- `ModEntry` contains no tomb gameplay logic.
