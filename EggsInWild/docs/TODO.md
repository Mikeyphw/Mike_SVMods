# TODO — EggsInWild

- [x] Rendered to render `AnimalProgressionMode` UI for all animals or only affected ones.
- [x] Added runtime config save handling for cache invalidation where needed (`ConfigUpdated` -> pipeline invalidate).


## Progression command follow-ups
- [ ] Expand the step effects catalog to include any progression-gated quests, special orders, items, and recipes beyond mail.
- [ ] Add automated tests for ledger reconciliation (forward/backward step forcing).

## Progression console command effects
- [ ] Consider adding `--json` output option for effects preview.
- [ ] Consider previewing quest completion-day adjustments in output.

## Localization
- [ ] If you maintain extra locales, port key renames from `i18n/default.json` (Phase 10).

- [x] (WEGG_DEV) Add debug toggle to force DesertTomb handler regardless of other config gates.
- [x] (WEGG_DEV) Add debug toggle to bypass main-player + mine-level bounds when forcing.
- [x] Add always-on Harmony patch install/status logging for DesertTomb patches.
- [x] Add hook-fired + success/error logs for DesertTomb Harmony callbacks (gated by debug logging or force).



## WildAnimals GMCM follow-ups
- [ ] Add GMCM page `wildAnimals` that lists per-wild-animal subpages.
- [ ] Add per-wild-animal override config: `Enabled` (override) with effective merge.
- [ ] Respect per-wild-animal `Enabled` across egg assignment and Animal Notes/progression.
- [ ] GMCM: add `wildAnimals` index page and per-wild-animal subpages.
- [ ] GMCM: per wild animal: `Enabled` toggle (config override) + validation/status.
- [ ] Optional: add a reset-to-default action for a wild animal override.

