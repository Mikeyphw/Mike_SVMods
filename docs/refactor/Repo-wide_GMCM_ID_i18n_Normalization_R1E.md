# Phase R1E — Final repo-wide GMCM id/i18n normalization (strict gates)

## Purpose
Finalize Phase R1 after:
- R1A docs rules
- R1B LetMeHatch renames
- R1C RoosterControl legacy alias prune
- R1D EggsInWild verification

This phase is **docs-only** unless a doc/example reference created by R1A–R1D is broken.

---

## Allowed files to edit (ONLY THESE)
- docs/PLAN.md
- docs/CHECKLIST.md
- docs/TODO.md

## Forbidden changes
- Do NOT edit any `.cs` file
- Do NOT edit any `.json` i18n file
- Do NOT edit `EggsInWild/docs/CHECKLIST.md.orig`
- Do NOT edit `RoosterControl/docs/BUILD.md`
- Do NOT edit `RoosterControl/docs/UPGRADE.md`
- Do NOT edit `RoosterControl/docs/PARITY_CHECKLIST.md`
- Do NOT edit `RoosterControl/docs/RELEASE_CHECKLIST.md`
- Do NOT edit `RoosterControl/docs/COMPAT_MATRIX.md`
- Do NOT edit `RoosterControl/docs/COMPAT_TEMPLATES.md`
- Do NOT edit `RoosterControl/docs/templates/**`
- Do NOT edit `NestCore/GMCM_BUILDER.md` in this phase
- Do NOT add new features, new ids, or new keys

---

## Mandatory step order
1. Run all repo verification gates
2. If any gate fails: STOP
3. If all gates pass: make exact docs-only updates
4. Run build gate
5. Mark build checkboxes
6. Commit

Do not change the order.

---

# Gate 1 — LetMeHatch old static GMCM ids must be gone

Expected: **0 matches**

```bash
rg -n '"(editsEggList|editsBirthText|editsCloneEggs|eggPalette|eggColorOverrides|breedingPacks|itemMappings|itemMappingsEgg|itemMappingsMilk|itemMappingsMisc)"' LetMeHatch -g'*.cs'
````

If any match: STOP.

---

# Gate 2 — LetMeHatch old optionId literals must be gone

Expected: **0 matches**

```bash
rg -n 'optionId:\s*"(EditEggListByName|EditBirthText|AlternateBirthText|CloneVanillaEggs|CloneMachineRules|UseDynamicCloneTextures|DinoEggsAsArtifact|OnlyFertilizedEggs|ColorMilk|ExclusiveRabbitWool|ChangeEggColors|DynamicCloneTintMode|LinkAnimalColorToEggs|BreedingSystemEnabled|DeterministicBreedingRng|DailyLuckBonus|StabilizeFarmAnimalsAssetOrder|ShowEdits|ShowAnimalKeys|VerboseLogging|TestingLogging)"' LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs
```

If any match: STOP.

---

# Gate 3 — LetMeHatch old manual GMCM text keys must be gone from C#

Expected: **0 matches**

```bash
rg -n 'config\.(ApplyDailyBoost|NormalEggChance|LargeEggChance|FertilizedEggChance|EnableMale|AddAsAlternatePurchase|MaleChance|BoostByMale)\.' LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs
```

If any match: STOP.

---

# Gate 4 — LetMeHatch deleted legacy page keys must be gone from both i18n files

Expected: **0 matches**

```bash
rg -n 'config\.page\.(BirthText|CloneEggs|EggColors|EggList|EggsEnabled|ManageChosenColors|eggRelations)\.name' LetMeHatch/i18n/default.json LetMeHatch/i18n/pt-br.json
```

If any match: STOP.

---

# Gate 5 — LetMeHatch canonical new ids must exist

Expected:

* each command returns **>= 1 match**
* if any returns 0: STOP

```bash
rg -n '"config\.page\.(edits\.eggList|edits\.birthText|edits\.cloneEggs|colors\.eggPalette|colors\.eggColorOverrides|advanced\.itemMappings|advanced\.itemMappings\.egg|advanced\.itemMappings\.milk|advanced\.itemMappings\.misc)\.name"' LetMeHatch/i18n/default.json LetMeHatch/i18n/pt-br.json

rg -n 'optionId:\s*"(general\.editEggListByName|general\.editBirthText|general\.alternateBirthText|cloning\.cloneVanillaEggs|cloning\.cloneMachineRules|cloning\.useDynamicCloneTextures|cloning\.dinoEggAsArtifact|cloning\.onlyFertilizedEggs|cloning\.colorClonedMilk|cloning\.exclusiveRabbitWool|colors\.changeEggColors|colors\.dynamicCloneTintMode|colors\.linkAnimalColorToEgg|breeding\.systemEnabled|breeding\.deterministicRng|breeding\.dailyLuckBonus|advanced\.stabilizeFarmAnimalsAssetOrder|debug\.showEdits|debug\.showAnimalKeys|debug\.verboseLogging|debug\.testingLogging)"' LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs
```

---

# Gate 6 — RoosterControl old legacy alias prefixes must be gone from i18n

Expected: **0 matches**

```bash
rg -n '"config\.(Bool|Number|Text)\.' RoosterControl/i18n/default.json RoosterControl/i18n/pt-br.json
```

If any match: STOP.

---

# Gate 7 — RoosterControl old legacy alias prefixes must still be absent from C#

Expected: **0 matches**

```bash
rg -n 'config\.(Bool|Number|Text|Page|Section)\.' RoosterControl -g'*.cs'
```

If any match: STOP.

---

# Gate 8 — RoosterControl canonical dotted GMCM keys must still exist

Expected: **>= 1 match**

```bash
rg -n '"config\.(page|section|text|value|paragraph)\.' RoosterControl/i18n/default.json RoosterControl/i18n/pt-br.json
```

If 0 matches: STOP.

---

# Gate 9 — EggsInWild legacy alias prefixes must be absent

Expected: **0 matches**

```bash
rg -n 'config\.(Bool|Number|Text|Page|Section)\.' EggsInWild
```

If any match: STOP.

---

# Gate 10 — EggsInWild fake-bool button pattern must be absent

Expected: **0 matches**

```bash
rg -n 'getValue:\s*\(\)\s*=>\s*false' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs'
```

If any match: STOP.

---

# Gate 11 — EggsInWild Action(...) usage must still exist

Expected: **>= 1 match**
Snapshot expectation: **5 matches**

```bash
rg -n '\.Action\(' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs' | wc -l
```

If result is 0: STOP.

---

# Gate 12 — EggsInWild expected file-state must still hold

Expected:

* `EggsInWild/i18n/default.json` exists
* `EggsInWild/i18n/pt-br.json` does NOT exist
* `EggsInWild/ModEntry.Gmcm.WildAnimals.cs` does NOT exist

```bash
test -f EggsInWild/i18n/default.json
test ! -f EggsInWild/i18n/pt-br.json
test ! -f EggsInWild/ModEntry.Gmcm.WildAnimals.cs
```

If any check fails: STOP.

---

# Gate 13 — NestCore docs must already mention the normalization rule set

Expected:

* each command returns **>= 1 match**
* if any returns 0: STOP

```bash
rg -n 'GMCM id and i18n normalization' NestCore/GMCM_BUILDER.md
rg -n 'dotted lowerCamel' NestCore/GMCM_BUILDER.md
rg -n 'Avoid PascalCase GMCM ids' NestCore/GMCM_BUILDER.md
```

If any returns 0: STOP.

---

# Gate 14 — This phase must be docs-only before edits

Expected: **0 matches**

```bash
git diff --name-only -- NestCore LetMeHatch RoosterControl EggsInWild | rg -v '(^NestCore/docs/|^LetMeHatch/docs/|^RoosterControl/docs/|^EggsInWild/docs/)'
```

If any match: STOP.

---

# If ALL gates pass → exact docs updates

## 1) docs/PLAN.md

Append this exact section at the end of the file:

## Phase R1E — Final repo GMCM id/i18n normalization validation

Goal: finalize repo-wide GMCM naming normalization after Phase R1A–R1D and confirm all affected mods/docs now align to the canonical GMCM naming convention.

Acceptance:

* LetMeHatch no longer uses legacy mixed/PascalCase static GMCM ids.
* RoosterControl no longer carries unused legacy `config.Bool.*`, `config.Number.*`, or `config.Text.*` GMCM alias keys.
* EggsInWild remains already-normalized with no production rename required in Phase R1D.
* NestCore docs define the canonical GMCM naming convention for the repo.

---

## 2) docs/CHECKLIST.md

Append this exact section at the end of the file:

## Phase R1E — Final repo GMCM id/i18n normalization validation

* [x] Verified LetMeHatch normalized GMCM ids/i18n are present and legacy ids are absent
* [x] Verified RoosterControl legacy GMCM alias prefixes are removed from i18n and absent from C# usage
* [x] Verified EggsInWild remains already normalized
* [x] Verified NestCore docs define the canonical GMCM naming rules
* [ ] Verified build

---

## 3) docs/TODO.md

Append this exact line at the end of the file:

* [x] Phase R1E: Final repo GMCM id/i18n normalization validation complete.

---

# Build gate (required)

```bash
dotnet build NestCore/NestCore.csproj
dotnet build RoosterControl/RoosterControl.csproj
dotnet build LetMeHatch/LetMeHatch.csproj
dotnet build EggsInWild/EggsInWild.csproj
dotnet build Mike_SVMods.sln
```

If build fails:

* fix only docs/example-reference breakage introduced in this phase
* do not edit code or i18n unless the failure proves a doc/example path typo introduced by R1E

---

# After successful build, tick these exact checklist items

## docs/CHECKLIST.md

Change:

* `[ ] Verified build`
  to:
* `[x] Verified build`

---

# Final post-edit gates

## Gate 15 — only docs files should be changed in this phase

Expected: **0 matches**

```bash
git diff --name-only -- NestCore LetMeHatch RoosterControl EggsInWild | rg -v '(^NestCore/docs/|^LetMeHatch/docs/|^RoosterControl/docs/|^EggsInWild/docs/)'
```

If any match: STOP.

## Gate 16 — Phase R1E text should now exist in PLAN file

Expected: **1 match total**

```bash
rg -n 'Repo Phase R1E|Phase R1E — Final repo GMCM id/i18n normalization validation' docs/PLAN.md
```

If fewer than 1 match: STOP.

---

# Commit

```bash
git add docs/PLAN.md docs/CHECKLIST.md docs/TODO.md
git commit -m "phase-R1e: finalize repo GMCM id and i18n normalization"
```

---

# Hard stop conditions

Stop immediately if any of these happen:

* any gate from 1–14 fails
* any non-doc file would need editing in this phase
* any build failure is unrelated to Phase R1E docs text
* any previously normalized mod fails its verification gate and would require additional production changes
