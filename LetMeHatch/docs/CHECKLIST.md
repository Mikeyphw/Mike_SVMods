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
