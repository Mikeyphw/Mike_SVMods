# DN03-R8 — GitHub run/artifact provenance (non-release)

## Intent and trust distinctions

R7's R3/R4 verifiers reparse native TRX and correlate local source and receipt bytes. R8 is a **separate** GitHub API observer, not a substitute for R7. It fetches one completed, successful **main push** R7 run using GitHub's authenticated API; checks 8 expected jobs and 15 uploaded artifacts; downloads each GitHub ZIP and checks its GitHub-published SHA-256; correlates R4 and native node/host receipt bytes; and emits one bounded machine-readable report.

For trusted main-only `workflow_run` or manual dispatch, another job signs **that R8 report** using GitHub/Sigstore OIDC and verifies the signature. The attestation is about the R8 observer report, **not individual R7 archives, not a signed application release, not device acceptance**. The report explicitly retains `release_qualified=false`, `release_signing_verified=false` and `original_r7_artifacts_individually_sigstore_attested=false`.

## Why a new workflow rather than editing R7

R7's original job cannot query its own final `completed` status while executing. A distinct `workflow_run: completed` observer allows GitHub API checks on a fully finished R7 run. The R7 required check, and R5/R6 rollback workflows, are untouched. R8 is optional until positive and negative hosted acceptance of R8 itself.

## Events and containment

- `pull_request` affecting R8 files: read-only positive replay against frozen successful main run `42030323139` / source `5dc04f83acce009be8058e1ac5bab3d3aaaf6da2`, without signing permissions.
- `workflow_run`: only events for the R7 workflow on `main`; the verifier requires the originating run was a `push`, was completed and successful, and belongs to the expected repository/branch/commit/attempt. A failed R7 cannot be promoted.
- `workflow_dispatch`: explicit R7 run ID **and** exact R7 source SHA; may sign the resulting report only from the default branch.
- R8 uses a **pinned exact Devtool commit**, resolved at apply-time after the companion Devtool change is committed. It cannot silently follow a moving branch.
- GitHub token is used for read-only R8 API calls and ZIP downloads. The bearer token is never forwarded to the artifact blob CDN; the separately signed URL is followed without the bearer header.
- Fixed artifact names, count, byte size caps, SHA-256 matching, source/run binding, same-attempt checks and strict ZIP member sanitization are fail-closed.

## Hosted qualification

1. Apply **both** repositories from the single consolidated overlay and open the coordinated Devtool and consumer PRs.
2. Wait for consumer PR's read-only R8 observer and standard R7 gates. Inspect R8 report and Devtool's 19 negative/positive offline tests. This is still a **candidate**.
3. Merge Devtool R8 before consumer R8 so the exact pinned Devtool commit becomes reachable from its main history. Consumer PR already references immutable commit SHA.
4. Merge consumer R8 only after qualification. Trigger a fresh R7 main workflow run or dispatch R8 against the successful historic R7 main push. Confirm report, Sigstore attestation and `gh attestation verify` succeed.
5. Do not make R8 required or retire R5/R6 without new acceptance. Keep R7 required once branch protection is actually verified.

## Verification example (Termux)

```zsh
gh workflow run devtool-dn03-r8-provenance.yml -R Mikeyphw/Mike_SVMods \
  --ref main -f r7_run_id=42030323139 \
  -f r7_source_sha=5dc04f83acce009be8058e1ac5bab3d3aaaf6da2
```

Download the `dn03-r8-provenance-observation` artifact from the resulting run, then use:

```zsh
gh attestation verify /path/to/provenance.json -R Mikeyphw/Mike_SVMods
```

Note: GitHub branch metadata returned `main.protected=false` on 2026-11-05. Recheck actual repository rules/rulesets before claiming required CI enforcement; GitHub Apps may be denied protection details by API 403.

## Rollback

Disable only the R8 workflow or revert its R8 files with a normal PR. Do not reset, rebase, force-push or remove R7/R5/R6, and do not change signing or release processes.
