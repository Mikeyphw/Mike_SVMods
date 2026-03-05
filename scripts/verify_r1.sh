#!/usr/bin/env bash
set -euo pipefail

ROOT="${1:-.}"

cd "$ROOT"

fail() {
  echo "❌ $1" >&2
  exit 1
}

pass() {
  echo "✅ $1"
}

count_lines() {
  local cmd_output
  cmd_output="$(eval "$1" || true)"
  if [[ -z "$cmd_output" ]]; then
    echo 0
  else
    printf '%s\n' "$cmd_output" | wc -l | tr -d ' '
  fi
}

expect_zero_matches() {
  local name="$1"
  local pattern="$2"
  shift 2
  local output
  output="$(rg -n "$pattern" "$@" || true)"
  if [[ -n "$output" ]]; then
    echo "$output"
    fail "$name"
  fi
  pass "$name"
}

expect_some_matches() {
  local name="$1"
  local pattern="$2"
  shift 2
  local output
  output="$(rg -n "$pattern" "$@" || true)"
  if [[ -z "$output" ]]; then
    fail "$name"
  fi
  pass "$name"
}

expect_file_exists() {
  local path="$1"
  [[ -f "$path" ]] || fail "Expected file to exist: $path"
  pass "File exists: $path"
}

expect_file_absent() {
  local path="$1"
  [[ ! -e "$path" ]] || fail "Expected path to be absent: $path"
  pass "Path absent: $path"
}

expect_exact_count() {
  local name="$1"
  local expected="$2"
  local actual="$3"
  [[ "$actual" == "$expected" ]] || fail "$name (expected $expected, got $actual)"
  pass "$name = $expected"
}

echo "== R1 verification runner =="

# -----------------------------
# R1B — LetMeHatch normalization
# -----------------------------
echo
echo "== R1B: LetMeHatch =="

expect_zero_matches \
  "R1B old static page ids removed" \
  '"(editsEggList|editsBirthText|editsCloneEggs|eggPalette|eggColorOverrides|breedingPacks|itemMappings|itemMappingsEgg|itemMappingsMilk|itemMappingsMisc)"' \
  LetMeHatch

expect_zero_matches \
  "R1B old optionId literals removed" \
  'optionId:\s*"(EditEggListByName|EditBirthText|AlternateBirthText|CloneVanillaEggs|CloneMachineRules|UseDynamicCloneTextures|DinoEggsAsArtifact|OnlyFertilizedEggs|ColorMilk|ExclusiveRabbitWool|ChangeEggColors|DynamicCloneTintMode|LinkAnimalColorToEggs|BreedingSystemEnabled|DeterministicBreedingRng|DailyLuckBonus|StabilizeFarmAnimalsAssetOrder|ShowEdits|ShowAnimalKeys|VerboseLogging|TestingLogging)"' \
  LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs

expect_zero_matches \
  "R1B old manual GMCM text keys removed from C#" \
  'config\.(ApplyDailyBoost|NormalEggChance|LargeEggChance|FertilizedEggChance|EnableMale|AddAsAlternatePurchase|MaleChance|BoostByMale)\.' \
  LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs

expect_zero_matches \
  "R1B deleted legacy page keys removed from i18n" \
  'config\.page\.(BirthText|CloneEggs|EggColors|EggList|EggsEnabled|ManageChosenColors|eggRelations)\.name' \
  LetMeHatch/i18n/default.json LetMeHatch/i18n/pt-br.json

expect_some_matches \
  "R1B canonical new page keys exist" \
  '"config\.page\.(edits\.eggList|edits\.birthText|edits\.cloneEggs|colors\.eggPalette|colors\.eggColorOverrides|advanced\.itemMappings|advanced\.itemMappings\.egg|advanced\.itemMappings\.milk|advanced\.itemMappings\.misc)\.name"' \
  LetMeHatch/i18n/default.json LetMeHatch/i18n/pt-br.json

expect_some_matches \
  "R1B canonical new option ids exist" \
  'optionId:\s*"(general\.editEggListByName|general\.editBirthText|general\.alternateBirthText|cloning\.cloneVanillaEggs|cloning\.cloneMachineRules|cloning\.useDynamicCloneTextures|cloning\.dinoEggAsArtifact|cloning\.onlyFertilizedEggs|cloning\.colorClonedMilk|cloning\.exclusiveRabbitWool|colors\.changeEggColors|colors\.dynamicCloneTintMode|colors\.linkAnimalColorToEgg|breeding\.systemEnabled|breeding\.deterministicRng|breeding\.dailyLuckBonus|advanced\.stabilizeFarmAnimalsAssetOrder|debug\.showEdits|debug\.showAnimalKeys|debug\.verboseLogging|debug\.testingLogging)"' \
  LetMeHatch/Integrations/Gmcm/LmhGmcmMenu.cs

# -----------------------------
# R1C — RoosterControl prune
# -----------------------------
echo
echo "== R1C: RoosterControl =="

expect_zero_matches \
  "R1C no legacy GMCM alias prefixes remain in RoosterControl C#" \
  'config\.(Bool|Number|Text|Page|Section)\.' \
  RoosterControl -g'*.cs'

expect_zero_matches \
  "R1C no legacy GMCM alias prefixes remain in RoosterControl i18n" \
  '"config\.(Bool|Number|Text)\.' \
  RoosterControl/i18n/default.json RoosterControl/i18n/pt-br.json

expect_some_matches \
  "R1C canonical lowercase GMCM keys still exist in RoosterControl i18n" \
  '"config\.(page|section|text|value|paragraph)\.' \
  RoosterControl/i18n/default.json RoosterControl/i18n/pt-br.json

# -----------------------------
# R1D — EggsInWild verification
# -----------------------------
echo
echo "== R1D: EggsInWild =="

expect_file_exists "EggsInWild/i18n/default.json"
expect_file_absent "EggsInWild/i18n/pt-br.json"
expect_file_absent "EggsInWild/ModEntry.Gmcm.WildAnimals.cs"

expect_zero_matches \
  "R1D no legacy GMCM alias prefixes anywhere in EggsInWild" \
  'config\.(Bool|Number|Text|Page|Section)\.' \
  EggsInWild

expect_zero_matches \
  "R1D no PascalCase GMCM option ids in EggsInWild" \
  'optionId:\s*"[A-Z]' \
  EggsInWild/Infrastructure/Integrations/Gmcm EggsInWild/ModEntry.Gmcm.cs

expect_zero_matches \
  "R1D no PascalCase GMCM page/link ids in EggsInWild" \
  '\b(Page|Link|Links)\(\s*"[A-Z]' \
  EggsInWild/Infrastructure/Integrations/Gmcm EggsInWild/ModEntry.Gmcm.cs

expect_zero_matches \
  "R1D no fake-bool button pattern remains in EggsInWild" \
  'getValue:\s*\(\)\s*=>\s*false' \
  EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs'

action_count="$(count_lines "rg -n '\\.Action\\(' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs'")"
option_count="$(count_lines "rg -n 'optionId:\\s*\"' EggsInWild/Infrastructure/Integrations/Gmcm EggsInWild/ModEntry.Gmcm.cs -g'*.cs'")"
page_count="$(count_lines "rg -n '\\bPage\\(\\s*\"' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs'")"
link_count="$(count_lines "rg -n '\\bLink\\(\\s*\"' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs'")"

expect_exact_count "R1D EggsInWild .Action() count" 5 "$action_count"
expect_exact_count "R1D EggsInWild optionId count" 27 "$option_count"
expect_exact_count "R1D EggsInWild Page(\"...\") count" 7 "$page_count"
expect_exact_count "R1D EggsInWild Link(\"...\") count" 9 "$link_count"

expected_pages=$'animalNotes\nanimalProgression\ndesertTomb\nhatching\nitems\nprices\nwildAnimals'
actual_pages="$(
  rg -n '\bPage\(\s*"[^"]+"' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs' \
    | rg -o '"[^"]+"' \
    | tr -d '"' \
    | sort -u
)"

if [[ "$actual_pages" != "$expected_pages" ]]; then
  echo "Expected page ids:"
  printf '%s\n' "$expected_pages"
  echo
  echo "Actual page ids:"
  printf '%s\n' "$actual_pages"
  fail "R1D EggsInWild top-level page id set mismatch"
fi
pass "R1D EggsInWild top-level page id set matches expected 7-page set"

# -----------------------------
# R1E — final repo validation
# -----------------------------
echo
echo "== R1E: final repo validation =="

expect_some_matches \
  "R1E NestCore docs include GMCM normalization section title" \
  'GMCM id and i18n normalization' \
  NestCore/GMCM_BUILDER.md

expect_some_matches \
  "R1E NestCore docs mention dotted lowerCamel" \
  'dotted lowerCamel' \
  NestCore/GMCM_BUILDER.md

expect_some_matches \
  "R1E NestCore docs mention avoiding PascalCase GMCM ids" \
  'Avoid PascalCase GMCM ids' \
  NestCore/GMCM_BUILDER.md

pass "All R1 verification gates passed"

echo
echo "Next build gate:"
echo "  dotnet build NestCore/NestCore.csproj"
echo "  dotnet build RoosterControl/RoosterControl.csproj"
echo "  dotnet build LetMeHatch/LetMeHatch.csproj"
echo "  dotnet build EggsInWild/EggsInWild.csproj"
echo "  dotnet build Mike_SVMods.sln"

