# DN03-R5 — Mike_SVMods as a real RM-09 hosted consumer

This opt-in adoption does not rewrite `svmods/ci-hosted-dotnet`, normal CI,
existing Stardew Valley/game-dependent modules or NuGet lockfiles.

- `dn03_r5/native` contains **six direct RM-09 .NET restore/build/test nodes** with an
  explicit dependency chain, using real source projects and their committed locks.
- One node tests `RoosterControl.PureTests` (historical 21 tests); the second
  tests `EggsInWild.AnimalNotes.Tests` (historical 8 tests).
- `ci_dotnet_selected_project_only=true` explicitly restricts RM-09 static
  analysis to the selected tracked project and tracked `ProjectReference`
  closure, without changing the full Git source revision binding.
- Local static `Directory.Build.props` files are restricted to the three
  opted-in test/library subtrees, avoiding the unrelated game-dependent
  root props without changing other project builds.
- An exact `global.json` establishes the SDK 10.0.100 source pin;
  the generated exporter installs a SHA-512 verified standalone net6 runtime.
- A tracked empty lockfile for the package-free `RoosterControl.Pure` library
  completes the selected project-reference closure required by RM-09.
- `dist/pure/results.trx` and `dist/animal/results.trx` are separately
  declared, byte-hashed native outputs. R4 independently checks same-run
  host, output, and native typed receipts.

## Sequence: Devtool first, then this consumer

1. Apply and push the Devtool R5 overlay from the handoff.
2. Apply and auto-commit the Mike_SVMods R5 adoption overlay.
3. With **both checkouts present**, run `bash scripts/generate_dn03_r5.sh
   "$HOME/Code/devtool"`. It creates, verifies and commits the exact
   R4-generated workflow and review receipt using the pinned Devtool HEAD.
4. Push the Devtool branch **before** invoking GitHub Actions so the exact
   Devtool source SHA can be cloned by hosted jobs.
5. Push this Mike_SVMods branch and open a PR. Source-only qualification is
   possible before merging, but generated `workflow_dispatch` acceptance
   requires the generated workflow to be present on GitHub's default branch.
6. After reviewing and merging the consumer integration, dispatch the generated
   workflow in `Mikeyphw/Mike_SVMods` via `gh workflow run ... --ref main`.
7. Require all seven jobs (6 native RM-09 nodes + R4 cross-node observer)
   to pass, with both real TRX artifacts present. The R4 observer's receipt
   explicitly does NOT claim independent test report verification, remote
   attestation or release qualification.

No .NET command belongs on Termux; only Python source checks and export occur
there. CI acceptance occurs on GitHub Ubuntu hosted runners.
