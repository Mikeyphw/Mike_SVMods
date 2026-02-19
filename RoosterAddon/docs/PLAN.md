# PLAN — RoosterAddon

> Location (required): `Mike_SVMods/RoosterAddon/docs/PLAN.md`  
> Last updated: **2026-02-19T04:18:29Z**  
> Status: **Active** (Active | Paused | Done)

## Scope
- **Goal:** Complete a BOAF-first refactor of RoosterAddon with catalog-driven behavior, descriptor-driven GMCM, and strong validation/tooling.
- **Non-goals:** Backward compatibility with legacy config/option IDs; re-implementing BOAF infra.
- **Constraints:** BOAF mandatory; no local duplicates; no legacy compatibility; packaging rules apply.

## Working agreements
- Each **Phase** has **3–7 Steps**.
- A **Step is “Done”** only when:
  - code is delivered in its patch zip(s),
  - docs/checklist/todo are updated,
  - build/tests (if any) pass.
- On completion of any Step/Phase, update:
  - `PLAN.md` (this file),
  - `CHECKLIST.md`,
  - `TODO.md`.

---

## Milestones (high level)
- M1: Data-driven catalogs + validation + tooling.
- M2: Descriptor-driven GMCM registered via BOAF API.
- M3: Tests + CI build gate (repo-wide).

---

## Phase 0 — Baseline & guardrails
**Outcome:** Parity signals exist (dump + checklist) and can be used before/after changes.  
**Dependencies:** None.

### Steps
- [x] **0.1** Create/maintain parity checklist  
  - **Definition of done:** `docs/PARITY_CHECKLIST.md` exists and is usable.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Manual checklist only; no automation required.

- [x] **0.2** Add state dump console command  
  - **Definition of done:** `roosteraddon_dump_state` produces a JSON snapshot without crashing.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Used for parity checks between patches.

- [x] **0.3** Document baseline workflow  
  - **Definition of done:** README points to docs and describes dev workflow at a high level.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** 2026-02-19 (docs-only)  
  - **Notes:** Docs-only update in this commit.

### Completion log (append-only)
- 2026-02-19 — Consolidated docs into `docs/` and aligned with BOAF templates.

---

## Phase 1 — Stop-the-bleeding fixes
**Outcome:** No risky runtime behaviors (IO/invalidation) inside asset edits; stable GMCM lifecycle; correct percent rolls.  
**Dependencies:** Phase 0.

### Steps
- [x] **1.1** GMCM registration via BOAF only  
  - **Definition of done:** GMCM is registered once on GameLaunched via BOAF API.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `RoosterCoordinator.Gmcm.cs`.

- [x] **1.2** No file IO or cache invalidation inside AssetRequested  
  - **Definition of done:** Asset editors only edit in-memory data; invalidation/refresh scheduled elsewhere.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Asset editors contain no `File.*`/`InvalidateCache` calls.

- [x] **1.3** Correct percentage roll semantics  
  - **Definition of done:** 0% never, 100% always, otherwise RNG; centralized roller used.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `Application/Hatching/Decisions`.

### Completion log (append-only)
- pre-snapshot — Phase completed before this snapshot.

---

## Phase 2 — Typed config only (no legacy compatibility)
**Outcome:** Typed config is the only config surface; legacy adapters removed; schema mismatch is self-healing with backups.  
**Dependencies:** Phase 1.

### Steps
- [x] **2.1** Typed config models + schema  
  - **Definition of done:** `Framework/Config/*` defines config + current schema version.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Typed config only.

- [x] **2.2** Remove legacy config artifacts  
  - **Definition of done:** No `ModConfig.cs` / legacy adapters remain.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** No legacy compatibility allowed.

- [x] **2.3** Schema mismatch handling  
  - **Definition of done:** Backup invalid/older config; upgrade/warn deterministically.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** No “migrator missing” paths.

### Completion log (append-only)
- pre-snapshot — Phase completed before this snapshot.

---

## Phase 3 — Catalog-driven produce + random groups
**Outcome:** All produce/egg randomization is data-driven via catalogs; no hardcoded translation tables.  
**Dependencies:** Phase 2.

### Steps
- [x] **3.1** Define random groups in `produce.json`  
  - **Definition of done:** `data/catalogs/produce.json` includes `RandomGroups` and validates.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Includes egg/gem/mineral groups.

- [x] **3.2** Resolver service for random groups  
  - **Definition of done:** `IProduceResolver` resolves produce keys → concrete IDs.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Deterministic with injected RNG where applicable.

- [x] **3.3** Remove hardcoded produce translation  
  - **Definition of done:** No `ProduceTranslation` switch/table remains; eggs/producers use resolver.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Catalog is the source of truth.

### Completion log (append-only)
- pre-snapshot — Phase completed before this snapshot.

---

## Phase 4 — Asset edit boundaries + services
**Outcome:** Asset editors are thin; decision logic lives in services; no side effects during asset edits.  
**Dependencies:** Phase 3.

### Steps
- [x] **4.1** Thin asset editors  
  - **Definition of done:** Editors delegate to services and do not schedule invalidations.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** No coordinator mega-context.

- [x] **4.2** Move object tagging logic into service  
  - **Definition of done:** BetterTruffles tagging is a service called by the editor.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `BetterTrufflesContextTagService`.

- [x] **4.3** Coordinator orchestration only  
  - **Definition of done:** Coordinator wires services; avoids duplicating BOAF infra.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** BOAF owns routing/plumbing.

### Completion log (append-only)
- pre-snapshot — Phase completed before this snapshot.

---

## Phase 5 — Daily plan pipeline
**Outcome:** Day-start behavior is deterministic: compute a plan (pure), then apply (side effects).  
**Dependencies:** Phase 4.

### Steps
- [x] **5.1** Pure plan computation  
  - **Definition of done:** Planner accepts snapshots and produces a plan without SMAPI/game calls.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `DailyHatchingPlanService`.

- [x] **5.2** Deterministic plan application  
  - **Definition of done:** Applier mutates game state from a plan; no recompute per animal.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `DailyHatchingPlanApplier`.

- [x] **5.3** Backup/restore plan artifacts  
  - **Definition of done:** Plan backup models exist for troubleshooting/regressions.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `DailyHatchingPlanBackupModels`.

### Completion log (append-only)
- pre-snapshot — Phase completed before this snapshot.

---

## Phase 6 — Legacy purge + service hardening
**Outcome:** Dead code removed; catalogs validated/sanitized; failures are actionable and fail fast.  
**Dependencies:** Phase 5.

### Steps
- [x] **6.1** Remove unused legacy types/paths  
  - **Definition of done:** Delete unused enums/paths and stale compatibility helpers.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** No legacy runtime strings.

- [x] **6.2** Catalog sanitize + validate fail-fast  
  - **Definition of done:** Invalid catalogs are logged with file/path context and throw.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `CatalogValidation`.

- [x] **6.3** Fix compile-sanity issues  
  - **Definition of done:** Broken/duplicated coordinator partials cleaned up.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Keep coordinator thin and readable.

### Completion log (append-only)
- pre-snapshot — Phase completed before this snapshot.

---

## Phase 7 — Tests (repo-wide, deterministic)
**Outcome:** Unit tests exist under `Mike_SVMods/Tests/` and cover pure logic; local SMAPI tests optional.  
**Dependencies:** Repo root + `Tests/` subdir (not in this zip).

### Steps
- [ ] **7.1** Create Tests docs + project layout  
  - **Definition of done:** `Mike_SVMods/Tests/...` exists with its own docs per BOAF rules.  
  - **Artifacts / touched subdirs:** Tests  
  - **Patch IDs:** TBD  
  - **Notes:** Tracked in Tests subdir; verify current repo root.

- [ ] **7.2** Add unit tests for pure logic  
  - **Definition of done:** Catalog validation, random group resolver, daily planner have tests.  
  - **Artifacts / touched subdirs:** Tests  
  - **Patch IDs:** TBD  
  - **Notes:** Keep tests deterministic and SMAPI-free.

- [ ] **7.3** Wire tests into CI  
  - **Definition of done:** GitHub Actions runs pure tests on push/PR.  
  - **Artifacts / touched subdirs:** Core + Tests  
  - **Patch IDs:** TBD  
  - **Notes:** Implemented in Phase 10.

### Completion log (append-only)
- 2026-02-19 — This phase is tracked in Tests/Core docs; not part of RoosterAddon-only snapshot.

---

## Phase 8 — JSON catalogs (all knobs)
**Outcome:** All tunables are represented in catalogs; apply modes behave predictably.  
**Dependencies:** Phase 6.

### Steps
- [x] **8.1** Complete catalogs for all knobs  
  - **Definition of done:** `roosters.json`, `produce.json`, `hatching.json` include all tunables.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Catalogs are the single source of truth.

- [x] **8.2** Apply modes are consistent  
  - **Definition of done:** Seed/Merge/Repair/Authoritative apply with clear precedence.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Validated during apply.

- [x] **8.3** Validation covers required fields  
  - **Definition of done:** Validator reports actionable errors per file/path.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Fail fast on invalid data.

### Completion log (append-only)
- pre-snapshot — Phase completed before this snapshot.

---

## Phase 8.1 — Catalog tooling + validation (ra_catalog)
**Outcome:** `ra_catalog` provides validate/reload/export/diff/mode with backups and per-file status output.  
**Dependencies:** Phase 8.

### Steps
- [x] **8.1.1** Implement `ra_catalog` commands  
  - **Definition of done:** validate/reload/export/diff/mode are registered and functional.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Console service lives under `Application/Catalogs/Console`.

- [x] **8.1.2** Per-file status output + backups  
  - **Definition of done:** Validate reports OK/MIGRATED/ERROR; --fix writes backups.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** Backups stored under `data/catalogs/backups/`.

- [x] **8.1.3** Reload applies and invalidates safely  
  - **Definition of done:** Reload loads from disk, applies, and invalidates relevant assets.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** No side effects inside AssetRequested.

### Completion log (append-only)
- pre-snapshot — Phase completed before this snapshot.

---

## Phase 9 — Descriptor-driven GMCM via BOAF API
**Outcome:** GMCM is generated from descriptors and registered via BOAF; no direct GMCM calls in RoosterAddon.  
**Dependencies:** Phase 8.1.

### Steps
- [x] **9.1** Descriptor model + registry  
  - **Definition of done:** Descriptors define pages/options/sections with stable IDs.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `Application/Gmcm/Descriptors`.

- [x] **9.2** Layout compiler + sanity checker  
  - **Definition of done:** Compiler produces layout and validates descriptor integrity.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `GmcmLayoutCompiler`.

- [x] **9.3** Register menu via BOAF API  
  - **Definition of done:** Uses `IBirdsOfAFeatherApi.RegisterGmcmMenu(...)`.  
  - **Artifacts / touched subdirs:** RoosterAddon  
  - **Patch IDs:** pre-snapshot  
  - **Notes:** See `RoosterCoordinator.Gmcm.cs`.

### Completion log (append-only)
- pre-snapshot — Phase completed before this snapshot.

---

## Phase 10 — CI build gate + release hardening (repo-wide)
**Outcome:** CI runs on push/PR and enforces restore/build/test; adds guard tests for legacy strings/i18n.  
**Dependencies:** Repo root + Tests/ (not in this zip).

### Steps
- [ ] **10.1** Add GitHub Actions build gate  
  - **Definition of done:** Workflow restores/builds/tests on push/PR.  
  - **Artifacts / touched subdirs:** Core  
  - **Patch IDs:** TBD  
  - **Notes:** Requires `.github/workflows/ci.yml` in repo root.

- [ ] **10.2** Run pure tests in CI  
  - **Definition of done:** CI runs deterministic unit tests (no SMAPI/game dependency).  
  - **Artifacts / touched subdirs:** Core + Tests  
  - **Patch IDs:** TBD  
  - **Notes:** Prefer a Pure project if needed.

- [ ] **10.3** Add guard tests  
  - **Definition of done:** No legacy strings; no orphaned i18n keys.  
  - **Artifacts / touched subdirs:** Tests  
  - **Patch IDs:** TBD  
  - **Notes:** Add tests; wire into CI.

### Completion log (append-only)
- 2026-02-19 — Repo-wide; tracked in Core/Tests docs.

---

## Notes / Decisions
- Consolidated docs into `docs/` and aligned with BOAF templates — 2026-02-19
