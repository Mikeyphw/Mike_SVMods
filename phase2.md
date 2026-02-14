## Phase 2 (spawn parity, excluding DesertTomb)

### 5 options

**a.** I want spawning brought back  rewritten into **Planner → Service → Placement** layers (cleaner, but slightly slower to reach parity)
**b.** I want tile logic redesigned behind an `ITileFinder` rules engine (best long-term, more work now)
**c.** I want spawn triggers to  start with **DayStart only** and add the rest later
**d.** I want spawn limits identical to orig (**per-map + global egg-type caps**), BUT PLEASE IMPROVE THE VALUES AND LOGIC, SOME LIMITS SEEM REDUNDANT
**e.** I want RNG seeded deterministically (day + save + location → reproducible) 

### 5 improvements

1. Add `SpawnPlanner` that expands `LocationPacks` → concrete `(location, area)` jobs for today.
2. Add `SpawnLimits` that tracks existing eggs + applies caps **before** tile search (big perf win).
3. Port tile filtering parity: include/exclude rectangles, terrain checks, object occupancy, passability.  BUT PLEASE IMPROVE THE LOGIC
4. Restore spawn attempt loop behavior (tries, early exits, logging of failure reasons) and keep it deterministic with injected RNG.
5. Add xUnit tests for `SpawnPlanner`, `SpawnLimits`, and `SpawnChanceCalculator` using a seeded RNG + fake tile finder.

---
