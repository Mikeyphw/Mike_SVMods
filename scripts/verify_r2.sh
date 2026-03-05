#!/usr/bin/env bash
# Mike_SVMods/scripts/verify_r2.sh
set -euo pipefail

ROOT="."
DO_BUILD=0
PHASES=()

usage() {
  cat <<'USAGE'
Usage:
  verify_r2.sh [repo_root] [--phase ID]... [--build]

Examples:
  verify_r2.sh /path/to/Mike_SVMods
  verify_r2.sh /path/to/Mike_SVMods --phase EIW-R2-1
  verify_r2.sh /path/to/Mike_SVMods --phase EIW-R2-1 --phase LMH-R2-2
  verify_r2.sh /path/to/Mike_SVMods --build
  verify_r2.sh /path/to/Mike_SVMods --phase RC-R2-1 --build

Phase IDs:
  EIW-R2-1  Split DebugKnobs config types
  EIW-R2-2  Split WeggVerifier helper types
  EIW-R2-3  Extract AnimalNotes dump models
  EIW-R2-4  AnimalNotesOrchestrator partial split
  LMH-R2-1  Split ItemManager Eggs region
  LMH-R2-2  Split ItemManager Items region
  RC-R2-1   Extract RoosterHelper debug record models
  RC-R2-2   Split RoosterHelper debug methods partial
  ALL       Run all phases (default)
USAGE
}

fail() { echo "❌ $1" >&2; exit 1; }
pass() { echo "✅ $1"; }

need_cmd() {
  command -v "$1" >/dev/null 2>&1 || fail "Missing required command: $1"
}

need_file() {
  [[ -f "$1" ]] || fail "Missing file: $1"
}

need_dir() {
  [[ -d "$1" ]] || fail "Missing dir: $1"
}

count_rg() {
  local pattern="$1"; shift
  local out
  out="$(rg -n "$pattern" "$@" 2>/dev/null || true)"
  if [[ -z "$out" ]]; then
    echo 0
  else
    printf '%s\n' "$out" | wc -l | tr -d ' '
  fi
}

expect_zero() {
  local name="$1"; local pattern="$2"; shift 2
  local n
  n="$(count_rg "$pattern" "$@")"
  [[ "$n" == "0" ]] || fail "$name (expected 0, got $n) pattern=$pattern"
  pass "$name"
}

expect_nonzero() {
  local name="$1"; local pattern="$2"; shift 2
  local n
  n="$(count_rg "$pattern" "$@")"
  [[ "$n" != "0" ]] || fail "$name (expected >0, got 0) pattern=$pattern"
  pass "$name"
}

expect_exact() {
  local name="$1"; local expected="$2"; local pattern="$3"; shift 3
  local n
  n="$(count_rg "$pattern" "$@")"
  [[ "$n" == "$expected" ]] || fail "$name (expected $expected, got $n) pattern=$pattern"
  pass "$name = $expected"
}

parse_args() {
  local seen_root=0
  while [[ $# -gt 0 ]]; do
    case "$1" in
      -h|--help)
        usage
        exit 0
        ;;
      --build)
        DO_BUILD=1
        shift
        ;;
      --phase)
        [[ $# -ge 2 ]] || fail "--phase requires an ID"
        PHASES+=("$2")
        shift 2
        ;;
      --phase=*)
        PHASES+=("${1#--phase=}")
        shift
        ;;
      --*)
        fail "Unknown option: $1"
        ;;
      *)
        if [[ $seen_root -eq 0 ]]; then
          ROOT="$1"
          seen_root=1
          shift
        else
          fail "Unexpected argument: $1"
        fi
        ;;
    esac
  done

  if [[ ${#PHASES[@]} -eq 0 ]]; then
    PHASES=("ALL")
  fi
}

run_build_gate() {
  [[ $DO_BUILD -eq 1 ]] || return 0
  need_cmd dotnet

  echo
  echo "== dotnet build gate =="

  dotnet build NestCore/NestCore.csproj
  dotnet build EggsInWild/EggsInWild.csproj
  dotnet build LetMeHatch/LetMeHatch.csproj
  dotnet build RoosterControl/RoosterControl.csproj
  dotnet build Mike_SVMods.sln

  pass "dotnet build gate passed"
}

# -----------------------------
# Phase implementations
# -----------------------------

verify_eiw_r2_1() {
  echo
  echo "== EIW-R2-1: DebugKnobs =="

  need_file "EggsInWild/Infrastructure/Config/DebugKnobs.cs"
  need_dir  "EggsInWild/Infrastructure/Config/DebugKnobs"

  local DBG_CLASSES=(
    DebugKnobsConfig
    DesertTombKnobs
    DesertTombGenerationKnobs
    DesertTombSpawningKnobs
    DesertTombPopulationKnobs
    DesertTombLootLuckKnobs
    DesertTombBreakablesKnobs
    SpawningChanceModifiersKnobs
    WeightingKnobs
    MonstersKnobs
    PharaohKnobs
    SandiKnobs
    CrawlerKnobs
    HurricaneKnobs
    MatangoKnobs
  )

  expect_zero \
    "EIW-R2-1 no moved config types remain in DebugKnobs.cs" \
    "class ($(IFS='|'; echo "${DBG_CLASSES[*]}"))\\b" \
    EggsInWild/Infrastructure/Config/DebugKnobs.cs

  expect_exact \
    "EIW-R2-1 moved config types exist in DebugKnobs/ folder" \
    "15" \
    "^\\s*(internal|public)?\\s*(sealed\\s+)?class ($(IFS='|'; echo "${DBG_CLASSES[*]}"))\\b" \
    EggsInWild/Infrastructure/Config/DebugKnobs -g'*.cs'

  expect_nonzero \
    "EIW-R2-1 DebugKnobs.cs still declares DebugKnobs" \
    "class\\s+DebugKnobs\\b" \
    EggsInWild/Infrastructure/Config/DebugKnobs.cs
}

verify_eiw_r2_2() {
  echo
  echo "== EIW-R2-2: WeggVerifier =="

  need_file "EggsInWild/Infrastructure/Diagnostics/WeggVerifier.cs"
  need_dir  "EggsInWild/Infrastructure/Diagnostics/WeggVerify"

  local WEGG_TYPES=(
    WeggVerifyOptions
    WeggVerifyResult
    WeggVerifyStatus
    WeggVerifyCheck
    WeggVerifyReport
    IContentPatcherApiProbe
    WeightRow
    WeightRowSet
  )

  expect_zero \
    "EIW-R2-2 no helper types remain in WeggVerifier.cs" \
    "^(public|internal)?\\s*(record|class|enum|interface)\\s+($(IFS='|'; echo "${WEGG_TYPES[*]}"))\\b" \
    EggsInWild/Infrastructure/Diagnostics/WeggVerifier.cs

  expect_exact \
    "EIW-R2-2 helper types exist in WeggVerify/ folder" \
    "8" \
    "^(public|internal)?\\s*(record|class|enum|interface)\\s+($(IFS='|'; echo "${WEGG_TYPES[*]}"))\\b" \
    EggsInWild/Infrastructure/Diagnostics/WeggVerify -g'*.cs'

  expect_nonzero \
    "EIW-R2-2 WeggVerifier.cs still declares WeggVerifier" \
    "class\\s+WeggVerifier\\b" \
    EggsInWild/Infrastructure/Diagnostics/WeggVerifier.cs
}

verify_eiw_r2_3() {
  echo
  echo "== EIW-R2-3: AnimalNotes dump models =="

  need_file "EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.cs"
  need_file "EggsInWild/Application/AnimalNotes/DumpModels.cs"

  local DUMP_TYPES=(
    DumpModel
    DumpConfigModel
    DumpFarmerModel
    DumpRecordModel
    DumpRecordFarmerModel
  )

  expect_zero \
    "EIW-R2-3 dump models removed from orchestrator" \
    "class ($(IFS='|'; echo "${DUMP_TYPES[*]}"))\\b" \
    EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.cs

  expect_exact \
    "EIW-R2-3 dump models present in DumpModels.cs" \
    "5" \
    "class ($(IFS='|'; echo "${DUMP_TYPES[*]}"))\\b" \
    EggsInWild/Application/AnimalNotes/DumpModels.cs
}

verify_eiw_r2_4() {
  echo
  echo "== EIW-R2-4: AnimalNotesOrchestrator partials =="

  need_file "EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.cs"
  need_file "EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.Api.cs"
  need_file "EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.ClientEvents.cs"
  need_file "EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.GameEvents.cs"
  need_file "EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.InitAndSave.cs"
  need_file "EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.Reconcile.cs"
  need_file "EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.PendingProcessing.cs"
  need_file "EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.FarmerState.cs"

  expect_nonzero \
    "EIW-R2-4 orchestrator is partial" \
    "public\\s+sealed\\s+partial\\s+class\\s+AnimalNotesOrchestrator\\b" \
    EggsInWild/Application/AnimalNotes/AnimalNotesOrchestrator.cs

  local AN_METHODS=(
    DumpState Repair ForceRecomputeStates
    TryHandleClientNoteFound TryHandleClientQuestCompleted
    OnSaveLoaded OnSaving OnDayStarted OnDayEnding OnWarped HandleLocation
    ShouldRun EnsureInitialized ValidateSaveData Save GetEligibleAnimalTypes RefreshAnimalsOnFarm GetAnimalsOnFarm IsCoopOrBarn FindNewHatchedAnimals
    ToHashSetDict ReconcileState CreateUniqueToken EnsureUniqueTokens UpdateHomeReferencesFromCurrentAnimals ReconcileExistingNotesInAnimalHouses TryFindAnimalIdInHouse IsJunimoNoteId ExtractAnimalTokenFromNoteId NormalizeFlagsFromWorldState DeduplicatePending
    TranslateOrFallback ProcessPending TrySpawnNotes ResolveAnimalHome GetNoteBlockedReason GetQuestBlockedReason GetMailBlockedReason BuildCleanupBlockers ResetPlacementFailures RecordPlacementFailure TryFindPlacementTile
    TryIssueUnlockMailForAllFarmers TrackQuestCompletionsForAllFarmers SafeGetAllFarmers GetOrCreateFarmerState TryAddQuestToFarmer HasCompletedQuest HasReceivedMail HasMailForTomorrow AddMailForTomorrow TryGetDaysToMature CleanupCompleted
  )

  for m in "${AN_METHODS[@]}"; do
    local n
    n="$(rg -n "^[[:space:]]*(public|internal|private|protected).*[[:space:]]${m}[[:space:]]*\\(" \
      EggsInWild/Application/AnimalNotes -g'AnimalNotesOrchestrator*.cs' 2>/dev/null | wc -l | tr -d ' ')"
    [[ "$n" == "1" ]] || fail "EIW-R2-4 method signature count for ${m} (expected 1, got $n)"
  done

  pass "EIW-R2-4 all orchestrator method signature counts are 1"
}

verify_lmh_r2_1() {
  echo
  echo "== LMH-R2-1: ItemManager Eggs region =="

  need_file "LetMeHatch/Cloning/ItemManager.cs"
  need_file "LetMeHatch/Cloning/ItemManager.Eggs.cs"

  expect_zero \
    "LMH-R2-1 ItemManager.cs has no Eggs region" \
    "#region Eggs" \
    LetMeHatch/Cloning/ItemManager.cs

  expect_nonzero \
    "LMH-R2-1 Eggs region exists in ItemManager.Eggs.cs" \
    "#region Eggs" \
    LetMeHatch/Cloning/ItemManager.Eggs.cs

  expect_nonzero \
    "LMH-R2-1 ItemManager is partial (base file)" \
    "partial\\s+class\\s+ItemManager\\b" \
    LetMeHatch/Cloning/ItemManager.cs
}

verify_lmh_r2_2() {
  echo
  echo "== LMH-R2-2: ItemManager Items region =="

  need_file "LetMeHatch/Cloning/ItemManager.cs"
  need_file "LetMeHatch/Cloning/ItemManager.Items.cs"
  need_file "LetMeHatch/Cloning/ItemManager.Eggs.cs"

  expect_zero \
    "LMH-R2-2 ItemManager.cs has no Items/Eggs regions" \
    "#region (Items|Eggs)" \
    LetMeHatch/Cloning/ItemManager.cs

  expect_nonzero \
    "LMH-R2-2 Items region exists in ItemManager.Items.cs" \
    "#region Items" \
    LetMeHatch/Cloning/ItemManager.Items.cs

  expect_nonzero \
    "LMH-R2-2 Eggs region exists in ItemManager.Eggs.cs" \
    "#region Eggs" \
    LetMeHatch/Cloning/ItemManager.Eggs.cs

  expect_nonzero \
    "LMH-R2-2 ItemManager is partial (base file)" \
    "partial\\s+class\\s+ItemManager\\b" \
    LetMeHatch/Cloning/ItemManager.cs
}

verify_rc_r2_1() {
  echo
  echo "== RC-R2-1: RoosterHelper debug record models =="

  need_file "RoosterControl/Helpers/RoosterControl.cs"
  need_file "RoosterControl/Helpers/RoosterDebugModels.cs"

  local RC_RECORDS=(
    DebugProduceDump
    DebugProduceCacheStats
    DebugWeightAnomaly
    DebugProduceSlotDump
    DebugResolutionStep
  )

  expect_zero \
    "RC-R2-1 debug record models removed from RoosterControl.cs" \
    "record ($(IFS='|'; echo "${RC_RECORDS[*]}"))\\b" \
    RoosterControl/Helpers/RoosterControl.cs

  expect_exact \
    "RC-R2-1 debug record models present in RoosterDebugModels.cs" \
    "5" \
    "record ($(IFS='|'; echo "${RC_RECORDS[*]}"))\\b" \
    RoosterControl/Helpers/RoosterDebugModels.cs
}

verify_rc_r2_2() {
  echo
  echo "== RC-R2-2: RoosterHelper debug methods partial =="

  need_file "RoosterControl/Helpers/RoosterControl.cs"
  need_file "RoosterControl/Helpers/RoosterHelper.Debug.cs"

  expect_nonzero \
    "RC-R2-2 RoosterHelper is partial (base file)" \
    "public\\s+sealed\\s+partial\\s+class\\s+RoosterHelper\\b" \
    RoosterControl/Helpers/RoosterControl.cs

  local RC_METHODS=(
    LogOncePerDay
    MaybeLogDailyProduceSummary
    DebugDumpChosenProduce
    BuildSlotDump
  )

  for m in "${RC_METHODS[@]}"; do
    local n
    n="$(rg -n "^[[:space:]]*(public|internal|private|protected).*[[:space:]]${m}[[:space:]]*\\(" \
      RoosterControl/Helpers -g'*.cs' 2>/dev/null | wc -l | tr -d ' ')"
    [[ "$n" == "1" ]] || fail "RC-R2-2 method signature count for ${m} (expected 1, got $n)"
  done

  pass "RC-R2-2 all RoosterHelper debug method signature counts are 1"
}

run_phase() {
  local id="$1"
  case "$id" in
    ALL)
      verify_eiw_r2_1
      verify_eiw_r2_2
      verify_eiw_r2_3
      verify_eiw_r2_4
      verify_lmh_r2_1
      verify_lmh_r2_2
      verify_rc_r2_1
      verify_rc_r2_2
      ;;
    EIW-R2-1) verify_eiw_r2_1 ;;
    EIW-R2-2) verify_eiw_r2_2 ;;
    EIW-R2-3) verify_eiw_r2_3 ;;
    EIW-R2-4) verify_eiw_r2_4 ;;
    LMH-R2-1) verify_lmh_r2_1 ;;
    LMH-R2-2) verify_lmh_r2_2 ;;
    RC-R2-1)  verify_rc_r2_1 ;;
    RC-R2-2)  verify_rc_r2_2 ;;
    *)
      fail "Unknown --phase ID: $id (use --help)"
      ;;
  esac
}

main() {
  parse_args "$@"

  need_cmd rg
  need_cmd wc

  cd "$ROOT" || fail "Unable to cd to repo root: $ROOT"

  echo "== R2 verification runner =="
  echo "Repo root: $(pwd)"
  echo "Selected phases: ${PHASES[*]}"
  echo "Build gate: $([[ $DO_BUILD -eq 1 ]] && echo enabled || echo disabled)"

  for p in "${PHASES[@]}"; do
    run_phase "$p"
  done

  pass "All selected R2 grep gates passed"

  run_build_gate

  echo
  pass "Done"
}

main "$@"

