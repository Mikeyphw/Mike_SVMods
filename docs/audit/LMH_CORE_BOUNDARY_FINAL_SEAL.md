# LetMeHatch Core Boundary — Final Convergence Seal

This seal closes the four-overlay Core-boundary remediation campaign.

## Delivered sequence

1. Core purity validator truth: source-level runtime namespace detection replaced the old arbitrary-string check.
2. Breeding boundary cleanup: SMAPI/Stardew-dependent breeding execution and live-world traversal moved to Infrastructure.
3. Cloning boundary cleanup: FarmAnimalData/XNA-dependent clone mapping, transformation, and color resolution moved to Infrastructure.
4. Final convergence seal: the Core validator is mandatory in the canonical test entry point and complete Devtool validation is the release gate.

## Boundary contract

`LetMeHatch/Core` is the domain layer. C# source under that tree must not reference runtime namespaces from `StardewValley`, `StardewModdingAPI`, or `Microsoft.Xna.Framework`. Runtime adapters belong outside Core, principally under `LetMeHatch/Infrastructure`. The validator also retains the NestCore duplicate-regression checks.

## Canonical validation

```sh
devtool --copy -r "$PWD" --target svmods validate
```

The `svmods` target supplies `/data/data/com.termux/files/home/stardew/game` as `GamePath` through `.devtool.toml`. `Tests/run-tests.sh` is the canonical test entry point. It now fails closed if `LetMeHatch/Tools/validate-core.sh` is missing or not executable, then runs the complete `Tests/Tests.sln` Release test suite.

## Seal criteria

The final gate is green only when Devtool restore and build succeed, the mandatory Core purity/NestCore duplicate validator succeeds, the complete Release test suite succeeds, and Devtool reports zero validation errors. Any failure means the campaign is not sealed.
