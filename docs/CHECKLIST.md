# CHECKLIST — LetMeHatch

## Repo Phase R1 — GMCM id/i18n Normalization

### Phase R1A — Define repo normalization rules
- [x] Update NestCore/GMCM_BUILDER.md with GMCM id and i18n normalization rules
- [x] Update docs/PLAN.md with Phase R1 entry
- [x] Update docs/CHECKLIST.md (this file)
- [x] Update docs/TODO.md with Phase R1 current work
- [x] Run build gate
- [x] Commit Phase R1A

### Phase R1B — Normalize LetMeHatch GMCM ids and i18n keys
- [x] Apply C# page-id literal renames in LmhGmcmMenu.cs
- [x] Apply C# option-id renames in LmhGmcmMenu.cs
- [x] Apply C# GetString(...) key renames in LmhGmcmMenu.cs
- [x] Apply i18n page key renames in default.json and pt-br.json
- [x] Apply i18n option key renames in both i18n files
- [x] Apply i18n manual GMCM key renames in both i18n files
- [x] Remove legacy page keys from both i18n files
- [x] Run verification greps to confirm old keys are gone
- [x] Run build gate
- [x] Commit Phase R1B

### Phase R1C — Prune RoosterControl legacy GMCM i18n aliases
- [x] Verify no config.(Bool|Number|Text|Page|Section). references in RoosterControl C#
- [x] Delete legacy config.Bool.* alias keys from both i18n files
- [x] Delete legacy config.Number.* alias keys from both i18n files
- [x] Delete legacy config.Text.* alias keys from both i18n files
- [x] Verify canonical dotted lowercase GMCM keys remain intact
- [x] Run build gate
- [x] Commit Phase R1C

### Phase R1D — Verify EggsInWild GMCM ids already normalized
- [x] Run verification checks for EggsInWild
- [x] Confirm EggsInWild production code unchanged (docs-only)
- [x] Run build gate
- [ ] Commit Phase R1D

### Phase R1E — Finalize repo GMCM id/i18n normalization
- [ ] Verify all phases completed correctly
- [ ] Update final docs
- [ ] Run build gate
- [ ] Commit Phase R1E

## Scope
## Phase 0 — Keep docs in sync
- [ ] Update `docs/PLAN.md`, `docs/CHECKLIST.md`, `docs/TODO.md` as phases/steps complete.
- [ ] Keep all debug/diagnostics options on GMCM page id `debug` only.
- [x] Keep option IDs stable forever (translation keys and field IDs depend on this).
## Phase 3 — Baseline GMCM menu via NestCore host + DSL
- [x] Remove/retire legacy GMCM UI plumbing in favor of NestCore host + DSL.
- [x] Wire `GmcmConfigHost` in `GameLaunched`; if GMCM missing, return.
- [x] Build pages: `root`, `cloning`, `colors`, `breeding`, `advanced`, `debug`.
- [x] Bind scalar options with stable `optionId`s; put all logging/show-debug toggles on `debug`.
- [x] Add i18n keys under `config.*`; use `onSaved` only if cache refresh is needed.
## Phase 7A — Dynamic per-pack/per-animal toggles (low risk)
- [x] Add page `edits` populated from LMH’s known sources (packs/animal keys).
- [x] Render one toggle per entry with custom label funcs (friendly name fallback to key).
- [x] Use default-vs-override dictionary storage (remove key when equals default).
- [x] Rebuild on `SaveLoaded` so dynamic sources are available.
- [x] Keep option IDs stable using NestCore dynamic ID helper.
## Phase 7B — Egg color controls (medium risk)
- [x] Keep scalar toggles on `colors` page (`ChangeEggColors`, `LinkAnimalColorToEgg`).
- [x] Add page `eggPalette` to control allowed color families per egg item (`EggColors`).
- [x] Add page `eggColorOverrides` to force a fixed color per egg item (`EggColors_ColorObject`) with GMCM Options when available (fallback to native GMCM).
- [x] Keep UI bounded; enumerate eggs from `Data/FarmAnimals` + `Data/Objects` with config fallback.
- [x] Use stable element IDs derived from eggId + color key via `GmcmDynamicIds`.


## Phase 7C — ItemToClone editor (highest risk; bounded UI)
- [x] Add index page `itemMappings` with sub-pages per clone type.
- [x] Populate animal list from `AnimalVanillaItemRelationOrder` when available (fallback to non-vanilla animals).
- [x] Use curated dropdowns for known choices + a “Custom…” fallback text entry.
- [x] Normalize IDs (strip qualifiers like `(O)`) and apply overrides at runtime.
- [x] Keep config clean: remove overrides when equal to computed defaults.

### Phase 6 — Dynamic per-clone texture loader (flagged)
- [ ] Add `DynamicCloneObjectTextureLoader`.
- [ ] Register loader in content pipeline.
- [ ] `dotnet build` remains green.
