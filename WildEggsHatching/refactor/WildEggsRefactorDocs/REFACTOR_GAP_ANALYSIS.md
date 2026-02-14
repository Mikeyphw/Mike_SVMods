# WildEggs Refactoring - Remaining Work Items

## Purpose
This document tracks what's still missing from the `refactor` directory compared to the original `WildEggsHatching_orig` code.

---

## Current Status (Per REFACTOR_INTEGRATION_PLAN)

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1: Boundary Isolation | ✅ DONE | SmapiEventRouter, Coordinator with DI |
| Phase 2: Save Migration | ✅ DONE | V1/V2 DTOs, Repository |
| Phase 3: Spawn Isolation | ⚠️ PARTIAL | Basic done, but WeightedTable still uses Game1.random |
| Phase 4: Progression Isolation | ⚠️ PARTIAL | UnlockContextProvider still uses Game1.player directly |
| Phase 5: Asset/Content Boundary | ⚠️ PARTIAL | SmapiContentEditor methods are stubs (no actual logic) |
| Phase 6: Legacy Removal | ❌ NOT STARTED | ModEntry not refactored |

---

## Verified Issues in Current refactor Code

### 1. Phase 3 Issue: Game1.random still used in WeightedTable
**File**: `refactor/Spawning/WeightedTable.cs`
```csharp
roll = Game1.random.Next(totalWeight);
```
This violates the "Game1.random removed from spawn" claim.

### 2. Phase 3 Issue: Game1.viewport in EggSpawnService  
**File**: `refactor/Spawning/EggSpawnService.cs`
```csharp
if (!location.isTilePassable(tile, Game1.viewport))
```

### 3. Phase 4 Issue: Game1.player in UnlockContextProvider
**File**: `refactor/Progression/Rules/UnlockContextProvider.cs`
Still reads directly from Game1.player and ModEntry static fields.

### 4. Phase 5 Issue: SmapiContentEditor is empty stubs
All methods contain only placeholder comments, no actual asset editing logic.

### 5. Phase 6 Issue: ModEntry is NOT refactored
Original ModEntry still contains all event handlers and game logic.

---

## Detailed Gap Analysis

### 1. ModEntry.cs Refactoring - NOT STARTED ❌

**Original Location**: `WildEggsHatching_orig/ModEntry.cs`

**Original Contains** (~660 lines):
- Event handlers: OnDayStart, OnDayEnding, OnWarped, OnAssetRequested, OnSaving, HandleEarlyStuff
- Asset editing logic: EditObjectsAsset, EditFarmAnimalsAsset, EditBigCraftableAsset, etc.
- DesertTomb integration and mine level editing
- GMCMenu setup
- Custom commands registration
- Static field initialization

**Needed**:
- Thin ModEntry that only:
  - Creates infrastructure instances
  - Creates SmapiEventRouter
  - Registers events
  - No game logic

---

### 2. GameAssetManager Refactoring - NOT STARTED ❌

**Original Location**: `WildEggsHatching_orig/Managers/GameAssetManagement/GameAssetManager.cs`

**Original Contains**:
- BigCraftableEditor
- CraftingRecipeEditor
- FarmAnimalEditor
- MachineEditor
- MailEditor
- ObjectEditor
- QuestEditor
- SpecialOrderEditor

**Needed in refactor**:
- Port all editor logic to new Infrastructure/Content folder
- Create SmapiContentEditor with all editing methods
- Move static helper methods (HasMail, HasItem, etc.)

---

### 3. ProgressionManager Refactoring - PARTIAL ⏳

**Original Location**: `WildEggsHatching_orig/Managers/ProgressionManager.cs`

**Original Contains** (~700 lines):
- 6-step progression logic (PartI through PartVI)
- Quest/mail tracking
- Unlock step logic
- ProcessHatchedAnimals
- Note spawning integration

**Current in refactor**:
- `PhaseProgressionService` - has skeleton but incomplete step logic
- `UnlockService` - basic unlock logic
- Missing: quest completion day tracking, full step conditions

**Needed**:
- Complete PhaseProgressionService with all 6 step conditions
- Add quest completion tracking to PlayerStateRepository
- Port GoToPart/UnlockStep logic

---

### 4. EggSpawning Service Refactoring - PARTIAL ⏳

**Original Location**: `WildEggsHatching_orig/Services/EggSpawning.cs`

**Original Contains** (~1100 lines):
- Location pack loading from JSON
- Spawn area management
- Complex spawn logic with luck modifiers
- Tile finding and validation
- Egg quantity determination

**Current in refactor**:
- `EggSpawnService` - basic spawn decision
- `SpawnChanceCalculator` - simplified 3-layer system

**Needed**:
- Port area/location pack loading
- Add multi-location spawning support
- Port tile finding logic
- Port quantity determination with luck

---

### 5. WildListManager Refactoring - NOT STARTED ❌

**Original Location**: `WildEggsHatching_orig/Managers/WildListManager.cs`

**Original Contains**:
- Animal list management
- Valid wild animals tracking
- Animal definitions

**Current in refactor**:
- `AnimalCatalog` - basic skeleton

**Needed**:
- Complete AnimalCatalog with full animal data
- Port animal loading logic

---

### 6. NoteSpawning Refactoring - PARTIAL ⏳

**Original Location**: `WildEggsHatching_orig/Services/NoteSpawning.cs`

**Current in refactor**:
- `NoteService` - basic skeleton

**Needed**:
- Complete note spawning logic
- Port note type selection

---

### 7. NewAnimalMonitoring Refactoring - PARTIAL ⏳

**Original Location**: `WildEggsHatching_orig/Services/NewAnimalMonitoring.cs`

**Current in refactor**:
- `AnimalTrackingService` - partial implementation

**Needed**:
- Complete hatch detection logic
- Port "animals needing notes" tracking

---

### 8. DesertTomb - NOT STARTED ❌

**Original Location**: `WildEggsHatching_orig/DesertTomb/`

**Status**:
- Per PROMPT.md, correctness fixes go to main code
- Modular rewrite planned separately
- NOT part of refactor scope

---

## Files to Create/Modify

### New Files Needed in refactor:

1. **Infrastructure/Content/BigCraftableEditor.cs**
2. **Infrastructure/Content/CraftingRecipeEditor.cs**
3. **Infrastructure/Content/FarmAnimalEditor.cs**
4. **Infrastructure/Content/MachineEditor.cs**
5. **Infrastructure/Content/MailEditor.cs**
6. **Infrastructure/Content/ObjectEditor.cs**
7. **Infrastructure/Content/QuestEditor.cs**
8. **Infrastructure/Content/SpecialOrderEditor.cs**

### Files to Modify:

1. **ModEntry.cs** - Replace with thin bootstrap
2. **Progression/PhaseProgressionService.cs** - Complete step logic
3. **Progression/UnlockService.cs** - Complete unlock logic
4. **Spawning/EggSpawnService.cs** - Add area/location support
5. **Data/AnimalCatalog.cs** - Complete animal definitions
6. **Notes/NoteService.cs** - Complete note spawning
7. **Tracking/AnimalTrackingService.cs** - Complete hatch detection
8. **State/PlayerStateRepository.cs** - Add quest tracking

---

## Priority Order for Implementation

1. **Priority 1**: Complete Phase 4 (Progression Isolation)
   - Complete PhaseProgressionService
   - Add quest completion tracking

2. **Priority 2**: Asset Editing Boundary (Phase 5)
   - Port all GameAssetManager editors
   - Complete SmapiContentEditor

3. **Priority 3**: Spawn Logic (Phase 3 completion)
   - Add area/location pack support to EggSpawnService

4. **Priority 4**: Complete ModEntry Refactoring (Phase 6 prep)
   - Create thin ModEntry
   - Wire up SmapiEventRouter

5. **Priority 5**: Legacy Removal (Phase 6)
   - Remove old manager dependencies

---

## Verification Checklist

When refactoring is complete, verify:
- [ ] ModEntry has no game logic
- [ ] All Game1 references removed from domain
- [ ] Save goes through PlayerStateRepository only
- [ ] SpawnContext includes all needed data (mine level, luck, etc.)
- [ ] Event subscriptions centralized in SmapiEventRouter
- [ ] Behavior parity confirmed for:
  - [ ] Progression step advancement
  - [ ] Egg spawning decisions and placement
  - [ ] Mail/quest/special-order effects
  - [ ] Asset edits for objects/machines/mail/quests
- [ ] Build compiles without errors

