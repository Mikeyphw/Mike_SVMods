# R02a — portable script entry points

Source base: Mike_SVMods `9dd767cb64b98298f866c584977cf42829bbfff8`.

Changes: 3 test scripts use a portable Bash shebang and determine the checkout
root from `${BASH_SOURCE[0]}`. They continue to run the **same** .NET test
projects in the same order, but no longer depend on the calling directory.

`Tests/run-tests.sh` launches its mandatory Core purity validator through
`bash`, avoiding a dependency on `/usr/bin/env` in Termux. The Windows
`Tests/run-tests.ps1` now treats a missing Core validator and nonzero native
command exit codes as failures instead of silently skipping.

## Execution policy

**All real .NET restore, build, test, and packaging remains GitHub-Actions-only.**
Do not run the test scripts locally in Termux. The new
`scripts/tests/test_script_portability.py` uses `bash -n` and static assertions
only; it does not invoke any `dotnet` executable.

```bash
python3 -m unittest discover -s scripts/tests -p 'test_*.py'
python3 scripts/devtool_actions_contract.py
```

The repo-wide configuration still forces `native-termux` and injects an absolute
Termux `GamePath`; R02b must remove those host assumptions in a separately
source-pinned `.devtool.toml` migration. Do not claim R02 complete before R02b
and actual GitHub Actions acceptance.

## Acceptance

1. Python-only local validator succeeds; source-pin and inventory pass.
2. GitHub Actions `source-contracts` passes including the new source tests.
3. GitHub Actions `.NET pure-tests` remains passing at the new SHA.
4. Source semantics for all three per-mod shell scripts are unchanged apart
   from root resolution/interpreter selection.
5. R02b execution config and hosted workflow integration are still pending.
