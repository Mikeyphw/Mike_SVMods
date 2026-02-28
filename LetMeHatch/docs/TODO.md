# LetMeHatch TODO

- [ ] Phase 8: finalize dynamic textures migration
- [ ] Phase 9: legacy cleanup + tint parity + cache invalidation
- [x] Phase 10: tint modes + clone texture diagnostics
- [x] Phase 11: no-behavior-change cleanup + NestCore-first loader refactor
- [x] Phase 12: breeding correctness + invariants (male clone never lays, RNG unification)
- [x] Phase 13: introduce BreedingPlan (planner + executor split)
- [x] Add Core records: BreedingPlan, BreedingPairPlan, BreedingMutation.
- [x] Add contracts: IBreedingPlanner, IBreedingPlanExecutor.
- [x] Implement planner reproducing current selection (no behavior drift).
- [x] Implement executor applying canonical egg-domain rebuild (destructive by design).
- [x] Wire breeding pipeline: inputs → plan → apply (flag optional).


## Phase 14
- [x] Egg-domain-only destructive rebuild (preserve non-egg produce)
- [x] Track LMH-managed egg ids (CustomFields) and only clear LMH state
- [x] Coop-scoped male counts
- [x] Add diagnostics + perf index

## Phase 15
- [x] Add tests: validator, executor invariants, coop index.
- [x] Add breeding debug console commands (plan dump, coop index dump, validate).
- [x] Add BreedingPlanValidator and wire validation before apply.


## Phase 16
- [x] Stable BreedingPlan dump DTOs + serializer (no DateOnly in JSON)
- [x] More invariants tests: fertilized path + LMH-managed egg tracking
- [x] Keep managed egg tracking consistent with EggItemIds
