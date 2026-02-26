# CHECKLIST — LetMeHatch

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


## Clone pipeline modernization (2026)

### Phase 1 — Scaffolding + contracts (no behavior changes)
- [x] Add Core contracts: CloneSpec/ClonePlan + interfaces.
- [x] Add config toggle `UseDynamicCloneTextures`.
- [x] Register placeholders in composition root (throw if called).
- [x] Keep old cloning + texture pipeline untouched.
- [x] `dotnet build` remains green.

### Phase 2 — Deterministic color resolver + clone plan builder
- [x] Implement Core `ColorResolver` (deterministic; no mutation).
- [x] Implement Core `ClonePlanBuilder`.
- [x] Add unit tests under `LetMeHatch.Core.Tests`.
- [x] Wire default implementations in composition root (still unused).
- [x] `dotnet build` remains green.


### Phase 3 — Minimal ObjectData builder (no wiring)
- [x] Implement Core `CloneObjectDataBuilder` (whitelist-copy blueprint).
- [x] Update Core request/response models to support whitelist-copy.
- [x] Add unit tests under `LetMeHatch.Core.Tests`.
- [x] Keep runtime cloning untouched.
- [x] `dotnet build` remains green.
