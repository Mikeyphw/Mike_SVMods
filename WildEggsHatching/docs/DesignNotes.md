# Design Notes — WildEggsHatching

> Last updated: **2026-02-19T18:33:11Z**

## Weighting intent
- Profitable animals should be rarer: `weight ∝ 1/(effective_value+K)^alpha`.
- Prevent degenerate outcomes:
  - clamp weights around the candidate median (`cap_low`, `cap_high`);
  - apply a small per-animal unlocked penalty (`unlocked_factor`) to reduce repeats.
- Handle `PurchasePrice=-1` safely:
  - anchored variants inherit listing price via `AlternatePurchaseTypes`;
  - optional penalty for truly non-shop animals without anchors.

## Default constants
- `horizon_days = 7`
- `p_deluxe = 0.1`
- `K = 1000`
- `alpha = 0.5`
- `cap_high = 8`
- `cap_low = 20`
- `unlocked_factor = 0.85`
- `hidden_no_anchor_penalty = 0.5` (optional)

