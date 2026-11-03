# R03 — Devtool target ownership, source-only dependency graph

Source baseline: `Mikeyphw/Mike_SVMods@928dd6e0c9824d3937034294b83e00005c814c0b`.

## Purpose

Previous `root = "."` values made every configured target own every path. R03
assigns real roots to the eight existing targets and narrows explicit `inputs`
to shared metadata, per-mod tests, and genuinely linked C# source. The `svmods`
aggregate is rooted in `Tests` (not the entire repository) and propagates
changes through a new *source-only* `ownership-graph` workflow. `NestCore` and
`tools/EmbedTextAsCode` are explicit prerequisites for the three SMAPI mods.

`pure` is a compatibility alias for the pure-test target and continues to be
selectable. It remains intentionally overlapping `roostercontrol_pure` until a
separate migration can retire the alias without breaking consumers.

The new `target-ownership-contract` job is Python-only, read-only, and rooted in
the repository. The existing `ci-source` target-local job is explicitly rooted
one directory above its new `svmods` root so the working directory remains the
repository root.

## What remains unchanged

- All .NET 6 project files, game build semantics, existing validation jobs and
  workflows, wrapper commands, and Gradle/Android setup
- GitHub-hosted .NET restore/build/test/packaging only; no local .NET execution
- R02b automatic execution environment and absence of hardcoded `GamePath`

## Validations

Local source-only:

```sh
python3 scripts/devtool_target_ownership.py --json
python3 -m unittest discover -s scripts/tests -p 'test_*.py' -v
python3 scripts/devtool_actions_contract.py
```

Read-only real Devtool planning after a successful apply (run in Termux; these
commands do NOT compile or test .NET projects):

```sh
devtool -r "$PWD" affected --explain
devtool -r "$PWD" plan ownership-graph --target svmods --explain
devtool -r "$PWD" plan ownership-graph --target eggsinwild --json
```

`affected` without a base/head examines local Git changes. After the automatic
commit, use `devtool -r "$PWD" affected --base HEAD^ --head HEAD --json` if you
want the R03 commit range; that commit intentionally touches shared Devtool
configuration and therefore selects all targets. The Python fixture matrix
simulates individual file changes and checks that EggInWild changes only affect
its mod plus the aggregate, while NestCore/generator changes propagate to
consumers. These Python fixtures **do not replace** Devtool's real affected
engine or a hosted run.

## Conservative and outstanding

Unknown/unowned paths still select all targets. `Directory.Build.*` and CI
workflow changes are shared owner inputs, so their edits intentionally select
all targets. Since the embedding generator may write shared `obj` data,
concurrent .NET jobs need R07 write-scope validation. R04 will migrate job
ownership to target-local definitions, and R06 will run actual Devtool
orchestration inside GitHub Actions.

Any .NET command in existing Devtool target definitions remains **hosted-only by
operator policy**. Do not invoke `devtool test`, `validate`, `build`, or
`ci-local` locally when they include .NET; use the plan and source-only
`ownership-graph` commands above.

If the Devtool configuration loader/plan rejects any ownership definitions,
report the exact output. Do not bypass the gate or claim R03 accepted.
