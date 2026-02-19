# RoosterAddon Detailed Refactor Phase Plan (BOAF-first)

This plan is the single source of truth for the refactor phases in this workspace. It assumes:

- **BirdsOfAFeather (BOAF) is mandatory**. If BOAF provides an equivalent service/API, RoosterAddon must not ship a duplicate.
- **No legacy compatibility**. Old runtime config strings/paths should be removed, not supported indefinitely.
- **Data-driven first**. Prefer JSON catalogs and typed config over hardcoded tables.

---

## Packaging rules (zip2git)

- Every patch zip must contain **at zip root**:
  - `zip2git.json` (metadata only)
  - `Mike_SVMods/RoosterAddon/...` (repo folder)
- Zip filename: `RoosterAddon_<patch.id>_<brief_patch_name>.zip`
- Never mix multiple `subdir`s in one zip.

---

## Phases

### Phase 0 — Baseline & guardrails
**Goal:** Establish parity signals and safe refactor scaffolding.

**Deliverables**
- `roosteraddon_dump_state` console command (JSON snapshot)
- `PARITY_CHECKLIST.md`
- Known-good save + baseline dumps

**DoD**
- Baseline dumps can be generated before/after each phase.

---

### Phase 1 — Stop-the-bleeding fixes
**Goal:** Remove high-risk runtime behaviors and obvious correctness bugs.

**Deliverables**
- GMCM registration only once on `GameLaunched`
- No file IO during `AssetRequested`
- No cache invalidation inside asset edits
- Correct percent-roll behavior (0% never, 100% always)

**DoD**
- No per-tick GMCM work; no asset edit re-entrancy hazards.

---

### Phase 2 — Typed config only (no legacy compatibility) ✅
**Goal:** Typed, stable config surface with self-healing schema behavior.

**Deliverables**
- `Framework/Config/*` typed config (`RoosterAddonConfig`, `ConfigManager`, `ConfigSchema`)
- Remove legacy config artifacts

**DoD**
- No `Framework/ModConfig.cs` or legacy adapters.
- Schema mismatch is handled via backup + upgrade/warn; no “migrator missing” paths.

---

### Phase 3 — Catalog-driven produce + random groups ✅
**Goal:** Remove hardcoded produce randomization and make produce resolution data-driven.

**Deliverables**
- `ProduceCatalog.RandomGroups` in `data/catalogs/produce.json`
- `Application/Produces/IProduceResolver` + `RandomGroupProduceResolver`
- No `RoosterControl.ProduceTranslation` / hardcoded random tables

**DoD**
- Any “Random …” selection is defined purely in catalog JSON.
- Eggs + produce choices are resolved into concrete IDs via resolver.

---

### Phase 4 — Asset edit boundaries + services ✅
**Goal:** Asset editors become thin; all decisions live in services.

**Deliverables**
- FarmAnimals edits fully delegated to services (produce rules, exclusions, building/location rules)
- Object edits fully delegated to services (fertilized eggs, custom fields, tagging)
- Coordinator orchestrates only

**DoD**
- `AssetRequested` handlers call services that are pure (no SMAPI calls inside decision logic).

---

### Phase 5 — Daily plan pipeline ✅
**Goal:** Make day-start behavior deterministic and testable.

**Deliverables**
- Daily planning service that computes actions without touching game state
- Execution layer that applies a plan (edits, swaps, updates)

**DoD**
- Planning can run in isolation with injected state snapshots.

---

### Phase 6 — Legacy purge + service hardening ✅
**Goal:** Remove dead/legacy types and harden service contracts.

**Deliverables**
- Legacy folder quarantined or deleted
- Eliminate duplicate/unused code paths and stringly-typed remnants
- Improve logging + error handling (fail fast on invalid catalogs)

**DoD**
- No “legacy” runtime fallbacks; invalid inputs produce actionable errors.

---

### Phase 7 — Tests (xUnit) + deterministic harness
**Goal:** Lock behavior with fast, deterministic tests.

**Deliverables**
- `RoosterAddon.Tests` project
- Deterministic RNG injection for services that roll randomness
- Coverage for random groups, hatching strategies, and catalog validation

**DoD**
- Tests run locally and in CI with stable outputs.

---

### Phase 8 — JSON catalogs (all knobs)
**Goal:** All per-rooster and general knobs are catalog-driven, with clear precedence.

**Deliverables**
- `data/catalogs/roosters.json`, `produce.json`, `hatching.json` complete
- Catalog apply mode (seed/merge/repair) behaves predictably

**DoD**
- A new install can run purely from catalogs + typed config seed.

---

### Phase 8.1 — Catalog tooling + validation ✅
**Goal:** Make catalogs safe to edit and easy to troubleshoot.

**Deliverables**
- `ra_catalog` command suite: `validate`, `reload`, `export`, `diff`, `mode`
- Validation output is actionable per-file (OK/migrated/errors)

**DoD**
- Editing JSON + `ra_catalog reload` updates runtime without restart.

---

### Phase 9 — Descriptor-driven GMCM via BOAF API
**Goal:** GMCM UI is generated from descriptors and registered through BOAF.

**Deliverables**
- Descriptor model + layout compiler
- `IBirdsOfAFeatherApi.RegisterGmcmMenu(...)` wiring
- No direct GMCM `Add*Option` calls in RoosterAddon

**DoD**
- Full GMCM is produced from descriptors; localization keys resolve cleanly.

---

### Phase 10 — CI build gate + release hardening
**Goal:** Prevent regressions and make patches compile-verified.

**Deliverables**
- Minimal GitHub Actions: restore/build/test
- Fail build on warnings-as-errors (where safe)
- Add guard tests (no legacy strings, orphaned i18n keys)

**DoD**
- Every patch is compile-verified; tests run on PR/push.
