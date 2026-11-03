#!/usr/bin/env bash
set -euo pipefail
# Portable when explicitly invoked with bash on Termux, Linux, or other Bash hosts.
REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
bash "$REPO_ROOT/LetMeHatch/Tools/validate-core.sh"
dotnet test Tests/LetMeHatch.Core.Tests/LetMeHatch.Core.Tests.csproj -c Release
dotnet test Tests/LetMeHatch.Tests/LetMeHatch.Tests.csproj -c Release
