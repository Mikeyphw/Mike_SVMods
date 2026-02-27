# CHECKLIST — EggsInWild

## Scope
## Phase 0 — Keep docs in sync
- [x] Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
- [x] Keep GMCM page id `debug` as the hub; debug/diagnostics options may live on `debug.*` subpages.
- [x] Keep option IDs stable forever (translation keys and field IDs depend on this).
## Phase 2 — Baseline GMCM menu via NestCore host + DSL
- [x] Wire `GmcmConfigHost` in `GameLaunched`; if GMCM missing, return.
- [x] Build pages: `root` (General), `desertTomb`, `items`, `hatching`, `prices`, `animalNotes`, `debug`.
- [x] Bind options using stable `optionId`s; ensure debug-only fields live on `debug`.
- [x] Add i18n keys for pages and options under `config.*`.
- [x] If config changes require refresh, use `onSaved` to invalidate caches.
## Phase 6 — Dynamic per-animal progression mode editor
- [x] Add save-dependent page `animalProgression` populated only when world is ready.
- [x] Enumerate animal keys from `Data/FarmAnimals` (and any mod-specific anchors, if applicable).
- [x] Render a toggle per animal using NestCore dynamic-label overloads with stable element IDs.
- [x] Store overrides in `AnimalProgressionMode` dictionary using default-vs-override removal (clean config).
- [x] Rebuild menu on `SaveLoaded` and when relevant assets change.


## Phase 1 — Progression console command effects
- [x] Define a cumulative side-effect catalog for progression steps (mail/quests/orders/recipes).
- [x] Track console-applied side effects in player modData to enable safe rollback.
- [x] Update `wegg step set --effects` to apply cumulative effects and reconcile on rollback.
- [x] Extend game service abstractions to support removals required by reconciliation.
- [x] Update command help text to document the new semantics.

## Progression console command effects
- [x] Cumulative step-effects include mail, quests, special orders, and crafting recipes.
- [x] Rollback-safe reconcile removes only ledger-granted state.
- [x] Add preview command: `wegg step effects preview <step>`.
- [x] Update test doubles for new service APIs.

## Phase 10 — Normalize i18n key taxonomy
- [x] Normalize UI keys under `ui.*` (HUD / HUDMessage).
- [x] Normalize mail keys (`mail.*.body`, variant bodies as `mail.*.body.<n>`), remove legacy typos.
- [x] Normalize quest group IDs + variant keys.
- [x] Normalize GMCM free-text keys under `config.section.*` and `config.paragraph.*`.
- [x] Update verifiers/diagnostics to validate normalized keys only.
