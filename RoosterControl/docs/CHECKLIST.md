# RoosterControl Checklist

## Phase 8 — Compat catalogs + CP-patchable assets
- [x] Catalog models for egg sources + rooster sources
- [x] In-code defaults
- [x] CP-patchable mod assets
- [x] JSON reference copies under `data/compat/`

## Phase 9A — Selections + catalog loading (read-only)
- [x] Save-state schema bump + compat selection payload
- [x] Catalog loading from mod assets
- [x] Deterministic repair for invalid/missing selections
- [x] `ra_compat list` command
- [x] Docs updated (PLAN/CHECKLIST/TODO)

## Phase 9B — Cycling + apply selections to runtime
- [x] Cycle egg sources per EggKind
- [x] Enable/disable/cycle rooster providers
- [x] Persist changes immediately
- [x] Wire selections into runtime (planning + edits)
- [x] Remove remaining hardcoded rooster key detection

## Phase 10 — Late apply + validation
- [x] Late-apply FarmAnimalRef resolution against post-edit Data/FarmAnimals
- [x] Use NestCore invalidation batching + next-tick scheduling (no direct InvalidateCache)
- [x] Optional: `ra_compat validate` (read-only) FarmAnimalRef report
- [x] Diagnostics: explain final resolved source/value

## Phase 11 — Fertilized egg synthesis
- [x] Prefer native fertilized eggs when provided by the active egg source
- [x] Otherwise synthesize fertilized variants into `Data/Objects`
- [x] Ensure fertilized-only hatching uses the resolved fertilized IDs (rooster + female)
- [ ] Tests/regression harness (Phase 11D follow-up)
