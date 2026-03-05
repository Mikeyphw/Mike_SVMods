# Repo-wide R2 — Monolithic File Splits

## Overview

This document defines the R2 refactoring effort to split monolithic files across the Mike_SVMods solution into smaller, more maintainable files using partial classes and dedicated type files.

## Verification Script

Use `scripts/verify_r2.sh` to verify phase completions:

```bash
# All phases, grep-gates only
bash scripts/verify_r2.sh /path/to/Mike_SVMods

# Only EIW-R2-1
bash scripts/verify_r2.sh /path/to/Mike_SVMods --phase EIW-R2-1

# Multiple phases
bash scripts/verify_r2.sh /path/to/Mike_SVMods --phase EIW-R2-1 --phase LMH-R2-2

# Grep-gates + build
bash scripts/verify_r2.sh /path/to/Mike_SVMods --build

# Selected phases + build
bash scripts/verify_r2.sh /path/to/Mike_SVMods --phase RC-R2-1 --build
```

### Valid `--phase` IDs

- `EIW-R2-1`, `EIW-R2-2`, `EIW-R2-3`, `EIW-R2-4`
- `LMH-R2-1`, `LMH-R2-2`
- `RC-R2-1`, `RC-R2-2`
- `ALL` (default)

## Goals

- Split large monolithic files into focused, single-responsibility files
- Convert target classes to `partial` where appropriate
- Move helper types into their own dedicated files
- Preserve all behavior (no logic changes)
- Maintain build-clean at each phase

## Global Rules

- **Preserve behavior**: YES (no logic changes; only move code between files)
- **Do not rename**:
  - namespaces
  - types
  - methods
  - members
  - config keys / i18n keys
- **Do not change public APIs**
- **Do not introduce new abstractions**
- **Every phase must**:
  - End build-clean
  - Create **exactly one** commit (message provided)

## Build Gate

Run from repo root `Mike_SVMods/`:

```bash
dotnet build NestCore/NestCore.csproj
dotnet build EggsInWild/EggsInWild.csproj
dotnet build LetMeHatch/LetMeHatch.csproj
dotnet build RoosterControl/RoosterControl.csproj
dotnet build Mike_SVMods.sln
```

## Stop Conditions

Stop and report if:

- Any moved type/method requires changing accessibility (public/internal/etc.)
- Any moved code requires adding/removing `using` directives outside moved blocks
- Build fails and the fix is not a missing `using` or missing file include (SDK-style should auto-include)
- Any file contains multiple partial definitions with conflicting modifiers

---

## Phase Summary

| Mod | Phase | Target File | Description |
|-----|-------|-------------|-------------|
| EggsInWild | EIW-R2-1 | DebugKnobs.cs | Split 15 config POCO classes into separate files |
| EggsInWild | EIW-R2-2 | WeggVerifier.cs | Split 8 helper types into separate files |
| EggsInWild | EIW-R2-3 | AnimalNotesOrchestrator.cs | Extract 5 Dump* models into DumpModels.cs |
| EggsInWild | EIW-R2-4 | AnimalNotesOrchestrator.cs | Split into 7 partial files by method group |
| LetMeHatch | LMH-R2-1 | ItemManager.cs | Move #region Eggs into partial file |
| LetMeHatch | LMH-R2-2 | ItemManager.cs | Move #region Items into partial file |
| RoosterControl | RC-R2-1 | RoosterControl.cs | Extract 5 debug record models into separate file |
| RoosterControl | RC-R2-2 | RoosterHelper.cs | Split debug methods into partial file |

---

# Plan A — EggsInWild

## Target Files

- `EggsInWild/Infrastructure/Config/DebugKnobs.cs` (805 LOC, **16 classes**)
- `EggsInWild/Infrastructure/Diagnostics/WeggVerifier.cs` (1123 LOC, **9 types**)
- `EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.cs` (1459 LOC, **6 classes** + 53 methods)

---

## Phase EIW-R2-1 — Split `DebugKnobs.cs` config types into separate files

### Objective

Move the 15 config POCO classes out of `DebugKnobs.cs`, leaving only `internal static class DebugKnobs` in that file.

### Allowed Files

- `EggsInWild/Infrastructure/Config/DebugKnobs.cs`
- `EggsInWild/Infrastructure/Config/DebugKnobs/*.cs` (new files)
- `EggsInWild/docs/PLAN.md`
- `EggsInWild/docs/CHECKLIST.md`
- `EggsInWild/docs/TODO.md`

### Forbidden Changes

- No changes inside any property bodies of `DebugKnobs`
- No namespace changes (keep `namespace EggsInWild.Infrastructure.Config;`)
- No type renames

### Steps

1. Create folder: `EggsInWild/Infrastructure/Config/DebugKnobs/`
2. Create **15 new files** and move **exactly one type** into each (copy/paste type as-is, same namespace):

   - `DebugKnobsConfig.cs` → `class DebugKnobsConfig`
   - `DesertTombKnobs.cs` → `class DesertTombKnobs`
   - `DesertTombGenerationKnobs.cs` → `class DesertTombGenerationKnobs`
   - `DesertTombSpawningKnobs.cs` → `class DesertTombSpawningKnobs`
   - `DesertTombPopulationKnobs.cs` → `class DesertTombPopulationKnobs`
   - `DesertTombLootLuckKnobs.cs` → `class DesertTombLootLuckKnobs`
   - `DesertTombBreakablesKnobs.cs` → `class DesertTombBreakablesKnobs`
   - `SpawningChanceModifiersKnobs.cs` → `class SpawningChanceModifiersKnobs`
   - `WeightingKnobs.cs` → `class WeightingKnobs`
   - `MonstersKnobs.cs` → `class MonstersKnobs`
   - `PharaohKnobs.cs` → `class PharaohKnobs`
   - `SandiKnobs.cs` → `class SandiKnobs`
   - `CrawlerKnobs.cs` → `class CrawlerKnobs`
   - `HurricaneKnobs.cs` → `class HurricaneKnobs`
   - `MatangoKnobs.cs` → `class MatangoKnobs`

3. In `DebugKnobs.cs`, delete the moved type declarations (leave only `DebugKnobs`).
4. Update `EggsInWild/docs/PLAN.md`, `CHECKLIST.md`, `TODO.md` with a new phase header `R2 (Monolithic file splits)` and mark Phase EIW-R2-1 complete.

### Build Gate

Run Global Build gate.

### Acceptance Criteria

- `DebugKnobs.cs` contains **only** `internal static class DebugKnobs`
- each moved config type exists exactly once in the new folder
- build passes

### Commit Message

`r2-eiw-01: split DebugKnobs config types into files`

### Strict Grep Gates

```bash
# must be 0:
rg -n 'class (DebugKnobsConfig|DesertTombKnobs|DesertTombGenerationKnobs|DesertTombSpawningKnobs|DesertTombPopulationKnobs|DesertTombLootLuckKnobs|DesertTombBreakablesKnobs|SpawningChanceModifiersKnobs|WeightingKnobs|MonstersKnobs|PharaohKnobs|SandiKnobs|CrawlerKnobs|HurricaneKnobs|MatangoKnobs)\b' EggsInWild/Infrastructure/Config/DebugKnobs.cs

# must be 15:
rg -n '^\s*(internal|public)?\s*class (DebugKnobsConfig|DesertTombKnobs|DesertTombGenerationKnobs|DesertTombSpawningKnobs|DesertTombPopulationKnobs|DesertTombLootLuckKnobs|DesertTombBreakablesKnobs|SpawningChanceModifiersKnobs|WeightingKnobs|MonstersKnobs|PharaohKnobs|SandiKnobs|CrawlerKnobs|HurricaneKnobs|MatangoKnobs)\b' EggsInWild/Infrastructure/Config/DebugKnobs -g'*.cs' | wc -l
```

---

## Phase EIW-R2-2 — Split `WeggVerifier.cs` helper types into separate files

### Objective

Move all helper types out of `WeggVerifier.cs` so it contains only `class WeggVerifier`.

### Allowed Files

- `EggsInWild/Infrastructure/Diagnostics/WeggVerifier.cs`
- `EggsInWild/Infrastructure/Diagnostics/WeggVerify/*.cs` (new)
- `EggsInWild/docs/PLAN.md`
- `EggsInWild/docs/CHECKLIST.md`
- `EggsInWild/docs/TODO.md`

### Forbidden Changes

- No changes to logic in `WeggVerifier.Run(...)`
- No namespace changes (keep original namespace used by these types)
- No type renames

### Steps

1. Create folder: `EggsInWild/Infrastructure/Diagnostics/WeggVerify/`
2. Create files (one type per file, moved as-is):

   - `WeggVerifyOptions.cs` → `record WeggVerifyOptions`
   - `WeggVerifyResult.cs` → `record WeggVerifyResult`
   - `WeggVerifyStatus.cs` → `enum WeggVerifyStatus`
   - `WeggVerifyCheck.cs` → `record WeggVerifyCheck`
   - `WeggVerifyReport.cs` → `class WeggVerifyReport`
   - `IContentPatcherApiProbe.cs` → `interface IContentPatcherApiProbe`
   - `WeightRow.cs` → `record WeightRow`
   - `WeightRowSet.cs` → `class WeightRowSet`

3. In `WeggVerifier.cs`, delete the moved declarations; keep only `class WeggVerifier` (and its members).
4. Update EggsInWild docs and mark Phase EIW-R2-2 complete.

### Build Gate

Run Global Build gate.

### Acceptance Criteria

- `WeggVerifier.cs` has exactly **one** type declaration: `class WeggVerifier`
- all moved helper types compile and are referenced normally
- build passes

### Commit Message

`r2-eiw-02: split WeggVerifier helper types into files`

### Strict Grep Gates

```bash
# must be 0:
rg -n '^(public|internal)?\s*(record|class|enum|interface)\s+(WeggVerifyOptions|WeggVerifyResult|WeggVerifyStatus|WeggVerifyCheck|WeggVerifyReport|IContentPatcherApiProbe|WeightRow|WeightRowSet)\b' EggsInWild/Infrastructure/Diagnostics/WeggVerifier.cs

# must be 8:
rg -n '^(public|internal)?\s*(record|class|enum|interface)\s+(WeggVerifyOptions|WeggVerifyResult|WeggVerifyStatus|WeggVerifyCheck|WeggVerifyReport|IContentPatcherApiProbe|WeightRow|WeightRowSet)\b' EggsInWild/Infrastructure/Diagnostics/WeggVerify -g'*.cs' | wc -l
```

---

## Phase EIW-R2-3 — Extract `Dump*` models from `AnimalNotesOrchestrator.cs`

### Objective

Move the 5 `Dump*` helper classes into a dedicated file.

### Allowed Files

- `EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.cs`
- `EggsInWild/Application/AnimalNotes/DumpModels.cs` (new)
- `EggsInWild/docs/PLAN.md`
- `EggsInWild/docs/CHECKLIST.md`
- `EggsInWild/docs/TODO.md`

### Forbidden Changes

- No changes to orchestrator logic
- No namespace changes (`EggsInWild.Application.AnimalNotes`)
- No type renames

### Steps

1. Create: `EggsInWild/Application/AnimalNotes/DumpModels.cs`
2. Move these types (as-is, same namespace):

   - `class DumpModel`
   - `class DumpConfigModel`
   - `class DumpFarmerModel`
   - `class DumpRecordModel`
   - `class DumpRecordFarmerModel`

3. Remove these 5 type declarations from `AnimalNotesOrchestrator.cs`.
4. Update EggsInWild docs and mark Phase EIW-R2-3 complete.

### Build Gate

Run Global Build gate.

### Acceptance Criteria

- Orchestrator file contains only `AnimalNotesOrchestrator` type
- DumpModels file contains exactly those 5 classes
- build passes

### Commit Message

`r2-eiw-03: extract AnimalNotes dump models into file`

### Strict Grep Gates

```bash
# must be 0:
rg -n 'class (DumpModel|DumpConfigModel|DumpFarmerModel|DumpRecordModel|DumpRecordFarmerModel)\b' EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.cs

# must be 5:
rg -n 'class (DumpModel|DumpConfigModel|DumpFarmerModel|DumpRecordModel|DumpRecordFarmerModel)\b' EggsInWild/Application/AnimalNotes/DumpModels.cs | wc -l
```

---

## Phase EIW-R2-4 — Split `AnimalNotesOrchestrator` into partials by method list

### Objective

Convert `AnimalNotesOrchestrator` into a `partial` class and move methods into deterministic partial files.

### Allowed Files

- `EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.cs`
- `EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.*.cs` (new partials)
- `EggsInWild/docs/PLAN.md`
- `EggsInWild/docs/CHECKLIST.md`
- `EggsInWild/docs/TODO.md`

### Forbidden Changes

- No changes to method bodies while moving
- No signature changes
- No renames
- No new helper abstractions

### Steps

1. In `AnimalNotesOrchestrator.cs`, change:

   - `public sealed class AnimalNotesOrchestrator` → `public sealed partial class AnimalNotesOrchestrator`

2. Create these files (same namespace, same `public sealed partial class AnimalNotesOrchestrator` header):

   - `AnimalNotesOrchestrator.Api.cs`
   - `AnimalNotesOrchestrator.ClientEvents.cs`
   - `AnimalNotesOrchestrator.GameEvents.cs`
   - `AnimalNotesOrchestrator.InitAndSave.cs`
   - `AnimalNotesOrchestrator.Reconcile.cs`
   - `AnimalNotesOrchestrator.PendingProcessing.cs`
   - `AnimalNotesOrchestrator.FarmerState.cs`

3. Move methods **exactly** as listed:

**Api.cs**
- `DumpState`
- `Repair`
- `ForceRecomputeStates`

**ClientEvents.cs**
- `TryHandleClientNoteFound`
- `TryHandleClientQuestCompleted`

**GameEvents.cs**
- `OnSaveLoaded`
- `OnSaving`
- `OnDayStarted`
- `OnDayEnding`
- `OnWarped`
- `HandleLocation`

**InitAndSave.cs**
- `ShouldRun`
- `EnsureInitialized`
- `ValidateSaveData`
- `Save`
- `GetEligibleAnimalTypes`
- `RefreshAnimalsOnFarm`
- `GetAnimalsOnFarm`
- `IsCoopOrBarn`
- `FindNewHatchedAnimals`

**Reconcile.cs**
- `ToHashSetDict`
- `ReconcileState`
- `CreateUniqueToken`
- `EnsureUniqueTokens`
- `UpdateHomeReferencesFromCurrentAnimals`
- `ReconcileExistingNotesInAnimalHouses`
- `TryFindAnimalIdInHouse`
- `IsJunimoNoteId`
- `ExtractAnimalTokenFromNoteId`
- `NormalizeFlagsFromWorldState`
- `DeduplicatePending`

**PendingProcessing.cs**
- `TranslateOrFallback`
- `ProcessPending`
- `TrySpawnNotes`
- `ResolveAnimalHome`
- `GetNoteBlockedReason`
- `GetQuestBlockedReason`
- `GetMailBlockedReason`
- `BuildCleanupBlockers`
- `ResetPlacementFailures`
- `RecordPlacementFailure`
- `TryFindPlacementTile`

**FarmerState.cs**
- `TryIssueUnlockMailForAllFarmers`
- `TrackQuestCompletionsForAllFarmers`
- `SafeGetAllFarmers`
- `GetOrCreateFarmerState`
- `TryAddQuestToFarmer`
- `HasCompletedQuest`
- `HasReceivedMail`
- `HasMailForTomorrow`
- `AddMailForTomorrow`
- `TryGetDaysToMature`
- `CleanupCompleted`

4. Leave fields/ctor/constants in the base `AnimalNotesOrchestrator.cs`.
5. Update EggsInWild docs and mark Phase EIW-R2-4 complete.

### Build Gate

Run Global Build gate.

### Acceptance Criteria

- Orchestrator compiles as partial class
- Each moved method exists exactly once
- build passes

### Commit Message

`r2-eiw-04: split AnimalNotesOrchestrator into partial files`

### Strict Grep Gates

```bash
# must be 1 each (method definition presence):
for m in DumpState Repair ForceRecomputeStates TryHandleClientNoteFound TryHandleClientQuestCompleted \
         OnSaveLoaded OnSaving OnDayStarted OnDayEnding OnWarped HandleLocation \
         ShouldRun EnsureInitialized ValidateSaveData Save GetEligibleAnimalTypes RefreshAnimalsOnFarm GetAnimalsOnFarm IsCoopOrBarn FindNewHatchedAnimals \
         ToHashSetDict ReconcileState CreateUniqueToken EnsureUniqueTokens UpdateHomeReferencesFromCurrentAnimals ReconcileExistingNotesInAnimalHouses TryFindAnimalIdInHouse IsJunimoNoteId ExtractAnimalTokenFromNoteId NormalizeFlagsFromWorldState DeduplicatePending \
         TranslateOrFallback ProcessPending TrySpawnNotes ResolveAnimalHome GetNoteBlockedReason GetQuestBlockedReason GetMailBlockedReason BuildCleanupBlockers ResetPlacementFailures RecordPlacementFailure TryFindPlacementTile \
         TryIssueUnlockMailForAllFarmers TrackQuestCompletionsForAllFarmers SafeGetAllFarmers GetOrCreateFarmerState TryAddQuestToFarmer HasCompletedQuest HasReceivedMail HasMailForTomorrow AddMailForTomorrow TryGetDaysToMature CleanupCompleted
do
  rg -n "^[[:space:]]*(public|internal|private|protected).*[[:space:]]${m}[[:space:]]*\\(" EggsInWild/Application/AnimalNotes -g'AnimalNotesOrchestrator*.cs' | wc -l
done
```

Stop if any count is not `1`.

---

# Plan B — LetMeHatch

## Target Files

- `LetMeHatch/Cloning/ItemManager.cs` (1362 LOC) with explicit regions `#region Items` and `#region Eggs`

---

## Phase LMH-R2-1 — Move `#region Eggs` into `ItemManager.Eggs.cs`

### Objective

Convert `ItemManager` to partial and move the entire `#region Eggs` block into its own file.

### Allowed Files

- `LetMeHatch/Cloning/ItemManager.cs`
- `LetMeHatch/Cloning/ItemManager.Eggs.cs` (new)
- `LetMeHatch/docs/PLAN.md`
- `LetMeHatch/docs/CHECKLIST.md`
- `LetMeHatch/docs/TODO.md`

### Forbidden Changes

- No edits inside moved methods
- No renames
- No logic changes

### Steps

1. In `ItemManager.cs`, change class declaration to `partial` (same modifiers).
2. Create `ItemManager.Eggs.cs`:
   - same namespace as `ItemManager.cs`
   - `partial class ItemManager` with same modifiers

3. Cut/paste the entire `#region Eggs` … `#endregion` block into `ItemManager.Eggs.cs`.
4. Remove that block from `ItemManager.cs`.
5. Update LetMeHatch docs and mark LMH-R2-1 complete.

### Build Gate

Run Global Build gate.

### Acceptance Criteria

- `#region Eggs` appears only in `ItemManager.Eggs.cs`
- build passes

### Commit Message

`r2-lmh-01: split ItemManager eggs region into partial file`

### Strict Grep Gates

```bash
# ItemManager.cs must not contain Eggs region:
rg -n '#region Eggs' LetMeHatch/Cloning/ItemManager.cs && exit 1 || true

# New file must contain it:
rg -n '#region Eggs' LetMeHatch/Cloning/ItemManager.Eggs.cs
```

---

## Phase LMH-R2-2 — Move `#region Items` into `ItemManager.Items.cs`

### Objective

Move the entire `#region Items` block into a second partial file.

### Allowed Files

- `LetMeHatch/Cloning/ItemManager.cs`
- `LetMeHatch/Cloning/ItemManager.Items.cs` (new)
- `LetMeHatch/docs/PLAN.md`
- `LetMeHatch/docs/CHECKLIST.md`
- `LetMeHatch/docs/TODO.md`

### Forbidden Changes

Same as LMH-R2-1.

### Steps

1. Create `ItemManager.Items.cs` (same namespace, same `partial class ItemManager` modifiers).
2. Cut/paste entire `#region Items` … `#endregion` block into `ItemManager.Items.cs`.
3. Remove that block from `ItemManager.cs`.
4. Update LetMeHatch docs and mark LMH-R2-2 complete.

### Build Gate

Run Global Build gate.

### Acceptance Criteria

- `ItemManager.cs` contains no `#region Items` or `#region Eggs`
- build passes

### Commit Message

`r2-lmh-02: split ItemManager items region into partial file`

### Strict Grep Gates

```bash
rg -n '#region (Items|Eggs)' LetMeHatch/Cloning/ItemManager.cs && exit 1 || true
rg -n '#region Items' LetMeHatch/Cloning/ItemManager.Items.cs
rg -n '#region Eggs' LetMeHatch/Cloning/ItemManager.Eggs.cs
```

---

# Plan C — RoosterControl

## Target Files

- `RoosterControl/Helpers/RoosterControl.cs` (1026 LOC, `RoosterHelper` + debug records)

---

## Phase RC-R2-1 — Extract debug record models into a separate file

### Objective

Move debug record types out of `RoosterControl.cs` into a dedicated models file.

### Allowed Files

- `RoosterControl/Helpers/RoosterControl.cs`
- `RoosterControl/Helpers/RoosterDebugModels.cs` (new)
- `RoosterControl/docs/PLAN.md`
- `RoosterControl/docs/CHECKLIST.md`
- `RoosterControl/docs/TODO.md`

### Forbidden Changes

- No changes to logic
- No namespace change (`RoosterControl.Helpers`)
- No type renames

### Steps

1. Create `RoosterControl/Helpers/RoosterDebugModels.cs` with same namespace.
2. Move these types as-is:

   - `record DebugProduceDump`
   - `record DebugProduceCacheStats`
   - `record DebugWeightAnomaly`
   - `record DebugProduceSlotDump`
   - `record DebugResolutionStep`

3. Remove those record declarations from `RoosterControl.cs`.
4. Update RoosterControl docs and mark RC-R2-1 complete.

### Build Gate

Run Global Build gate.

### Acceptance Criteria

- `RoosterControl.cs` contains only `class RoosterHelper`
- build passes

### Commit Message

`r2-rc-01: extract RoosterHelper debug record models into file`

### Strict Grep Gates

```bash
# must be 0:
rg -n 'record (DebugProduceDump|DebugProduceCacheStats|DebugWeightAnomaly|DebugProduceSlotDump|DebugResolutionStep)\b' RoosterControl/Helpers/RoosterControl.cs

# must be 5:
rg -n 'record (DebugProduceDump|DebugProduceCacheStats|DebugWeightAnomaly|DebugProduceSlotDump|DebugResolutionStep)\b' RoosterControl/Helpers/RoosterDebugModels.cs | wc -l
```

---

## Phase RC-R2-2 — Split debug methods into a partial file

### Objective

Convert `RoosterHelper` to `partial` and move the debug-only methods into `RoosterHelper.Debug.cs`.

### Allowed Files

- `RoosterControl/Helpers/RoosterControl.cs`
- `RoosterControl/Helpers/RoosterHelper.Debug.cs` (new)
- `RoosterControl/docs/PLAN.md`
- `RoosterControl/docs/CHECKLIST.md`
- `RoosterControl/docs/TODO.md`

### Forbidden Changes

- No edits inside moved methods
- No renames
- No signature changes

### Steps

1. Change `public sealed class RoosterHelper` to `public sealed partial class RoosterHelper`.
2. Create `RoosterControl/Helpers/RoosterHelper.Debug.cs` in same namespace, same class header.
3. Move these methods **as-is**:

   - `LogOncePerDay`
   - `MaybeLogDailyProduceSummary`
   - `DebugDumpChosenProduce`
   - `BuildSlotDump`

4. Update RoosterControl docs and mark RC-R2-2 complete.

### Build Gate

Run Global Build gate.

### Acceptance Criteria

- moved methods exist exactly once
- build passes

### Commit Message

`r2-rc-02: split RoosterHelper debug methods into partial file`

### Strict Grep Gates

```bash
for m in LogOncePerDay MaybeLogDailyProduceSummary DebugDumpChosenProduce BuildSlotDump
do
  rg -n "^[[:space:]]*(public|internal|private|protected).*[[:space:]]${m}[[:space:]]*\\(" RoosterControl/Helpers -g'*.cs' | wc -l
done
```

Stop if any count is not `1`.

---

# Strict "Monolithic Completion" Verification (Repo-wide)

Run after all phases:

```bash
rg -n 'class DebugKnobsConfig\b' EggsInWild/Infrastructure/Config/DebugKnobs.cs && exit 1 || true
rg -n 'record WeggVerifyOptions\b' EggsInWild/Infrastructure/Diagnostics/WeggVerifier.cs && exit 1 || true
rg -n 'class DumpModel\b' EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.cs && exit 1 || true
rg -n 'record DebugProduceDump\b' RoosterControl/Helpers/RoosterControl.cs && exit 1 || true
rg -n '#region (Items|Eggs)' LetMeHatch/Cloning/ItemManager.cs && exit 1 || true
```

All commands should exit with code 1 (meaning the pattern was NOT found), indicating successful completion.

