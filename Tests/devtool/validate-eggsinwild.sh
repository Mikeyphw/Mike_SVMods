#!/usr/bin/env bash
set -euo pipefail
# Portable when explicitly invoked with bash on Termux, Linux, or other Bash hosts.
REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
dotnet test Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj -c Release
dotnet test Tests/EggsInWild.Tests/EggsInWild.Tests.csproj -c Release
dotnet test Tests/EggsInWild.Phase7.Tests/EggsInWild.Phase7.Tests.csproj -c Release
