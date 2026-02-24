# LetMeHatch Refactor Phase Plan (EggsInWild-inspired)

**Hard rules (always-on):**
- **NestCore (NestCore) mandatory**: fail-fast if missing; **no local duplicates** where NestCore has equivalents.
- **No legacy compatibility**: full refactor; remove legacy runtime config/option strings.
- **Packaging discipline**: patch zips follow `zip2git` rules (separate subdirs, include full folder where required).

---

## Phase 0 — Guardrails + repo hygiene ✅ (implemented)

## Phase 1 — Build the EggsInWild “spine” ✅ (implemented)

---

## Phase 2 — Content pipeline refactor (replace GameAssetManager routing) ✅

**Inspiration:** mirror EggsInWild’s `Infrastructure/Content/*` pattern:
- `IAssetEditor` (+ optional `AssetEditorBase`)
- `ContentContext`
- `SmapiContentCoordinator` that orders editors by `AssetEditPriority` and routes `AssetRequested → e.Edit(...)`

### 2.0 Inventory + freeze behavior
- List every current `AssetRequested` branch and edit priority:
  - `Data/FarmAnimals`, `Data/Objects`, `Data/Machines`
  - any special loads (textures / NestCore `ModAssetHandler` paths)
- Confirm “must still happen first/last” ordering constraints.

### 2.1 Introduce content abstractions (EggsInWild-style)
- Add `Infrastructure/Content/IAssetEditor.cs` (EggsInWild shape: `AssetName`, `Priority`, `Edit(asset, ctx)`)
- Add `Infrastructure/Content/AssetEditorBase.cs` (optional, matches EggsInWild)
- Add `Infrastructure/Content/ContentContext.cs` (helper/monitor/i18n/config + any LMH refs)

### 2.2 Add `SmapiContentCoordinator` (EggsInWild-style)
- Add `Infrastructure/Content/SmapiContentCoordinator.cs`:
  - holds a list of registrations
  - subscribes to `Helper.Events.Content.AssetRequested`
  - on request: filter by `IsEquivalentTo(AssetName)`, order by `Priority`, call `e.Edit(...)`
- Optional (EggsInWild feature): registration includes an **activation gate** (e.g., `InitStage`) if LMH benefits.

### 2.3 Separate **loads** vs **edits** (LMH-specific)
LMH has both patterns; keep them explicit:
- Add `IAssetLoader` (or keep loads in coordinator with a clear “load-first” block).
- Migrate:
  - `Animals/MaleClones*` texture loads
  - NestCore `ModAssetHandler.GamePaths` asset loads
  - cloned-eggs special-cased loader

### 2.4 Port edits into editors (thin wrappers first)
Create editors that initially **delegate to existing logic** (no behavior change):
- `FarmAnimals*` editors (clones + egg list + birth text + breeding bits)
- `Objects*` editors (cloned eggs, fertilized eggs)
- `Machines*` editor (machine rule cloning)

### 2.5 Register editors in one place
- Build the registrations list in coordinator creation (single source of truth).
- Centralize priorities (constants or config), and (if present) push into public API priority reporting.

### 2.6 Swap routing ownership
- `LmhCoordinator.OnAssetRequested` becomes a single delegation to `SmapiContentCoordinator`.
- Remove/retire old routing branches.

### 2.7 Reduce `GameAssetManager`
- Keep only shared caches/helpers that editors genuinely share.
- Remove it as “the router”.

### 2.8 Add diagnostics
- Console command: `lmh_content_handlers` → list registered loaders/editors + priorities + gating.
- Optional: `lmh_invalidate_all` → coordinator invalidates all registered asset caches.

**Phase 2 done when:** content flow is **AssetRequested → Coordinator → (Loaders) → (Editors)** and behavior matches pre-refactor.

---

## Phase 3 — Extract pure services (Core) + game adapters (Infrastructure)

**Goal:** move decision logic out of SMAPI/Game1 so it’s testable.

### 3.0 Define boundaries + contracts
- Identify “pure logic” vs “game access” seams:
  - clone mapping resolution
  - egg selection rules + RNG
  - breeding resolution
  - machine output rules
  - exclusions/filters

### 3.1 Create Core model layer
- Move/reshape data types into `Core/Models` (e.g., clone info, breeding models, rule DTOs).
- Ensure Core models have **no SMAPI** references.

### 3.2 Extract Core services (pure logic)
- `CloneMappingService`
- `EggSelectionService` (inject deterministic RNG)
- `BreedingResolutionService`
- `MachineRuleService`
- `ConfigValidation/Normalization` helpers (pure)

### 3.3 Create Infrastructure game adapters
- Add `Infrastructure/Game/*` adapters that wrap SDV data access:
  - read/write farm animal/object/machine dictionaries
  - lookups for items/animals
  - localization access (if needed)

### 3.4 Rewire editors to call Core services
- Editors become thin:
  - load asset dictionaries
  - call Core service
  - write back results

### 3.5 Remove global/static coupling
- Stop relying on `ModEntry.*` statics inside new code.
- Pass dependencies via constructor injection from `ModEntry → Coordinator → Services`.

**Phase 3 done when:** Core compiles in isolation (no SMAPI refs) and all game access is via adapters.

---

## Phase 4 — Config model rewrite + UI rewrite (no legacy)

**Goal:** typed config, stable UI, no nested-dictionary chaos.

### 4.0 Freeze config semantics
- Document what each existing setting *means* today (even if messy).
- Decide what gets removed/renamed (no backward compat).

### 4.1 Define new typed config model
- Replace dictionaries with typed records/classes.
- Add explicit enums for modes.
- Add a `ConfigVersion` and bump it.

### 4.2 Validation + normalization pipeline
- Implement `Core/Services/ConfigNormalizer`:
  - fill defaults
  - clamp ranges
  - resolve mutually-exclusive options
- Update `SmapiConfigProvider<T>` to:
  - read
  - normalize
  - write back (if changed)

### 4.3 Replace GMCM UI with NestCore GMCM Builder
- Remove/retire `UI/GMCMenu.cs` approach.
- Use NestCore `GmcmIntegration` + layout DSL:
  - `layout.Root(...)` + subpages
  - expression bindings `cfg => cfg.SomeOption`
  - stable deterministic field IDs

### 4.4 Decouple UI from runtime state
- UI reads/writes config only.
- No live references to `GameAssetManager` inside UI.

**Phase 4 done when:** config is typed + normalized; GMCM UI is NestCore-builder-based; old UI removed.

---

## Phase 5 — Integrations & public API correctness

**Goal:** optional mods don’t crash; API surface is clean.

### 5.0 Make integrations optional + safe
- Stop hard-throwing for optional mods (unless truly required by design).
- Replace `null!` + null-forgiveness with:
  - adapter wrapper that no-ops when missing
  - explicit capability flags

### 5.1 EggsInWild integration wrapper
- If EggsInWild API interface shape differs, add an adapter layer:
  - reflection-based binding or versioned interface wrapper
  - defensive feature detection

### 5.2 RoosterControl integration wrapper
- Same pattern: adapter with safe defaults.

### 5.3 Public API cleanup
- Implement/organize NestCore `ILetMeHatchApi` under `Infrastructure/Api/*`.
- Ensure API doesn’t expose internal mutable state.

### 5.4 Compatibility sanity checks
- Add logging that explains which integrations are active and why.

**Phase 5 done when:** LMH runs cleanly with/without optional mods; API is stable + documented.

---

## Phase 6 — Tests (under LetMeHatch/Tests)

**Goal:** lock behavior while refactoring.

### 6.0 Test project scaffold
- Add `Tests/LetMeHatch.Tests.csproj` (xUnit)
- Add CI-friendly test runner settings.

### 6.1 Pure Core service tests (highest ROI)
- clone mapping resolution
- egg selection probabilities (deterministic RNG)
- breeding resolution rules
- config normalization/validation

### 6.2 Golden-file style tests for content edits (optional)
- Given small sample dictionaries, apply editor and assert outputs.

### 6.3 Regression tests for past bugs
- Encode known edge cases as tests.

**Phase 6 done when:** tests are green and cover the riskiest logic.

---

## Phase 7 — Cleanup + docs + packaging discipline

**Goal:** ship-ready structure.

### 7.0 Remove remaining legacy/static patterns
- eliminate `using static ModEntry`
- remove `ModEntry.*` statics where possible

### 7.1 Namespace + folder alignment
- normalize namespaces to match folder layout
- remove “Managers/” leftovers if replaced

### 7.2 Docs refresh
- update README
- add/refresh troubleshooting + config docs

### 7.3 Packaging enforcement
- ensure patch slices map cleanly to zip-friendly subdir changes
- ensure NestCore rules (no duplicates) are revalidated

**Phase 7 done when:** architecture matches target tree; no dead code; docs current; packaging is repeatable.

---

## Suggested patch slices (zip-friendly)
- `phase2_content_pipeline`
- `phase3_core_services`
- `phase4_config_ui`
- `phase5_integrations_api`
- `phase6_tests`
- `phase7_cleanup_docs`
