# RoosterControl Checklist

## Phase 8 — Compat catalogs + CP-patchable assets
- [x] Catalog models for egg sources + rooster sources
- [x] In-code defaults
- [x] CP-patchable mod assets
- [x] JSON reference copies under `data/compat/`

## Phase 9A — Selections + catalog loading (read-only)
- [x] Save-state schema bump + compat selection payload
- [x] Catalog loading from mod assets
- [x] Deterministic repair for invalid/missing selections
- [x] `ra_compat list` command
- [x] Docs updated (PLAN/CHECKLIST/TODO)

## Phase 9B — Cycling + apply selections to runtime
- [x] Cycle egg sources per EggKind
- [x] Enable/disable/cycle rooster providers
- [x] Persist changes immediately
- [x] Wire selections into runtime (planning + edits)
- [x] Remove remaining hardcoded rooster key detection

## Phase 10 — Late apply + validation
- [x] Late-apply FarmAnimalRef resolution against post-edit Data/FarmAnimals
- [x] Use NestCore invalidation batching + next-tick scheduling (no direct InvalidateCache)
- [x] Optional: `ra_compat validate` (read-only) FarmAnimalRef report
- [x] Diagnostics: explain final resolved source/value

## Phase 11 — Fertilized egg synthesis
- [x] Prefer native fertilized eggs when provided by the active egg source
- [x] Otherwise synthesize fertilized variants into `Data/Objects`
- [x] Ensure fertilized-only hatching uses the resolved fertilized IDs (rooster + female)
- [x] Tests/regression harness (Phase 11D follow-up)

## Phase 12 — GMCM UI
- [x] Per-save compat source selection (egg sources + rooster providers)
- [x] Validation summary/status on compat page
- [x] Optional: cycle buttons / improved UX (GMCMOptions)
- [x] Optional: inline per-selection validation messaging

## Phase 13 — Release hardening
- [x] Fail-fast checks + actionable errors
- [x] Harden selection repair/persistence logs + no-op clarity
- [x] Performance pass (edits/invalidation batching)
- [x] Safe defaults for empty/missing catalogs
  - [x] Clear coop rooster scan overrides when enabled rooster keys list is empty
  - [x] Clear invalid egg selections when a catalog has no valid sources (prefer (unset) over stale IDs)
  - [x] Normalize null save-state fields (SelectedEggSources/EnabledRoosterSources) to empty collections
  - [x] Warn once when compat catalogs are empty (actionable asset names)
- [x] Build/test workflow verified (warnings pass)
  - [x] Linux verify script (`RoosterControl/scripts/verify.sh`) for build + tests
  - [x] `ra_compat status` command for a single-block health + selection dump
  - [x] GMCM empty-sources message localized (i18n)
- [x] Docs: upgrade notes + release checklist

## Phase 14 — UX polish (non-optional)
- [x] GMCM Compat page layout pass (grouping/headings/spacing)
- [x] Picker affordances (icons/labels) + robust fallback without GMCMOptions
- [x] Reset affordances (all/per-egg-kind/roosters)
- [x] Actionable validation messaging
- [x] QoL toggles (e.g., dedicated All providers toggle)

## Phase 15 — Compat test matrix + content-pack examples
- [x] Compat matrix doc (known mods + expected outcomes)
- [x] Example content packs/templates + schema notes
- [ ] Reference patterns (match types + gating)
- [ ] Unit tests: catalog loading + deterministic repair
- [ ] Unit tests: fertilized mapping determinism (native vs synthesized)
- [ ] Manual QA checklist + smoke-test notes
- [ ] Docs wired to templates + matrix

## Phase 16A — Adopt NestCore GMCM batching helpers
- [x] Consume NestCore `Links(...)` helper from the RoosterControl root GMCM page
- [x] Keep all existing page ids, option ids, and i18n keys unchanged
- [x] Update docs (PLAN/CHECKLIST/TODO)
- [x] Verify build

## Phase 16B — Extract static GMCM pages
- [x] Add `Integrations/Gmcm/Pages/RoosterRootPage.cs`
- [x] Add `Integrations/Gmcm/Pages/RoosterProbabilitiesPage.cs`
- [x] Add `Integrations/Gmcm/Pages/RoosterDebugPage.cs`
- [x] Delegate `DefineGmcmLayout(...)` to the extracted page helpers
- [x] Keep `compat` and `females` on the coordinator
- [ ] Verify build

## Phase 16C — Extract rooster-type GMCM pages
- [x] Add `Integrations/Gmcm/Pages/RoosterTypePages.cs`
- [x] Move rooster page registration and editor logic into that helper
- [x] Add only minimal coordinator access needed for translation/config
- [x] Keep `compat` and `females` on the coordinator
- [x] Verify build

## Phase 16D — Add RoosterControl GMCM orchestrator
- [x] Add `Integrations/Gmcm/RoosterControlGmcmMenu.cs`
- [x] Move layout composition into the orchestrator
- [x] Keep coordinator ownership only for host setup, rebuild/save/reset, and `compat`/`females`
- [x] Replace direct `DefineGmcmLayout(...)` usage with the orchestrator
- [x] Update docs (PLAN/CHECKLIST/TODO)
- [x] Verify build

## Phase 17 — GMCM page ownership decision
- [x] Classify `compat` page ownership
- [x] Classify `females` page ownership
- [x] Keep `compat` coordinator-owned (runtime-coupled)
- [x] Keep `females` coordinator-owned (runtime-coupled)
- [x] Document final ownership decisions in PLAN.md
- [x] Verify build

## R2 — Monolithic file splits

### Phase RC-R2-1 — Extract debug record models
- [ ] Create `RoosterControl/Helpers/RoosterDebugModels.cs`
- [ ] Move 5 debug record types to new file
- [ ] Run build gate
- [ ] Commit: `r2-rc-01: extract RoosterHelper debug record models into file`

### Phase RC-R2-2 — Split debug methods into partial file
- [ ] Convert `RoosterHelper` to `partial class`
- [ ] Create `RoosterHelper.Debug.cs`
- [ ] Move 4 debug methods to new file
- [ ] Run build gate
- [ ] Commit: `r2-rc-02: split RoosterHelper debug methods into partial file`


## R5 — StardewUI White Rooster Produce Editor
- [x] Added config keybind for White rooster StardewUI editor
- [x] Routed Input.ButtonPressed to coordinator
- [x] Added StardewUI view + view model for produce entry editing
- [x] Keybind opens StardewUI menu controller and saves config on close
- [ ] Verified build

## Phase SUI-SML-1 — Duplicate compat editor shell
- [x] Add `assets/views/RoosterCompatEditor.sml`
- [x] Keep the shell unwired from code
- [x] Update docs to mark code wiring deferred
- [ ] Verify build
