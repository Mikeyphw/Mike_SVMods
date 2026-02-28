# LetMeHatch Plan

## Phase 0 — Dev-only gating + docs hygiene
- Gate LetMeHatch dev-only code behind `LMH_DEV` (built via `dotnet build -c Dev`).
- Keep `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` in sync.
- Ensure `dotnet build` remains green across `Debug`, `Release`, and `Dev`.

## Phase 1 — Scaffolding + contracts
- Clone pipeline contracts and config toggle.

## Phase 2 — Deterministic color resolver + clone plan builder
- Core ColorResolver + ClonePlanBuilder + tests.

## Phase 3 — ObjectData blueprint builder
- Minimal whitelist blueprint + tests.

## Phase 4 — FarmAnimals clone-plan adapter
- Adapter to build ClonePlanBuildRequest from Data/FarmAnimals.

## Phase 5 — Wire plan + blueprint into cloning (flagged)
- Use plan-driven clone enumeration behind flag.
- Blueprint mapping into ObjectData (no deep clone).

## Phase 6 — Dynamic per-clone texture loader (flagged)
- Asset loader serves Mods/<UniqueId>/Objects/Clones/<cloneId> textures.

## Phase 7 — Wire dynamic textures + preview + cache invalidation + remove legacy loader
- Route cloned items to dynamic asset key, SpriteIndex=0.
- Add preview + cache helpers.
- Remove legacy loader from pipeline.

## Phase 8 — Finalize migration + expand coverage
1. Remove legacy cloned-items sheet plumbing (asset-load hook + adapter + legacy handler).
2. Route fertilized eggs through dynamic texture assets (SpriteIndex=0).
3. Implement tint precedence: config override → clone CustomFields hex → tag fallback.
4. Add cache clear/dump commands and enhance preview with source texture + crop rect.
5. Remove legacy sheet assets/files.

## Phase 9 — Legacy cleanup + tint parity + cache invalidation
1. Remove legacy cloned-items sheet loader plumbing (asset-load hook + handler + adapter).
2. Update dynamic clone tint precedence: config override → SavedColors per-animal → clone CustomFields hex → tags.
3. Clear dynamic clone texture cache on config updates (GMCM) via SmapiConfigProvider.ConfigUpdated.
4. Remove remaining legacy gray texture hooks and sheet asset references.
5. Build must remain green.

## Phase 10 — Tint modes + clone texture diagnostics
1. Add DynamicCloneTintMode enum + config + GMCM + i18n.
2. Implement tint modes in DynamicCloneObjectTextureLoader (None/Grayscale/Multiply).
3. Add console commands: healthcheck, stats, clear cache.
4. Enhance clone texture preview output (mode + tint source).
5. Build must remain green.

## Phase 11 — No-behavior-change cleanup + NestCore-first loader refactor
1. Remove unused fields/warnings and keep build green.
2. Refactor dynamic clone texture loader to a NestCore IAssetRequestHandler (remove LMH loader adapter layer).
3. Delete unused legacy loader files/interfaces now superseded by NestCore pipeline.
4. Remove LINQ allocations in clone texture diagnostics + cache key enumeration.
5. Keep diagnostics output stable (no new commands/features; refactor only).
6. Update PLAN/CHECKLIST/TODO to reflect Phase 10–11 completion.

## Phase 12 — Breeding correctness + invariants (destructive, but safe)
1. Fix female filtering bug (`Males.Count <= 0`).
2. Unify RNG: construct once per run/day and thread through selection + application.
3. Enforce invariant: male animal keys (including clones) never have `EggItemIds` (final write in executor).
4. Clarify semantics: `maleChance` drives fertilization chance; eggs remain on the female egg-domain only.
5. Add plan-style diagnostics logging (selected pairs, chances, canonical egg-domain outputs).

## Phase 13 — Introduce `BreedingPlan` (planner + executor split)

Status: completed (planner + executor + wiring).

1. Add Core contracts: `BreedingPlan`, `BreedingPairPlan`, and `BreedingMutation` operations.
2. Add planner/executor interfaces: `IBreedingPlanner`, `IBreedingPlanExecutor`.
3. Implement planner that reproduces current pair selection + fertilized egg selection (no behavior drift).
4. Implement executor that applies a canonical (destructive) egg-domain rebuild per animal key.
5. Wire current breeding entrypoint to: build inputs → plan → apply (optionally behind a config flag first).

## Phase 14 — Destructive rebuild hardening (still destructive)
1. Scope destruction to egg-domain only:
   - rewrite `EggItemIds` canonically,
   - remove only egg-like entries from `ProduceItemIds`/`DeluxeProduceItemIds`,
   - never remove unrelated produce.
2. Optional: track LMH-owned egg ids (clone/fertilized) to make canonical rebuild deterministic and auditable.
3. Performance: compute male counts in a single pass (dictionary) and reuse for all pair calculations.
4. Add plan validation (fail-fast):
   - forbid adding eggs to male keys,
   - require all clone placeholders exist before apply.
5. Add plan diff logging: per animal key (added/removed egg-like ids, fertilized egg choice, enforcement actions).

## Phase 15 — Tests + documentation upkeep

Status: completed (tests + debug commands + validator).

1. Add xUnit tests for invariants:
   - male clone never gets `EggItemIds`,
   - fertilized replacement only touches egg-like entries,
   - deterministic plan given seed.
2. Add snapshot-style tests for small synthetic `Data/FarmAnimals` maps.
3. Update `PLAN.md`, `TODO.md`, and `CHECKLIST.md` as phases complete.

## Phase 16 — Deterministic debug dumps + expanded invariants

Status: completed (stable dump DTOs + more tests).

1. Replace debug dump JSON with stable DTO schema (no `DateOnly` serialization dependency).
2. Ensure deterministic ordering in dumps (sorted scope keys / other-male keys / plan list).
3. Add additional xUnit coverage:
   - fertilized path upserts produce entries and preserves non-egg produce,
   - LMH-managed egg tracking stays consistent and doesn’t retain cleared ids.

### Appendix — Concrete `BreedingPlan` structure (Core)
```csharp
public sealed record BreedingPlan(
    string PlanId,
    DateOnly InGameDate,
    int Seed,
    IReadOnlyList<BreedingPairPlan> Pairs,
    IReadOnlyList<BreedingMutation> Mutations,
    IReadOnlyList<string> Diagnostics);

public sealed record BreedingPairPlan(
    string FemaleAnimalKey,
    string MaleAnimalKey,
    double FertilizeChance,
    string? FertilizedEggItemId,
    string Mode);

public abstract record BreedingMutation
{
    public sealed record EnsureCloneExists(string OriginalKey, string CloneKey) : BreedingMutation;

    public sealed record RebuildFemaleEggDomain(
        string FemaleKey,
        IReadOnlyList<string> CanonicalEggItemIds,
        string? FertilizedEggItemId) : BreedingMutation;

    public sealed record StripEggLikeFromProduces(string AnimalKey) : BreedingMutation;

    public sealed record EnforceMaleNeverLays(string MaleKey) : BreedingMutation;
}

public interface IBreedingPlanner
{
    BreedingPlan BuildPlan(BreedingPlanInput input);
}

public sealed record BreedingPlanInput(
    DateOnly InGameDate,
    int Seed,
    IReadOnlyDictionary<string, FarmAnimalData> GameData,
    IReadOnlyList<BreedingFemaleConfig> Females,
    IReadOnlyDictionary<string, int> MaleCountsByAnimalKey);

public interface IBreedingPlanExecutor
{
    void Apply(IDictionary<string, FarmAnimalData> gameDataFarmAnimals, BreedingPlan plan);
}
```
