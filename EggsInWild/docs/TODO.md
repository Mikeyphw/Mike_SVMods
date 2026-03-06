# TODO — EggsInWild (WildAnimals)

## Next (after Phase 8)
- [ ] Phase 7 UX improvements:
  - [ ] Bulk add/update/remove workflows that fit GMCM static-page limits.
    - [x] Bulk stage + preview + apply (Paste: `modId | csvKeys`; `*` to reset/remove).
    - [x] Bulk apply quick-fix toggles (trim/dedupe/drop unknown; also applied to per-block edits).
    - [x] Bulk preview shows before/after and what quick-fixes will change (trim/dedupe/drop unknown).
    - [x] Bulk preview includes planned action summary (set/clear/no-op) + clearer per-modId actions.
    - [x] WildAnimals index shows group counts and flags staged bulk entries (with warn count).
    - [x] Clear staged bulk input toggle.
    - [x] Export current effective blocks as `modId | csvKeys` for round-trip editing.
    - [x] Copy exported effective blocks into Bulk stage (overwrite/append).
  - [x] Surface richer validation warnings in GMCM (unknown modIds, empty/duplicate keys, keys not present in game data when available).
    - [x] ExtraModAnimalKeys: GMCM shows inline validation warnings (unloaded modIds, blank/whitespace/dupes, unknown FarmAnimalKeys when Data/FarmAnimals is available).
  - [x] Dev: `wegg wildanimals dump_effective` includes validation warnings (unloaded modIds + unknown FarmAnimalKeys when Data/FarmAnimals is available).
- [x] Index-page navigation improvements that fit GMCM static-page limits (no live search).
  - [x] Group index pages: All, A–F, G–L, M–R, S–Z, Other (with counts).
- [x] Keep build/warning budget tight (no obsolete legacy support; avoid config churn).
  - [x] Phase 9E: dedupe bulk planning logic + shared export builder + reuse cached Data/FarmAnimals load.

## Notes
- Keep changes inside `EggsInWild` and prefer NestCore helpers for GMCM plumbing.
- Avoid fragile syntax edits in `WeggCommandDispatcher.cs` (braces/strings/switch expressions).

## Wiring follow-up
- [x] Wire AnimalNotes runtime services into normal `ModEntry` flow (save/day/warp/message handling).
- [x] Wire `Phase8HealthReporter` at startup.
- [x] Wire `WeggDiagnosticsState` for current NestCore API version + GMCM registration state.
- [x] Wire `WildAnimalPreviewResolver` into GMCM as preview-source text.
- [x] Replace text-only WildAnimals preview info with StardewUI sprite previews in the WildAnimals editor.
- [ ] `WeggDiagnosticsState` still has no user-facing surface beyond the stored state.


## Phase 10 — Bulk tooling parity
- [x] 10C: unit tests for bulk parsing + planning (set/clear/no-op).
- [x] 10D: dev commands `export_effective_bulk` + `validate_bulk`.
- [ ] Add dev-command-focused tests for export/validate output formatting.

## Phase 18C — GMCM migration
- [x] Replace pseudo-button fake bool GMCM controls with `Action(...)` on WildAnimals pages.
- [x] Keep `EiwGmcmMenu` as the GMCM orchestrator and preserve save-aware rebuild.
- [x] Remove duplicated Animal Progression page body from legacy `ModEntry.Gmcm.cs`.
- [x] Run full build gate after GMCM consolidation.

## Phase 18D — GMCM convention
- [x] Mark EggsInWild docs aligned with repo-wide GMCM convention.

## Phase 18E — Finalization
- [x] Final grep/review completed: no fake-bool action patterns remain.
- [x] Legacy `ModEntry.Gmcm*` inline layout partials removed; `EiwGmcmMenu + Pages/*` is canonical.

## StardewUI migration
- [x] Phase SUI-1: extract shared WildAnimals catalog/service/helpers for GMCM + StardewUI reuse.
- [x] Phase SUI-2: add StardewUI WildAnimals editor alongside GMCM.
- [x] Phase SUI-3: retire GMCM WildAnimals pages and keep one launcher action.
- [x] Phase SUI-UX-1: upgrade the WildAnimals StardewUI editor with previews, filters, dirty-state UX, and structured mod-group editing.
- [x] Phase SUI-UX-2: add a GMCM-configurable direct keybind for the WildAnimals editor.
- [x] Phase SUI-SML-1: add inert `animalProgression/editor.sml` shell.
- [x] Phase SUI-4: extract shared Animal Progression editor service/catalog and route the GMCM page through it.
- [x] Phase SUI-5: wire the duplicate Animal Progression StardewUI editor and GMCM launcher.
- [x] Verify Phase SUI-5 build gates.
- [ ] Unblock `Tests/Tests.sln` from unrelated `RoosterControl.Pure` SMAPI reference errors before using it as a migration gate.

## R2 — Monolithic file splits

### Phase EIW-R2-1 — Split DebugKnobs.cs config types
- [ ] Create folder `EggsInWild/Infrastructure/Config/DebugKnobs/`
- [ ] Create 15 new files for config POCO classes
- [ ] Move types from DebugKnobs.cs to new files
- [ ] Run build gate
- [ ] Commit: `r2-eiw-01: split DebugKnobs config types into files`

### Phase EIW-R2-2 — Split WeggVerifier.cs helper types
- [ ] Create folder `EggsInWild/Infrastructure/Diagnostics/WeggVerify/`
- [ ] Create 8 new files for helper types
- [ ] Move types from WeggVerifier.cs to new files
- [ ] Run build gate
- [ ] Commit: `r2-eiw-02: split WeggVerifier helper types into files`

### Phase EIW-R2-3 — Extract Dump* models from AnimalNotesOrchestrator
- [ ] Create `EggsInWild/Application/AnimalNotes/DumpModels.cs`
- [ ] Move 5 Dump* classes to new file
- [ ] Run build gate
- [ ] Commit: `r2-eiw-03: extract AnimalNotes dump models into file`

### Phase EIW-R2-4 — Split AnimalNotesOrchestrator into partials
- [ ] Convert to `partial class`
- [ ] Create 7 partial files by method group
- [ ] Move methods to respective partial files
- [ ] Run build gate
- [ ] Commit: `r2-eiw-04: split AnimalNotesOrchestrator into partial files`
