# CHECKLIST — RoosterAddon

> Location (required): `Mike_SVMods/RoosterAddon/docs/CHECKLIST.md`  
> Last updated: **2026-02-19T04:18:29Z**  
> Rule: must mirror `PLAN.md` steps 1:1.

## Phase 0 — Baseline & guardrails
- [x] 0.1 Create/maintain parity checklist
- [x] 0.2 Add state dump console command
- [x] 0.3 Document baseline workflow

## Phase 1 — Stop-the-bleeding fixes
- [x] 1.1 GMCM registration via BOAF only
- [x] 1.2 No file IO or cache invalidation inside AssetRequested
- [x] 1.3 Correct percentage roll semantics

## Phase 2 — Typed config only (no legacy compatibility)
- [x] 2.1 Typed config models + schema
- [x] 2.2 Remove legacy config artifacts
- [x] 2.3 Schema mismatch handling

## Phase 3 — Catalog-driven produce + random groups
- [x] 3.1 Define random groups in `produce.json`
- [x] 3.2 Resolver service for random groups
- [x] 3.3 Remove hardcoded produce translation

## Phase 4 — Asset edit boundaries + services
- [x] 4.1 Thin asset editors
- [x] 4.2 Move object tagging logic into service
- [x] 4.3 Coordinator orchestration only

## Phase 5 — Daily plan pipeline
- [x] 5.1 Pure plan computation
- [x] 5.2 Deterministic plan application
- [x] 5.3 Backup/restore plan artifacts

## Phase 6 — Legacy purge + service hardening
- [x] 6.1 Remove unused legacy types/paths
- [x] 6.2 Catalog sanitize + validate fail-fast
- [x] 6.3 Fix compile-sanity issues

## Phase 7 — Tests (repo-wide, deterministic)
- [ ] 7.1 Create Tests docs + project layout
- [ ] 7.2 Add unit tests for pure logic
- [ ] 7.3 Wire tests into CI

## Phase 8 — JSON catalogs (all knobs)
- [x] 8.1 Complete catalogs for all knobs
- [x] 8.2 Apply modes are consistent
- [x] 8.3 Validation covers required fields

## Phase 8.1 — Catalog tooling + validation (ra_catalog)
- [x] 8.1.1 Implement `ra_catalog` commands
- [x] 8.1.2 Per-file status output + backups
- [x] 8.1.3 Reload applies and invalidates safely

## Phase 9 — Descriptor-driven GMCM via BOAF API
- [x] 9.1 Descriptor model + registry
- [x] 9.2 Layout compiler + sanity checker
- [x] 9.3 Register menu via BOAF API

## Phase 10 — CI build gate + release hardening (repo-wide)
- [ ] 10.1 Add GitHub Actions build gate
- [ ] 10.2 Run pure tests in CI
- [ ] 10.3 Add guard tests
