# RoosterAddon Parity Checklist

Use this checklist to verify refactors didn’t break behavior.

## GMCM
- [ ] GMCM page opens from title + in-game without errors.
- [ ] Reset restores defaults.
- [ ] Save applies changes and persists `roosterFemales.json`.

## Farm animals patching
- [ ] Roosters appear with correct:
  - required building
  - purchase/sell prices
  - maturation + production days
  - harvest type
- [ ] Rooster reload does not spam errors.

## Hatching behavior
- [ ] Original mode: normal/large eggs show up as expected.
- [ ] Fertilized-only mode: fertilized eggs replace produce for mapped females.
- [ ] Rooster-to-chicken ratio option affects egg sharing as expected.

## Data/Objects patching
- [ ] Fertilized egg objects exist (cloned/created as expected).
- [ ] BetterTruffles context tag applied only to intended objects.

## Interop
- [ ] LetMeHatch integration works when installed.
- [ ] No hard dependency when not installed.

## Diagnostics
- [ ] `roosteraddon_dump_state` creates a JSON file under `data/state-dumps/`.
