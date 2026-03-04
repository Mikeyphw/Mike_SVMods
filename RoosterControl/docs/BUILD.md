# Build & Test (Linux)

## Quick verify
From repo root:

```bash
bash RoosterControl/scripts/verify.sh
```

## What it does
- `dotnet restore` the solution
- `dotnet build -c Release -v minimal`
  - if build fails, reruns with `ErrorsOnly` console logger so errors aren’t buried by warnings
- `dotnet test` RoosterControl unit tests

## Useful commands
```bash
dotnet build Mike_SVMods.sln -c Release -v minimal
dotnet test Tests/RoosterControl.Tests/RoosterControl.Tests.csproj -c Release -v minimal
```
