# R05b — Hosted NuGet lockfile candidate and locked restore qualification

## Status

**Source workflow only:** Nothing in this overlay qualifies a hosted lockfile
candidate until `.github/workflows/r05b-nuget-lock-candidate.yml` has completed
successfully on the intended commit. This step does not commit lockfiles or
enable locked mode in baseline CI.

## What the opt-in workflow does

1. Checks out the exact `main` SHA without persistent GitHub credentials.
2. Uses the existing pinned SDK10 policy under `.github/r05-sdk10/global.json`.
3. Installs .NET SDK10 plus the .NET6 runtime on the GitHub-hosted runner.
4. Performs fresh NuGet restores with `--use-lock-file --force-evaluate`
   for the two game-independent test projects.
5. Verifies complete NuGet JSON lock entries, hashed package content, and
   direct PackageReference coverage, then copies the two test-project lockfiles
   to a run-scoped artifact directory and records SHA-256 receipts.
6. Restores those same projects with `--locked-mode` and a different, isolated
   NuGet packages cache, failing if any dependency lock or digest has changed.
7. Uploads the **qualified candidate** only when all steps pass.

The generated lockfiles are candidates. They must be reviewed and source-pinned
in a subsequent change before `--locked-mode` is enabled in baseline CI.
Library project lockfiles are intentionally not staged for commit; NuGet's
recommendation is to commit locks primarily for top-level application/test
entry points, not independent library project locks.

## Dispatch on GitHub (Termux CLI; runs no local .NET)

```zsh
REPO='Mikeyphw/Mike_SVMods'
WF='r05b-nuget-lock-candidate.yml'
gh workflow run "$WF" -R "$REPO" --ref main
gh run list -R "$REPO" --workflow "$WF" --limit 3
```

On success, use `gh run download RUN_ID -R "$REPO" -n
r05b-lock-candidate-RUN_ID-1 -D "$TMPDIR/r05b-candidate"` to inspect the
`manifest.json` and the two `locks/Tests/*/packages.lock.json` files.
Do not commit these without verifying their manifest source SHA matches the
checked-out source and their package contents are expected.

## Limitations

- No local .NET operations permitted. This is enforced by workflow placement,
  script-only local source gates and the opt-in workflow dispatch.
- Full game-dependent SMAPI tests remain out of scope until the hosted game
  runtime is available and qualified.
- The pilot's `.NET 10` SDK / `.NET 6` runtime combination does not itself
  imply that application/release build outputs are byte-reproducible.
- R05c must import and pin candidates and enforce locked mode on GitHub.
