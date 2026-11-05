# DN03-FINAL — cross-platform .NET evidence integration

## What is actually qualified?

Linux and Windows run the exact locked .NET test projects
`Tests/RoosterControl.PureTests` (21) and
`Tests/EggsInWild.AnimalNotes.Tests` (8). On each host:

1. Check the exact event source SHA and immutable Devtool code SHA.
2. Verify SDK `10.0.100` and .NET runtime `6.0.36`.
3. Perform NuGet locked-mode restore with an isolated cache.
4. Run `dotnet test` and generate an ordinary native TRX.
5. Have Devtool independently reparse the TRX counters, each UnitTestResult,
   duplicate execution IDs, failed/skipped cases, and source/run/attempt.
6. Upload exactly two typed JSON receipts.

A final job intentionally runs `if: always()` and fails if either native host
failed. It downloads **all four** expected receipts, rejects missing,
unexpected, cross-run or tampered identity/trust fields, and emits an explicit
58/58 gate. It cannot convert a skipped Windows job into a green required gate.

**No local evidence is labelled remotely authenticated or release qualified.**
R8 separately verifies and signs its *own* R7 remote observation, as before.

## Coverage

`devtool.actions_ci_qualification` supports Cobertura input and a
`--min-line-rate` threshold. It rejects malformed XML, DTD/entity declarations,
inconsistent line counters, missing class records, and missing required
reports. The test projects currently have no locked compatible coverage
collector configured, so **this overlay does not pretend to collect line
coverage**. Receipts state `coverage.collected=false`,
`coverage.status=NOT_COLLECTED`. Adding a version-pinned compatible collector
and updating NuGet locks requires a separate hosted dependency qualification;
only then may `--require-coverage` be enabled.

## Required checks after acceptance

Set as required (subject to successful hosted verification):

* `DN03-R7 / verify 29 TRX tests (non-release)`
* `DN03-FINAL / Linux+Windows 58 TRX executions gate (non-release)`

R8 is downstream `workflow_run`, so it is **not** a PR status check for branch
protection. R8 remains a mandatory *post-merge verification process*, not a
release approval. Do not require a job that only runs after a merge.

`BRANCH_RULESET.zsh` is explicit opt-in, create-only and rejects an existing
same-name ruleset. It does not alter preexisting branch rules.

## Rollback

Revert only the final consumer workflow/source/docs and the new Devtool provider
via ordinary PRs. Do not touch frozen R7/R8, R5/R6 sidecars or release signing.
