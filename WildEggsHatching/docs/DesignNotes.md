# Design Notes — WildEggsHatching

> Last updated: **2026-02-19T20:04:41Z**

## Weighting intent
- Profitable animals should be rarer: `weight ∝ 1/(effective_value+K)^alpha`.
- Prevent degenerate outcomes:
  - clamp weights around the candidate median (`cap_low`, `cap_high`);
  - apply a small per-animal unlocked penalty (`unlocked_factor`) to reduce repeats.
- Handle `PurchasePrice=-1` safely:
  - anchored variants inherit listing price via `AlternatePurchaseTypes`;
  - optional penalty for truly non-shop animals without anchors.

## Selection touchpoints
- **Daily chosen mapping**: `Application/Animals/ChosenEggAssignments.cs` (`RecomputeIfNeeded`) computes and stores `eggItemId -> farmAnimalKey` in-memory (`Current`).
- **Entry point for recompute**: `Infrastructure/Content/Editors/FarmAnimalsEditor.cs` calls `_chosen.RecomputeIfNeeded(...)` when editing `Data/FarmAnimals` (from Part VI onward).
- **Seed/stability**: `ChosenEggAssignments.ComputeSeed()` is day-based; mapping is recomputed when the seed changes or after `Clear()`.

## Default constants
- Canonical code location: `Core/Weighting/WeggWeightingDefaults.cs`
- `horizon_days = 7`
- `p_deluxe = 0.1`
- `K = 1000`
- `alpha = 0.5`
- `cap_high = 8`
- `cap_low = 20`
- `unlocked_factor = 0.85`
- `hidden_no_anchor_penalty = 0.5` (optional)

## Shop anchors (AlternatePurchaseTypes)
Some animal variants are not directly purchasable (`PurchasePrice < 0`) but are available via a visible parent listing's `AlternatePurchaseTypes`.
We build a `ShopAnchorIndex` mapping variant IDs → anchor listing price (`2 * parent.PurchasePrice`, capped by the variant's own listing price if it is directly purchasable).
This is used by later phases to preserve shop parity and estimate effective value for hidden variants.
