# PLAN — WildAnimals JSON Editing via GMCM (EggsInWild)

## Status
- Phases 0–8 complete (see `docs/CHECKLIST.md`).
- Phase SUI-1 complete: shared WildAnimals editor catalog/service extracted for GMCM + StardewUI reuse.
- Phase SUI-2 complete: StardewUI WildAnimals editor added alongside GMCM with a temporary launcher action.
- Current focus: Phase 7+ UX polish (bulk workflows, richer validation surfaced in GMCM) and maintenance fixes.
- Index navigation: GMCM WildAnimals index provides grouped page links with counts (All, A–F, G–L, M–R, S–Z, Other).
- Bulk UX: staged bulk entries are indicated in index links; bulk preview includes planned set/clear/no-op summary; staging can be cleared.
- Validation warnings: dev diagnostics now include warnings for unloaded modIds and unknown FarmAnimalKeys (when Data/FarmAnimals is available).
- Validation warnings: GMCM displays ExtraModAnimalKeys warnings inline (shared validator with dev dump).
- Bulk UX: GMCM ExtraModAnimalKeys supports stage + preview + apply bulk edits (export to SMAPI log available).
- Export: GMCM action writes effective blocks as `modId | csvKeys` to the SMAPI log for round-trip editing.
- Export: GMCM can also copy the exported `modId | csvKeys` lines into the Bulk stage (overwrite/append).
- Quick-fixes: bulk fix toggles also apply to per-block edits (drop unknown FarmAnimalKeys when available).

## Scope
- Subdir: `EggsInWild`
- Framework: **NestCore-first** (GMCM pages are built using NestCore GMCM integration).
- Goal: make `ModData/WildAnimalsList.json` **effectively editable at runtime** via GMCM by layering config overrides (no direct editing of shipped JSON).

## Constraints (hard)
- **No live search / dynamic filtering UI** inside GMCM:
  - NestCore/GMCM pages are static option graphs; GMCM does not support “type to filter” without menu re-registering.
- Keep all changes inside `EggsInWild` (NestCore is already in the base snapshot).
- Preserve existing dev commands, especially `wegg tomb force_patches` if present.

## Definitions
- **Base JSON**: `ModData/WildAnimalsList.json` loaded through `ModDataLoader`.
- **Override layer**: config entries applied on top of base JSON at load time.
- **Effective WildAnimal**: the post-override runtime model used by the pipeline.

## Non-goals (for this iteration)
- Editing complex collections beyond WildAnimalsList (e.g. SpawnAreas rules) from GMCM.
- Any UI that hides/shows options dynamically (search, disabled-only filters, etc.).

## Done when
- A user can change a WildAnimal’s **Enabled / Price / LargeEgg / IsMagic** in GMCM and the runtime behavior uses the effective values.
- Overrides are normalized/cleaned so config stays stable across casing/whitespace.
- Dev commands can inspect and modify effective values safely.
- Docs + i18n reflect the new settings.

---

## Phase 0 — Docs + tracking (this folder)
**Files**
- `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md`

**Steps**
1. Define phases + acceptance criteria (this document).
2. Keep checklist aligned with completed work.
3. Keep TODO aligned with remaining work.

---

## Phase 1 — Override schema + normalization (safe core)
**Goal**: make scalar WildAnimal fields overridable, with stable key matching and cleanup.

**Primary files**
- `Infrastructure/ModData/ConfiguredModDataLoader.cs`
- `Infrastructure/ModData/Models/WildAnimalJson.cs`
- `Infrastructure/Config/Models/*` (where WildAnimalOverrides config lives)
- `Infrastructure/Helpers/*` (normalization helpers if needed)

**Steps**
1. Extend config override model to support:
   - `bool? Enabled`
   - `int? Price`
   - `bool? LargeEgg`
   - `bool? IsMagic`
2. Add normalization/cleanup pass for overrides:
   - trim keys
   - case-insensitive matching against known WildAnimal keys
   - remove unknown keys
   - remove entries where **all override fields are null**
3. Apply overrides during configured mod-data load:
   - base JSON remains unchanged
   - effective WildAnimal model reflects overrides
4. Add logs (debug level) for:
   - removed unknown keys
   - removed empty overrides
   - normalized key mapping (when casing differs)

**Acceptance**
- Existing configs keep working (Enabled-only) without breaking.
- No compilation errors; no switch-expression/braces regressions.

---

## Phase 2 — GMCM per-animal editor (scalar fields)
**Goal**: expose scalar edits in a stable, non-dynamic GMCM UI.

**Primary files**
- `Infrastructure/Gmcm/*` (WildAnimals pages)
- `ModEntry.cs` (registration of GMCM pages/options)
- `i18n/default.json` (+ `i18n/pt-br.json` if present)

**Steps**
1. Per WildAnimal page:
   - Toggle: Enabled
   - Number: Price
   - Toggle: LargeEgg
   - Toggle: IsMagic
2. Add reset controls:
   - “Reset all overrides for this animal”
   - Optional per-field reset actions (clears only that field)
3. Show read-only text:
   - base JSON value
   - override value (or “none”)
   - effective value
4. Wire `ConfigUpdated`/save handling so changes take effect as expected.

**Acceptance**
- Setting values in GMCM updates config and the effective runtime model reflects the change.
- i18n keys exist for all labels/helptext.

---

## Phase 3 — Dev commands (inspection + safe edits)
**Goal**: dev CLI can introspect and adjust overrides without touching JSON.

**Primary files**
- `Infrastructure/Commands/WeggCommandDispatcher.cs`
- `Infrastructure/Commands/*` (command handlers if separated)
- `Infrastructure/ModData/*` (for querying effective state)

**Commands**
1. `wegg wildanimals dump_effective`
2. `wegg wildanimals status <wildAnimalKey>`
3. `wegg wildanimals set_enabled <wildAnimalKey> <true|false|1|0>`
4. Keep `wegg tomb force_patches` intact.

**Acceptance**
- Commands validate keys, report normalized key resolution, and never crash on bad input.

---

## Phase 4 — Diagnostics (raw sources + effective ids display)
**Goal**: display provenance for each WildAnimal (read-only).

**Primary files**
- `Infrastructure/Gmcm/*` (per-animal page)
- `Infrastructure/ModData/AnimalCatalogBuilder.cs` (for effective farm-animal id resolution)
- `Infrastructure/ModData/Models/WildAnimalJson.cs`

**Steps**
1. On per-animal page, show read-only diagnostics:
   - `ExtraModAnimalKeys` blocks (vanilla + modded) and whether each referenced mod is loaded
   - raw farm-animal keys derived from those blocks
   - resolved effective farm-animal ids (post installed-mod filtering + dedupe)
2. Ensure long text is formatted as paragraphs (GMCM-safe).

**Acceptance**
- Diagnostics match runtime resolution logic and help debug mismatches.

---

## Phase 5 — Polish + cleanup
**Goal**: keep config clean and UX understandable.

**Steps**
1. Ensure override cleanup runs on load and on config save (when reasonable).
2. Add more i18n helptext and keep docs/checklist up to date.

---

## Phase 6 — LocationTypes editor
**Goal**: allow per-wild-animal LocationTypes editing via override layer + GMCM.

**Steps**
1. Add override schema + normalization/cleanup + merge into effective WildAnimals on load. ✅
2. Add per-animal GMCM controls for each LocationType (static UI, no live search). ✅
3. Add i18n keys + docs updates. ✅

**Acceptance**
- Config stores only per-type diffs vs base JSON; casing/whitespace normalized.
- Effective runtime LocationTypes reflect overrides.

---

## Phase 7 — ExtraModAnimalKeys editor
**Goal**: allow per-wild-animal ExtraModAnimalKeys editing via override layer + GMCM.

**Steps**
1. Add override schema + normalization/cleanup + merge into effective WildAnimals on load. ✅
2. Add per-animal GMCM UI to edit mod-group blocks and FarmAnimalKeys lists (CSV-style inputs). ✅
   - Add static helper controls to add/remove custom modId override blocks (no dynamic search). ✅
3. Update diagnostics/dev outputs to show override vs base sources.
4. Add i18n keys + docs updates.

**Acceptance**
- Config stores only diffs vs base JSON; empty blocks removed; keys trimmed and de-duped.
- Effective runtime ExtraModAnimalKeys reflect overrides.

---

## Phase 8 — Polish/hardening
**Goal**: improve outputs + ensure cleanup is safe and stable.

**Steps**
1. Enhance dev commands (status/dump_effective) to show override sources for complex fields.
2. ✅ Guard against accidental config churn (only save when normalization changes).
3. Docs + checklist alignment.

---

## Phase 9 — GMCM bulk UX polish (post-Phase 8)
**Goal**: make bulk workflows fast and round-trip friendly within GMCM static-page limits.

**Steps**
1. Add a per-animal bulk header summary (staged counts + planned set/clear/no-op). ✅
2. Add a Skip-noop Bulk Apply toggle (default on) so no-op apply keeps staged text. ✅
3. Add an export action to write effective blocks as `modId | csvKeys` to SMAPI log. ✅
4. Expand validation warnings surfaced in GMCM (unknown modIds, empty/duplicate keys, missing FarmAnimalKeys when available). ✅
5. Maintenance pass: keep build/warning budget tight (dedupe bulk planning logic; share export builder; reuse cached Data/FarmAnimals load). ✅

**Acceptance**
- Bulk preview/plan remains readable without scrolling.
- No-op bulk applies do not clear staged text (unless user disables skip-noop).
- User can export effective blocks and paste back into bulk stage to reproduce state.
- Bulk planning is computed consistently for header, preview, and apply; no redundant game-data loads.


---

## Phase 10 — Bulk tooling parity (tests + dev commands)
**Goal**: keep bulk editing logic stable by covering it with tests, and expose the same bulk workflows via dev commands.

**Steps**
1. Add unit tests for bulk parsing + planning (set/clear/no-op) behavior. ✅
2. Add dev commands:
   - `wegg wildanimals export_effective_bulk <wildAnimalKey>`
   - `wegg wildanimals validate_bulk <wildAnimalKey> <rawBulk|@filePath>`
   ✅
3. (Later) Add dev-command-focused tests for export/validate output formatting.

**Acceptance**
- `dotnet test Tests/Tests.sln` passes.
- `export_effective_bulk` output matches GMCM export lines (`modId | csvKeys`), one line per mod-group.
- `validate_bulk` reports parsing issues and FarmAnimalKey warnings when `Data/FarmAnimals` is available.

## Phase 18C — NestCore-first GMCM consolidation

1. Keep `EiwGmcmMenu` as the single GMCM layout orchestrator.
2. Keep WildAnimals page bodies under `Infrastructure/Integrations/Gmcm/Pages/*`.
3. Replace pseudo-button fake-bool command controls with NestCore `Action(...)`.
4. Remove no-longer-used GMCM layout bodies from `ModEntry.Gmcm*` where safe.
5. Preserve page IDs, option IDs, field IDs, translation keys, and save-aware rebuild behavior.

## Phase 18D — Repo GMCM convention alignment

1. Confirm NestCore is the GMCM plumbing layer and `EiwGmcmMenu` is the orchestrator.
2. Keep page bodies under `Infrastructure/Integrations/Gmcm/Pages/*`.
3. Keep one-shot commands on `Action(...)` controls instead of fake bool toggles.
4. Keep save-aware rebuild behavior for world-dependent pages.
5. Document any intentionally retained GMCM wrappers only when runtime-coupling requires it.

## Phase 18E — Final validation

1. Re-scan GMCM code for pseudo-button fake-bool patterns.
2. Re-scan for leftover inline GMCM layout bodies in `ModEntry`/coordinator files.
3. Remove remaining dead legacy GMCM partials where safe and build-stable.

## Phase SUI-1 — Shared WildAnimals editor extraction
1. Add shared WildAnimals catalog/draft records under `Infrastructure/Integrations/StardewUi/WildAnimals/`.
2. Add `WildAnimalsEditorService` for loading base JSON + config overlays and applying draft changes.
3. Move shared helper logic out of `ModEntry.Gmcm.cs` into `WildAnimalsEditorHelpers`.
4. Switch `WildAnimalsPages.Define(...)` to load through the shared service while keeping GMCM behavior unchanged.
5. Add unit coverage for catalog ordering and draft apply behavior.
6. Verify `EggsInWild` and `EggsInWild.Tests` build; note `Tests/Tests.sln` remains blocked by unrelated `RoosterControl.Pure` compile errors.

## Phase SUI-2 — StardewUI WildAnimals editor alongside GMCM
1. Add `ModEntry.StardewUi.cs` and wire `TryCreateStardewUiHost(...)` in `OnGameLaunched`.
2. Register one StardewUI menu id `wildAnimalsEditor` backed by `assets/views/wildAnimals/editor.sml`.
3. Add `WildAnimalsEditorViewModel` and `WildAnimalDraftViewModel` with save/discard/reload flows.
4. Add one temporary GMCM launcher action `wildAnimals.openEditor` while keeping the existing `wildAnimals` link page.
5. Add unit coverage for save/discard/reload behavior in the new viewmodel.
6. Verify `EggsInWild` and `EggsInWild.Tests` build.
4. Record final architecture notes in docs.

---

## R2 — Monolithic file splits

This section documents the R2 refactoring effort to split monolithic files into smaller, more maintainable files.

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

**Reference**: [Repo-wide R2 Documentation](../refactor/Repo-wide_R2_Monolithic_File_Splits.md)
