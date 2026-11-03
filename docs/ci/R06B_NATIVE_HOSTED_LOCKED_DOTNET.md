# R06b — Hosted native Devtool orchestration of locked .NET jobs

**Source:** `Mikeyphw/Mike_SVMods` commit `1ce2100f0b7a94114e5562ce53af9c8e279bba16` (R06a accepted).

**Frozen Devtool runtime:** `Mikeyphw/devtool` commit `b164463db51e2cffd12f753043b9e55a08cc43ae`.

**R06a evidence:** GitHub Actions run 41676501509 (success); baseline CI 41676477680 (success).

## Implementation

- Adds `svmods/ci-hosted-dotnet`, an explicit 5-node Devtool DAG with local jobs:
  1. `ci-source` (the existing Python contract, read-only),
  2. `r06b_restore_pure` (`dotnet restore ... --locked-mode`),
  3. `r06b_test_pure` (`dotnet test ... --no-restore`, TRX),
  4. `r06b_restore_animal` (`dotnet restore ... --locked-mode`),
  5. `r06b_test_animal` (`dotnet test ... --no-restore`, TRX).
- Explicit edges serialize the two restores/tests to avoid unsafe shared NuGet/MSBuild mutation.
- The four jobs have declared inputs/outputs scoped to the repository-root `root = ".."` relative to `svmods` target's `Tests` root.
- This workflow is **not** registered as a local wrapper command, to avoid accidentally running .NET in Termux.
- Adds `.github/workflows/r06b-native-devtool-dotnet.yml` as a manual, read-only, hosted Ubuntu job with .NET SDK 10 + .NET 6 runtime provisioned.
- The workflow checks source identities, runs native `plan`, `preflight`, `invoke`, verifies the five nodes and both passing TRX receipts, and uploads machine evidence regardless of success/failure.
- Existing baseline .NET CI, locked NuGet packages and R06a remain unchanged.

## Qualification and limitations

This is **real hosted Devtool workflow execution**, orchestrating four `runner = "command"` .NET jobs. It is **not yet qualification of Devtool's RM-09 native .NET provider** or a full game-dependent SMAPI build. Lockfile coverage remains scoped to `RoosterControl.PureTests` and `EggsInWild.AnimalNotes.Tests`. SDK installation uses `10.0.x` hosted resolution, not byte-for-byte toolchain reproducibility. Devtool dependencies installed by pip are also not yet fully hashed/pinned. Artifact upload is evidence, not release attestation.

## Apply on Termux (Python-only validation)

```zsh
cd ~/Code/Mike_SVMods
ZIP="$HOME/storage/downloads/MIKE_SVMODS_R06B_NATIVE_LOCKED_DOTNET_1ce2100.overlay.zip"
devtool -r "$PWD" --target svmods apply-overlay "$ZIP"
git log -1 --oneline
git status --short
git push origin HEAD
```

## Hosted execution

```zsh
REPO=Mikeyphw/Mike_SVMods
WF=r06b-native-devtool-dotnet.yml
gh workflow run "$WF" -R "$REPO" --ref main
gh run list -R "$REPO" --workflow "$WF" --limit 3
```

After hosted acceptance, R06c may adopt Devtool's stricter native .NET provider under the provider's own source and lockfile requirements. Never run .NET locally on Termux.
