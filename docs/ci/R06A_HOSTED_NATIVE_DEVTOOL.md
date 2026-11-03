# R06a — hosted native Devtool source workflow

## Qualification boundary

Source baseline: `203b75d48d1116193de84a30eaf62a684ed3c0aa`. Pinned Devtool runtime: `b164463db51e2cffd12f753043b9e55a08cc43ae`.

This phase adds **real native Devtool** plan, preflight, and invoke execution to a separate, manually dispatched GitHub Actions workflow. It executes only the existing `svmods/ci-preflight` target-local `ci-source` Python command. It never restores, builds, tests, or packages .NET locally or as part of this new Devtool job. The existing R05c locked .NET jobs remain the independent hosted .NET gate.

The workflow checks out the exact consumer commit, clones the frozen Devtool commit, installs it in a temporary isolated Python venv on hosted Ubuntu, plans the job DAG, checks toolchain preflight, and invokes the source job through Devtool's **actual** runtime. The machine result is checked for a successful, nonempty source DAG; plan, preflight, invocation, and run-scoped checksums are published even when the run fails.

The Python CI source-only gate is a contract check, not evidence that GitHub execution has occurred. Success of the dispatched run and its receipt are required for R06a acceptance.

## Commands (GitHub Actions only)

```zsh
cd ~/Code/Mike_SVMods
gh workflow run r06a-native-devtool-source.yml -R Mikeyphw/Mike_SVMods --ref main
gh run list -R Mikeyphw/Mike_SVMods --workflow r06a-native-devtool-source.yml --limit 5
```

## Next

R06b integrates a reviewed native .NET job on GitHub Actions after R06a reaches an accepted hosted execution receipt. Devtool RM-09's broad native .NET provider requires every relevant project lock and root SDK policy; only two independent project locks exist today, so do not claim full .NET provider admission. R06a is intentionally source-only and does not require a machine with SMAPI game DLLs.
