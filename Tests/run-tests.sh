#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TESTS_DIR="${ROOT_DIR}/Tests"

echo "== Validate Core (LMH) =="
if [[ -x "${ROOT_DIR}/LetMeHatch/Tools/validate-core.sh" ]]; then
  "${ROOT_DIR}/LetMeHatch/Tools/validate-core.sh"
else
  echo "WARN: validate-core.sh not found/executable; skipping."
fi

echo "== dotnet test =="
dotnet test "${TESTS_DIR}/Tests.sln" -c Release
