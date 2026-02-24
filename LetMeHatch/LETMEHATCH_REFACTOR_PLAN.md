# LetMeHatch Refactor Phase Plan (EggsInWild-inspired)

This plan refactors **LetMeHatch** toward the **EggsInWild** architecture:

**Thin ModEntry → Coordinator/Facade → Services (pure logic) → Infrastructure (SMAPI/game adapters + content editing)**  
…and uses **NestCore** as the mandatory shared utility layer (**fail-fast**, **no local duplicates**).

---

## Target architecture (end state)

```
LetMeHatch/
  ModEntry.cs                          (wiring only)
  Application/
    Coordination/
      LmhCoordinator.cs                (SMAPI event coordination)
    Facade/                            (optional: public use-cases)
  Core/                                (pure logic, testable)
    Models/
    Rules/
    Services/
  Infrastructure/
    Commands/
    Config/
    Content/Editors/                   (asset editors)
    Game/                              (game adapters)
    Logging/
    Mods/                              (optional integrations)
    SaveData/
    Smapi/
  UI/
    Gmcm/
  Tests/                               (under LetMeHatch/)
```

---

## Phase 0 — Guardrails + repo hygiene ✅ (implemented)

**Goal:** make refactor safe and enforce hard rules early.

- **NestCore required**
  - Fail-fast if `Mikew.nestcore` is not loaded.
- **No embedded NestCore**
  - Detect `NestCore` folders and `*NestCore*.dll` inside this mod folder (ignoring `bin/` + `obj/`) and throw.
- **Remove backup/copy artifacts from repo**
  - Remove `*copy*` / `*.bak` source artifacts.
- **Delete legacy commented blocks**
  - Remove large commented legacy code blocks to reduce noise.

**Done when:** mod startup throws clear errors if NestCore is missing/embedded; repo has no legacy copy/bak artifacts.

---

## Phase 1 — Build the EggsInWild “spine” ✅ (implemented)

**Goal:** stop piling logic into ModEntry; create a coordination layer.

- Add **Coordinator**:
  - `Application/Coordination/LmhCoordinator.cs`
- Add minimal infrastructure:
  - `Infrastructure/Smapi/DependencyGuards.cs`
  - `Infrastructure/Config/SmapiConfigProvider.cs`
  - `Infrastructure/Logging/ILog.cs`, `Infrastructure/Logging/NestCoreLog.cs`
  - `Infrastructure/Commands/LmhConsoleCommands.cs`
- Make `ModEntry` thin:
  - Instantiate context/config/log/coordinator
  - Bind SMAPI events to coordinator
  - Register console commands via `LmhConsoleCommands`

**Done when:** ModEntry is primarily wiring; coordinator owns event handling + early init.

---

## Phase 2 — Content pipeline refactor (replace GameAssetManager routing)

**Goal:** asset edits become composable editors like EggsInWild.

- Introduce `IAssetEditor`:
  - `AssetName`, `Priority`, `Edit(asset, ctx)`
- Add `SmapiContentCoordinator`:
  - Routes `AssetRequested` to registered editors
- Convert:
  - `EggListEditor`, `BirthTextEditor`, `MachineEditor` → implement editor interface
- Reduce `GameAssetManager` to:
  - registration + small shared caches only (if needed)

**Done when:** `AssetRequested` is routed by a content coordinator, not a god class.

---

## Phase 3 — Extract pure services (Core) + game adapters (Infrastructure)

**Goal:** logic becomes testable and not tied to SMAPI/Game1.

- Move decision logic into `Core/Services`:
  - breeding resolution, clone mapping, egg selection, color rules, etc.
- Create adapters in `Infrastructure/Game`:
  - read/write animal data, object data, machine data, etc.

**Done when:** core services compile without SMAPI references and are unit-tested.

---

## Phase 4 — Config model rewrite + UI rewrite (no legacy)

**Goal:** remove nested dictionaries + decouple UI from runtime state.

- Replace config dictionaries with typed models
- Bump/reset config (no legacy compatibility)
- Rebuild GMCM UI with a builder approach (NestCore/EggsInWild style)

**Done when:** config is readable/stable; UI touches config only.

---

## Phase 5 — Integrations & public API correctness

**Goal:** make optional integrations safe and fix API mismatches.

- EggsInWild integration:
  - replace mismatched `IWildEggsApi` usage with adapter/reflection wrapper
  - never null-forgive optional APIs
- Public API:
  - implement NestCore `ILetMeHatchApi`
  - namespace/organize the API under `Infrastructure/Api`

**Done when:** mod runs without optional mods installed; API surface is clean.

---

## Phase 6 — Tests (under LetMeHatch/)

**Goal:** lock behavior while refactoring.

- Add xUnit tests for:
  - clone mapping resolution
  - egg chance selection (deterministic RNG)
  - config normalization/validation
  - breeding resolution rules

**Done when:** tests run green and cover the highest-risk logic.

---

## Phase 7 — Cleanup + docs + packaging discipline

**Goal:** ship-ready structure.

- Remove remaining static/global coupling (`using static ModEntry`)
- Normalize namespaces to match folders
- Update README + docs

**Done when:** tree matches target architecture; no dead code; docs explain behavior.

---

## Suggested patch slices (zip-friendly)

- `phase0_guardrails`
- `phase1_spine_coordinator`
- `phase2_content_pipeline`
- `phase3_core_services`
- `phase4_config_ui`
- `phase5_integrations_api`
- `phase6_tests`
- `phase7_cleanup_docs`
