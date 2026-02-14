```text
You are a senior C# refactoring agent working in this repository:

/home/mike/Documents/Projects/WildEggsHatching

Read and follow these documents first:
1) REFACTOR_INTEGRATION_PLAN.md
2) DESERT_TOMB_MODULE_PLAN.md
3) refactor/README.md
4) refactor/TODO.md

Goal:
Implement the full architecture migration described in those docs, with behavior parity and staged delivery.

Critical constraints:
- Keep ModEntry.cs as bootstrap/forwarder only.
- No domain logic should directly use Game1/SMAPI types.
- Save read/write must be centralized in repository boundaries.
- Keep DesertTomb as a separate feature module migration track.
- Do not mix DesertTomb internals into WildEggs `refactor` domain layers.
- Preserve compatibility with existing saves.
- Prefer incremental, testable commits with clear migration checkpoints.

Execution order (strict):
A) Stabilization first (DesertTomb Phase 0 correctness fixes)
- Fix all correctness bugs listed in DESERT_TOMB_MODULE_PLAN.md section “Critical Correctness Fixes (Do First)”.
- Ensure no repeated tomb generation per visit.
- Remove hardcoded teleport and duplicate lifecycle calls.
- Fix signature mismatches and logic issues (difficulty filter, config usage, wanderer caps/counters, hardcoded ChooseLevelType input).

B) Main WildEggs refactor migration (REFACTOR_INTEGRATION_PLAN phases)
1. Phase 1 boundary isolation:
   - Introduce router boundary for SMAPI event registration.
   - Move direct event registration out of `refactor/Coordination/WildEggCoordinator.cs`.
   - Reduce ModEntry orchestration.
2. Phase 2 save migration:
   - Add versioned DTOs and compatibility mapping.
   - Keep old save key compatibility.
3. Phase 3 spawn isolation:
   - Remove remaining global/game coupling from domain-facing spawn logic.
4. Phase 4 progression isolation:
   - Remove temporary legacy/static save access in progression paths.
5. Phase 5 asset/content boundary:
   - Isolate asset-edit logic into infrastructure boundary.
6. Phase 6 legacy removal:
   - Remove old manager-driven orchestration after parity verification.

C) DesertTomb modular rewrite track (post stabilization)
- Implement module-based architecture from DESERT_TOMB_MODULE_PLAN.md:
  - Core module contracts and manager integration.
  - DesertTombModule + coordinator + activation service.
  - Immutable runtime model (TombRunContext, TombTemplate, SpawnPlan, SpawnBudget).
  - Explicit stages: ActivationService, TemplateResolver, LevelMutator, SpawnPlanner, MonsterFactory, SpawnApplier.
  - Deterministic seeded RNG; no Game1.random in domain decisions.
  - Data-driven templates with schemaVersion and startup validation.
  - Monster profile registry + strategy spawners.
  - Diagnostics command and structured counters.
  - Idempotency and failure isolation.

Implementation requirements:
- Update docs as you go when status changes:
  - REFACTOR_INTEGRATION_PLAN.md
  - DESERT_TOMB_MODULE_PLAN.md
  - refactor/README.md
  - refactor/TODO.md
- Keep status truthful (“in progress”, “done”, “not started”).
- Add/adjust tests for migrated logic where feasible.
- If a test framework is missing, add lightweight validation commands and diagnostics output checks.
- Keep changes compile-safe at each phase boundary.

Definition of done:
- All phases in REFACTOR_INTEGRATION_PLAN are implemented and reflected in code.
- DesertTomb Phase 0 bugs are fixed and validated.
- DesertTomb modular architecture is implemented per plan.
- ModEntry is thin; event routing centralized; domain boundaries enforced.
- Save compatibility preserved.
- Docs and code status are in agreement.

Output format for each work cycle:
1) What changed (files + purpose)
2) Why (phase/requirement)
3) Verification performed (build/tests/manual checks)
4) Remaining items
```
