# CI regression fix — CatalogApplyMode

Source baseline: `Mikeyphw/Mike_SVMods@04d931e4f77a2c0b89640c013a1013f707438281`.
GitHub Actions reference: run `41557673116` (3 failures, 13 passes).

The fix addresses the behavior of the existing `CatalogApplyMode` contract:

- `SeedOnly`: **existing** configurations must not be changed (even missing rooster entries). New configurations still receive catalog values.
- `RepairInvalid`: restores an empty `BlueEggChoice` from the catalog if one exists, without replacing valid user-provided choices.
- `Authoritative`: applies *all supplied* hatching general and rooster-specific values, not just enums; missing nullable overrides preserve existing user values.
- `MergeMissing`: existing merge and repair behavior remains unchanged.

## Acceptance in GitHub Actions only

The source-pinned overlay includes five **new C# regression tests** in
`Tests/RoosterControl.PureTests/CatalogApplyModePolicyRegressionTests.cs`,
plus the four original mode tests. The existing GitHub Actions workflow already
runs `dotnet test Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj`.

No .NET command is permitted on Termux. The Devtool overlay explicitly disables
restore/build/test/lint/package locally and executes **only Python** in its
embedded validator. Push the resulting commit, then verify both workflow jobs
and the test counts in GitHub Actions.

Until that hosted run passes, the patch is source-validated only; there is no
assertion that a C# compilation or test run has succeeded.
