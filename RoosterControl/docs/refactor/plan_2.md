# RoosterControl GMCM Ownership Decision Slice — IDE Agent Instruction File

## Mission

Resolve the deferred ownership decision for these two GMCM pages in `RoosterControl`:

* `BuildCompatPage(...)`
* `BuildFemalesPage(...)`

Goal:

* decide **deterministically** whether each page should be:

  * **extracted** into `Integrations/Gmcm/Pages/*`, or
  * **intentionally kept coordinator-owned**

Default policy:

* **KEEP coordinator-owned unless the page is proven extraction-safe by the exact criteria below.**

This slice is intentionally conservative.

---

## Non-negotiable execution rules

* Execute phases strictly in order:

  * `17A`
  * `17B` if eligible
  * `17C` if eligible
  * `17D`
* Do not skip required phases.
* Do not merge phases.
* Do not rename:

  * GMCM page ids
  * GMCM option ids
  * translation keys
  * config properties
* Do not change config schema.
* Do not change runtime behavior.
* Do not touch:

  * `EggsInWild/**`
  * `LetMeHatch/**`
  * `NestCore/**` in this slice
* Do not introduce a new NestCore DSL/widget in this slice.
* Do not change save-aware rebuild timing in this slice.
* End every phase with a successful build.
* Create exactly one commit per successful phase.
* If anything requires judgment not explicitly covered here, stop and report.

---

## Build gate for every phase

Run from repo root:

```bash
dotnet build RoosterControl/RoosterControl.csproj
dotnet build Mike_SVMods.sln
```

If build fails:

* fix only errors introduced by the current phase,
* do not expand scope,
* if the failure appears unrelated, stop and report the first 3 errors.

---

## Commit messages

* Phase 17A:

  * `phase-17a: classify RoosterControl GMCM page ownership`
* Phase 17B:

  * `phase-17b: extract RoosterControl females GMCM page`
* Phase 17C:

  * `phase-17c: extract RoosterControl compat GMCM page`
* Phase 17D:

  * `phase-17d: document RoosterControl GMCM ownership decisions`

If both pages remain coordinator-owned, Phase 17D is still required.

---

## Scope

### In scope

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
* `RoosterControl/Application/RoosterCoordinator.cs`
* `RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs`
* `RoosterControl/Integrations/Gmcm/Pages/RoosterCompatPage.cs` if created
* `RoosterControl/Integrations/Gmcm/Pages/RoosterFemalesPage.cs` if created
* `RoosterControl/docs/PLAN.md`
* `RoosterControl/docs/CHECKLIST.md`
* `RoosterControl/docs/TODO.md`

### Out of scope

* all other mods
* NestCore changes
* tests
* packaging
* config schema changes
* translation key cleanup
* runtime refactors unrelated to GMCM ownership

---

## Ownership decision policy

### Extraction-safe criteria

A page may be extracted **only if all of the following are true**:

1. Its GMCM page body depends only on:

   * `owner.Config`
   * `owner.I18n`
   * static constants/enums
   * pure helper methods that can be moved with the page
   * simple deterministic formatting logic

2. It does **not** require direct access to:

   * `Helper`
   * `Monitor`
   * mod registry / API lookup state
   * event state
   * scheduled rebuild state
   * save lifecycle state
   * asset pipeline state
   * world objects / `Game1` / locations / animals / player state
   * runtime caches that are not simple config-backed data
   * mutation outside the config object and local GMCM builder flow

3. It does **not** require callbacks that depend on coordinator-only orchestration.

4. It can be expressed as:

   * a static page builder class under `Integrations/Gmcm/Pages/*`, plus
   * at most one narrow owner accessor already present or explicitly allowed below.

### Keep-owned criteria

A page must remain coordinator-owned if **any** of these are true:

* it reads runtime/world state,
* it depends on compat discovery/runtime APIs,
* it needs coordinator-only scheduling/rebuild/save flow,
* it uses coordinator fields other than `Config` and `I18n`,
* extracting it would require adding multiple new owner accessors,
* extracting it would blur GMCM layout ownership with live gameplay/runtime coordination.

### Allowed new owner access

In this slice, extraction may add **at most one** new narrow accessor **per extracted page**, and only if it is read-only and unavoidable.

Preferred outcome:

* **no new accessors**
* second-best:

  * one read-only accessor
* if more than one is needed:

  * **do not extract**
  * keep coordinator-owned

---

## Expected default outcome

Unless the criteria above prove otherwise:

* `BuildCompatPage(...)` should remain coordinator-owned
* `BuildFemalesPage(...)` should remain coordinator-owned

This is the default because these page types commonly drift into runtime-coupled behavior.

---

## Phase 17A — Classify page ownership

### Objective

Classify `compat` and `females` as either extraction-safe or intentionally coordinator-owned, using only the criteria above.

### Allowed files

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
* `RoosterControl/docs/PLAN.md`
* `RoosterControl/docs/CHECKLIST.md`
* `RoosterControl/docs/TODO.md`

### Forbidden changes

* no production code changes beyond comments/doc notes in this phase
* do not create page classes in this phase
* do not move methods in this phase
* do not modify runtime logic

### Steps

#### Step 17A.1

Inspect these methods in `RoosterCoordinator.Gmcm.cs`:

* `BuildCompatPage(...)`
* `BuildFemalesPage(...)`

Also inspect every helper method they call directly or indirectly.

#### Step 17A.2

For each page, classify every dependency into one of:

* config-only
* translation-only
* pure local helper
* runtime-coupled
* coordinator-orchestration-coupled

#### Step 17A.3

Apply this decision rule separately to each page:

* If all dependencies are only:

  * config-only
  * translation-only
  * pure local helper
    then mark page:
  * `EXTRACT`

* Otherwise mark page:

  * `KEEP`

#### Step 17A.4

Update `RoosterControl/docs/PLAN.md` with a new section:

## Phase 17 — GMCM page ownership decision

Goal: determine whether `compat` and `females` should be extracted into `Integrations/Gmcm/Pages/*` or intentionally remain coordinator-owned.

Decision rule:

* Extract only if page logic is config/i18n/pure-helper only.
* Keep coordinator-owned if page logic depends on runtime state, compat discovery, orchestration, rebuild flow, or multiple coordinator internals.

Expected default:

* Keep coordinator-owned unless extraction is clearly safe.

#### Step 17A.5

Update `RoosterControl/docs/CHECKLIST.md` with:

## Phase 17 — GMCM page ownership decision

* [ ] Classify `compat` page ownership
* [ ] Classify `females` page ownership
* [ ] Extract only pages that are config/i18n/pure-helper only
* [ ] Keep runtime-coupled pages coordinator-owned
* [ ] Update docs with final ownership decisions
* [ ] Verify build

#### Step 17A.6

Update `RoosterControl/docs/TODO.md` so the Phase 17 block includes:

* [ ] Classify `compat` as extractable or coordinator-owned
* [ ] Classify `females` as extractable or coordinator-owned
* [ ] Extract only pages that meet the extraction-safe criteria
* [ ] Document retained coordinator ownership for pages that fail the criteria

#### Step 17A.7

Run build gate.

#### Step 17A.8

Create commit:

```bash
git add RoosterControl/docs/PLAN.md RoosterControl/docs/CHECKLIST.md RoosterControl/docs/TODO.md
git commit -m "phase-17a: classify RoosterControl GMCM page ownership"
```

---

## Phase 17B — Extract `females` only if eligible

### Execute this phase only if

`BuildFemalesPage(...)` was classified as `EXTRACT` in Phase 17A.

If `BuildFemalesPage(...)` was classified as `KEEP`, skip Phase 17B entirely.

### Objective

Extract the females page into a dedicated page class without changing behavior.

### Allowed files

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
* `RoosterControl/Application/RoosterCoordinator.cs` only if one narrow read-only accessor is unavoidable
* `RoosterControl/Integrations/Gmcm/Pages/RoosterFemalesPage.cs`
* `RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs`
* `RoosterControl/docs/PLAN.md`
* `RoosterControl/docs/CHECKLIST.md`
* `RoosterControl/docs/TODO.md`

### Forbidden changes

* do not change page id `females`
* do not change option ids
* do not change behavior
* do not add more than one new owner accessor
* do not extract compat in this phase

### Steps

#### Step 17B.1

Create file:

* `RoosterControl/Integrations/Gmcm/Pages/RoosterFemalesPage.cs`

Structure:

* namespace: `RoosterControl.Integrations.Gmcm.Pages`
* class: `internal static class RoosterFemalesPage`
* method:

  * `public static void Build(GmcmPageBuilder<RoosterControlConfig> b, RoosterCoordinator owner)`

#### Step 17B.2

Move `BuildFemalesPage(...)` body and only the helper methods it needs into `RoosterFemalesPage.cs`.

#### Step 17B.3

Adapt moved code to use:

* `owner.Config`
* `owner.I18n`
* at most one new read-only accessor if unavoidable

If more access is required, abort extraction and stop. Do not improvise.

#### Step 17B.4

In `RoosterControlGmcmMenu.cs`, replace the `females` delegate registration with:

* `layout.Page("females", b => RoosterFemalesPage.Build(b, _owner));`

#### Step 17B.5

In `RoosterCoordinator.Gmcm.cs`, remove the extracted `BuildFemalesPage(...)` and any moved helpers.

#### Step 17B.6

Update docs to mark:

* `females` = extracted
* rationale = config/i18n/pure-helper only

#### Step 17B.7

Run build gate.

#### Step 17B.8

Create commit:

```bash
git add RoosterControl/Application/RoosterCoordinator.Gmcm.cs RoosterControl/Application/RoosterCoordinator.cs RoosterControl/Integrations/Gmcm/Pages/RoosterFemalesPage.cs RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs RoosterControl/docs/PLAN.md RoosterControl/docs/CHECKLIST.md RoosterControl/docs/TODO.md
git commit -m "phase-17b: extract RoosterControl females GMCM page"
```

---

## Phase 17C — Extract `compat` only if eligible

### Execute this phase only if

`BuildCompatPage(...)` was classified as `EXTRACT` in Phase 17A.

If `BuildCompatPage(...)` was classified as `KEEP`, skip Phase 17C entirely.

### Objective

Extract the compat page into a dedicated page class without changing behavior.

### Allowed files

* `RoosterControl/Application/RoosterCoordinator.Gmcm.cs`
* `RoosterControl/Application/RoosterCoordinator.cs` only if one narrow read-only accessor is unavoidable
* `RoosterControl/Integrations/Gmcm/Pages/RoosterCompatPage.cs`
* `RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs`
* `RoosterControl/docs/PLAN.md`
* `RoosterControl/docs/CHECKLIST.md`
* `RoosterControl/docs/TODO.md`

### Forbidden changes

* do not change page id `compat`
* do not change option ids
* do not change compat behavior
* do not add more than one new owner accessor
* do not extract females in this phase

### Steps

#### Step 17C.1

Create file:

* `RoosterControl/Integrations/Gmcm/Pages/RoosterCompatPage.cs`

Structure:

* namespace: `RoosterControl.Integrations.Gmcm.Pages`
* class: `internal static class RoosterCompatPage`
* method:

  * `public static void Build(GmcmPageBuilder<RoosterControlConfig> b, RoosterCoordinator owner)`

#### Step 17C.2

Move `BuildCompatPage(...)` body and only the helper methods it needs into `RoosterCompatPage.cs`.

#### Step 17C.3

Adapt moved code to use:

* `owner.Config`
* `owner.I18n`
* at most one new read-only accessor if unavoidable

If more access is required, abort extraction and stop. Do not improvise.

#### Step 17C.4

In `RoosterControlGmcmMenu.cs`, replace the `compat` delegate registration with:

* `layout.Page("compat", b => RoosterCompatPage.Build(b, _owner));`

#### Step 17C.5

In `RoosterCoordinator.Gmcm.cs`, remove the extracted `BuildCompatPage(...)` and any moved helpers.

#### Step 17C.6

Update docs to mark:

* `compat` = extracted
* rationale = config/i18n/pure-helper only

#### Step 17C.7

Run build gate.

#### Step 17C.8

Create commit:

```bash
git add RoosterControl/Application/RoosterCoordinator.Gmcm.cs RoosterControl/Application/RoosterCoordinator.cs RoosterControl/Integrations/Gmcm/Pages/RoosterCompatPage.cs RoosterControl/Integrations/Gmcm/RoosterControlGmcmMenu.cs RoosterControl/docs/PLAN.md RoosterControl/docs/CHECKLIST.md RoosterControl/docs/TODO.md
git commit -m "phase-17c: extract RoosterControl compat GMCM page"
```

---

## Phase 17D — Finalize and document ownership decisions

### Objective

Make the ownership result explicit in docs and leave the code in the correct final state.

### Allowed files

* `RoosterControl/docs/PLAN.md`
* `RoosterControl/docs/CHECKLIST.md`
* `RoosterControl/docs/TODO.md`

### Required outcome

For each page, docs must explicitly say one of:

* `compat: extracted`

* `compat: intentionally coordinator-owned`

* `females: extracted`

* `females: intentionally coordinator-owned`

### Required rationale format

Use one of these exact rationale patterns:

* `Extracted because the page depends only on config, i18n, and pure helper logic.`
* `Kept coordinator-owned because the page depends on runtime/compat/orchestration state beyond config+i18n.`

### Steps

#### Step 17D.1

Update `RoosterControl/docs/PLAN.md` final Phase 17 result section with explicit final ownership for both pages.

#### Step 17D.2

Update `RoosterControl/docs/CHECKLIST.md`:

* mark completed items
* leave future work unchecked if not done

#### Step 17D.3

Update `RoosterControl/docs/TODO.md`:

* remove resolved decision ambiguity
* if either page remains owned, add:

  * `No further extraction planned unless runtime coupling is removed in a future refactor.`

#### Step 17D.4

Run build gate.

#### Step 17D.5

Create commit:

```bash
git add RoosterControl/docs/PLAN.md RoosterControl/docs/CHECKLIST.md RoosterControl/docs/TODO.md
git commit -m "phase-17d: document RoosterControl GMCM ownership decisions"
```

---

## Hard stop conditions

Stop immediately and report if any of these happen:

* extracting `compat` or `females` requires access beyond:

  * `owner.Config`
  * `owner.I18n`
  * at most one narrow read-only accessor
* extracting either page requires runtime/world access
* extracting either page requires event/scheduling/rebuild internals
* extracting either page requires multiple new owner accessors
* extraction would force id/key renames
* build failure appears unrelated to the current phase

---

## Final decision rules summary

### `compat`

* If it touches compat discovery/runtime state:

  * **KEEP**
* Only extract if it is fully config/i18n/pure-helper driven.

### `females`

* If it touches live runtime plan/state/orchestration:

  * **KEEP**
* Only extract if it is fully config/i18n/pure-helper driven.

### Tie-break rule

If uncertain:

* **KEEP coordinator-owned**

---

## Expected likely outcome

Most likely final result for this slice:

* `compat`: intentionally coordinator-owned
* `females`: intentionally coordinator-owned

Only override that if the code clearly proves extraction-safe under the exact criteria above.
