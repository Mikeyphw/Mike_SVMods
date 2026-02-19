# PARITY_CHECKLIST — RoosterAddon

> Location: `Mike_SVMods/RoosterAddon/docs/PARITY_CHECKLIST.md`  
> Last updated: **2026-02-19T04:18:29Z**

---
Use this after each phase to confirm behavior hasn't drifted unexpectedly.

## Setup
- Load the same test save.
- Run `roosteraddon_dump_state` and keep the JSON output.
- Toggle debug option if you rely on debug logs.

## GMCM (Generic Mod Config Menu)
- Open GMCM page.
- Change a few options, save, reload menu:
  - Values persist.
  - No lag spikes when opening GMCM.
- Change **Female assignments**, save:
  - Reload game content (e.g. warp / sleep) and confirm changes apply.

## Farm animals patch
- Day start:
  - Rooster types exist in `Data/FarmAnimals`.
  - Shop icons/textures still load.
- Warp between locations:
  - If UpdateRate is OnLocationChange, confirm FarmAnimals edits re-apply.

## Hatching behavior
- HatchOnlyByFertilized = ON:
  - Females produce fertilized eggs as expected.
  - Roosters hatch from fertilized eggs as expected.
- HatchOnlyByFertilized = OFF:
  - Normal/Large eggs logic still works.
- If LetMeHatch integration is installed:
  - Cloned eggs detected and supported females appear.

## Objects patch
- Fertilized egg objects exist where expected.
- BetterTruffles tag applied to default rooster produces.

## Diagnostics
- `roosteraddon_dump_state farmanimals` works (includes farm animals key list).
- No errors/spam in SMAPI console during asset loads.
