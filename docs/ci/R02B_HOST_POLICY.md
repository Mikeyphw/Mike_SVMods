# R02b — host-safe Devtool execution configuration

Pinned source: Mike_SVMods `74c850d406807993017f01953f5dcb3955bb36f9`.

## Changes

* `execution.environment` changes from `native-termux` to `auto`, allowing
  Devtool to discover `native-termux` in Termux and `desktop-linux` on the
  GitHub Ubuntu runner rather than selecting an impossible host.
* The five absolute Termux `GamePath` declarations in `.devtool.toml` are
  removed. All other target configuration, DAG jobs, .NET 6 targets,
  `DOTNET_ROLL_FORWARD` flags, and scheduler settings remain untouched.
* A read-only Python policy checker and regression tests fail when a hardcoded
  `GamePath` or `native-termux` default returns.

## Game runtime access is separate

When game/SMAPI integration tests become eligible for hosted CI, provide a
legitimately available game directory **on that hosted runner** and pass its
path through the job's execution environment or explicit MSBuild property.
Do not commit local developer directories, fabricate game DLLs, or silently
skip game-required test cases. A portable configuration does not guarantee
full game-dependent .NET builds will succeed on GitHub without assets.

## Strict .NET execution boundary

**All actual .NET restore, build, test, and packaging must run exclusively in
GitHub Actions.** Termux validation for this overlay executes Python only.
`devtoolw ci-preflight` is source-only; DO NOT launch `ci-local`, per-mod
`validate`, or the direct test scripts locally, because they run .NET.
The R06 hosted Devtool workflow integration will make this CI execution
boundary fully enforceable and source-bound.

## Validation

```
python3 -m unittest discover -s scripts/tests -p 'test_*.py'
python3 scripts/devtool_host_policy.py
python3 scripts/devtool_actions_contract.py
```

The overlay uses an `after_extract` declared-write preflight, with exact Git
HEAD, remote identity, and source blob fingerprint guards. The `.devtool.toml`
change is tracked as a declared write in Devtool's transaction/rollback scope.
The artifact automatically creates a single Git commit after validation.

R03 will separately repair ownership/root scoping and the missing build graph
edges. No R03 changes are mixed into this R02b overlay.
