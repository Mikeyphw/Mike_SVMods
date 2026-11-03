# R01 — SMAPI manifest and Devtool source contracts

This phase adds **read-only** source validation of the four shipped mod manifests,
CI gate wiring, declared C# target paths and existing wrapper workflow references.
It does not build the mods, access proprietary Stardew/SMAPI files, install tools,
or publish anything.

Local commands (Python 3.11+):

```bash
python3 scripts/validate_mod_manifests.py --json
python3 -m unittest discover -s scripts/tests -p 'test_*.py' -v
python3 scripts/devtool_actions_contract.py
```

The validator checks mod IDs, duplicate dependencies, stale internal ID aliases,
`EntryDll` vs project output, version syntax, Nexus update keys, and the minimum
SMAPI API version implied by required in-repository dependencies.

The updated CI source job executes all three commands. The preexisting `pure-tests`
job remains untouched until R06 replaces it with real Devtool-hosted acceptance.
The CI-source validator reports `hosted_devtool_execution=NOT_RUN` rather than
claiming a hosted Devtool run occurred.

## Deferred or unresolved

- `RoosterControl/manifest.json` has corrected `Mikew.lmh` (actual LetMeHatch ID),
  `Nexus:22555`, and minimum SMAPI 4.0.0 (required NestCore's minimum).
- LetMeHatch's README currently describes Content Patcher as required while the
  manifest declares it optional. Preserve the manifest behavior until a runtime
  audit resolves that contradiction.
- No target ownership/parallelism rewrite, Dotnet SDK/TFM migration, game tests,
  ZIP packaging, or release authorization occurs in R01.

SMAPI source documentation:
https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Manifest
https://wiki.stardewvalley.net/Modding:Modder_Guide/APIs/Update_checks
