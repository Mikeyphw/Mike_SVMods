# PLAN — WildEggsHatching

> Location (required): `Mike_SVMods/WildEggsHatching/docs/PLAN.md`  
> \12026-02-19T21:46:00Z\3  
> Status: **Active** (Active | Paused | Done)

## Scope
- **Goal:** Implement economy-based, repeat-resistant weighted selection for chosen animals (profitable = rarer), while preserving original shop lock/unlock behavior and AlternatePurchaseTypes handling.
- **Non-goals:**
  - Reintroducing legacy GMCM UI parity beyond what is needed for progression toggles.
  - Using `WildAnimalsList.json` weights for selection (explicitly excluded).
- **Constraints:** BOAF mandatory; no local duplicates; no legacy compatibility; packaging rules apply.

## Working agreements
- Each **Phase** has **3–7 Steps**.
- A **Step is done** when: code compiles, targeted tests pass (or are added), and diagnostics (if applicable) are updated.
- Prefer **built-in GSQs** (e.g., `PLAYER_HAS_MAIL Host ... Received`) over custom queries unless strictly required.

## Phase 1 — Baseline and constants
- **1.1** Identify selection touchpoints (where chosen animals are computed/stored) and the `Data/FarmAnimals` editor entrypoint.
- **1.2** Add/confirm constants (defaults) in a single place:
  - `horizon_days=7`, `p_deluxe=0.1`, `K=1000`, `alpha=0.5`, `cap_high=8`, `cap_low=20`, `unlocked_factor=0.85`, `hidden_no_anchor_penalty=0.5` (optional).
- **1.3** Document constants and intended behavior in `docs/DesignNotes.md`.

### Implementation notes
- Touchpoints documented in `docs/DesignNotes.md`.
- Constants implemented in `Core/Weighting/WeggWeightingDefaults.cs`.
- Shop anchor mapping implemented in `Core/ShopAnchors/ShopAnchorIndex.cs` and verified via `wegg verify` (`farmanimals/anchors`).


## Phase 2 — WildAnimalsList single source of truth
- **2.1** Ensure editors can access `ModDataSnapshot` via the existing content pipeline context.
- **2.2** Refactor `ObjectsEditor` to enumerate `snapshot.SpawnAreas.Keys` and `snapshot.WildAnimals.Keys` (no direct file IO).
- **2.3** Refactor `QuestEditor` to enumerate `snapshot.WildAnimals.Keys` (no direct file IO).
- **2.4** Refactor `MailEditor` to enumerate `snapshot.WildAnimals.Keys` (no direct file IO).

## Phase 3 — Shop-anchor index for AlternatePurchaseTypes
- **3.1** Implement `ShopAnchorIndex` builder from `Data/FarmAnimals`:
  - For each parent with `PurchasePrice>0`, map each `AlternatePurchaseTypes[*].AnimalIds` child to `min(anchorPrice, 2*PurchasePrice)`.
- **3.2** Expose helpers: `HasAnchor(id)`, `TryGetAnchorListingPrice(id)`.
- **3.3** Add diagnostics (debug log / verifier) showing anchored variant counts and a few sample mappings.

## Phase 4 — EffectiveValueCalculator + weight (profitable = rarer) + clamps + repeat penalty
- **4.1** Implement `EffectiveValueCalculator(id)` with priority order:
  1) `PurchasePrice>0` → `2*PurchasePrice`
  2) anchored variant → `anchorListingPrice`
  3) `SellPrice>0` → `SellPrice`
  4) produce estimate (uses `DaysToMature`, `DaysToProduce`, `horizon_days`, `p_deluxe`, and `Data/Objects` sell prices)
  5) fallback → median effective value across candidates
- **4.2** Implement base weight: `w_base = 1 / (effective_value + K)^alpha`.
- **4.3** Implement median-based clamping:
  - `median_w = median(w_base candidates)`
  - `w_clamped = clamp(w_base, median_w/cap_low, median_w*cap_high)`
- **4.4** Apply optional hidden/no-anchor penalty only when `PurchasePrice<0 && !HasAnchor(id)`.
- **4.5** Apply **per-animal unlocked penalty**:
  - `w *= unlocked_factor` when unlocked (mail received and/or save-state unlocked list).
- **4.6** Add deterministic weighted picker (seeded so reloads don’t reroll) and unit tests for edge cases.

## Phase 5 — Apply weighted selection to chosen-animal mapping

### Phase 5A — Core behavior change (implemented)
- Replaced uniform/JSON-weight selection with economy-derived weights.
- Deterministic per (day + eggId).

### Phase 5B — Diagnostics (implemented)
- **5.1** Define candidate set per selection event (eligible farm animal keys, correct house, installed mods, valid assets).
- **5.2** Use `ShopAnchorIndex` + `EffectiveValueCalculator` + `WeightCalculator` to compute `w_final` (no JSON weights).
- **5.3** Integrate the picker into chosen-animal computation and persist results in the existing progression state.
- **5.4** Added `wegg verify` output (`farmanimals/weights`) showing top/bottom N by weight, with effective value and penalty flags.

## Phase 6 — Restore original shop lock/unlock behavior (parity) with GSQ gating
- **6.1** ✅ Restore/ensure `AnimalProgressionMode` defaulting behavior from the WildAnimalsList-derived keys (default locked unless disabled).
- **6.2** ✅ Implement locked edits:
  - set `PurchasePrice=-1`
  - remove locked animals from any parent `AlternatePurchaseTypes[*].AnimalIds`
- **6.3** ✅ Implement unlocked edits:
  - if master listing is locked, copy master shop fields onto the unlocked entry
  - merge master + variant conditions where needed
  - append/set `PLAYER_HAS_MAIL Host <noteMailId> Received` to gate visibility
- **6.4** Preserve special cases (e.g., Blue Chicken purchasability, LetMeHatch clone handling if still applicable).

## Phase 7 — Tests and verification
- **7.1** Tests: anchor mapping; effective value priority; produce math (DaysToMature/DaysToProduce); clamp bounds; unlocked penalty; locked variant removal.
- **7.2** Add/extend verifier output: min/median/max weight stats and anchored variant coverage.
- **7.3** Update `docs/TODO.md` to reflect remaining work only; mark plan steps done as implemented.

