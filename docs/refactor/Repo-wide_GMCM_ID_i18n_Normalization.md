# Repo-wide GMCM ID / i18n Normalization — IDE Agent Instruction File

## Mission

Normalize GMCM-facing ids and GMCM-facing i18n keys across the repo **after the architecture standardization already present in this snapshot**.

This slice is primarily:

* **LetMeHatch**: real rename/normalization work
* **RoosterControl**: safe cleanup of legacy GMCM i18n aliases already unused by code
* **EggsInWild**: verification + docs only unless the exact checks below find a small safe cleanup
* **NestCore**: docs/convention updates only

This slice is **naming normalization only**.
It is **not** an architecture refactor.

---

## Preconditions

Do not start unless all of these are already true in the working tree:

* `NestCore` already has:

  * `GmcmPageBuilder.Links(...)`
  * `GmcmMenuLayout.Pages(...)`
  * `GmcmPageBuilder.Action(...)`
* `RoosterControl` already has:

  * `RoosterControlGmcmMenu`
  * extracted `Pages/*`
  * Phase 17 docs stating `compat` and `females` are intentionally coordinator-owned
* `EggsInWild` already has:

  * `EiwGmcmMenu`
  * page bodies under `Infrastructure/Integrations/Gmcm/Pages/*`
  * no fake-bool action pattern
* repo builds clean before Phase R1 starts

If any precondition is false:

* stop
* report the missing precondition
* do not improvise

---

## Non-negotiable execution rules

* Execute phases strictly in order:

  * `R1A`
  * `R1B`
  * `R1C`
  * `R1D`
  * `R1E`
* Do not skip phases.
* Do not merge phases.
* Do not reorder steps.
* End every phase with a successful build.
* Create exactly one commit per successful phase.
* Do not create fixup commits.
* Do not amend previous commits.
* Do not change config schema.
* Do not change gameplay/runtime behavior.
* Do not change GMCM layout structure except where a page id rename explicitly requires link/registration updates.
* Do not add new GMCM features in this slice.
* Do not change `RoosterControl` ownership decisions in this slice.
* Do not touch packaging in this slice.
* If any step requires a design choice not explicitly described here, stop and report.

---

## Build gate for every phase

Run from repo root:

```bash
dotnet build NestCore/NestCore.csproj
dotnet build RoosterControl/RoosterControl.csproj
dotnet build LetMeHatch/LetMeHatch.csproj
dotnet build EggsInWild/EggsInWild.csproj
dotnet build Mike_SVMods.sln
```

If build fails:

* fix only errors introduced by the current phase,
* do not expand scope,
* if failure appears unrelated, stop and report the first 3 errors.

---

## Commit messages

* Phase R1A:

  * `phase-R1a: define repo GMCM id and i18n normalization rules`
* Phase R1B:

  * `phase-R1b: normalize LetMeHatch GMCM ids and i18n keys`
* Phase R1C:

  * `phase-R1c: prune RoosterControl legacy GMCM i18n aliases`
* Phase R1D:

  * `phase-R1d: verify EggsInWild GMCM ids already normalized`
* Phase R1E:

  * `phase-R1e: finalize repo GMCM id and i18n normalization`

---

## Canonical naming rules for this slice

Apply these rules exactly.

### Page ids

* Use **dotted lowerCamel** for nested page ids.
* Top-level root pages may remain single-segment lowerCamel if already canonical:

  * `edits`
  * `cloning`
  * `colors`
  * `breeding`
  * `advanced`
  * `debug`
  * `compat`
  * `probabilities`
  * `females`
  * etc.

Examples:

* `edits.eggList`
* `colors.eggPalette`
* `advanced.itemMappings.egg`
* `breeding.pack.<slug>`
* `breeding.female.<packSlug>.<femaleSlug>`

### Option ids

* Use:

  * `<scope>.<lowerCamelName>`
* Scope must reflect the page/domain, not the C# property casing.

Examples:

* `general.editEggListByName`
* `cloning.cloneVanillaEggs`
* `colors.dynamicCloneTintMode`
* `breeding.dailyLuckBonus`
* `debug.verboseLogging`

### Section ids

* Use:

  * `general`
  * or dotted lowerCamel if nested
* Examples:

  * `general`
  * `breeding.loadedSystems`
  * `breeding.normalHatching`
  * `breeding.fertilizedHatching`
  * `breeding.males`

### i18n keys

Use these exact patterns:

* page:

  * `config.page.<pageId>.name`
  * `config.page.<pageId>.description`
* option:

  * `config.<optionId>.name`
  * `config.<optionId>.description`
* section:

  * `config.section.<sectionId>.text`
  * `config.section.<sectionId>.description`

### Manual GMCM text keys

If a GMCM-facing label is not a direct page/option/section id, scope it under the closest page/domain.

Examples:

* `config.breeding.applyDailyBoost.name`
* `config.breeding.normalEggChance.description`

### Out of scope for this slice

Do **not** rename:

* dynamic `elementId` strings unless a step below explicitly says to
* `GmcmDynamicIds.ElementId(...)` formats
* non-GMCM gameplay/config keys
* data file keys unrelated to GMCM

---

## Phase R1A — Define repo normalization rules in docs

### Objective

Record the repo-wide GMCM naming convention in docs before changing code/i18n.

### Allowed files

* `NestCore/GMCM_BUILDER.md`
* `docs/PLAN.md`
* `docs/CHECKLIST.md`
* `docs/TODO.md`

### Forbidden changes

* no NestCore production code changes
* no mod code changes
* no i18n JSON changes in this phase

### Steps

#### Step R1A.1

Update `NestCore/GMCM_BUILDER.md` with a new section:

* title:

  * `GMCM id and i18n normalization`
* include these exact rules:

  1. Nested page ids use dotted lowerCamel.
  2. Direct config option ids use dotted lowerCamel scoped by page/domain.
  3. Section ids use `config.section.<id>.text`.
  4. Manual GMCM labels should use the closest page/domain namespace.
  5. Avoid PascalCase GMCM ids and legacy mixed page-id naming.
  6. Do not keep duplicate legacy i18n aliases once code is migrated.

#### Step R1A.2

Update `docs/PLAN.md` with a new Phase R1 entry describing this normalization slice.

#### Step R1A.3

Update `docs/CHECKLIST.md` with:

* define naming rules
* normalize LetMeHatch
* prune RoosterControl legacy aliases
* verify EggsInWild already normalized
* finalize docs + validation

#### Step R1A.4

Update `docs/TODO.md` to record Phase R1 as the current GMCM naming follow-up after architecture standardization.

#### Step R1A.5

Run build gate.

#### Step R1A.6

Create commit:

```bash
git add NestCore/GMCM_BUILDER.md docs/PLAN.md docs/CHECKLIST.md docs/TODO.md
git commit -m "phase-R1a: define repo GMCM id and i18n normalization rules"
```

---

## Phase R1B — Normalize LetMeHatch GMCM ids and i18n keys

### Objective

Normalize the remaining mixed GMCM ids in `LetMeHatch` to the canonical dotted lowerCamel convention and update both i18n files to match.

### Allowed files

* `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
* `LetMeHatch/i18n/default.json`
* `LetMeHatch/i18n/pt-br.json`
* `docs/PLAN.md`
* `docs/CHECKLIST.md`
* `docs/TODO.md`

### Forbidden changes

* do not change config schema
* do not change GMCM behavior
* do not change runtime breeding/cloning logic
* do not change generated `elementId` values except where explicitly stated below
* do not touch other `LetMeHatch` files in this phase

### Exact page-id rename map

Apply these exact page-id renames in `LmhGmcmMenu.cs` and both i18n files:

* `editsEggList` → `edits.eggList`

* `editsBirthText` → `edits.birthText`

* `editsCloneEggs` → `edits.cloneEggs`

* `eggPalette` → `colors.eggPalette`

* `eggColorOverrides` → `colors.eggColorOverrides`

* `breedingPacks` → `breeding.packs`

* `itemMappings` → `advanced.itemMappings`

* `itemMappingsEgg` → `advanced.itemMappings.egg`

* `itemMappingsMilk` → `advanced.itemMappings.milk`

* `itemMappingsMisc` → `advanced.itemMappings.misc`

### Exact generated page-id rules

Change these helper methods in `LmhGmcmMenu.cs` exactly:

* `BuildPackPageId(string contentPackId)`

  * from:

    * `breeding_pack_<slug>`
  * to:

    * `breeding.pack.<slug>`

* `BuildFemalePageId(string contentPackId, string femaleKey)`

  * from:

    * `breeding_female_<packSlug>_<femaleSlug>`
  * to:

    * `breeding.female.<packSlug>.<femaleSlug>`

Do not change the `Slug(...)` algorithm in this phase.

### Exact option-id rename map

Change these exact option ids in `LmhGmcmMenu.cs` and the corresponding i18n keys in both JSON files:

#### Root / general

* `EditEggListByName` → `general.editEggListByName`
* `EditBirthText` → `general.editBirthText`
* `AlternateBirthText` → `general.alternateBirthText`

#### Cloning

* `CloneVanillaEggs` → `cloning.cloneVanillaEggs`
* `CloneMachineRules` → `cloning.cloneMachineRules`
* `UseDynamicCloneTextures` → `cloning.useDynamicCloneTextures`
* `DinoEggsAsArtifact` → `cloning.dinoEggAsArtifact`
* `OnlyFertilizedEggs` → `cloning.onlyFertilizedEggs`
* `ColorMilk` → `cloning.colorClonedMilk`
* `ExclusiveRabbitWool` → `cloning.exclusiveRabbitWool`

#### Colors

* `ChangeEggColors` → `colors.changeEggColors`
* `DynamicCloneTintMode` → `colors.dynamicCloneTintMode`
* `LinkAnimalColorToEggs` → `colors.linkAnimalColorToEgg`

#### Breeding

* `BreedingSystemEnabled` → `breeding.systemEnabled`
* `DeterministicBreedingRng` → `breeding.deterministicRng`
* `DailyLuckBonus` → `breeding.dailyLuckBonus`

#### Advanced

* `StabilizeFarmAnimalsAssetOrder` → `advanced.stabilizeFarmAnimalsAssetOrder`

#### Debug

* `ShowEdits` → `debug.showEdits`
* `ShowAnimalKeys` → `debug.showAnimalKeys`
* `VerboseLogging` → `debug.verboseLogging`
* `TestingLogging` → `debug.testingLogging`

### Exact manual GMCM text-key rename map

Rename these translation keys in both i18n files and update `GetString(...)` references in `LmhGmcmMenu.cs`:

* `config.ApplyDailyBoost.*` → `config.breeding.applyDailyBoost.*`
* `config.NormalEggChance.*` → `config.breeding.normalEggChance.*`
* `config.LargeEggChance.*` → `config.breeding.largeEggChance.*`
* `config.FertilizedEggChance.*` → `config.breeding.fertilizedEggChance.*`
* `config.EnableMale.*` → `config.breeding.enableMale.*`
* `config.AddAsAlternatePurchase.*` → `config.breeding.addAsAlternatePurchase.*`
* `config.MaleChance.*` → `config.breeding.maleChance.*`
* `config.BoostByMale.*` → `config.breeding.boostByMale.*`

### Exact section-id rename map

Update section ids / section translation keys:

* `General` → `general`
* `LoadedBreedingSystems` → `breeding.loadedSystems`

And rename these section translation keys in both i18n files + update code references:

* `config.section.Females.*` → `config.section.breeding.females.*`
* `config.section.NormalHatching.*` → `config.section.breeding.normalHatching.*`
* `config.section.FertilizedHatching.*` → `config.section.breeding.fertilizedHatching.*`
* `config.section.Males.*` → `config.section.breeding.males.*`

### Exact page-translation-key rename map

Rename these page keys in both i18n files:

* `config.page.editsEggList.*` → `config.page.edits.eggList.*`

* `config.page.editsBirthText.*` → `config.page.edits.birthText.*`

* `config.page.editsCloneEggs.*` → `config.page.edits.cloneEggs.*`

* `config.page.eggPalette.*` → `config.page.colors.eggPalette.*`

* `config.page.eggColorOverrides.*` → `config.page.colors.eggColorOverrides.*`

* `config.page.itemMappings.*` → `config.page.advanced.itemMappings.*`

* `config.page.itemMappingsEgg.*` → `config.page.advanced.itemMappings.egg.*`

* `config.page.itemMappingsMilk.*` → `config.page.advanced.itemMappings.milk.*`

* `config.page.itemMappingsMisc.*` → `config.page.advanced.itemMappings.misc.*`

### Exact legacy page-key removals

After all code/i18n renames above are complete, remove these unused legacy page keys from both `LetMeHatch/i18n/default.json` and `LetMeHatch/i18n/pt-br.json`:

* `config.page.BirthText.name`
* `config.page.CloneEggs.name`
* `config.page.EggColors.name`
* `config.page.EggList.name`
* `config.page.EggsEnabled.name`
* `config.page.ManageChosenColors.name`
* `config.page.eggRelations.name`

Do not remove any other keys in this step.

### Steps

#### Step R1B.1

Edit `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs` and apply the exact page-id, option-id, generated page-id, section-id, and `GetString(...)` rename maps above.

#### Step R1B.2

Edit `LetMeHatch/i18n/default.json`:

* rename keys exactly per the maps above
* preserve the existing values
* remove the exact listed legacy page keys

#### Step R1B.3

Edit `LetMeHatch/i18n/pt-br.json`:

* rename keys exactly per the maps above
* preserve the translated values
* remove the exact listed legacy page keys

#### Step R1B.4

Do not leave alias/duplicate old keys behind after the rename.

#### Step R1B.5

Update `docs/PLAN.md`, `CHECKLIST.md`, and `TODO.md` with a new Phase R1 entry that explicitly says:

* GMCM ids/i18n normalized to repo convention
* build gate required after rename
* no config/schema changes

#### Step R1B.6

Run build gate.

#### Step R1B.7

Create commit:

```bash
git add LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs LetMeHatch/i18n/default.json LetMeHatch/i18n/pt-br.json docs/PLAN.md docs/CHECKLIST.md docs/TODO.md
git commit -m "phase-R1b: normalize LetMeHatch GMCM ids and i18n keys"
```

### Hard stop conditions for R1B

Stop immediately if:

* any rename would require config schema changes
* any referenced translation key is shared with non-GMCM runtime logic and a safe scoped replacement is not explicit
* any additional ids outside the exact maps above also need renaming to keep the mod compiling
* any page id rename breaks a path not listed above

---

## Phase R1C — Prune RoosterControl legacy GMCM i18n aliases

### Objective

Keep RoosterControl code ids unchanged and remove legacy GMCM alias keys that are already unused by the current canonical code.

### Allowed files

* `RoosterControl/i18n/default.json`
* `RoosterControl/i18n/pt-br.json`
* `docs/PLAN.md`
* `docs/CHECKLIST.md`
* `docs/TODO.md`

### Forbidden changes

* do not change any `RoosterControl` C# files
* do not change page ids
* do not change option ids
* do not change Phase 17 ownership decisions
* do not rename canonical dotted keys

### Exact verification rule before deletion

Before deleting anything, run a repo grep limited to `RoosterControl/**/*.cs` for these literal prefixes:

* `config.Bool.`
* `config.Number.`
* `config.Text.`
* `config.Page.`
* `config.Section.`

If **any** C# source under `RoosterControl/` still contains one of those prefixes:

* stop
* report the matching file(s)
* do not delete any alias keys

### Exact deletion rule

If the grep above returns zero matches, delete from both `RoosterControl/i18n/default.json` and `RoosterControl/i18n/pt-br.json` **all keys** that start with any of:

* `config.Bool.`
* `config.Number.`
* `config.Text.`
* `config.Page.`
* `config.Section.`

Do not delete:

* `config.page.*`
* `config.section.*`
* `config.text.*`
* `config.value.*`
* `config.paragraph.*`
* any canonical dotted-lowerCamel key

### Steps

#### Step R1C.1

Run the exact verification grep above.

#### Step R1C.2

If zero matches, remove the exact legacy-key prefix groups above from both RoosterControl i18n files.

#### Step R1C.3

Do not change any canonical dotted GMCM keys.

#### Step R1C.4

Update `docs/PLAN.md`, `CHECKLIST.md`, and `TODO.md` to record:

* code ids were already normalized in current snapshot
* Phase R1C only pruned unused legacy GMCM translation aliases

#### Step R1C.5

Run build gate.

#### Step R1C.6

Create commit:

```bash
git add RoosterControl/i18n/default.json RoosterControl/i18n/pt-br.json docs/PLAN.md docs/CHECKLIST.md docs/TODO.md
git commit -m "phase-R1c: prune RoosterControl legacy GMCM i18n aliases"
```

### Hard stop conditions for R1C

Stop immediately if:

* any old-prefix key is still referenced in `RoosterControl/**/*.cs`
* deleting old-prefix keys causes missing-translation regressions during validation
* any canonical replacement key is missing and the alias is still the only source of text

---

## Phase R1D — Verify EggsInWild GMCM ids are already normalized

### Objective

Confirm that EggsInWild already matches the repo convention and make only safe docs/no-op cleanup changes.

### Allowed files

* `docs/PLAN.md`
* `docs/CHECKLIST.md`
* `docs/TODO.md`

### Forbidden changes

* do not change any `EggsInWild` C# files in this phase
* do not change any `EggsInWild` i18n JSON in this phase unless one of the exact checks below fails with a single trivial fix
* do not rename ids in this phase by default

### Exact verification checks

Run these checks exactly:

1. Under:

   * `EggsInWild/Infrastructure/Integrations/Gmcm/**/*.cs`
   * `EggsInWild/ModEntry.Gmcm.cs`

   verify there are **no** GMCM option ids or section/page ids using obvious PascalCase legacy names like:

   * `Edit...`
   * `Clone...`
   * `Show...`
   * `Verbose...`
   * `Testing...`
   * or any other top-level PascalCase GMCM id token

2. In:

   * `EggsInWild/i18n/default.json`

   verify there are **no** keys starting with:

   * `config.Bool.`
   * `config.Number.`
   * `config.Text.`
   * `config.Page.`
   * `config.Section.`

3. Verify `Action(...)` is already used for one-shot commands and no fake-bool action pattern remains.

### Exact action rule

If all checks pass:

* make this phase docs-only
* do not change EggsInWild production code or i18n

If any check fails:

* stop
* report the failing file and exact offending key/id
* do not improvise a broader rename

### Steps

#### Step R1D.1

Run the exact verification checks above.

#### Step R1D.2

If all checks pass, update `docs/PLAN.md`, `CHECKLIST.md`, and `TODO.md` to record:

* EggsInWild was already normalized at Phase R1 start
* no production rename was required

#### Step R1D.3

Run build gate.

#### Step R1D.4

Create commit:

```bash
git add docs/PLAN.md docs/CHECKLIST.md docs/TODO.md
git commit -m "phase-R1d: verify EggsInWild GMCM ids already normalized"
```

---

## Phase R1E — Finalize repo GMCM id / i18n normalization

### Objective

Finalize docs and verify the repo ends in one consistent GMCM naming state.

### Allowed files

* `docs/PLAN.md`
* `docs/CHECKLIST.md`
* `docs/TODO.md`

### Forbidden changes

* no production code changes in this phase unless required to fix a broken doc/example reference introduced by R1A–R1D
* no new features
* no schema changes
* no opportunistic cleanup

### Final validation checklist

Confirm all of the following:

* `NestCore` docs define the canonical naming rules
* `LetMeHatch` no longer uses mixed PascalCase static GMCM ids in `LmhGmcmMenu.cs`
* `LetMeHatch` page ids follow the exact normalized map from R1B
* `LetMeHatch` both i18n files use the normalized key set
* `RoosterControl` code ids remain canonical dotted form
* `RoosterControl` legacy old-prefix GMCM aliases are pruned if unused
* `EggsInWild` remains already-normalized and unchanged except docs
* all affected docs reflect final Phase R1 state

### Steps

#### Step R1E.1

Update final Phase R1 notes in:

* `docs/PLAN.md`
* `docs/CHECKLIST.md`
* `docs/TODO.md`

#### Step R1E.2

Mark Phase R1 complete only where the phase steps actually completed.

#### Step R1E.3

Run build gate.

#### Step R1E.4

Create commit:

```bash
git add docs/PLAN.md docs/CHECKLIST.md docs/TODO.md
git commit -m "phase-R1e: finalize repo GMCM id and i18n normalization"
```

---

## Hard stop conditions

Stop immediately and report if any of these happen:

* any rename would require config schema changes
* any rename would require gameplay/runtime behavior changes
* any page id rename affects files not listed in Phase R1B
* any old-prefix RoosterControl i18n key is still referenced by C# source
* EggsInWild fails the exact verification checks and would need more than a trivial documented fix
* any build failure appears unrelated to the current phase

---

## Snapshot-specific expectations

For this uploaded snapshot, the expected practical outcome is:

### NestCore

* docs updated only

### LetMeHatch

* **largest actual rename set**
* static GMCM ids and GMCM-facing i18n moved to canonical dotted lowerCamel
* old mixed page-key leftovers removed

### RoosterControl

* **code stays unchanged**
* only legacy unused GMCM i18n alias keys get pruned

### EggsInWild

* **likely docs-only no-op**
* production code and i18n should already pass the normalization checks

