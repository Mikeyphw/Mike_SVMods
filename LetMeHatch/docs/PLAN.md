# LetMeHatch Plan

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
