# R05a — .NET 10 hosted SDK compatibility pilot (net6 projects)

**Source base:** `299c74543708de0fc67c991e4d68440e297aa741`.
**Scope:** GitHub Actions only; no `dotnet` executed by Devtool overlay preflight or validation.

The Stardew Valley/SMAPI project target framework remains `net6.0`. The opt-in
`.github/workflows/r05-hosted-dotnet-sdk10.yml` workflow chooses the .NET 10 SDK
from its **local** `.github/r05-sdk10/global.json`, while installing the .NET 6
runtime required by the test assemblies. It performs fresh restores (no
cross-run NuGet cache), runs the two currently game-independent test projects,
and uploads TRX receipts. Receipt verification rejects missing, empty, failed,
inconclusive, skipped, or non-passing test results. The existing CI .NET 6 lane
is unchanged, to preserve a known-good control while the new SDK is tested.

## Start and inspect the real hosted qualification

```bash
cd "$HOME/Code/Mike_SVMods"
git push origin HEAD
gh workflow run r05-hosted-dotnet-sdk10.yml -R Mikeyphw/Mike_SVMods --ref main
gh run list -R Mikeyphw/Mike_SVMods --workflow r05-hosted-dotnet-sdk10.yml --limit 5
# After selecting the correct run ID:
gh run watch RUN_ID -R Mikeyphw/Mike_SVMods --exit-status
```

Only an actual hosted run for the applied commit may qualify this pilot.
Do not claim full R05 acceptance based on this source-only contract.

## Gating and deferred work

- **Pilot:** SDK 10 selection, net6 runtime, 2 game-independent projects,
  test discovery count, and immutable TRX run evidence — pending real CI.
- **All eight Tests.sln projects:** not yet qualified. Game-dependent tests
  may need legitimately provided game and SMAPI assemblies; do not package
  proprietary game files into public Actions artifacts.
- **Locked NuGet restore:** not qualified. The project presently uses
  heterogeneous explicit package versions, and tracked lockfiles must be
  generated and verified by GitHub Actions before enabling locked mode.
- **Coverage and central package management:** deferred until compatibility
  and restore determinism have concrete hosted evidence.
- **Devtool hosted native provider:** reserved for R06; no change to Devtool
  or wrapper workflow execution in this pilot.
