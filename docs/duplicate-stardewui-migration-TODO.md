# Duplicate StardewUI Migration - Implementation TODO

## Overview
This TODO tracks the implementation of duplicate StardewUI editors for GMCM-heavy sections while keeping all GMCM pages intact.

---

## Phase 1: RoosterControl - Compat Editor

### Step 1.1: Extract Shared Compat Editor State
- [ ] Create `RoosterControl/Integrations/StardewUi/Compat/RoosterCompatEditorService.cs`
  - Add shared load/apply methods for egg-source selections
  - Add shared load/apply methods for rooster-provider selections
  - Add summary text generation
  - Add validation summary generation
- [ ] Create `RoosterControl/Integrations/StardewUi/Compat/RoosterCompatEditorCatalog.cs`
  - Add immutable snapshot records: `RoosterCompatEggKindRecord`, `RoosterCompatProviderRecord`, `RoosterCompatValidationRecord`
- [ ] Update `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
  - Replace inline catalog/summary construction with service calls
  - Keep GMCM layout and IDs unchanged
- [ ] Update docs: mark duplicate StardewUI compat work as planned

### Step 1.2: Add Duplicate StardewUI Compat Editor
- [ ] Create `RoosterControl/Integrations/StardewUi/Compat/RoosterCompatEditorViewModel.cs`
  - Add grouped selection state
  - Add summary/validation text
  - Add save/discard/reset commands
  - Add StarML-safe wrapper methods
- [ ] Update `RoosterControl/Application/RoosterCoordinator.StardewUi.cs`
  - Register new menu id `rooster.compat.editor`
  - Wire to existing SML file
- [ ] Update `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`
  - Add `compat.openStardewUi` action
  - Add status/help paragraph
- [ ] Add i18n strings for new launcher
- [ ] Update docs: mark GMCM compat for later removal only

### Step 1.3: Add Duplicate StardewUI Females Editor
- [ ] Create `RoosterControl/Integrations/StardewUi/Females/RoosterFemalesEditorService.cs`
  - Add shared load/apply logic for female assignments
- [ ] Create `RoosterControl/Integrations/StardewUi/Females/RoosterFemalesEditorViewModel.cs`
  - Add searchable StardewUI assignment state
- [ ] Update `RoosterControl/Application/RoosterCoordinator.StardewUi.cs`
  - Register new menu id `rooster.females.editor`
- [ ] Update `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`
  - Add `females.openStardewUi` action
- [ ] Add i18n strings
- [ ] Update docs: mark GMCM females for later removal only

---

## Phase 2: LetMeHatch - Breeding Editor

### Step 2.1: Extract Shared Breeding Editor State
- [ ] Create `LetMeHatch/Integrations/StardewUi/Breeding/LmhBreedingEditorService.cs`
  - Add load/apply helpers for packs
  - Add load/apply helpers for females, chances
  - Add per-female settings helpers
- [ ] Create `LetMeHatch/Integrations/StardewUi/Breeding/LmhBreedingEditorCatalog.cs`
  - Add immutable snapshot/draft records: `LmhBreedingPackRecord`, `LmhBreedingFemaleRecord`, `LmhBreedingDraft`
- [ ] Update `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
  - Replace inline snapshot construction with service calls
  - Preserve GMCM page tree
- [ ] Update docs: mark duplicate StardewUI breeding work as planned

### Step 2.2: Add Duplicate StardewUI Breeding Editor
- [ ] Create `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs`
  - Add StardewUI host fields
  - Add `OpenBreedingEditor()` method
  - Register menu id `lmh.breeding.editor`
- [ ] Create `LetMeHatch/Integrations/StardewUi/Breeding/LmhBreedingEditorViewModel.cs`
  - Add pack/female navigation
  - Add detail tabs
  - Add save/discard/reload
  - Add StarML-safe event wrapper methods
- [ ] Update `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
  - Add `breeding.openStardewUi` action
  - Add status/help paragraph
- [ ] Add i18n strings
- [ ] Update docs: mark GMCM breeding for later removal only

### Step 2.3: Add Duplicate StardewUI Colors Editor
- [ ] Create `LetMeHatch/Integrations/StardewUi/Colors/LmhColorsEditorService.cs`
  - Add shared palette/override snapshot helpers
- [ ] Create `LetMeHatch/Integrations/StardewUi/Colors/LmhColorsEditorViewModel.cs`
  - Add preview-first StardewUI viewmodel state
- [ ] Update `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs`
  - Register menu id `lmh.colors.editor`
- [ ] Update `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
  - Add `colors.openStardewUi` action
- [ ] Add i18n strings
- [ ] Update docs: mark GMCM colors for later removal only

### Step 2.4: Add Duplicate StardewUI Advanced Mappings Editor
- [ ] Create `LetMeHatch/Integrations/StardewUi/Advanced/LmhItemMappingsEditorService.cs`
  - Add grouped mapping snapshot/apply helpers
- [ ] Create `LetMeHatch/Integrations/StardewUi/Advanced/LmhItemMappingsEditorViewModel.cs`
  - Add comparison-oriented StardewUI state
- [ ] Update `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs`
  - Register menu id `lmh.advancedMappings.editor`
- [ ] Update `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
  - Add `advanced.itemMappings.openStardewUi` action
- [ ] Add i18n strings
- [ ] Update docs: mark GMCM advanced mappings for later removal only

---

## Phase 3: EggsInWild - Animal Progression Editor

### Step 3.1: Extract Shared Animal Progression Editor State
- [ ] Create `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/AnimalProgressionEditorService.cs`
  - Add load/apply helpers for farm-animal progression entries
  - Add configured/default resolution helpers
- [ ] Create `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/AnimalProgressionEditorCatalog.cs`
  - Add immutable snapshot and draft records: `AnimalProgressionAnimalRecord`, `AnimalProgressionDraft`
- [ ] Update `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`
  - Replace inline farm-animal iteration with service snapshot calls
  - Preserve page and IDs
- [ ] Update docs: mark duplicate StardewUI Animal Progression as planned

### Step 3.2: Add Duplicate StardewUI Animal Progression Editor
- [ ] Create `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/AnimalProgressionEditorViewModel.cs`
  - Add searchable list/detail progression state
  - Add status badges
  - Add save/discard/reload commands
  - Add StarML-safe wrapper methods
- [ ] Update `EggsInWild/ModEntry.StardewUi.cs`
  - Register menu id `animalProgressionEditor`
  - Add `OpenAnimalProgressionEditor()` method
- [ ] Update `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`
  - Add `animalProgression.openEditor` duplicate launcher
  - Add help/status text
- [ ] Add i18n strings
- [ ] Update docs: explicitly say GMCM removal is deferred

---

## Build Gates

After completing each phase, run:
```bash
dotnet build RoosterControl/RoosterControl.csproj
dotnet build LetMeHatch/LetMeHatch.csproj
dotnet build EggsInWild/EggsInWild.csproj -p:EnableModDeploy=false

# Tests
dotnet build Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj
dotnet build Tests/RoosterControl.Tests/RoosterControl.Tests.csproj
dotnet build Tests/LetMeHatch.Core.Tests/LetMeHatch.Core.Tests.csproj
dotnet build Tests/LetMeHatch.Tests/LetMeHatch.Tests.csproj
dotnet build Tests/EggsInWild.Tests/EggsInWild.Tests.csproj
```

---

## Grep Gates (Verification)

After each phase, verify with:
- RoosterControl Phase 1: `rg -n "class RoosterCompatEditorService\b" RoosterControl/Integrations/StardewUi/Compat` → 1 match
- RoosterControl Phase 2: `rg -n "rooster\.compat\.editor|compat\.openStardewUi" RoosterControl` → at least 3 matches
- RoosterControl Phase 3: `rg -n "rooster\.females\.editor|females\.openStardewUi" RoosterControl` → at least 3 matches
- LetMeHatch Phase 1: `rg -n "class LmhBreedingEditorService\b" LetMeHatch/Integrations/StardewUi/Breeding` → 1 match
- LetMeHatch Phase 2: `rg -n "lmh\.breeding\.editor|breeding\.openStardewUi" LetMeHatch` → at least 3 matches
- LetMeHatch Phase 3: `rg -n "lmh\.colors\.editor|colors\.openStardewUi" LetMeHatch` → at least 3 matches
- LetMeHatch Phase 4: `rg -n "lmh\.advancedMappings\.editor|advanced\.itemMappings\.openStardewUi" LetMeHatch` → at least 3 matches
- EggsInWild Phase 1: `rg -n "class AnimalProgressionEditorService\b" EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression` → 1 match
- EggsInWild Phase 2: `rg -n "animalProgressionEditor|animalProgression\.openEditor" EggsInWild` → at least 3 matches

