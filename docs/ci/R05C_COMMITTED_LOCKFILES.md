# R05c — Commit hosted NuGet locks and fail-closed CI restores

## Source qualification and provenance

The files `Tests/RoosterControl.PureTests/packages.lock.json` and
`Tests/EggsInWild.AnimalNotes.Tests/packages.lock.json` were imported **byte-for-byte**
from GitHub Actions run [41663506662](https://github.com/Mikeyphw/Mike_SVMods/actions/runs/41663506662),
artifact `r05b-lock-candidate-41663506662-1` (artifact ID `13241868660`).
The artifact was produced at source commit
`24cee7e92111348ed834cad344f80f8d3c88a101` by SDK `10.0.403`.
The hosted R05b job validated these lockfiles, then restored both test projects
using `--locked-mode --force --no-http-cache` with a **fresh second NuGet cache**.

The committed file SHA-256 digests and the artifact digest are recorded in
`docs/ci/R05C_LOCKFILE_PROVENANCE.json` and independently cross-checked by
`scripts/check_committed_nuget_locks.py` against current project references.
The checker and its regression fixtures perform **no .NET operations**.

## CI changes

The existing `.NET 6` pure-tests job now uses `dotnet restore --locked-mode`;
its test execution still uses `--no-restore`. A new isolated hosted job
validates the AnimalNotes lock under the `.NET 10` SDK and `.NET 6` runtime,
requires actual passing tests, and records a TRX receipt.
The source-contract job verifies the committed lockfiles and CI policy.

This is limited to the two game-independent test projects. It does **not** lock
all eight solution test projects or release builds and does not qualify any
SMAPI/game-dependent tests. Those require separate hosted game-runtime
qualification. The R05b candidate workflow is intentionally left intact as a
manual refresh/qualification tool.

## Dependency change procedure

Do not edit the generated lockfiles by hand or use `--force-evaluate` in the
locked CI jobs. If package versions change, generate new candidates in the
explicit GitHub Actions workflow, review the candidate manifest and new
digests, run a second clean-cache `--locked-mode` restore, then update both the
committed lockfiles and their provenance in a new source-pinned overlay.

## Acceptance

R05c is **not complete** until the push-triggered GitHub Actions jobs for the
exact source commit all pass. Never run local `dotnet restore`, `dotnet build`,
`dotnet test`, or `dotnet pack` on Termux for this campaign.
