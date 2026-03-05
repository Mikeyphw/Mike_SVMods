# Repo-wide GMCM Standardization — IDE Agent Instruction File

## Mission

Standardize GMCM usage across `Mike_SVMods` **after RoosterControl ownership is already settled**.

Primary goals:

* make all mods use a consistent **NestCore-first GMCM architecture**
* reduce custom/manual GMCM plumbing
* improve use of the **NestCore DSL**
* keep every phase buildable
* create exactly one commit per phase

This slice covers:

* `NestCore`
* `LetMeHatch`
* `EggsInWild`
* repo docs for GMCM conventions

This slice does **not** reopen RoosterControl ownership decisions.

---

## Preconditions

Do not start this slice until all of these are already true:

* RoosterControl Phase 16 is complete
* RoosterControl Phase 17 is complete
* `RoosterControl` GMCM ownership is explicitly documented
* repo builds clean before this slice begins

If any precondition is false:

* stop and report
* do not improvise

---

## Non-negotiable execution rules

* Execute phases strictly in order:

  * `18A`
  * `18B`
  * `18C`
  * `18D`
  * `18E`
* Do not skip phases.
* Do not merge phases.
* Do not reorder steps.
* End every phase with a successful build.
* Create exactly one commit per successful phase.
* Do not create fixup commits.
* Do not amend previous commits.
* Prefer NestCore GMCM DSL over local/manual patterns.
* Do not add legacy shims.
* Do not duplicate functionality if NestCore already provides it.
* Do not rename existing GMCM page ids, option ids, field ids, or translation keys in this slice unless a phase explicitly says to.
* Do not change config schema in this slice.
* Do not change gameplay/runtime behavior.
* Do not touch patch packaging in this slice.
* If a step requires a design choice not explicitly described here, stop and report.

---

## Build gate for every phase

Run from repo root:

```bash
dotnet build NestCore/NestCore.csproj
dotnet build LetMeHatch/LetMeHatch.csproj
dotnet build EggsInWild/EggsInWild.csproj
dotnet build RoosterControl/RoosterControl.csproj
dotnet build Mike_SVMods.sln
```

If build fails:

* fix only errors introduced by the current phase,
* do not expand scope,
* if failure appears unrelated, stop and report the first 3 errors.

---

## Commit messages

* Phase 18A:

  * `phase-18a: add NestCore GMCM action option`
* Phase 18B:

  * `phase-18b: standardize LetMeHatch GMCM DSL usage`
* Phase 18C:

  * `phase-18c: complete EggsInWild GMCM NestCore migration`
* Phase 18D:

  * `phase-18d: align repo GMCM conventions and docs`
* Phase 18E:

  * `phase-18e: finalize repo-wide GMCM standardization`

---

## Scope

### In scope

* `NestCore/**`
* `LetMeHatch/**`
* `EggsInWild/**`
* related `docs/PLAN.md`
* related `docs/CHECKLIST.md`
* related `docs/TODO.md`

### Out of scope

* `RoosterControl/**` except build validation
* tests
* packaging
* config schema changes
* translation key renames
* unrelated refactors
* gameplay logic changes

---

## Standardization target architecture

By the end of this slice:

### All mods must follow this shape

* `Integrations/Gmcm/<Mod>GmcmMenu.cs`
* `Integrations/Gmcm/Pages/*`
* one composition/orchestrator class per mod
* no giant inline GMCM layout bodies in `ModEntry` or runtime coordinators
* NestCore owns plumbing
* mod code owns only mod-specific page logic

### DSL usage rules

* use bound expression options for real config members where possible
* use unbound/manual options only for:

  * dynamic dictionary values
  * derived values
  * true action/command controls
* use NestCore action DSL for one-shot commands
* use NestCore page batching helpers for repeated page registration/linking
* use `RebuildLayoutSaveAware(...)` for save/world-dependent menus
* use plain `RebuildLayout(...)` only for static menus

---

## Phase 18A — Add NestCore GMCM action option

### Objective

Add a first-class NestCore GMCM action option so mods stop using fake bool options as buttons.

### Allowed files

* `NestCore/Integrations/Gmcm/**`
* `NestCore/GMCM_BUILDER.md`
* `NestCore/docs/PLAN.md`
* `NestCore/docs/CHECKLIST.md`
* `NestCore/docs/TODO.md`

### Forbidden changes

* do not change existing page id/field id generation
* do not change existing option behavior
* do not modify mods in this phase
* do not add multiple action control variants in this phase

### Required design

Add exactly one new DSL primitive with this intent:

* page builder method name:

  * `Action`
* purpose:

  * show a clickable GMCM control for one-shot commands
* call shape:

  * label by option id
  * optional tooltip
  * enabled predicate optional
  * action callback required

### Required behavior

* no fake persisted value
* no checkbox semantics
* invokes callback when activated
* integrates with existing translation/id conventions
* does not require callers to simulate buttons with bool options

### Steps

#### Step 18A.1

Inspect existing NestCore GMCM builder abstractions and identify the narrowest place to add a one-shot option without changing unrelated option types.

#### Step 18A.2

Add one new NestCore GMCM DSL method:

* `Action(...)`

#### Step 18A.3

Implement the backing option registration using the existing GMCM integration surface with minimal new plumbing.

#### Step 18A.4

Do not add more than one new public DSL method in this phase.

#### Step 18A.5

Update `NestCore/GMCM_BUILDER.md`:

* explain `Action(...)`
* include one simple example
* include one example for reset/remove/apply style commands

#### Step 18A.6

Update `NestCore/docs/PLAN.md`, `CHECKLIST.md`, `TODO.md` to track this feature.

#### Step 18A.7

Run build gate.

#### Step 18A.8

Create commit:

```bash
git add NestCore
git commit -m "phase-18a: add NestCore GMCM action option"
```

### Stop conditions

Stop if:

* the chosen implementation would require breaking changes to existing DSL methods
* action support requires redesigning the entire GMCM abstraction layer
* more than one new public DSL primitive becomes necessary

---

## Phase 18B — Standardize LetMeHatch GMCM DSL usage

### Objective

Make `LetMeHatch` use the NestCore GMCM DSL more consistently, without changing ids, config schema, or menu behavior.

### Allowed files

* `LetMeHatch/Integrations/Gmcm/**`
* `LetMeHatch/docs/PLAN.md`
* `LetMeHatch/docs/CHECKLIST.md`
* `LetMeHatch/docs/TODO.md`

### Forbidden changes

* do not rename page ids
* do not rename option ids
* do not change config schema
* do not change menu behavior
* do not change runtime hatching/gameplay logic
* do not introduce a new architecture outside `Integrations/Gmcm/**`

### Required outcome

`LetMeHatch` must:

* keep `LmhGmcmMenu` as the orchestrator
* prefer bound options for direct config members
* keep manual/unbound options only where truly required
* use page batching helpers for repeated generated pages where applicable
* use action controls only if a current one-shot command exists and is currently simulated awkwardly

### Steps

#### Step 18B.1

Inspect `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`.

#### Step 18B.2

Classify every current GMCM option into one of:

* direct config member
* dynamic/dictionary/derived
* one-shot action

#### Step 18B.3

Convert direct config-member options to the most declarative bound DSL form available.

#### Step 18B.4

Keep manual/unbound options only when the option is:

* derived
* dictionary-backed
* not a direct config member

#### Step 18B.5

For repeated generated subpages, replace repeated manual page registration with NestCore batching helpers where safe.

#### Step 18B.6

Do not change existing GMCM ids, translations, or page grouping.

#### Step 18B.7

Update `LetMeHatch/docs/PLAN.md`, `CHECKLIST.md`, `TODO.md` with a new GMCM standardization phase entry.

#### Step 18B.8

Run build gate.

#### Step 18B.9

Create commit:

```bash
git add LetMeHatch
git commit -m "phase-18b: standardize LetMeHatch GMCM DSL usage"
```

### Stop conditions

Stop if:

* any conversion would require id/key renames
* any direct/manual distinction is ambiguous
* any extracted/generated page logic depends on runtime state not already handled safely

---

## Phase 18C — Complete EggsInWild GMCM NestCore migration

### Objective

Finish consolidating `EggsInWild` GMCM onto the NestCore-first menu architecture and remove pseudo-button bool patterns.

### Allowed files

* `EggsInWild/Infrastructure/Integrations/Gmcm/**`
* `EggsInWild/ModEntry.Gmcm.cs`
* `EggsInWild/ModEntry.Gmcm.WildAnimals.cs`
* `EggsInWild/docs/PLAN.md`
* `EggsInWild/docs/CHECKLIST.md`
* `EggsInWild/docs/TODO.md`

### Forbidden changes

* do not rename page ids
* do not rename option ids
* do not change translation keys
* do not change config schema
* do not change gameplay/runtime behavior
* do not touch unrelated `ModEntry` runtime logic outside GMCM extraction/migration

### Required outcome

By end of phase:

* `EiwGmcmMenu` is the only GMCM layout orchestrator
* page bodies live under `Infrastructure/Integrations/Gmcm/Pages/*`
* pseudo-button bool controls are replaced with NestCore `Action(...)`
* old inline/partial GMCM code is removed if no longer used
* save/world-dependent layout continues using the appropriate save-aware rebuild path

### Steps

#### Step 18C.1

Inspect:

* `EggsInWild/Infrastructure/Integrations/Gmcm/EiwGmcmMenu.cs`
* `EggsInWild/ModEntry.Gmcm.cs`
* `EggsInWild/ModEntry.Gmcm.WildAnimals.cs`

#### Step 18C.2

Identify all remaining GMCM layout/page logic that still lives in `ModEntry.Gmcm*`.

#### Step 18C.3

Move remaining GMCM page bodies into dedicated files under:

* `EggsInWild/Infrastructure/Integrations/Gmcm/Pages/*`

#### Step 18C.4

Keep `EiwGmcmMenu` as the sole layout orchestrator.

#### Step 18C.5

Replace every pseudo-button or fake-bool one-shot command with NestCore `Action(...)`.

#### Step 18C.6

Do not convert true persisted boolean options into actions.

#### Step 18C.7

Preserve existing page ids, option ids, and translation keys.

#### Step 18C.8

Remove dead GMCM code from `ModEntry.Gmcm*` only after references are eliminated.

#### Step 18C.9

Keep the current save-aware rebuild behavior if the menu depends on loaded save/world data.

#### Step 18C.10

Update `EggsInWild/docs/PLAN.md`, `CHECKLIST.md`, `TODO.md` with the completed GMCM migration notes.

#### Step 18C.11

Run build gate.

#### Step 18C.12

Create commit:

```bash
git add EggsInWild
git commit -m "phase-18c: complete EggsInWild GMCM NestCore migration"
```

### Stop conditions

Stop if:

* any page body still requires runtime orchestration that cannot safely move out of `ModEntry`
* any pseudo-button is actually a persisted bool setting
* moving page logic would require config/id/schema changes

---

## Phase 18D — Align repo GMCM conventions and docs

### Objective

Document one repo-wide GMCM convention and align all affected docs to it.

### Allowed files

* `NestCore/GMCM_BUILDER.md`
* `NestCore/docs/**`
* `LetMeHatch/docs/**`
* `EggsInWild/docs/**`

### Forbidden changes

* no production code changes unless required to fix doc/example compile issues
* no id renames
* no config/schema changes

### Required convention text

Document these rules clearly:

1. NestCore is the required GMCM plumbing layer.
2. Mods own only mod-specific pages/logic.
3. Menu orchestration belongs in one `<Mod>GmcmMenu` class.
4. Page bodies belong in `Integrations/Gmcm/Pages/*`.
5. Use bound options for direct config members.
6. Use unbound/manual options only for dynamic/derived values.
7. Use `Action(...)` for one-shot commands.
8. Use batching helpers for repeated page links/page registration.
9. Use save-aware rebuild for menus that depend on save/world data.
10. Do not leave giant inline GMCM layout bodies in `ModEntry`/coordinators.

### Steps

#### Step 18D.1

Update `NestCore/GMCM_BUILDER.md` with a “Repo convention” section containing the 10 rules above.

#### Step 18D.2

Update `NestCore/docs/PLAN.md`, `CHECKLIST.md`, `TODO.md` to record the GMCM convention as established.

#### Step 18D.3

Update `LetMeHatch/docs/PLAN.md`, `CHECKLIST.md`, `TODO.md` to mark the mod aligned to repo GMCM convention.

#### Step 18D.4

Update `EggsInWild/docs/PLAN.md`, `CHECKLIST.md`, `TODO.md` to mark the mod aligned to repo GMCM convention.

#### Step 18D.5

Run build gate.

#### Step 18D.6

Create commit:

```bash
git add NestCore/GMCM_BUILDER.md NestCore/docs LetMeHatch/docs EggsInWild/docs
git commit -m "phase-18d: align repo GMCM conventions and docs"
```

---

## Phase 18E — Finalize repo-wide GMCM standardization

### Objective

Perform the final validation pass and leave the repo in a consistent GMCM state.

### Allowed files

* `NestCore/**`
* `LetMeHatch/**`
* `EggsInWild/**`

### Forbidden changes

* no new features
* no renames
* no schema changes
* no opportunistic cleanup outside GMCM standardization

### Required validation checklist

Confirm all of the following:

* `RoosterControl` remains settled and untouched
* `LetMeHatch` uses declarative/bound options wherever appropriate
* `EggsInWild` no longer uses fake-bool action patterns
* `EggsInWild` GMCM layout is no longer split across legacy `ModEntry.Gmcm*` bodies except for any explicitly documented retained runtime-coupled wrapper
* `NestCore` documents and provides:

  * batch page-link helper
  * batch page registration helper
  * action control
* all affected docs reflect the final architecture

### Steps

#### Step 18E.1

Do a final grep/review for pseudo-button patterns in GMCM code, especially fake bools used only to trigger commands.

#### Step 18E.2

Do a final grep/review for large inline GMCM layout bodies still living in:

* `ModEntry`
* runtime coordinator files

#### Step 18E.3

If any remaining cases exist:

* remove them only if clearly in scope and build-safe
* otherwise document them as intentionally retained with rationale
* do not improvise a broad refactor

#### Step 18E.4

Update final docs notes in the affected `docs/PLAN.md`, `CHECKLIST.md`, and `TODO.md`.

#### Step 18E.5

Run build gate.

#### Step 18E.6

Create commit:

```bash
git add NestCore LetMeHatch EggsInWild
git commit -m "phase-18e: finalize repo-wide GMCM standardization"
```

---

## Hard stop conditions

Stop immediately and report if any of these happen:

* any phase requires renaming existing GMCM ids or translation keys
* any phase requires config schema changes
* any moved page body requires runtime/world/orchestration dependencies not explicitly allowed
* NestCore action support cannot be added without breaking the existing DSL surface
* a build failure appears unrelated to the current phase
* a mod depends on a local GMCM pattern that conflicts with NestCore and cannot be migrated without behavioral change

---

## Final expected end state

### NestCore

* provides batch page-link helper
* provides batch page registration helper
* provides first-class action control
* documents repo GMCM conventions

### LetMeHatch

* uses NestCore DSL more declaratively
* keeps existing ids/behavior
* reduces unnecessary manual/unbound options
* uses batching helpers where appropriate

### EggsInWild

* fully consolidated under `EiwGmcmMenu + Pages/*`
* replaces fake-bool action patterns with `Action(...)`
* keeps save-aware rebuild behavior
* removes dead legacy GMCM partial code when safe

### Repo-wide

* one clear GMCM convention
* NestCore owns plumbing
* mods own only page logic
* no giant inline GMCM layout bodies left unless explicitly documented and justified

