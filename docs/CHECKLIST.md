# CHECKLIST — LetMeHatch

## Repo Phase R1 — GMCM id/i18n Normalization

See [Refactor/R1 Documentation](refactor/Repo-wide_GMCM_ID_i18n_Normalization_R1D.md) for full checklist.

## Repo Phase R2 — Monolithic File Splits

### EggsInWild

| Phase | Status |
|-------|--------|
| EIW-R2-1: Split DebugKnobs config types | [ ] |
| EIW-R2-2: Split WeggVerifier helper types | [ ] |
| EIW-R2-3: Extract Dump* models | [ ] |
| EIW-R2-4: Split AnimalNotesOrchestrator | [ ] |

### LetMeHatch

| Phase | Status |
|-------|--------|
| LMH-R2-1: Move #region Eggs | [ ] |
| LMH-R2-2: Move #region Items | [ ] |

### RoosterControl

| Phase | Status |
|-------|--------|
| RC-R2-1: Extract debug record models | [ ] |
| RC-R2-2: Split debug methods | [ ] |

**Reference**: [Repo-wide R2 Documentation](refactor/Repo-wide_R2_Monolithic_File_Splits.md)

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
