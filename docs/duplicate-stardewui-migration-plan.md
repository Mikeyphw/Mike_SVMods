# Duplicate StardewUI Migration Plan

## Summary

Implement duplicate StardewUI editors for the shortlisted GMCM-heavy sections while keeping all current GMCM pages intact. This migration is split into one plan per subdir so it can be executed deterministically. GMCM remains the fallback surface for now and is only marked for later removal in docs.

## Current Status

The inert SML shell phase is already complete for the duplicate editors listed below. Those files exist in the repo today and are intentionally unwired:

- `RoosterControl/assets/views/RoosterCompatEditor.sml`
- `RoosterControl/assets/views/RoosterFemalesEditor.sml`
- `LetMeHatch/assets/views/LmhBreedingEditor.sml`
- `LetMeHatch/assets/views/LmhColorsEditor.sml`
- `LetMeHatch/assets/views/LmhAdvancedMappingsEditor.sml`
- `EggsInWild/assets/views/animalProgression/editor.sml`

This document is therefore the migration plan from this point forward:

- Keep those SML files as the locked initial shells unless a later phase explicitly updates them.
- Treat SML-shell creation work as already done.
- Focus next on shared service extraction, viewmodel wiring, menu registration, GMCM launcher duplication, and tests.

## Global Execution Contract

- Preserve behavior: `yes`
- Duplicate only: do not remove any GMCM page, link, option, or page ID in this migration.
- NestCore-first: use existing `INestCoreApi.TryCreateStardewUiHost(...)` and current StardewUI host patterns.
- Keep every phase buildable.
- Update `PLAN.md`, `CHECKLIST.md`, and `TODO.md` in the target subdir during each plan.
- Use the older-safe StardewUI markup subset already proven in this repo/runtime.
- Do not change persisted config property names or public APIs.
- Do not use `Tests/Tests.sln` as a build gate.

---

## Plan A — `RoosterControl`

**Status note**: the inert duplicate SML shells already exist for `RoosterCompatEditor.sml` and `RoosterFemalesEditor.sml`. The phases below describe the remaining migration work to wire them.

### Inputs

- Target subdir: `RoosterControl`
- Exact files/symbols likely involved:
  - `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
  - `RoosterControl/Application/RoosterCoordinator.StardewUi.cs`
  - `RoosterControl/Framework/Config/RoosterControlConfig.cs`
  - `RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs`
  - `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`
  - `RoosterControl/Integrations/StardewUi/Compat/*.cs`
  - `RoosterControl/Integrations/StardewUi/Females/*.cs`
  - `RoosterControl/assets/views/*.sml`
  - `RoosterControl/i18n/default.json`
  - `RoosterControl/i18n/pt-br.json`
  - `RoosterControl/docs/PLAN.md`
  - `RoosterControl/docs/CHECKLIST.md`
  - `RoosterControl/docs/TODO.md`
  - `Tests/RoosterControl.Tests/*.cs`
  - `Tests/RoosterControl.PureTests/*.cs`
- Must preserve behavior: `yes`
- Forbidden scope:
  - Do not change the existing White rooster produce StardewUI editor.
  - Do not remove `BuildCompatPage(...)` or `BuildFemalesPage(...)`.
  - Do not change compat reset semantics.

### Phase 1 — Extract reusable compat editor state

**Objective**: move RoosterControl compat-page selection, summary, and validation logic into shared non-GMCM services while keeping the current GMCM page unchanged.

**Allowed files**

- `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
- `RoosterControl/Integrations/StardewUi/Compat/*.cs`
- `RoosterControl/docs/PLAN.md`
- `RoosterControl/docs/CHECKLIST.md`
- `RoosterControl/docs/TODO.md`
- `Tests/RoosterControl.Tests/*.cs`
- `Tests/RoosterControl.PureTests/*.cs`

**Forbidden changes**

- Do not add StardewUI `.sml` files in this phase.
- Do not change GMCM option IDs.
- Do not change reset/save behavior.

**Steps**

1. File(s): `RoosterControl/Integrations/StardewUi/Compat/RoosterCompatEditorService.cs`
   - Symbol(s): `RoosterCompatEditorService`
   - Exact edit: add shared load/apply methods for egg-source selections, rooster-provider selections, summary text, and validation summary.
2. File(s): `RoosterControl/Integrations/StardewUi/Compat/RoosterCompatEditorCatalog.cs`
   - Symbol(s): `RoosterCompatEditorCatalog`, `RoosterCompatEggKindRecord`, `RoosterCompatProviderRecord`, `RoosterCompatValidationRecord`
   - Exact edit: add immutable snapshot records used by both GMCM and future StardewUI.
3. File(s): `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
   - Symbol(s): `BuildCompatPage(...)`, summary/validation helpers used there
   - Exact edit: replace inline catalog/summary construction with service calls; keep the GMCM layout and IDs unchanged.
4. File(s): `Tests/RoosterControl.PureTests/RoosterCompatEditorServiceTests.cs`
   - Symbol(s): new test class
   - Exact edit: add tests for ordering, summary generation, egg-source apply behavior, and rooster-provider apply behavior.
5. File(s): docs files
   - Symbol(s): relevant phase sections
   - Exact edit: mark duplicate StardewUI compat work as planned; mark GMCM compat for later removal only.

**Build gate command(s)**

- `dotnet build RoosterControl/RoosterControl.csproj`
- `dotnet build Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj`
- `dotnet build Tests/RoosterControl.Tests/RoosterControl.Tests.csproj`

**Acceptance criteria**

- [ ] `RoosterCompatEditorService` exists and compiles.
- [ ] `BuildCompatPage(...)` still exists and uses shared service state.
- [ ] No current GMCM compat option ID changed.
- [ ] New tests compile.
- [ ] Docs mark GMCM compat as retained for now.

**Commit message**

- `phase-rctrl-sui-compat-01: extract shared compat editor state`

**Stop conditions**

- Stop if any current compat behavior depends on a GMCM-only callback that cannot be represented in the shared service.
- Stop if extraction changes current summary/reset behavior.
- Stop if persisted config members would need renaming.

### Phase 2 — Add duplicate StardewUI compat editor

**Objective**: add a StardewUI compat editor while keeping the GMCM compat page intact.

**Allowed files**

- `RoosterControl/Application/RoosterCoordinator.StardewUi.cs`
- `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
- `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`
- `RoosterControl/Integrations/StardewUi/Compat/*.cs`
- `RoosterControl/assets/views/RoosterCompatEditor.sml`
- `RoosterControl/i18n/default.json`
- `RoosterControl/i18n/pt-br.json`
- `RoosterControl/docs/PLAN.md`
- `RoosterControl/docs/CHECKLIST.md`
- `RoosterControl/docs/TODO.md`
- `Tests/RoosterControl.Tests/*.cs`

**Forbidden changes**

- Do not remove the GMCM compat page.
- Do not remove the White rooster produce editor.
- Do not add a new public API.

**Steps**

1. File(s): `RoosterControl/Application/RoosterCoordinator.StardewUi.cs`
   - Symbol(s): new menu id `rooster.compat.editor`, open method
   - Exact edit: register a second menu on the existing StardewUI host for compat editing.
2. File(s): `RoosterControl/Integrations/StardewUi/Compat/RoosterCompatEditorViewModel.cs`
   - Symbol(s): `RoosterCompatEditorViewModel`
   - Exact edit: add grouped selection state, summary/validation text, save/discard/reset commands, and StarML-safe wrapper methods.
3. File(s): `RoosterControl/assets/views/RoosterCompatEditor.sml`
   - Symbol(s): StarML view
   - Exact edit: add safe-subset list/detail editor for compat selections and warnings.
4. File(s): `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`, i18n, docs
   - Symbol(s): root page build method
   - Exact edit: add `compat.openStardewUi` action and status/help paragraph; keep current GMCM compat path.
5. File(s): `Tests/RoosterControl.Tests/RoosterCompatEditorViewModelTests.cs`
   - Symbol(s): new test class
   - Exact edit: add tests for selection persistence, validation display, and save/reset behavior.

**Build gate command(s)**

- `dotnet build RoosterControl/RoosterControl.csproj`
- `dotnet build Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj`
- `dotnet build Tests/RoosterControl.Tests/RoosterControl.Tests.csproj`

**Acceptance criteria**

- [ ] `rooster.compat.editor` is registered.
- [ ] GMCM root exposes `compat.openStardewUi`.
- [ ] GMCM compat page still exists.
- [ ] The StardewUI editor uses shared service state.
- [ ] Docs mark GMCM compat for later removal only.

**Commit message**

- `phase-rctrl-sui-compat-02: add duplicate StardewUI compat editor`

**Stop conditions**

- Stop if the current StardewUI runtime cannot support the required selection UI without a new NestCore primitive.
- Stop if save/reset diverges from current GMCM behavior.
- Stop if existing GMCM root IDs would need to change.

### Phase 3 — Add duplicate StardewUI females editor

**Objective**: add a StardewUI females assignment editor while keeping the GMCM females page unchanged.

**Allowed files**

- `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
- `RoosterControl/Application/RoosterCoordinator.StardewUi.cs`
- `RoosterControl/Integrations/Gmcm/Pages/RoosterRootPage.cs`
- `RoosterControl/Integrations/StardewUi/Females/*.cs`
- `RoosterControl/assets/views/RoosterFemalesEditor.sml`
- `RoosterControl/i18n/default.json`
- `RoosterControl/i18n/pt-br.json`
- `RoosterControl/docs/PLAN.md`
- `RoosterControl/docs/CHECKLIST.md`
- `RoosterControl/docs/TODO.md`
- `Tests/RoosterControl.Tests/*.cs`

**Forbidden changes**

- Do not remove or rename the GMCM females page.
- Do not change `FemalesOptions` schema.

**Steps**

1. File(s): `RoosterControl/Integrations/StardewUi/Females/RoosterFemalesEditorService.cs`, `RoosterControl/Integrations/StardewUi/Females/RoosterFemalesEditorViewModel.cs`
   - Symbol(s): `RoosterFemalesEditorService`, `RoosterFemalesEditorViewModel`
   - Exact edit: add shared load/apply logic plus searchable StardewUI assignment state.
2. File(s): `RoosterControl/Application/RoosterCoordinator.StardewUi.cs`
   - Symbol(s): new menu id `rooster.females.editor`
   - Exact edit: register and open the duplicate females editor through the existing host.
3. File(s): `RoosterControl/assets/views/RoosterFemalesEditor.sml`, root page, i18n, docs
   - Symbol(s): root page build method
   - Exact edit: add `females.openStardewUi` action and StardewUI view while keeping GMCM intact.
4. File(s): `Tests/RoosterControl.Tests/RoosterFemalesEditorViewModelTests.cs`
   - Symbol(s): new test class
   - Exact edit: add tests for filtering, assignment changes, discard, and save.

**Build gate command(s)**

- `dotnet build RoosterControl/RoosterControl.csproj`
- `dotnet build Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj`
- `dotnet build Tests/RoosterControl.Tests/RoosterControl.Tests.csproj`

**Acceptance criteria**

- [ ] `rooster.females.editor` is registered.
- [ ] GMCM root exposes `females.openStardewUi`.
- [ ] GMCM females page still exists.
- [ ] The StardewUI editor saves back into `FemalesOptions`.
- [ ] Docs mark GMCM females for later removal only.

**Commit message**

- `phase-rctrl-sui-females-03: add duplicate StardewUI females editor`

**Stop conditions**

- Stop if female assignment behavior depends on GMCM-only widgets.
- Stop if `FemalesOptions` would need schema changes.
- Stop if root-page IDs would need renaming.

---

## Plan B — `LetMeHatch`

**Status note**: the inert duplicate SML shells already exist for `LmhBreedingEditor.sml`, `LmhColorsEditor.sml`, and `LmhAdvancedMappingsEditor.sml`. The phases below describe the remaining migration work to wire them.

### Inputs

- Target subdir: `LetMeHatch`
- Exact files/symbols likely involved:
  - `LetMeHatch/Application/Coordination/LmhCoordinator.cs`
  - `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
  - `LetMeHatch/Config/ModConfig.cs`
  - `LetMeHatch/Integrations/StardewUi/Breeding/*.cs`
  - `LetMeHatch/Integrations/StardewUi/Colors/*.cs`
  - `LetMeHatch/Integrations/StardewUi/Advanced/*.cs`
  - `LetMeHatch/assets/views/*.sml`
  - `LetMeHatch/i18n/default.json`
  - `LetMeHatch/i18n/pt-br.json`
  - `LetMeHatch/docs/PLAN.md`
  - `LetMeHatch/docs/CHECKLIST.md`
  - `LetMeHatch/docs/TODO.md`
  - `Tests/LetMeHatch.Tests/*.cs`
  - `Tests/LetMeHatch.Core.Tests/*.cs`
- Must preserve behavior: `yes`
- Forbidden scope:
  - Do not remove any GMCM breeding, colors, or advanced pages.
  - Do not rename `ItemToClone`, `EggColors`, or breeding config members.
  - Do not change content-pack resolution logic.

### Phase 1 — Extract reusable breeding editor state

**Objective**: move breeding pack/female snapshot and apply logic into shared services while keeping current GMCM breeding pages unchanged.

**Allowed files**

- `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
- `LetMeHatch/Integrations/StardewUi/Breeding/*.cs`
- docs files
- `Tests/LetMeHatch.Tests/*.cs`
- `Tests/LetMeHatch.Core.Tests/*.cs`

**Forbidden changes**

- Do not add a StardewUI host yet.
- Do not change page IDs or the existing page tree.

**Steps**

1. File(s): `LetMeHatch/Integrations/StardewUi/Breeding/LmhBreedingEditorService.cs`
   - Symbol(s): `LmhBreedingEditorService`
   - Exact edit: add load/apply helpers for packs, females, chances, and per-female settings.
2. File(s): `LetMeHatch/Integrations/StardewUi/Breeding/LmhBreedingEditorCatalog.cs`
   - Symbol(s): `LmhBreedingPackRecord`, `LmhBreedingFemaleRecord`, `LmhBreedingDraft`
   - Exact edit: add immutable snapshot/draft records.
3. File(s): `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
   - Symbol(s): `BuildBreedingPages(...)`, `BuildBreedingPackIndexPage(...)`, `BuildFemaleBreedingPage(...)`
   - Exact edit: replace inline snapshot construction with service calls while preserving the GMCM page tree.
4. File(s): `Tests/LetMeHatch.Tests/LmhBreedingEditorServiceTests.cs`
   - Symbol(s): new test class
   - Exact edit: add tests for pack ordering, female ordering, apply behavior, and no-op saves.
5. File(s): docs files
   - Symbol(s): relevant phase sections
   - Exact edit: mark duplicate StardewUI breeding work as planned with GMCM retained.

**Build gate command(s)**

- `dotnet build LetMeHatch/LetMeHatch.csproj`
- `dotnet build Tests/LetMeHatch.Core.Tests/LetMeHatch.Core.Tests.csproj`
- `dotnet build Tests/LetMeHatch.Tests/LetMeHatch.Tests.csproj`

**Acceptance criteria**

- [ ] `LmhBreedingEditorService` exists and compiles.
- [ ] GMCM breeding pages still exist and use shared state.
- [ ] No page ID or option ID changed.
- [ ] New tests compile.
- [ ] Docs mark GMCM breeding as retained for now.

**Commit message**

- `phase-lmh-sui-breeding-01: extract shared breeding editor state`

**Stop conditions**

- Stop if shared extraction changes the current page-tree behavior.
- Stop if required data only exists in GMCM-local callbacks.
- Stop if persisted config keys would need renaming.

### Phase 2 — Add duplicate StardewUI breeding editor

**Objective**: add a StardewUI breeding editor while keeping GMCM breeding pages intact.

**Allowed files**

- `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs`
- `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
- `LetMeHatch/Integrations/StardewUi/Breeding/*.cs`
- `LetMeHatch/assets/views/LmhBreedingEditor.sml`
- i18n/docs files
- `Tests/LetMeHatch.Tests/*.cs`

**Forbidden changes**

- Do not remove the GMCM breeding page tree.
- Do not change breeding save timing.

**Steps**

1. File(s): `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs`
   - Symbol(s): StardewUI host fields, `OpenBreedingEditor()`
   - Exact edit: add a coordinator StardewUI host path and register menu id `lmh.breeding.editor`.
2. File(s): `LetMeHatch/Integrations/StardewUi/Breeding/LmhBreedingEditorViewModel.cs`
   - Symbol(s): `LmhBreedingEditorViewModel`
   - Exact edit: add pack/female navigation, detail tabs, save/discard/reload, and StarML-safe event wrapper methods.
3. File(s): `LetMeHatch/assets/views/LmhBreedingEditor.sml`
   - Symbol(s): StarML view
   - Exact edit: add safe-subset master-detail breeding editor.
4. File(s): `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`, i18n, docs
   - Symbol(s): root or breeding page build method
   - Exact edit: add `breeding.openStardewUi` action and status/help paragraph while keeping existing GMCM pages.
5. File(s): `Tests/LetMeHatch.Tests/LmhBreedingEditorViewModelTests.cs`
   - Symbol(s): new test class
   - Exact edit: add tests for pack/female selection, save/discard, and absent-StardewUI launcher no-op behavior.

**Build gate command(s)**

- `dotnet build LetMeHatch/LetMeHatch.csproj`
- `dotnet build Tests/LetMeHatch.Core.Tests/LetMeHatch.Core.Tests.csproj`
- `dotnet build Tests/LetMeHatch.Tests/LetMeHatch.Tests.csproj`

**Acceptance criteria**

- [ ] `lmh.breeding.editor` is registered.
- [ ] GMCM breeding pages still exist.
- [ ] GMCM exposes `breeding.openStardewUi`.
- [ ] The StardewUI editor uses shared breeding service state.
- [ ] Docs mark GMCM breeding for later removal only.

**Commit message**

- `phase-lmh-sui-breeding-02: add duplicate StardewUI breeding editor`

**Stop conditions**

- Stop if the current StardewUI runtime cannot express the pack/female editor without a new NestCore primitive.
- Stop if save/discard behavior would diverge from current GMCM behavior.
- Stop if existing GMCM page IDs would need changing.

### Phase 3 — Add duplicate StardewUI colors editor

**Objective**: add a StardewUI colors editor while keeping current GMCM colors pages intact.

**Allowed files**

- `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs`
- `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
- `LetMeHatch/Integrations/StardewUi/Colors/*.cs`
- `LetMeHatch/assets/views/LmhColorsEditor.sml`
- i18n/docs files
- `Tests/LetMeHatch.Tests/*.cs`

**Forbidden changes**

- Do not remove `colors.eggPalette` or `colors.eggColorOverrides`.
- Do not change `EggColors` or `EggColors_ColorObject` schema names.

**Steps**

1. File(s): `LetMeHatch/Integrations/StardewUi/Colors/LmhColorsEditorService.cs`, `LmhColorsEditorViewModel.cs`
   - Symbol(s): `LmhColorsEditorService`, `LmhColorsEditorViewModel`
   - Exact edit: add shared palette/override snapshot helpers and a preview-first StardewUI viewmodel.
2. File(s): `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs`
   - Symbol(s): menu id `lmh.colors.editor`
   - Exact edit: register and open a colors editor on the same StardewUI host created in Phase 2.
3. File(s): `LetMeHatch/assets/views/LmhColorsEditor.sml`, `LmhGmcmMenu.cs`, i18n, docs
   - Symbol(s): `BuildColorsPages(...)`
   - Exact edit: add `colors.openStardewUi` launcher and the duplicate view while keeping GMCM pages.
4. File(s): `Tests/LetMeHatch.Tests/LmhColorsEditorViewModelTests.cs`
   - Symbol(s): new test class
   - Exact edit: add tests for palette selection, override save/discard, and preview-state changes.

**Build gate command(s)**

- `dotnet build LetMeHatch/LetMeHatch.csproj`
- `dotnet build Tests/LetMeHatch.Core.Tests/LetMeHatch.Core.Tests.csproj`
- `dotnet build Tests/LetMeHatch.Tests/LetMeHatch.Tests.csproj`

**Acceptance criteria**

- [ ] `lmh.colors.editor` is registered.
- [ ] GMCM colors pages still exist.
- [ ] GMCM exposes `colors.openStardewUi`.
- [ ] The new editor uses shared service state.
- [ ] Docs mark GMCM colors for later removal only.

**Commit message**

- `phase-lmh-sui-colors-03: add duplicate StardewUI colors editor`

**Stop conditions**

- Stop if color preview/editing needs a missing NestCore primitive.
- Stop if save behavior diverges from current config semantics.
- Stop if duplicate launchers would require deleting GMCM links.

### Phase 4 — Add duplicate StardewUI advanced item-mappings editor

**Objective**: add a StardewUI advanced mapping editor while leaving existing GMCM advanced pages intact.

**Allowed files**

- `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs`
- `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
- `LetMeHatch/Integrations/StardewUi/Advanced/*.cs`
- `LetMeHatch/assets/views/LmhAdvancedMappingsEditor.sml`
- i18n/docs files
- `Tests/LetMeHatch.Tests/*.cs`

**Forbidden changes**

- Do not remove advanced GMCM pages or links.
- Do not rename `ItemToClone`.

**Steps**

1. File(s): `LetMeHatch/Integrations/StardewUi/Advanced/LmhItemMappingsEditorService.cs`, `LmhItemMappingsEditorViewModel.cs`
   - Symbol(s): `LmhItemMappingsEditorService`, `LmhItemMappingsEditorViewModel`
   - Exact edit: add grouped mapping snapshot/apply helpers and comparison-oriented StardewUI state.
2. File(s): `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs`
   - Symbol(s): menu id `lmh.advancedMappings.editor`
   - Exact edit: register and open a third editor on the existing host.
3. File(s): `LetMeHatch/assets/views/LmhAdvancedMappingsEditor.sml`, `LmhGmcmMenu.cs`, i18n, docs
   - Symbol(s): `BuildAdvancedPages(...)`
   - Exact edit: add `advanced.itemMappings.openStardewUi` launcher and duplicate view while keeping GMCM pages.
4. File(s): `Tests/LetMeHatch.Tests/LmhItemMappingsEditorViewModelTests.cs`
   - Symbol(s): new test class
   - Exact edit: add tests for grouped mapping display, edit/save/discard, and comparison summaries.

**Build gate command(s)**

- `dotnet build LetMeHatch/LetMeHatch.csproj`
- `dotnet build Tests/LetMeHatch.Core.Tests/LetMeHatch.Core.Tests.csproj`
- `dotnet build Tests/LetMeHatch.Tests/LetMeHatch.Tests.csproj`

**Acceptance criteria**

- [ ] `lmh.advancedMappings.editor` is registered.
- [ ] GMCM advanced item-mapping pages still exist.
- [ ] GMCM exposes `advanced.itemMappings.openStardewUi`.
- [ ] The duplicate editor saves back into `ItemToClone` with no schema changes.
- [ ] Docs mark GMCM advanced mappings for later removal only.

**Commit message**

- `phase-lmh-sui-advanced-04: add duplicate StardewUI item mappings editor`

**Stop conditions**

- Stop if mapping preview requires a missing NestCore primitive.
- Stop if the mapping editor requires changing `ItemToClone` shape.
- Stop if launcher additions would rename/remove current GMCM IDs.

---

## Plan C — `EggsInWild`

**Status note**: the inert duplicate SML shell already exists for `assets/views/animalProgression/editor.sml`. The phases below describe the remaining migration work to wire it.

### Inputs

- Target subdir: `EggsInWild`
- Exact files/symbols likely involved:
  - `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`
  - `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmContext.cs`
  - `EggsInWild/ModEntry.StardewUi.cs`
  - `EggsInWild/Infrastructure/Config/ModConfig.cs`
  - `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/*.cs`
  - `EggsInWild/assets/views/animalProgression/editor.sml`
  - `EggsInWild/i18n/default.json`
  - `EggsInWild/docs/PLAN.md`
  - `EggsInWild/docs/CHECKLIST.md`
  - `EggsInWild/docs/TODO.md`
  - `Tests/EggsInWild.Tests/*.cs`
- Must preserve behavior: `yes`
- Forbidden scope:
  - Do not remove the current GMCM Animal Progression page.
  - Do not change `AnimalProgressionMode` schema.
  - Do not change WildAnimals editor behavior.

### Phase 1 — Extract reusable Animal Progression editor state

**Objective**: move Animal Progression list/build/apply logic into shared service types while keeping the GMCM page unchanged.

**Allowed files**

- `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`
- `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/*.cs`
- docs files
- `Tests/EggsInWild.Tests/*.cs`

**Forbidden changes**

- Do not add StardewUI assets in this phase.
- Do not change GMCM Animal Progression page ID or option IDs.
- Do not alter progression policy behavior.

**Steps**

1. File(s): `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/AnimalProgressionEditorService.cs`
   - Symbol(s): `AnimalProgressionEditorService`
   - Exact edit: add load/apply helpers for current farm-animal progression entries and configured/default resolution.
2. File(s): `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/AnimalProgressionEditorCatalog.cs`
   - Symbol(s): `AnimalProgressionAnimalRecord`, `AnimalProgressionDraft`
   - Exact edit: add immutable snapshot and draft records.
3. File(s): `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`
   - Symbol(s): `BuildAnimalProgressionPage(...)`
   - Exact edit: replace direct inline farm-animal iteration with service snapshot calls while preserving the page and IDs.
4. File(s): `Tests/EggsInWild.Tests/AnimalProgressionEditorServiceTests.cs`
   - Symbol(s): new test class
   - Exact edit: add tests for ordering, configured/default resolution, and apply behavior.
5. File(s): docs files
   - Symbol(s): relevant phase sections
   - Exact edit: mark duplicate StardewUI Animal Progression as planned with GMCM retained.

**Build gate command(s)**

- `dotnet build EggsInWild/EggsInWild.csproj -p:EnableModDeploy=false`
- `dotnet build Tests/EggsInWild.Tests/EggsInWild.Tests.csproj`

**Acceptance criteria**

- [ ] `AnimalProgressionEditorService` exists and compiles.
- [ ] GMCM Animal Progression page still exists and uses shared service state.
- [ ] No `AnimalProgressionMode` schema or GMCM IDs changed.
- [ ] New tests compile.
- [ ] Docs mark GMCM Animal Progression for later removal only.

**Commit message**

- `phase-eiw-sui-progression-01: extract shared animal progression editor state`

**Stop conditions**

- Stop if the GMCM page depends on inline logic that cannot be shared without changing behavior.
- Stop if persisted config keys would need renaming.
- Stop if farm-animal loading behavior changes.

### Phase 2 — Add duplicate StardewUI Animal Progression editor

**Objective**: add a StardewUI Animal Progression editor while keeping the GMCM page intact.

**Allowed files**

- `EggsInWild/ModEntry.StardewUi.cs`
- `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`
- `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/*.cs`
- `EggsInWild/assets/views/animalProgression/editor.sml`
- `EggsInWild/i18n/default.json`
- docs files
- `Tests/EggsInWild.Tests/*.cs`

**Forbidden changes**

- Do not remove the GMCM Animal Progression page.
- Do not change the WildAnimals editor menu id.
- Do not add a second persisted keybind in this phase.

**Steps**

1. File(s): `EggsInWild/ModEntry.StardewUi.cs`
   - Symbol(s): menu id `animalProgressionEditor`, `OpenAnimalProgressionEditor()`
   - Exact edit: register a second StardewUI menu on the existing EggsInWild host.
2. File(s): `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/AnimalProgressionEditorViewModel.cs`
   - Symbol(s): `AnimalProgressionEditorViewModel`
   - Exact edit: add searchable list/detail progression state, status badges, save/discard/reload commands, and StarML-safe wrapper methods.
3. File(s): `EggsInWild/assets/views/animalProgression/editor.sml`
   - Symbol(s): StarML view
   - Exact edit: add safe-subset master-detail progression editor.
4. File(s): `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`, i18n, docs
   - Symbol(s): root and/or Animal Progression page build methods
   - Exact edit: add `animalProgression.openEditor` duplicate launcher and help/status text while keeping the current page link and page body.
5. File(s): `Tests/EggsInWild.Tests/AnimalProgressionEditorViewModelTests.cs`
   - Symbol(s): new test class
   - Exact edit: add tests for search/filter behavior, configured/default status, and save/discard.

**Build gate command(s)**

- `dotnet build EggsInWild/EggsInWild.csproj -p:EnableModDeploy=false`
- `dotnet build Tests/EggsInWild.Tests/EggsInWild.Tests.csproj`

**Acceptance criteria**

- [ ] `animalProgressionEditor` is registered on the existing EggsInWild StardewUI host.
- [ ] GMCM still exposes the original page and the new duplicate launcher.
- [ ] The new editor saves back into `AnimalProgressionMode`.
- [ ] New tests compile.
- [ ] Docs explicitly say GMCM removal is deferred.

**Commit message**

- `phase-eiw-sui-progression-02: add duplicate StardewUI animal progression editor`

**Stop conditions**

- Stop if the current StardewUI runtime subset cannot support the required UI without a new NestCore primitive.
- Stop if the editor would require deleting the current GMCM page.
- Stop if save behavior diverges from current GMCM behavior.

## Strict rename table / grep gates

| Phase end | Exact gate | Expected result | Fail-fast rule |
|---|---|---|---|
| RoosterControl Phase 1 | `rg -n "class RoosterCompatEditorService\\b" RoosterControl/Integrations/StardewUi/Compat` | `1` match | If `0`, stop; do not start the StardewUI compat phase. |
| RoosterControl Phase 2 | `rg -n "rooster\\.compat\\.editor|compat\\.openStardewUi" RoosterControl` | at least `3` matches | If fewer than `3`, stop; duplicate compat wiring is incomplete. |
| RoosterControl Phase 2 | `rg -n "BuildCompatPage\\(" RoosterControl/Application/RoosterCoordinator.Gmcm.cs` | `1` match | If `0`, stop; GMCM compat was removed or renamed. |
| RoosterControl Phase 3 | `rg -n "rooster\\.females\\.editor|females\\.openStardewUi" RoosterControl` | at least `3` matches | If fewer than `3`, stop; duplicate females wiring is incomplete. |
| LetMeHatch Phase 1 | `rg -n "class LmhBreedingEditorService\\b" LetMeHatch/Integrations/StardewUi/Breeding` | `1` match | If `0`, stop; do not start the StardewUI breeding phase. |
| LetMeHatch Phase 2 | `rg -n "lmh\\.breeding\\.editor|breeding\\.openStardewUi" LetMeHatch` | at least `3` matches | If fewer than `3`, stop; duplicate breeding wiring is incomplete. |
| LetMeHatch Phase 2 | `rg -n "BuildBreedingPages\\(" LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs` | `1` match | If `0`, stop; GMCM breeding was removed or renamed. |
| LetMeHatch Phase 3 | `rg -n "lmh\\.colors\\.editor|colors\\.openStardewUi" LetMeHatch` | at least `3` matches | If fewer than `3`, stop; duplicate colors wiring is incomplete. |
| LetMeHatch Phase 4 | `rg -n "lmh\\.advancedMappings\\.editor|advanced\\.itemMappings\\.openStardewUi" LetMeHatch` | at least `3` matches | If fewer than `3`, stop; duplicate advanced mappings wiring is incomplete. |
| EggsInWild Phase 1 | `rg -n "class AnimalProgressionEditorService\\b" EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression` | `1` match | If `0`, stop; do not start the StardewUI progression phase. |
| EggsInWild Phase 2 | `rg -n "animalProgressionEditor|animalProgression\\.openEditor" EggsInWild` | at least `3` matches | If fewer than `3`, stop; duplicate progression wiring is incomplete. |
| EggsInWild Phase 2 | `rg -n "DefineAnimalProgressionPage\\(" EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs` | `1` match | If `0`, stop; GMCM Animal Progression was removed or renamed. |

## Strict checklist / exact edits

| Subdir | Exact add/edit | Expected grep/count |
|---|---|---|
| `RoosterControl` | Add `RoosterControl/Integrations/StardewUi/Compat/RoosterCompatEditorService.cs` | `1` class match |
| `RoosterControl` | Add `RoosterControl/Integrations/StardewUi/Compat/RoosterCompatEditorViewModel.cs` | `1` class match |
| `RoosterControl` | Add `RoosterControl/assets/views/RoosterCompatEditor.sml` | at least `2` `RoosterCompatEditor` matches |
| `RoosterControl` | Add GMCM action `compat.openStardewUi` | at least `2` matches |
| `RoosterControl` | Add `RoosterControl/Integrations/StardewUi/Females/RoosterFemalesEditorService.cs` | `1` class match |
| `RoosterControl` | Add `RoosterControl/assets/views/RoosterFemalesEditor.sml` | at least `2` `RoosterFemalesEditor` matches |
| `RoosterControl` | Add GMCM action `females.openStardewUi` | at least `2` matches |
| `LetMeHatch` | Add `LetMeHatch/Application/Coordination/LmhCoordinator.StardewUi.cs` | at least `3` matches for open methods/host wiring |
| `LetMeHatch` | Add `LetMeHatch/Integrations/StardewUi/Breeding/LmhBreedingEditorService.cs` | `1` class match |
| `LetMeHatch` | Add `LetMeHatch/assets/views/LmhBreedingEditor.sml` | at least `2` `LmhBreedingEditor` matches |
| `LetMeHatch` | Add GMCM action `breeding.openStardewUi` | at least `2` matches |
| `LetMeHatch` | Add `LetMeHatch/Integrations/StardewUi/Colors/LmhColorsEditorService.cs` | `1` class match |
| `LetMeHatch` | Add `LetMeHatch/assets/views/LmhColorsEditor.sml` | at least `2` `LmhColorsEditor` matches |
| `LetMeHatch` | Add GMCM action `colors.openStardewUi` | at least `2` matches |
| `LetMeHatch` | Add `LetMeHatch/Integrations/StardewUi/Advanced/LmhItemMappingsEditorService.cs` | `1` class match |
| `LetMeHatch` | Add `LetMeHatch/assets/views/LmhAdvancedMappingsEditor.sml` | at least `2` `LmhAdvancedMappingsEditor` matches |
| `LetMeHatch` | Add GMCM action `advanced.itemMappings.openStardewUi` | at least `2` matches |
| `EggsInWild` | Add `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/AnimalProgressionEditorService.cs` | `1` class match |
| `EggsInWild` | Add `EggsInWild/Infrastructure/Integrations/StardewUi/AnimalProgression/AnimalProgressionEditorViewModel.cs` | `1` class match |
| `EggsInWild` | Add `EggsInWild/assets/views/animalProgression/editor.sml` | at least `2` `animalProgressionEditor` or view-path matches |
| `EggsInWild` | Add GMCM action `animalProgression.openEditor` while keeping `root.Link("animalProgression"` | both grep matches present |

## Assumptions

- No new NestCore primitive is needed if the duplicate editors stay within the older-safe StardewUI markup subset already required by the current runtime.
- `RoosterControl` can extend its existing StardewUI host pattern to additional editors.
- `LetMeHatch` needs a new coordinator StardewUI partial because it currently has no StardewUI host path.
- `EggsInWild` can host Animal Progression on the existing StardewUI host used by WildAnimals.
- If a phase discovers a missing reusable primitive in NestCore, that subdir’s plan pauses and a new NestCore-first phase must be inserted before continuing.
