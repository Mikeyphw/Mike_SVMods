# LetMeHatch Checklist

## Phase 8 — Finalize dynamic textures migration
- [ ] Remove legacy cloned-items sheet plumbing.
- [ ] Route fertilized eggs through dynamic texture assets.
- [ ] Implement tint precedence (config → custom field → tags).
- [ ] Add cache debug commands and enhance preview.
- [ ] `dotnet build` remains green.

## Phase 9 — Legacy cleanup + tint parity + cache invalidation
- [ ] Legacy cloned-items sheet load hook removed.
- [ ] Loader tint precedence matches legacy behavior.
- [ ] Cache clears on config updates.
- [ ] No GrayTextures hooks outside TextureManager.
- [ ] `dotnet build` remains green.

## Phase 10 — Tint modes + clone texture diagnostics
- [x] Add DynamicCloneTintMode enum + config + GMCM + i18n.
- [x] Implement tint modes in DynamicCloneObjectTextureLoader (None/Grayscale/Multiply).
- [x] Add console commands: healthcheck, stats, clear cache.
- [x] Enhance clone texture preview output (mode + tint source).
- [x] `dotnet build` remains green.

## Phase 11 — No-behavior-change cleanup + NestCore-first loader refactor
- [x] Remove unused fields/warnings; `dotnet build` remains green.
- [x] Dynamic clone loader implements NestCore IAssetRequestHandler (no LMH loader adapter layer).
- [x] Remove unused legacy loader files/interfaces.
- [x] Remove LINQ allocations in diagnostics + cache key enumeration.
- [x] No behavior/feature changes (refactor/perf only).

## Phase 12 — Breeding correctness + invariants (destructive, but safe)
- [x] Female filtering bug fixed (`Males.Count <= 0`).
- [x] RNG unified across planning + application.
- [x] Male keys (including clones) never have `EggItemIds` (enforced at end of apply).
- [x] Fertilization semantics updated: `maleChance` affects fertilization only; eggs remain female-owned.
- [x] Diagnostics logging includes plan summary and canonical egg-domain results.

## Phase 13 — Introduce `BreedingPlan` (planner + executor split)
- [x] Core records added: `BreedingPlan`, `BreedingPairPlan`, `BreedingMutation`.
- [x] Contracts added: `IBreedingPlanner`, `IBreedingPlanExecutor`.
- [x] Planner reproduces current selection (no behavior drift).
- [x] Executor applies canonical egg-domain rebuild (destructive by design).
- [x] Breeding pipeline wired: inputs → plan → apply (flag optional).

## Phase 14 — Destructive rebuild hardening (still destructive)
- [x] Destruction scoped to egg-domain only (no unrelated produce removal).
- [x] Optional LMH-owned egg tracking implemented (clone/fertilized ids).
- [x] Male counts computed in a single pass and reused.
- [x] Plan validation fail-fast rules added (no eggs on males, clone placeholders required).
- [x] Plan diff logging added (added/removed egg-like ids, enforcement actions).

## Phase 15 — Tests + documentation upkeep
- [x] Invariant tests: male never lays; egg-like-only replacement; deterministic plan.
- [x] Snapshot tests added for synthetic `Data/FarmAnimals` maps.
- [x] PLAN/TODO/CHECKLIST updated as phases complete.
