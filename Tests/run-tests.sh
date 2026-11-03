#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TESTS_DIR="${ROOT_DIR}/Tests"

echo "== Validate Core (LMH) =="
CORE_VALIDATOR="${ROOT_DIR}/LetMeHatch/Tools/validate-core.sh"
if [[ ! -f "${CORE_VALIDATOR}" ]]; then
  echo "ERROR: required Core validator is missing: ${CORE_VALIDATOR}" >&2
  exit 1
fi
# Use the discovered Bash executable; do not rely on /usr/bin/env existing
# in native Termux or on the Git executable bit of checked-out scripts.
bash "${CORE_VALIDATOR}"

echo "== dotnet test =="
dotnet test "${TESTS_DIR}/Tests.sln" -c Release
