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
- Phase 9A: complete (per-save selection state + catalog loading + list command).
- Phase 9B: complete (cycling + apply selections to runtime).
- Phase 10: in progress (late apply done; validation/diagnostics next).

---

## Phase 8 — Compat catalogs + CP-patchable assets (DONE)

Steps:
1) Define compat catalog models for egg sources + rooster sources.
2) Provide in-code defaults.
3) Expose CP-patchable mod assets:
   - `Mods/<UniqueID>/compat/egg_sources`
   - `Mods/<UniqueID>/compat/rooster_sources`
4) Ship JSON reference copies under `data/compat/`.

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

## Phase 10 — Apply selections late (conflict-minimizing) (IN PROGRESS)

Goal: resolve `FarmAnimalRef` sources after all CP edits and RoosterControl asset edits run at a safe late priority.

Steps:
1) Late apply resolution (DONE):
   - Always resolve `FarmAnimalRef` egg sources against the *post-edit* `Data/FarmAnimals` snapshot.
   - Only apply compat selections to runtime after `DayStarted` so RoosterControl's late `Data/FarmAnimals` editor has run.
   - Ensure refresh paths (plan/daily systems/location refresh) re-apply selections using the same loaded `Data/FarmAnimals`.
2) Diagnostics (NEXT): explain what resolved value was used when multiple packs affect the same base animal.
3) Optional validate (NEXT): add `ra_compat validate` (read-only) to report invalid `FarmAnimalRef` targets/indexes.

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
