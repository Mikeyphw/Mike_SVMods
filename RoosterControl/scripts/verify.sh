#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SOLUTION="${ROOT}/Mike_SVMods.sln"
TEST_PROJ="${ROOT}/Tests/RoosterControl.Tests/RoosterControl.Tests.csproj"

log() { printf '%s\n' "$*"; }

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found. Install the .NET SDK (repo requires modern .NET; e.g. SDK 8.x)." >&2
  exit 127
fi

log "[1/3] Restore"
dotnet restore "$SOLUTION" -v minimal

log "[2/3] Build (Release)"
if ! dotnet build "$SOLUTION" -c Release -v minimal -clp:Summary; then
  log "Build failed; rerunning with ErrorsOnly so errors aren't buried by warnings..."
  dotnet build "$SOLUTION" -c Release -v minimal -clp:ErrorsOnly
  exit 1
fi

log "[3/3] Tests (RoosterControl.Tests)"
if ! dotnet test "$TEST_PROJ" -c Release -v minimal; then
  log "Tests failed; rerunning with normal verbosity..."
  dotnet test "$TEST_PROJ" -c Release -v normal
  exit 1
fi

log "OK"
