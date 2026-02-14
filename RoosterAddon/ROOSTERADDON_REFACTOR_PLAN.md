# RoosterAddon Refactor Plan (Phased)

## Scope and Goals

This plan refactors `RoosterAddon` into a modular, testable architecture aligned with the patterns used in `WildEggsHatching/_refactor`:

- thin `ModEntry` and SMAPI boundary
- separated domain logic and infrastructure
- typed configuration instead of nested string dictionaries
- data-driven rooster/produce/egg definitions
- strategy-based hatching behavior
- incremental migration with parity checks at each phase

Primary source project analyzed: `/home/mike/Documents/Projects/RoosterAddon`.

---

## Architectural Target

### Directory Structure (Target)

```text
RoosterAddon/
├── Core/
│   ├── Models/
│   ├── Enums/
│   ├── Rules/
│   └── Services/
├── Data/
│   ├── Catalogs/
│   └── Defaults/
├── Infrastructure/
│   ├── Smapi/
│   ├── Game/
│   ├── Content/
│   └── Config/
├── Coordination/
├── UI/
│   └── Gmcm/
└── Legacy/
```

### Core Design Choices

- `ModEntry` becomes composition root only.
- `SmapiEventRouter` owns event subscriptions and forwards to coordinator.
- `RoosterCoordinator` orchestrates application flow.
- Core services are pure C# where possible.
- Game and SMAPI interactions are accessed through interfaces (`IGameStateReader`, `IFarmAnimalEditor`, `IObjectDataEditor`, `IModRegistry`, etc.).
- Config is versioned and strongly typed.
- Rooster definitions and defaults are data-driven.

---

## Phase 0: Baseline, Safety, and Documentation

### Objectives

- Freeze behavior before structural changes.
- Establish parity checks and migration safety.

### Tasks

1. Create architecture notes documenting current responsibilities per class:
   - `ModEntry.cs`
   - `RoosterControl.cs`
   - `ModConfig.cs`
   - `GMCMenu.cs`
2. Add temporary diagnostics snapshot command/log for:
   - rooster data
   - farm animals edits
   - object data edits
3. Add "behavior baseline checklist" for manual validation:
   - GMCM options render and persist
   - farm animal patches applied
   - hatching behavior (original + fertilized mode)
   - clone eggs compatibility behavior
4. Create a migration tracking file (`TODO_REFRACTOR_CHECKLIST.md`) with per-phase checkboxes.

### Deliverables

- Baseline behavior checklist
- Migration checklist
- Current architecture notes

### Exit Criteria

- Baseline captured and reproducible.
- No refactor begins without a pre-change behavior reference.

---

## Phase 1: Extract Boundaries (ModEntry Slimming)

### Objectives

- Remove orchestration and logic from `ModEntry`.
- Introduce explicit app boundary and coordination layer.

### Tasks

1. Add `Infrastructure/Smapi/SmapiEventRouter.cs`:
   - registers SMAPI events
   - forwards callbacks to coordinator
2. Add `Coordination/RoosterCoordinator.cs`:
   - handles day start lifecycle
   - handles asset request pipeline dispatch
   - coordinates config/UI/api sync
3. Refactor `ModEntry` to:
   - build dependencies
   - instantiate coordinator + router
   - expose API only
4. Move API retrieval and optional dependency checks into infrastructure services:
   - Content Patcher API provider
   - LetMeHatch API provider
5. Keep existing behavior unchanged (same patch order, same triggers).

### Deliverables

- `SmapiEventRouter`
- `RoosterCoordinator`
- reduced `ModEntry` (<200 lines target)

### Exit Criteria

- Events still fire in the same scenarios.
- Asset edits still occur with same priorities.
- Baseline checklist passes.

---

## Phase 2: Typed Config Model + Versioned Migration

### Objectives

- Replace nested string dictionaries with typed DTOs.
- Introduce config schema versioning and migration.

### Tasks

1. Add typed config classes in `Infrastructure/Config`:
   - `ModConfigV2`
   - `GeneralSettings`
   - `RoosterSettings`
   - `ProduceSlotSettings`
   - `ProbabilitySettings`
2. Add enums for previously stringly-typed values:
   - update rate
   - harvest type
   - hatching mode
   - rooster assignment values
3. Build `ConfigRepository`:
   - `Load()`, `Save()`
   - `Validate()`
   - default fill for missing fields
4. Add migration layer:
   - `ModConfigV1LegacyAdapter` reads old schema
   - maps old nested dictionary values into typed V2
5. Add config validation report logs (invalid values clamped or defaulted).
6. Keep backward compatibility for existing users.

### Deliverables

- versioned config DTOs
- migration mapper V1 -> V2
- typed access replacing raw dictionary lookups

### Exit Criteria

- Existing config files load without manual edits.
- GMCM persists values correctly in V2.
- Runtime no longer depends on string dictionary traversals.

---

## Phase 3: Data-Driven Catalogs (Roosters, Eggs, Produces)

### Objectives

- Move hardcoded rooster/produce/egg data out of logic methods.
- Centralize definitions in catalogs.

### Tasks

1. Create `Data/Catalogs/RoosterCatalog`:
   - rooster key
   - default female mapping
   - base egg definitions
   - default shop metadata
2. Create `Data/Catalogs/ProduceCatalog`:
   - allowed produce values per rooster type
   - default produce slots and weights
   - random-group definitions (gems, minerals, random egg variants)
3. Move large switch/dictionary blocks out of:
   - `ModConfig.BuildProducesDictionaries`
   - `RoosterControl.ProduceTranslation`
4. Replace random produce switch with resolver service:
   - `IProduceResolver`
   - `RandomGroupProduceResolver`
5. Keep constants for IDs centralized, but source selection from catalog data.

### Deliverables

- `RoosterCatalog`
- `ProduceCatalog`
- produce resolver service

### Exit Criteria

- No large produce switch remains in runtime path.
- Adding a new rooster or produce set does not require touching core logic.

---

## Phase 4: Hatching and FarmAnimal Logic into Services

### Objectives

- Separate mutation logic from event callbacks.
- Introduce strategy pattern for hatching modes.

### Tasks

1. Create `Core/Services/FarmAnimalPatchService`:
   - applies rooster settings to `FarmAnimalData`
   - purchase/sell/building/harvest/shop metadata
2. Create `Core/Services/HatchingService` with strategies:
   - `IHatchingModeStrategy`
   - `OriginalHatchingStrategy`
   - `FertilizedOnlyHatchingStrategy`
3. Move methods from `ModEntry` into services:
   - `SetRoosterData`
   - `RunHatchingSystem`
   - normal/fertilized mode logic
4. Add `Core/Services/LuckAdjustmentService`:
   - daily luck evaluation
   - percentage adjustment rules
5. Add `Core/Services/FemaleAssignmentService`:
   - discovery of compatible females
   - exclusion filtering
   - persisted mapping update

### Deliverables

- farm animal patch service
- strategy-based hatching service
- luck and female assignment services

### Exit Criteria

- `ModEntry`/router only invoke coordinator methods.
- Hatching behavior remains parity-equivalent under baseline checks.

---

## Phase 5: ObjectData/Fertilized Egg Pipeline Isolation

### Objectives

- Isolate object edits and fertilized egg generation into dedicated services.

### Tasks

1. Create `Core/Services/FertilizedEggService`:
   - detect base egg source
   - generate fertilized variants
   - maintain mapping to female egg table
2. Create `Core/Services/ObjectDataPatchService`:
   - applies object data changes
   - BetterTruffles compatibility tag updates
3. Move regex/item-id normalization into utility service:
   - egg id parsing and conversion helpers
4. Add deterministic handling when source object entries are missing.

### Deliverables

- object patch service
- fertilized egg service
- egg ID utility helpers

### Exit Criteria

- Object edits are isolated behind one entrypoint.
- Edge cases (missing custom fields, missing large egg variants) handled safely.

---

## Phase 6: GMCM Refactor to Descriptor-Driven UI

### Objectives

- Replace manual option rendering/switch logic with reusable option descriptors.

### Tasks

1. Create `UI/Gmcm/OptionDescriptor` model:
   - key
   - type
   - title/description translation keys
   - getter/setter delegates
   - constraints (min/max/interval/allowed values)
2. Create `GmcmPageBuilder`:
   - renders sections/pages from descriptor collections
   - supports rooster-scoped and global descriptors
3. Replace `GMCMenu.AddOptions` switch with descriptor registry + renderer map.
4. Keep existing page layout and translations.

### Deliverables

- descriptor-driven GMCM framework
- reduced `GMCMenu.cs` complexity

### Exit Criteria

- UI behavior unchanged for users.
- Adding a new option requires descriptor registration only.

---

## Phase 7: Testability Layer + Initial Automated Tests

### Objectives

- Add test coverage for core logic without SMAPI runtime.

### Tasks

1. Add test project (if not present) and cover pure services:
   - luck adjustment logic
   - weighted produce selection
   - hatching strategy outcomes
   - exclusion parsing and female assignment
2. Add interfaces and fakes for game-bound dependencies.
3. Add regression tests derived from baseline scenarios.
4. Add serialization tests for config migration V1 -> V2.

### Deliverables

- unit tests for core services
- migration tests for config compatibility

### Exit Criteria

- Critical logic has deterministic tests.
- Major regressions are caught in CI/local test run.

---

## Phase 8: Legacy Cleanup and Hardening

### Objectives

- Remove deprecated code paths and finalize the new architecture.

### Tasks

1. Remove or archive legacy helpers no longer used.
2. Eliminate duplicated logic remaining in coordinator/router.
3. Standardize naming, comments, and file organization.
4. Add `README` architecture section with module responsibilities.
5. Add changelog migration notes for mod users.

### Deliverables

- legacy removal PR
- finalized docs

### Exit Criteria

- No active production path depends on legacy dictionary-driven architecture.
- New architecture is documented and maintainable.

---

## Cross-Phase Standards

### Coding Standards

- Prefer typed models over dictionary-string maps.
- Keep pure logic free of SMAPI and `Game1` where possible.
- No large condition chains where a map/table/strategy applies.
- Small methods with single responsibilities.

### Compatibility Standards

- Preserve save and config compatibility unless explicitly version-bumped.
- Preserve behavior of existing GMCM options.
- Preserve current mod dependency semantics.

### Performance Standards

- Avoid repeated full scans of farm/object datasets where cached indexes can be used.
- Keep asset patch callbacks minimal and delegate logic to services.

---

## Risk Register and Mitigations

1. Behavior drift during migration  
Mitigation: baseline checklist + parity snapshots each phase.

2. Config migration edge cases  
Mitigation: V1 adapter tests + safe defaults + validation/clamping logs.

3. Compatibility regressions with optional mods  
Mitigation: explicit integration checks for `Mikew.letmehatchthisegg.smapi` and `6480.blueegg`.

4. Event ordering regressions  
Mitigation: preserve callback timing and asset edit priorities in boundary layer.

---

## Suggested Phase Milestones

1. Milestone A (Phases 0-2): Boundary and typed config foundation complete.
2. Milestone B (Phases 3-5): Core behavior logic fully service-driven and data-driven.
3. Milestone C (Phases 6-8): UI modernization, tests, and legacy removal complete.

---

## Definition of Done (Project Level)

- `ModEntry` is composition-only.
- Coordinator + event router control application flow.
- Config is typed and versioned with migration support.
- Rooster/produce/egg definitions are data-driven.
- Hatching logic uses strategy pattern.
- Object/farm edits are isolated services.
- GMCM is descriptor-driven.
- Core behaviors are covered by unit tests.
- Documentation explains module responsibilities and extension points.
