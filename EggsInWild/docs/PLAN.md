# PLAN — GMCM Menu (EggsInWild)

## Scope
- Subdir: `EggsInWild`
- Dependency: NestCore Phase 1 (host helper). Dynamic editor work depends on NestCore Phase 5.

## Phase 0 — Keep docs in sync
1. Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
2. Keep GMCM page id `debug` as the hub; debug/diagnostics options may live on `debug.*` subpages.
3. Keep option IDs stable forever (translation keys and field IDs depend on this).

## Phase 2 — Baseline GMCM menu via NestCore host + DSL

## Phase 2.5 — DesertTomb debug force handler (WEGG_DEV)
1. Add dev-only config toggles to force-install DesertTomb Harmony hooks and force-run the handler regardless of other DesertTomb gates.
2. Expose toggles under GMCM `debug` and `debug.desertTomb` pages with stable option IDs.
3. Add always-on Harmony patch install/status logging; add hook-fired + success/error logs gated by DesertTomb debug logging or force flags.

1. Wire `GmcmConfigHost` in `GameLaunched`; if GMCM missing, return.
2. Build pages: `root` (General), `desertTomb`, `items`, `hatching`, `prices`, `animalNotes`, `debug`.
3. Bind options using stable `optionId`s; ensure debug-only fields live on `debug`.
4. Add i18n keys for pages and options under `config.*`.
5. If config changes require refresh, use `onSaved` to invalidate caches.

## Phase 6 — Dynamic per-animal progression mode editor
1. Add save-dependent page `animalProgression` populated only when world is ready.
2. Enumerate animal keys from `Data/FarmAnimals` (and any mod-specific anchors, if applicable).
3. Render a toggle per animal using NestCore dynamic-label overloads with stable element IDs.
4. Store overrides in `AnimalProgressionMode` dictionary using default-vs-override removal (clean config).
5. Rebuild menu on `SaveLoaded` and when relevant assets change.


## Phase 1 — Progression console command effects
1. Define a cumulative side-effect catalog for progression steps (mail/quests/orders/recipes).
2. Track console-applied side effects in player modData to enable safe rollback.
3. Update `wegg step set --effects` to apply cumulative effects and reconcile on rollback.
4. Extend game service abstractions to support removals required by reconciliation.
5. Update command help text to document the new semantics.

## Phase 9 — Progression console command effects
1. Expand progression step-effects catalog to include cumulative quests, special orders, and crafting recipes.
2. Ensure `wegg step set ... --effects` applies effects cumulatively and reconciles on rollback.
3. Add `wegg step effects preview <step>` command to show add/remove diff.
4. Keep effects rollback-safe by tracking only command-granted state in player modData ledger.
5. Update tests to reflect new service capabilities.

## Phase 10 — Normalize i18n key taxonomy
1. Normalize UI keys under `ui.*` (HUD / HUDMessage).
2. Normalize mail keys (`mail.*.body`, variant bodies as `mail.*.body.<n>`), remove legacy typos.
3. Normalize quest group IDs + variant keys (e.g. `quest.DesertScroll.*`, `quest.NoteDelivery.reactionText.<n>`).
4. Normalize GMCM free-text keys under `config.section.*` and `config.paragraph.*`.
5. Update verifiers/diagnostics to validate normalized keys only.