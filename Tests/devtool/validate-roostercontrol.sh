#!/data/data/com.termux/files/usr/bin/bash
set -euo pipefail
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
dotnet test Tests/RoosterControl.Descriptors.Tests/RoosterControl.Descriptors.Tests.csproj -c Release
dotnet test Tests/RoosterControl.Tests/RoosterControl.Tests.csproj -c Release
