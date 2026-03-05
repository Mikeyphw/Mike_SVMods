Below is a **Phase R1B "zero-interpretation" rename checklist/table** for **LetMeHatch** based on the current snapshot (`Mike_SVMods_2026-03-05T02-09-36-0300.zip`). Every row includes **exact old → new** tokens and **expected occurrence counts** in `LmhGmcmMenu.cs` so the agent can verify each replacement mechanically.

---

# Phase R1B — LetMeHatch strict rename checklist (mechanical)

## Files allowed to edit (ONLY THESE)

1. `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
2. `LetMeHatch/i18n/default.json`
3. `LetMeHatch/i18n/pt-br.json`
4. `docs/PLAN.md`
5. `docs/CHECKLIST.md`
6. `docs/TODO.md`

---

## Step order (MANDATORY)

1. Apply **all C# renames** in `LmhGmcmMenu.cs` (tables A–E)
2. Apply **all i18n key renames** in `default.json` and `pt-br.json` (tables F–J)
3. Add **one new page name key** (table K)
4. Remove **exact legacy page keys** (table L)
5. Run **grep verification** (commands provided)
6. `dotnet build …` gate + commit

---

# A) C# page-id literal renames (`"old"` → `"new"`)

**File:** `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
**Replace exact string literals including quotes**.
**Each old page-id appears exactly 2 times in quotes** in this file.

| Old page id         | New page id                  | Expected `"`occurrences in file |
| ------------------- | ---------------------------- | ------------------------------: |
| `editsEggList`      | `edits.eggList`              |                               2 |
| `editsBirthText`    | `edits.birthText`            |                               2 |
| `editsCloneEggs`    | `edits.cloneEggs`            |                               2 |
| `eggPalette`        | `colors.eggPalette`          |                               2 |
| `eggColorOverrides` | `colors.eggColorOverrides`   |                               2 |
| `breedingPacks`     | `breeding.packs`             |                               2 |
| `itemMappings`      | `advanced.itemMappings`      |                               2 |
| `itemMappingsEgg`   | `advanced.itemMappings.egg`  |                               2 |
| `itemMappingsMilk`  | `advanced.itemMappings.milk` |                               2 |
| `itemMappingsMisc`  | `advanced.itemMappings.misc` |                               2 |

---

# B) Generated breeding page-id helpers (exact line edits)

**File:** `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`

Locate these exact lines:

```csharp
private static string BuildPackPageId(string contentPackId)
    => "breeding_pack_" + Slug(contentPackId);

private static string BuildFemalePageId(string contentPackId, string femaleKey)
    => "breeding_female_" + Slug(contentPackId) + "_" + Slug(femaleKey);
```

Replace them with these exact lines:

```csharp
private static string BuildPackPageId(string contentPackId)
    => "breeding.pack." + Slug(contentPackId);

private static string BuildFemalePageId(string contentPackId, string femaleKey)
    => "breeding.female." + Slug(contentPackId) + "." + Slug(femaleKey);
```

---

# C) C# `optionId:` literal renames (`optionId: "Old"` → `optionId: "new.scope.id"`)

**File:** `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
Each old `optionId:` appears **exactly 1 time**.

| Old `optionId`                   | New `optionId`                            | Expected occurrences |
| -------------------------------- | ----------------------------------------- | -------------------: |
| `EditEggListByName`              | `general.editEggListByName`               |                    1 |
| `EditBirthText`                  | `general.editBirthText`                   |                    1 |
| `AlternateBirthText`             | `general.alternateBirthText`              |                    1 |
| `CloneVanillaEggs`               | `cloning.cloneVanillaEggs`                |                    1 |
| `CloneMachineRules`              | `cloning.cloneMachineRules`               |                    1 |
| `UseDynamicCloneTextures`        | `cloning.useDynamicCloneTextures`         |                    1 |
| `DinoEggsAsArtifact`             | `cloning.dinoEggAsArtifact`               |                    1 |
| `OnlyFertilizedEggs`             | `cloning.onlyFertilizedEggs`              |                    1 |
| `ColorMilk`                      | `cloning.colorClonedMilk`                 |                    1 |
| `ExclusiveRabbitWool`            | `cloning.exclusiveRabbitWool`             |                    1 |
| `ChangeEggColors`                | `colors.changeEggColors`                  |                    1 |
| `DynamicCloneTintMode`           | `colors.dynamicCloneTintMode`             |                    1 |
| `LinkAnimalColorToEggs`          | `colors.linkAnimalColorToEgg`             |                    1 |
| `BreedingSystemEnabled`          | `breeding.systemEnabled`                  |                    1 |
| `DeterministicBreedingRng`       | `breeding.deterministicRng`               |                    1 |
| `DailyLuckBonus`                 | `breeding.dailyLuckBonus`                 |                    1 |
| `StabilizeFarmAnimalsAssetOrder` | `advanced.stabilizeFarmAnimalsAssetOrder` |                    1 |
| `ShowEdits`                      | `debug.showEdits`                         |                    1 |
| `ShowAnimalKeys`                 | `debug.showAnimalKeys`                    |                    1 |
| `VerboseLogging`                 | `debug.verboseLogging`                    |                    1 |
| `TestingLogging`                 | `debug.testingLogging`                    |                    1 |

---

# D) C# `GetString("...")` key renames (manual GMCM labels)

**File:** `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
Each old key appears **exactly 1 time**.

| Old translation key                         | New translation key                                  | Expected occurrences |
| ------------------------------------------- | ---------------------------------------------------- | -------------------: |
| `config.ApplyDailyBoost.name`               | `config.breeding.applyDailyBoost.name`               |                    1 |
| `config.ApplyDailyBoost.description`        | `config.breeding.applyDailyBoost.description`        |                    1 |
| `config.NormalEggChance.name`               | `config.breeding.normalEggChance.name`               |                    1 |
| `config.NormalEggChance.description`        | `config.breeding.normalEggChance.description`        |                    1 |
| `config.LargeEggChance.name`                | `config.breeding.largeEggChance.name`                |                    1 |
| `config.LargeEggChance.description`         | `config.breeding.largeEggChance.description`         |                    1 |
| `config.FertilizedEggChance.name`           | `config.breeding.fertilizedEggChance.name`           |                    1 |
| `config.FertilizedEggChance.description`    | `config.breeding.fertilizedEggChance.description`    |                    1 |
| `config.EnableMale.name`                    | `config.breeding.enableMale.name`                    |                    1 |
| `config.EnableMale.description`             | `config.breeding.enableMale.description`             |                    1 |
| `config.AddAsAlternatePurchase.name`        | `config.breeding.addAsAlternatePurchase.name`        |                    1 |
| `config.AddAsAlternatePurchase.description` | `config.breeding.addAsAlternatePurchase.description` |                    1 |
| `config.MaleChance.name`                    | `config.breeding.maleChance.name`                    |                    1 |
| `config.MaleChance.description`             | `config.breeding.maleChance.description`             |                    1 |
| `config.BoostByMale.name`                   | `config.breeding.boostByMale.name`                   |                    1 |
| `config.BoostByMale.description`            | `config.breeding.boostByMale.description`            |                    1 |

---

# E) C# `GetString("config.page.*.name")` key renames for page titles

**File:** `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
Each old key appears **exactly 1 time**.

| Old page title key                  | New page title key                            | Expected occurrences |
| ----------------------------------- | --------------------------------------------- | -------------------: |
| `config.page.itemMappings.name`     | `config.page.advanced.itemMappings.name`      |                    1 |
| `config.page.itemMappingsEgg.name`  | `config.page.advanced.itemMappings.egg.name`  |                    1 |
| `config.page.itemMappingsMilk.name` | `config.page.advanced.itemMappings.milk.name` |                    1 |
| `config.page.itemMappingsMisc.name` | `config.page.advanced.itemMappings.misc.name` |                    1 |

---

# E2) C# Section ID renames (ONLY these two)

**File:** `LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs`
Each appears **exactly 1 time** as `.Section("...")`.

| Old section id          | New section id           | Expected occurrences |
| ----------------------- | ------------------------ | -------------------: |
| `General`               | `general`                |                    1 |
| `LoadedBreedingSystems` | `breeding.loadedSystems` |                    1 |

---

# F) i18n page key renames (default.json + pt-br.json)

**Files:**

* `LetMeHatch/i18n/default.json`
* `LetMeHatch/i18n/pt-br.json`

Rename these keys (values unchanged):

| Old key                              | New key                                       |
| ------------------------------------ | --------------------------------------------- |
| `config.page.editsEggList.name`      | `config.page.edits.eggList.name`              |
| `config.page.editsBirthText.name`    | `config.page.edits.birthText.name`            |
| `config.page.editsCloneEggs.name`    | `config.page.edits.cloneEggs.name`            |
| `config.page.eggPalette.name`        | `config.page.colors.eggPalette.name`          |
| `config.page.eggColorOverrides.name` | `config.page.colors.eggColorOverrides.name`   |
| `config.page.itemMappings.name`      | `config.page.advanced.itemMappings.name`      |
| `config.page.itemMappingsEgg.name`   | `config.page.advanced.itemMappings.egg.name`  |
| `config.page.itemMappingsMilk.name`  | `config.page.advanced.itemMappings.milk.name` |
| `config.page.itemMappingsMisc.name`  | `config.page.advanced.itemMappings.misc.name` |

---

# G) i18n option key renames (name + description)

**Files:**

* `LetMeHatch/i18n/default.json`
* `LetMeHatch/i18n/pt-br.json`

For each row, rename **both** keys if they exist (values unchanged).
Note: `OnlyFertilizedEggs` has **no `.description`** key today; rename only `.name`.

| Old `.name` key                              | New `.name` key                                       |
| -------------------------------------------- | ----------------------------------------------------- |
| `config.EditEggListByName.name`              | `config.general.editEggListByName.name`               |
| `config.EditBirthText.name`                  | `config.general.editBirthText.name`                   |
| `config.AlternateBirthText.name`             | `config.general.alternateBirthText.name`              |
| `config.CloneVanillaEggs.name`               | `config.cloning.cloneVanillaEggs.name`                |
| `config.CloneMachineRules.name`              | `config.cloning.cloneMachineRules.name`               |
| `config.UseDynamicCloneTextures.name`        | `config.cloning.useDynamicCloneTextures.name`         |
| `config.DinoEggsAsArtifact.name`             | `config.cloning.dinoEggAsArtifact.name`               |
| `config.OnlyFertilizedEggs.name`             | `config.cloning.onlyFertilizedEggs.name`              |
| `config.ColorMilk.name`                      | `config.cloning.colorClonedMilk.name`                 |
| `config.ExclusiveRabbitWool.name`            | `config.cloning.exclusiveRabbitWool.name`             |
| `config.ChangeEggColors.name`                | `config.colors.changeEggColors.name`                  |
| `config.DynamicCloneTintMode.name`           | `config.colors.dynamicCloneTintMode.name`             |
| `config.LinkAnimalColorToEggs.name`          | `config.colors.linkAnimalColorToEgg.name`             |
| `config.BreedingSystemEnabled.name`          | `config.breeding.systemEnabled.name`                  |
| `config.DeterministicBreedingRng.name`       | `config.breeding.deterministicRng.name`               |
| `config.DailyLuckBonus.name`                 | `config.breeding.dailyLuckBonus.name`                 |
| `config.StabilizeFarmAnimalsAssetOrder.name` | `config.advanced.stabilizeFarmAnimalsAssetOrder.name` |
| `config.ShowEdits.name`                      | `config.debug.showEdits.name`                         |
| `config.ShowAnimalKeys.name`                 | `config.debug.showAnimalKeys.name`                    |
| `config.VerboseLogging.name`                 | `config.debug.verboseLogging.name`                    |
| `config.TestingLogging.name`                 | `config.debug.testingLogging.name`                    |

Now rename the matching `.description` keys where they exist (values unchanged):

| Old `.description` key                              | New `.description` key                                       |
| --------------------------------------------------- | ------------------------------------------------------------ |
| `config.EditEggListByName.description`              | `config.general.editEggListByName.description`               |
| `config.EditBirthText.description`                  | `config.general.editBirthText.description`                   |
| `config.AlternateBirthText.description`             | `config.general.alternateBirthText.description`              |
| `config.CloneVanillaEggs.description`               | `config.cloning.cloneVanillaEggs.description`                |
| `config.CloneMachineRules.description`              | `config.cloning.cloneMachineRules.description`               |
| `config.UseDynamicCloneTextures.description`        | `config.cloning.useDynamicCloneTextures.description`         |
| `config.DinoEggsAsArtifact.description`             | `config.cloning.dinoEggAsArtifact.description`               |
| `config.ColorMilk.description`                      | `config.cloning.colorClonedMilk.description`                 |
| `config.ExclusiveRabbitWool.description`            | `config.cloning.exclusiveRabbitWool.description`             |
| `config.ChangeEggColors.description`                | `config.colors.changeEggColors.description`                  |
| `config.DynamicCloneTintMode.description`           | `config.colors.dynamicCloneTintMode.description`             |
| `config.LinkAnimalColorToEggs.description`          | `config.colors.linkAnimalColorToEgg.description`             |
| `config.BreedingSystemEnabled.description`          | `config.breeding.systemEnabled.description`                  |
| `config.DeterministicBreedingRng.description`       | `config.breeding.deterministicRng.description`               |
| `config.DailyLuckBonus.description`                 | `config.breeding.dailyLuckBonus.description`                 |
| `config.StabilizeFarmAnimalsAssetOrder.description` | `config.advanced.stabilizeFarmAnimalsAssetOrder.description` |
| `config.ShowEdits.description`                      | `config.debug.showEdits.description`                         |
| `config.ShowAnimalKeys.description`                 | `config.debug.showAnimalKeys.description`                    |
| `config.VerboseLogging.description`                 | `config.debug.verboseLogging.description`                    |
| `config.TestingLogging.description`                 | `config.debug.testingLogging.description`                    |

(Do **not** invent `config.cloning.onlyFertilizedEggs.description`.)

---

# H) i18n manual GMCM key renames (breeding scoped)

**Files:**

* `LetMeHatch/i18n/default.json`
* `LetMeHatch/i18n/pt-br.json`

Rename these keys (values unchanged):

| Old key                                     | New key                                              |
| ------------------------------------------- | ---------------------------------------------------- |
| `config.ApplyDailyBoost.name`               | `config.breeding.applyDailyBoost.name`               |
| `config.ApplyDailyBoost.description`        | `config.breeding.applyDailyBoost.description`        |
| `config.NormalEggChance.name`               | `config.breeding.normalEggChance.name`               |
| `config.NormalEggChance.description`        | `config.breeding.normalEggChance.description`        |
| `config.LargeEggChance.name`                | `config.breeding.largeEggChance.name`                |
| `config.LargeEggChance.description`         | `config.breeding.largeEggChance.description`         |
| `config.FertilizedEggChance.name`           | `config.breeding.fertilizedEggChance.name`           |
| `config.FertilizedEggChance.description`    | `config.breeding.fertilizedEggChance.description`    |
| `config.EnableMale.name`                    | `config.breeding.enableMale.name`                    |
| `config.EnableMale.description`             | `config.breeding.enableMale.description`             |
| `config.AddAsAlternatePurchase.name`        | `config.breeding.addAsAlternatePurchase.name`        |
| `config.AddAsAlternatePurchase.description` | `config.breeding.addAsAlternatePurchase.description` |
| `config.MaleChance.name`                    | `config.breeding.maleChance.name`                    |
| `config.MaleChance.description`             | `config.breeding.maleChance.description`             |
| `config.BoostByMale.name`                   | `config.breeding.boostByMale.name`                   |
| `config.BoostByMale.description`            | `config.breeding.boostByMale.description`            |

---

# I) i18n section key renames (ONLY these two)

**Files:**

* `LetMeHatch/i18n/default.json`
* `LetMeHatch/i18n/pt-br.json`

Rename these keys (values unchanged):

| Old key                                     | New key                                      |
| ------------------------------------------- | -------------------------------------------- |
| `config.section.General.text`               | `config.section.general.text`                |
| `config.section.LoadedBreedingSystems.text` | `config.section.breeding.loadedSystems.text` |

---

# K) Add ONE new page name key (because old `breedingPacks` had none)

**Files:**

* `LetMeHatch/i18n/default.json`
* `LetMeHatch/i18n/pt-br.json`

Add this key/value pair exactly:

* Key: `config.page.breeding.packs.name`
* Value (default.json): `Breeding Content Packs`
* Value (pt-br.json): `Breeding Content Packs`

(Do not add a `.description` key.)

---

# L) Remove EXACT legacy page keys (both i18n files)

**Files:**

* `LetMeHatch/i18n/default.json`
* `LetMeHatch/i18n/pt-br.json`

Delete these keys exactly:

* `config.page.BirthText.name`
* `config.page.CloneEggs.name`
* `config.page.EggColors.name`
* `config.page.EggList.name`
* `config.page.EggsEnabled.name`
* `config.page.ManageChosenColors.name`
* `config.page.eggRelations.name`

Delete **no other keys** in this step.

---

## Mandatory verification (NO GUESSING)

Run these commands from repo root. All must return **zero matches** after edits.

### 1) Old page ids must be gone from LetMeHatch C#

```bash
rg -n '"(editsEggList|editsBirthText|editsCloneEggs|eggPalette|eggColorOverrides|breedingPacks|itemMappings|itemMappingsEgg|itemMappingsMilk|itemMappingsMisc)"' LetMeHatch -g'*.cs'
```

### 2) Old optionId literals must be gone

```bash
rg -n 'optionId:\s*"(EditEggListByName|EditBirthText|AlternateBirthText|CloneVanillaEggs|CloneMachineRules|UseDynamicCloneTextures|DinoEggsAsArtifact|OnlyFertilizedEggs|ColorMilk|ExclusiveRabbitWool|ChangeEggColors|DynamicCloneTintMode|LinkAnimalColorToEggs|BreedingSystemEnabled|DeterministicBreedingRng|DailyLuckBonus|StabilizeFarmAnimalsAssetOrder|ShowEdits|ShowAnimalKeys|VerboseLogging|TestingLogging)"' LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs
```

### 3) Old manual `GetString(...)` keys must be gone

```bash
rg -n 'config\.(ApplyDailyBoost|NormalEggChance|LargeEggChance|FertilizedEggChance|EnableMale|AddAsAlternatePurchase|MaleChance|BoostByMale)\.' LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs
```

### 4) Old itemMappings title keys must be gone

```bash
rg -n 'config\.page\.itemMappings(Egg|Milk|Misc)?\.name' LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs
```

### 5) Old section ids must be gone

```bash
rg -n '\.Section\("?(General|LoadedBreedingSystems)"?' LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs
```

### 6) Deleted legacy page keys must be gone from both i18n files

```bash
rg -n 'config\.page\.(BirthText|CloneEggs|EggColors|EggList|EggsEnabled|ManageChosenColors|eggRelations)\.name' LetMeHatch/i18n/default.json LetMeHatch/i18n/pt-br.json
```

---

## Docs updates (strict)

After code + i18n changes:

* In `docs/PLAN.md`: add a Phase R1B note stating "GMCM ids + i18n normalized; no schema changes".
* In `docs/CHECKLIST.md`: add + check Phase R1B steps after build passes.
* In `docs/TODO.md`: remove/mark done the Phase R1B TODO entries.

No other doc edits.

---

## Build gate + commit

Run build gate:

```bash
dotnet build LetMeHatch/LetMeHatch.csproj
dotnet build Mike_SVMods.sln
```

Commit:

```bash
git add LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs LetMeHatch/i18n/default.json LetMeHatch/i18n/pt-br.json docs/PLAN.md docs/CHECKLIST.md docs/TODO.md
git commit -m "phase-R1b: normalize LetMeHatch GMCM ids and i18n keys"
```

---

