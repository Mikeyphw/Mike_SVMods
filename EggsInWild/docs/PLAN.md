# PLAN — WildAnimals JSON Editing via GMCM (EggsInWild)

## Status
- Phases 0–8 complete (see `docs/CHECKLIST.md`).
- Current focus: Phase 7+ UX polish (bulk workflows, richer validation surfaced in GMCM) and maintenance fixes.
- Validation warnings: dev diagnostics now include warnings for unloaded modIds and unknown FarmAnimalKeys (when Data/FarmAnimals is available).
- Bulk UX: GMCM ExtraModAnimalKeys supports stage + preview + apply bulk edits (export pending).

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