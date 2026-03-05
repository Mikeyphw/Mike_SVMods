Below is the **strict Phase R1C prune-table** for **RoosterControl** based on the current snapshot (`Mike_SVMods_2026-03-05T02-09-36-0300.zip`).

This version is mechanical:

* exact files
* exact prefixes
* exact key counts
* exact keys to delete
* exact grep gates before and after
* no interpretation

---

# Phase R1C — RoosterControl strict legacy GMCM i18n prune table

## Files allowed to edit

Only these files may be edited in this phase:

1. `RoosterControl/i18n/default.json`
2. `RoosterControl/i18n/pt-br.json`
3. `docs/PLAN.md`
4. `docs/CHECKLIST.md`
5. `docs/TODO.md`

## Files that must not be edited

Do **not** edit any C# files in this phase, including:

* `RoosterControl/Application/**`
* `RoosterControl/Integrations/**`
* any `.cs` file anywhere under `RoosterControl/`

---

# Phase goal

Prune **unused legacy GMCM translation alias keys** from `RoosterControl` i18n files.

In this snapshot:

* `config.Bool.*` keys still exist
* `config.Number.*` keys still exist
* `config.Text.*` keys still exist
* `config.Page.*` keys are already absent
* `config.Section.*` keys are already absent

So this phase deletes only:

* `config.Bool.*`
* `config.Number.*`
* `config.Text.*`

It must **not** delete:

* `config.page.*`
* `config.section.*`
* `config.text.*`
* `config.value.*`
* `config.paragraph.*`
* any canonical dotted lowerCamel GMCM key

---

# Mandatory step order

1. Run **pre-delete grep gate**
2. Delete exact keys from `default.json`
3. Delete exact keys from `pt-br.json`
4. Run **post-delete grep gate**
5. Update docs
6. Run build gate
7. Commit

Do not change the order.

---

# 1) Pre-delete grep gate

Run these commands from repo root.

## Gate A — old legacy prefixes must not appear in RoosterControl C# source

```bash
rg -n 'config\.(Bool|Number|Text|Page|Section)\.' RoosterControl -g'*.cs'
```

### Expected result

* **0 matches**

### Rule

* If this returns any match:

  * stop
  * report the matching file(s)
  * do not delete any i18n keys

---

## Gate B — confirm exact legacy key counts in `default.json`

```bash
rg -o '"config\.Bool\.[^"]+"|"config\.Number\.[^"]+"|"config\.Text\.[^"]+"' RoosterControl/i18n/default.json | wc -l
```

### Expected result

* `80`

Breakdown expected:

* `config.Bool.*` = `16`
* `config.Number.*` = `34`
* `config.Text.*` = `30`

---

## Gate C — confirm exact legacy key counts in `pt-br.json`

```bash
rg -o '"config\.Bool\.[^"]+"|"config\.Number\.[^"]+"|"config\.Text\.[^"]+"' RoosterControl/i18n/pt-br.json | wc -l
```

### Expected result

* `80`

Breakdown expected:

* `config.Bool.*` = `16`
* `config.Number.*` = `34`
* `config.Text.*` = `30`

---

## Gate D — verify `config.Page.*` and `config.Section.*` are already absent

```bash
rg -n '"config\.(Page|Section)\.' RoosterControl/i18n/default.json RoosterControl/i18n/pt-br.json
```

### Expected result

* **0 matches**

### Rule

* Do not add or remove anything related to `config.Page.*` or `config.Section.*` in this phase.

---

# 2) Delete exact keys from `RoosterControl/i18n/default.json`

Delete these **80 exact keys**.

## A. Delete these 16 `config.Bool.*` keys

* `config.Bool.Buyable.description`
* `config.Bool.Buyable.name`
* `config.Bool.BypassBlueConditions.description`
* `config.Bool.BypassBlueConditions.name`
* `config.Bool.EnableDebug.description`
* `config.Bool.EnableDebug.name`
* `config.Bool.EnableRoosterHatching.description`
* `config.Bool.EnableRoosterHatching.name`
* `config.Bool.enableeggProbability.description`
* `config.Bool.enableeggProbability.name`
* `config.Bool.enablefertilizedEggProbability.description`
* `config.Bool.enablefertilizedEggProbability.name`
* `config.Bool.enablelargeEggProbability.description`
* `config.Bool.enablelargeEggProbability.name`
* `config.Bool.enablestuckInside.description`
* `config.Bool.enablestuckInside.name`

## B. Delete these 34 `config.Number.*` keys

* `config.Number.ChosenPercentage_DailyLuckBonus.description`
* `config.Number.ChosenPercentage_DailyLuckBonus.name`
* `config.Number.DeluxeProduce_1_Prob.description`
* `config.Number.DeluxeProduce_1_Prob.name`
* `config.Number.DeluxeProduce_2_Prob.description`
* `config.Number.DeluxeProduce_2_Prob.name`
* `config.Number.DeluxeProduce_3_Prob.description`
* `config.Number.DeluxeProduce_3_Prob.name`
* `config.Number.DeluxeProduce_4_Prob.description`
* `config.Number.DeluxeProduce_4_Prob.name`
* `config.Number.EggProbability.description`
* `config.Number.EggProbability.name`
* `config.Number.MatureDays.description`
* `config.Number.MatureDays.name`
* `config.Number.Price.description`
* `config.Number.Price.name`
* `config.Number.ProduceDays.description`
* `config.Number.ProduceDays.name`
* `config.Number.Produce_1_Prob.description`
* `config.Number.Produce_1_Prob.name`
* `config.Number.Produce_2_Prob.description`
* `config.Number.Produce_2_Prob.name`
* `config.Number.Produce_3_Prob.description`
* `config.Number.Produce_3_Prob.name`
* `config.Number.Produce_4_Prob.description`
* `config.Number.Produce_4_Prob.name`
* `config.Number.boostbyrooster.description`
* `config.Number.boostbyrooster.name`
* `config.Number.fertilizedEggProbability.description`
* `config.Number.fertilizedEggProbability.name`
* `config.Number.largeEggProbability.description`
* `config.Number.largeEggProbability.name`
* `config.Number.roosterToChickenRatio.description`
* `config.Number.roosterToChickenRatio.name`

## C. Delete these 30 `config.Text.*` keys

* `config.Text.BlueEggChoice.description`
* `config.Text.BlueEggChoice.name`
* `config.Text.BuildingRequired.description`
* `config.Text.BuildingRequired.name`
* `config.Text.DeluxeProduceOption.description`
* `config.Text.DeluxeProduceOption.name`
* `config.Text.DeluxeProduce_1.description`
* `config.Text.DeluxeProduce_1.name`
* `config.Text.DeluxeProduce_2.description`
* `config.Text.DeluxeProduce_2.name`
* `config.Text.DeluxeProduce_3.description`
* `config.Text.DeluxeProduce_3.name`
* `config.Text.DeluxeProduce_4.description`
* `config.Text.DeluxeProduce_4.name`
* `config.Text.ProduceOption.description`
* `config.Text.ProduceOption.name`
* `config.Text.Produce_1.description`
* `config.Text.Produce_1.name`
* `config.Text.Produce_2.description`
* `config.Text.Produce_2.name`
* `config.Text.Produce_3.description`
* `config.Text.Produce_3.name`
* `config.Text.Produce_4.description`
* `config.Text.Produce_4.name`
* `config.Text.harvestType.description`
* `config.Text.harvestType.name`
* `config.Text.hatchingMode.description`
* `config.Text.hatchingMode.name`
* `config.Text.updateRate.description`
* `config.Text.updateRate.name`

---

# 3) Delete exact keys from `RoosterControl/i18n/pt-br.json`

Delete the **same exact 80 keys** listed above.

Do not translate, rename, or move them.
Delete only those keys.

---

# 4) Post-delete grep gate

Run these commands after deletion.

## Gate E — all deleted legacy prefixes must now be gone

```bash
rg -n '"config\.(Bool|Number|Text)\.' RoosterControl/i18n/default.json RoosterControl/i18n/pt-br.json
```

### Expected result

* **0 matches**

---

## Gate F — canonical lowercase dotted prefixes must still exist

```bash
rg -n '"config\.(page|section|text|value|paragraph)\.' RoosterControl/i18n/default.json RoosterControl/i18n/pt-br.json
```

### Expected result

* **at least 1 match**

### Rule

* If this returns 0 matches, stop and inspect.
* That would indicate accidental over-deletion.

---

## Gate G — no C# code should have been edited

```bash
git diff --name-only -- RoosterControl | rg '\.cs$'
```

### Expected result

* **0 matches**

---

# 5) Docs updates

Only after the post-delete grep gates pass.

## `docs/PLAN.md`

Add a Phase R1C note stating:

* RoosterControl GMCM code ids were already canonical in this snapshot.
* Phase R1C deleted only unused legacy i18n alias keys:

  * `config.Bool.*`
  * `config.Number.*`
  * `config.Text.*`
* No C# code ids changed.
* No ownership decisions changed.

## `docs/CHECKLIST.md`

Add a Phase R1C checklist with these items:

* [x] Verified no `config.(Bool|Number|Text|Page|Section).` references remain in RoosterControl C# source
* [x] Deleted legacy `config.Bool.*` alias keys from both i18n files
* [x] Deleted legacy `config.Number.*` alias keys from both i18n files
* [x] Deleted legacy `config.Text.*` alias keys from both i18n files
* [x] Left canonical dotted lowercase GMCM keys intact
* [x] Verified build

## `docs/TODO.md`

Mark Phase R1C complete and remove any TODO wording implying RoosterControl GMCM ids still need code-level normalization.

Do not add any other doc scope.

---

# 6) Build gate

Run:

```bash
dotnet build RoosterControl/RoosterControl.csproj
dotnet build Mike_SVMods.sln
```

### Rule

* Fix only errors introduced by this phase.
* Do not edit C# files unless the build failure proves the alias deletion was still needed by code.
* If code still depends on deleted keys, restore the phase and stop.

---

# 7) Commit

```bash
git add RoosterControl/i18n/default.json RoosterControl/i18n/pt-br.json docs/PLAN.md docs/CHECKLIST.md docs/TODO.md
git commit -m "phase-R1c: prune RoosterControl legacy GMCM i18n aliases"
```

---

# Mechanical verification summary

## Expected delete counts per file

### `RoosterControl/i18n/default.json`

* delete `16` keys under `config.Bool.*`
* delete `34` keys under `config.Number.*`
* delete `30` keys under `config.Text.*`
* total deleted = `80`

### `RoosterControl/i18n/pt-br.json`

* delete `16` keys under `config.Bool.*`
* delete `34` keys under `config.Number.*`
* delete `30` keys under `config.Text.*`
* total deleted = `80`

### Total repo deletions in this phase

* `160` keys

---

# Hard stop conditions

Stop immediately if any of these happen:

* `rg -n 'config\.(Bool|Number|Text|Page|Section)\.' RoosterControl -g'*.cs'` returns any C# match
* post-delete grep still finds any `config.Bool.*`, `config.Number.*`, or `config.Text.*` keys
* canonical lowercase dotted keys were accidentally removed
* any `.cs` file under `RoosterControl/` shows up in the diff
* build fails for reasons unrelated to this phase

