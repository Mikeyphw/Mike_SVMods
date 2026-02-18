# Phase 2.0 — Content routing inventory (pre-refactor snapshot)

This document freezes the **current** SMAPI `Content.AssetRequested` behavior (before Phase 2 refactor).

> Note: As of **Phase 2.3**, the *load* branches were extracted into `Infrastructure/Content/Loaders/*` (behavior intended to be identical).
It is intended as a reference to verify that the coordinator/editor refactor preserves behavior.

## Event entrypoint

- `Application/Coordination/LmhCoordinator.OnAssetRequested(...)`
  - Writes the lowercase base name to `assetsRequested.json` (debug/tracking).
  - Delegates to `HandleAssetSpecificLogic(assetName, e)`.

## Asset loads (exclusive / non-edit)

### `Animals/MaleClones*` (regex)
- Match: `^Animals/MaleClones` (case-insensitive)
- Action: `TextureManager.LoadTextureFromContentPack(...)`
- Priority: `AssetLoadPriority.Exclusive`
- Purpose: load cloned texture assets from enabled content packs.

### BoAF `ModAssetHandler.GamePaths` (mod-owned asset loads)
- Match: `ModEntry.AssetHandler.GamePaths.Contains(assetName)`
- Action:
  - If asset is **not** `Assets.Object.ClonedItems` game path → `ModEntry.AssetHandler.LoadModAssets(e)`
  - Else → `_assetManager.HandleClonedEggsAsset(e)`
- Purpose: serve mod-provided assets and the special cloned-eggs list.

## Asset edits

### `Data/FarmAnimals`
Triggered when `e.NameWithoutLocale.IsEquivalentTo(AssetNames.FarmAnimals)`.

1) **Clone placeholder + clone injection**
- Guard: `_assetManager.ClonedAnimalsInfoList.Any()`
- Calls:
  - `_assetManager.CreateClonePlaceholders(e, AssetEditPriority.Early)`
  - `_assetManager.CreateAndAddAnimalClones(e, AssetEditPriority.Late)`

2) **Additional FarmAnimals edits** (`HandleAdditionalFarmAnimalsLogic`)
- `ReplaceOriginalItemForClone`:
  - Guard: `_assetManager.ItemsToAdd.Count > 0`
  - Priority: `AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY`
- `EditEggListByName`:
  - Guard: `config.EditEggListByName`
  - Priority: `AssetEditPriority.Late + EDIT_EGG_LIST_PRIORITY`
- `EditBirthTexts`:
  - Guard: `config.EditBirthText`
  - Priority: `AssetEditPriority.Late + EDIT_BIRTH_TEXT_PRIORITY`
- `RunBreedingSystem`:
  - Guard: `config.BreedingSystemEnabled && _assetManager.HasAddedMaleClones`
  - Priority: `AssetEditPriority.Late + BREEDING_SYSTEM_PRIORITY`

> Note: The Fancy Chickens hook is present but commented out.

### `Data/Objects`
Triggered when `e.NameWithoutLocale.IsEquivalentTo(AssetNames.Objects)`.

- Guard: `_assetManager.ItemsToAdd.Count >= 1`
- Calls:
  - `_assetManager.EditAssetWithNewClonedEggs(e, AssetEditPriority.Default)`
  - `_assetManager.EditAssetWithNewFertilizedEggs(e, AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY)`

### `Data/Machines`
Triggered when `e.NameWithoutLocale.IsEquivalentTo(AssetNames.Machines)`.

- Config-dependent gating:
  - If `config.OnlyFertilizedEggs && _assetManager.HasAddedFertilizedEggs`
  - OR `!config.OnlyFertilizedEggs && _assetManager.HasAddedClonedItems && config.CloneMachineRules`
- Call:
  - `_assetManager.CloneMachineData(e, AssetEditPriority.Late + BASE_PRIORITY)`

## Priority constants referenced
Defined in `EnumsAndStrings.Strings`:

- `BASE_PRIORITY`
- `NEW_EGGS_ADD_PRIORITY`
- `EDIT_EGG_LIST_PRIORITY`
- `EDIT_BIRTH_TEXT_PRIORITY`
- `BREEDING_SYSTEM_PRIORITY`
