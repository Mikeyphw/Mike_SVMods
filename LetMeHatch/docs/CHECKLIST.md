# LetMeHatch Checklist

## Phase 0 — Dev-only gating
- [x] Gate LetMeHatch dev-only code behind `LMH_DEV` (built via `dotnet build -c Dev`).

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


## Phase 16 — Debug dump stability + expanded invariants
- [x] Debug JSON dumps use stable DTO schema (no DateOnly serialization dependency).
- [x] Dump output ordering is deterministic (plan list and key lists sorted).
- [x] Fertilized path executor test added (produce upsert, preserve non-egg).
- [x] LMH-managed egg tracking test added (tracking shrinks as eggs are cleared).

## Phase 18B — GMCM DSL standardization
- [x] Keep `LmhGmcmMenu` as orchestrator and preserve page/option IDs.
- [x] Convert direct config-member manual options to bound DSL with explicit `optionId` parity.
- [x] Keep manual/unbound usage only for dynamic/dictionary/derived GMCM controls.
- [x] Use `layout.Pages(...)` batching helpers for repeated page registration where safe.
- [ ] Verify build gate (`NestCore`, `LetMeHatch`, `EggsInWild`, `RoosterControl`, solution).

## Phase 18D — Repo GMCM convention alignment
- [x] LetMeHatch is aligned to NestCore-first GMCM plumbing.
- [x] `LmhGmcmMenu` remains the single menu orchestrator.
- [x] Direct config options use bound DSL; manual options are limited to dynamic/derived needs.
- [x] No giant inline GMCM layout body remains in `ModEntry`.

## Phase 18E — Final validation
- [x] No fake-bool action pattern remains for one-shot commands.
- [x] No giant inline GMCM layout body remains in `ModEntry`/coordinators.

## Phase SUI-SML-1 — Duplicate breeding editor shell
- [x] Add `assets/views/LmhBreedingEditor.sml`
- [x] Keep the shell unwired from code
- [x] Update docs to mark code wiring deferred
- [x] Verify build

## Phase SUI-SML-2 — Duplicate colors editor shell
- [x] Add `assets/views/LmhColorsEditor.sml`
- [x] Keep the shell unwired from code
- [x] Update docs to mark code wiring deferred
- [x] Verify build

## Phase SUI-SML-3 — Duplicate advanced mappings editor shell
- [x] Add `assets/views/LmhAdvancedMappingsEditor.sml`
- [x] Keep the shell unwired from code
- [x] Update docs to mark code wiring deferred
- [ ] Verify build

## R2 — Monolithic file splits

### Phase LMH-R2-1 — Move #region Eggs into partial file
- [ ] Convert `ItemManager` to `partial class`
- [ ] Create `ItemManager.Eggs.cs`
- [ ] Move `#region Eggs` block to new file
- [ ] Run build gate
- [ ] Commit: `r2-lmh-01: split ItemManager eggs region into partial file`

### Phase LMH-R2-2 — Move #region Items into partial file
- [ ] Create `ItemManager.Items.cs`
- [ ] Move `#region Items` block to new file
- [ ] Run build gate
- [ ] Commit: `r2-lmh-02: split ItemManager items region into partial file`
