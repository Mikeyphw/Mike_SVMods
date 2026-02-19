# CHECKLIST — WildEggsHatching

> Location (required): `Mike_SVMods/WildEggsHatching/docs/CHECKLIST.md`  
> Last updated: **2026-02-19T18:33:11Z**  
> Rule: must mirror `PLAN.md` steps 1:1.

## Phase 1 — Baseline and constants
- [ ] 1.1 Identify selection touchpoints and the Data/FarmAnimals editor entrypoint
- [ ] 1.2 Add/confirm constants in a single place
- [ ] 1.3 Document constants in docs/DesignNotes.md

## Phase 2 — WildAnimalsList single source of truth
- [ ] 2.1 Provide ModDataSnapshot access to editors via pipeline context
- [ ] 2.2 Refactor ObjectsEditor to use snapshot keys (no direct file IO)
- [ ] 2.3 Refactor QuestEditor to use snapshot keys (no direct file IO)

## Phase 3 — Shop-anchor index for AlternatePurchaseTypes
- [ ] 3.1 Implement ShopAnchorIndex builder from Data/FarmAnimals
- [ ] 3.2 Expose HasAnchor/TryGetAnchorListingPrice helpers
- [ ] 3.3 Add diagnostics/verifier coverage for anchored variants

## Phase 4 — EffectiveValueCalculator + weight + clamps + repeat penalty
- [ ] 4.1 Implement EffectiveValueCalculator with purchase/anchor/sell/produce/median priority
- [ ] 4.2 Implement base inverse weight formula
- [ ] 4.3 Implement median-based clamp bounds
- [ ] 4.4 Apply hidden/no-anchor penalty only for non-anchored -1 animals (optional)
- [ ] 4.5 Apply per-animal unlocked penalty
- [ ] 4.6 Add deterministic weighted picker + unit tests

## Phase 5 — Apply weighted selection to chosen-animal mapping
- [ ] 5.1 Define candidate set per selection event
- [ ] 5.2 Compute final weights (no JSON weights) using anchor+effective value
- [ ] 5.3 Integrate picker into chosen-animal computation + persistence
- [ ] 5.4 Add diagnostic output for candidate weights

## Phase 6 — Restore original shop lock/unlock behavior with GSQ gating
- [ ] 6.1 Restore/ensure AnimalProgressionMode defaulting (locked by default)
- [ ] 6.2 Implement locked edits (PurchasePrice=-1, remove from alternates)
- [ ] 6.3 Implement unlocked edits (copy master fields, merge conditions, mail GSQ)
- [ ] 6.4 Preserve special cases (Blue Chicken, LetMeHatch clone if applicable)

## Phase 7 — Tests and verification
- [ ] 7.1 Add tests for mapping/value/weights/clamps/unlock behavior
- [ ] 7.2 Add/extend verifier output for weights + anchor coverage
- [ ] 7.3 Update docs/TODO.md (remaining work only) and mark completed checklist items

