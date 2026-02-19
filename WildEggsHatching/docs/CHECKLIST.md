# CHECKLIST — WildEggsHatching

> Location (required): `Mike_SVMods/WildEggsHatching/docs/CHECKLIST.md`  
> \12026-02-19T21:46:00Z\3  
> Rule: must mirror `PLAN.md` steps 1:1.

## Phase 1 — Baseline and constants
- [x] 1.1 Identify selection touchpoints and the Data/FarmAnimals editor entrypoint
- [x] 1.2 Add/confirm constants in a single place
- [x] 1.3 Document constants in docs/DesignNotes.md

## Phase 2 — WildAnimalsList single source of truth
- [x] 2.1 Provide ModDataSnapshot access to editors via pipeline context
- [x] 2.2 Refactor ObjectsEditor to use snapshot keys (no direct file IO)
- [x] 2.3 Refactor QuestEditor to use snapshot keys (no direct file IO)
- [x] 2.4 Refactor MailEditor to use snapshot keys (no direct file IO)

## Phase 3 — Shop-anchor index for AlternatePurchaseTypes
- [x] 3.1 Implement ShopAnchorIndex builder from Data/FarmAnimals
- [x] 3.2 Expose HasAnchor/TryGetAnchorListingPrice helpers
- [x] 3.3 Add diagnostics/verifier coverage for anchored variants

## Phase 4 — EffectiveValueCalculator + weight + clamps + repeat penalty
- [x] 4.1 Implement EffectiveValueCalculator with purchase/anchor/sell/produce/median priority
- [x] 4.2 Implement base inverse weight formula
- [x] 4.3 Implement median-based clamp bounds
- [x] 4.4 Apply hidden/no-anchor penalty only for non-anchored -1 animals (optional)
- [x] 4.5 Apply per-animal unlocked penalty
- [x] 4.6 Add deterministic weighted picker + unit tests

## Phase 5 — Apply weighted selection to chosen-animal mapping
- [x] 5.1 Define candidate set per selection event
- [x] 5.2 Compute final weights (no JSON weights) using anchor+effective value
- [x] 5.3 Integrate picker into chosen-animal computation + persistence
- [x] 5.4 Add diagnostic output for candidate weights

## Phase 6 — Restore original shop lock/unlock behavior with GSQ gating
- [ ] 6.1 Restore/ensure AnimalProgressionMode defaulting (locked by default)
- [ ] 6.2 Implement locked edits (PurchasePrice=-1, remove from alternates)
- [ ] 6.3 Implement unlocked edits (copy master fields, merge conditions, mail GSQ)
- [ ] 6.4 Preserve special cases (Blue Chicken, LetMeHatch clone if applicable)

## Phase 7 — Tests and verification
- [ ] 7.1 Add tests for mapping/value/weights/clamps/unlock behavior
- [ ] 7.2 Add/extend verifier output for weights + anchor coverage
- [ ] 7.3 Update docs/TODO.md (remaining work only) and mark completed checklist items

