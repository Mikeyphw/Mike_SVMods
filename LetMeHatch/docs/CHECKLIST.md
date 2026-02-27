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
