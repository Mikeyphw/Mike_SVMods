# R04 — Canonical target-local Devtool test jobs

Source baseline: `Mikeyphw/Mike_SVMods@757b7f320b4d06d340b75bf2254b95f9664ce725`.

This migration rehomes ten existing repository-wide command jobs to the owning
`[targets.<target>.jobs.*]` tables. No .NET command, argument, test selection,
or target input/output scope is intentionally changed. Existing jobs use
`root = ".."` relative to their owning one-level-deep target root, preserving
the previous repository-root working directory. Their `env`, `inputs` and
`outputs` are carried across verbatim. The three mod-level `test_command`
paths are corrected to be relative to the scoped target root.

| Target | New local job keys | Workflow |
|---|---|---|
| `svmods` | `core_purity`, `tests_sln` | `validate`, `ci-local` |
| `letmehatch` | `core_purity`, `core_tests`, `mod_tests` | `validate` |
| `eggsinwild` | `animalnotes`, `unit`, `phase7` | `validate` |
| `roostercontrol` | `descriptors`, `unit` | `validate` |
| `roostercontrol_pure` | `unit` (new) | `validate` (new) |
| `pure` | no duplicate .NET job | delegates to `roostercontrol_pure#validate` |

The global `jobs.target-ownership-contract` intentionally remains shared and
read-only because the ownership-probe workflow of several independent targets
uses it. `targets.svmods.jobs.ci-source` remains target-local and read-only.

## Local safety policy

**Never run .NET restore, build, test, or packaging locally** (including
Termux). Only invoke Devtool `apply-overlay`, source-only Python validators,
and read-only inspection/plan commands on your Termux checkout. Even though
.NET workflows are defined in `.devtool.toml`, they are for execution on
**GitHub Actions** (to be wired more fully in R06). Do **not** invoke
`devtool validate` or `devtoolw ci-local` from Termux.

Devtool applies the migration in an `after_extract` hook, with `.devtool.toml`
explicitly declared as its only write path. The hook refuses incorrect Git
HEAD, repository identity, original config Git blob, or unexpected schema.
Devtool must complete the Python-only validation script before committing.

## Source-only verification (Termux)

```sh
python3 scripts/devtool_target_jobs.py --json
python3 scripts/devtool_target_ownership.py --json
python3 scripts/devtool_actions_contract.py
python3 -m unittest discover -s scripts/tests -p 'test_*.py'
```

## Read-only Devtool views

```sh
devtool -r "$PWD" jobs --target eggsinwild --json
devtool -r "$PWD" jobs --target roostercontrol_pure --json
devtool -r "$PWD" plan validate --target eggsinwild --json
```

These are inspection/planning operations; no local .NET test command should be
launched.

## Acceptance and limitations

GitHub Actions `pure-tests` remains the existing hosted .NET gate and the
`source-contracts` Python job now checks target-local wiring. The extended
per-mod .NET validation in GitHub Actions, provider reproducibility, hosted
Devtool orchestration, and incremental scheduling require R05–R08. This
phase does not claim that all SMAPI integration test projects can run without
Stardew Valley/SMAPI runtime dependencies.
