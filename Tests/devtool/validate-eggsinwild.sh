#!/data/data/com.termux/files/usr/bin/bash
set -euo pipefail
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
dotnet test Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj -c Release
dotnet test Tests/EggsInWild.Tests/EggsInWild.Tests.csproj -c Release
dotnet test Tests/EggsInWild.Phase7.Tests/EggsInWild.Phase7.Tests.csproj -c Release
