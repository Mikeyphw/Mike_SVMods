#!/data/data/com.termux/files/usr/bin/bash
set -euo pipefail
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
./LetMeHatch/Tools/validate-core.sh
dotnet test Tests/LetMeHatch.Core.Tests/LetMeHatch.Core.Tests.csproj -c Release
dotnet test Tests/LetMeHatch.Tests/LetMeHatch.Tests.csproj -c Release
