# RoosterControl Plan

This document is the authoritative phased plan for RoosterControl.  
Rules:
- Each phase is 3–7 concrete steps.
- `CHECKLIST.md` tracks completion.
- `TODO.md` tracks upcoming work items and decisions.

## Goals

1) **Deterministic daily hatching/produce** decisions (stable across reloads).
2) **Compat catalogs** that allow multiple mods to contribute:
   - **Egg sources** per logical `EggKind` (including fertilized + rooster eggs).
   - **Rooster sources** (provider-owned `Data/FarmAnimals` keys + breeding/egg-layer roles).
3) **User choice** for conflicts:
   - Mods that change vanilla eggs (blue eggs, large eggs, fertilized variants, etc) often conflict due to load order.
   - RoosterControl provides **per-save selection** and **cycling** so users can pick the active source and switch when desired.
4) **Expandable**: CP packs can add new sources without requiring code updates.

## Key assets (CP-patchable)

RoosterControl exposes two assets which Content Patcher can edit/replace:

- `Mods/<UniqueID>/compat/egg_sources`
- `Mods/<UniqueID>/compat/rooster_sources`

The built-in defaults are generated in code:
- `Application/Compat/CompatDefaults.cs`

Models:
- `Application/Compat/CompatCatalogModels.cs`

## Phase status summary

- Phases 0–7: complete (GMCM foundations + deterministic produce/hatching pipeline).
- Phase 8: complete (compat catalogs + CP-patchable assets).
- **Phase 9A: in-progress/delivered** (per-save selection state + catalog loading + list command).
- Phase 9B: next (cycling + apply selections to runtime).
- Phase 10–12: next (late apply + fertilized-egg strategy + GMCM UI).

---

## Phase 8 — Compat catalogs + CP-patchable assets (DONE)

Steps:
1) Add cycling commands:
   - `ra_compat cycle-egg <EggKind> [prev]`
   - `ra_compat cycle-rooster [prev]`
   - `ra_compat enable-all-roosters`
2) Persist changes immediately to save data.
3) Apply selections to runtime:
   - Apply selected egg IDs to in-memory rooster egg entries (Normal/Large/Fertilized) so planning + asset edits use them.
   - Apply enabled rooster provider keys to coop rooster scanning.
4) Replace hardcoded rooster key detection with enabled provider keys.
5) Update PLAN/CHECKLIST/TODO to mark 9B done and outline Phase 10+.

Acceptance:
- Assets exist and can be patched by CP packs.

---

## Phase 9A — Selections + catalog loading (read-only) (DELIVERED)

Goal: Load catalogs, maintain a per-save selection state, validate/repair it, and provide visibility via commands.  
No runtime behavior changes yet.

Steps:
1) Add per-save compat selection payload:
   - selected egg source per `EggKind`
   - enabled rooster providers (union)
2) Load catalogs from mod assets:
   - `Mods/<UniqueID>/compat/egg_sources`
   - `Mods/<UniqueID>/compat/rooster_sources`
3) Repair invalid/missing selections deterministically.
4) Add a read-only command:
   - `ra_compat list`
5) Update docs (`PLAN.md`, `CHECKLIST.md`, `TODO.md`).

Code:
- `Application/Compat/CompatSelectionState.cs`
- `Application/Compat/CompatSelectionService.cs`
- `Application/RoosterCoordinator.CompatCommands.cs`
- `Application/State/RoosterControlSaveState.cs`
- `Application/RoosterCoordinator.cs`

Acceptance:
- No gameplay changes.
- `ra_compat list` shows catalogs + current per-save selections.
- Selections persist in save data.

---

## Phase 9B — Cycling + apply selections to runtime (DONE)

Goal: Make selections affect gameplay and allow cycling (user choice).

Steps:
1) Add cycling commands:
   - `ra_compat cycle-egg <EggKind> [prev]`
   - `ra_compat cycle-rooster [prev]` (single-provider mode cycling)
   - `ra_compat enable-all-roosters` (union mode; enable all providers)
2) Persist changes immediately to save data.
3) Apply selections to runtime:
   - Apply selected egg IDs to in-memory rooster egg entries (Normal/Large/Fertilized) so planning + asset edits use them.
   - Apply enabled rooster provider keys to coop rooster scanning (remove hardcoded rooster key list).
4) Ensure day-start ordering sets DailyLuck before rooster setup.
5) Update PLAN/CHECKLIST/TODO to mark 9B done and outline Phase 10+.

Code touchpoints (expected):
- `Helpers/RoosterControl.cs` (rooster scanning + egg resolution hooks)
- `Application/RoosterCoordinator.Hatching.cs` (setup order + data loads)
- `Application/Assets/*` (egg/farm-animal edits based on selected IDs)

Acceptance:
- Cycling changes observable egg/rooster behavior without needing mod load-order changes.
- Works with one compat mod or multiple simultaneously.

---

## Phase 10 — Apply selections late (conflict-minimizing)

Goal: resolve `FarmAnimalRef` sources after all CP edits, and apply our edits in a late pass.

Steps:
1) Resolve egg refs against final `Data/FarmAnimals` state.
2) Move sensitive edits to late asset editor priority.
3) Add diagnostics to explain what “won” when multiple packs edit the same base animal.

---

## Phase 11 — Fertilized egg strategy

Goal: If a provider doesn’t ship fertilized eggs, RoosterControl creates them.

Steps:
1) Prefer native fertilized eggs if available.
2) Otherwise clone a fertilized variant into `Data/Objects`.
3) Wire selection so fertilized mapping follows chosen egg sources.

Code:
- `Application/FertilizedEggs/*`
- `Application/Assets/FertilizedEggsAssetEditor.cs`

---

## Phase 12 — GMCM UI for compat selections

Goal: expose selections/cycling via GMCM with validation.

Steps:
1) GMCM “Compat” page lists egg kinds and rooster providers.
2) Drop-down / buttons for cycling.
3) Validation warnings and reset-to-default options.
