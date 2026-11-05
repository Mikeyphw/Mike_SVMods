# DN03-R7 — Dynamic, fail-closed native .NET CI candidate

## Scope and source pins

- Accepted consumer baseline: `Mikeyphw/Mike_SVMods` `6b662ab69416b4caaf7be91a73b04a91658b1650`.
- Accepted Devtool runtime: `Mikeyphw/devtool` `0ebd22c17ac3058704012a1e03ee3265328b0659`.
- Original generated R6 workflow, blob `d62880a27b21ba45734d1779c8d2613ec4715560`, remains unchanged at `.github/workflows/devtool-dn03r4-dn03_r5-native_r6.yml`.
- Original R6 sidecar and historical R5 workflow remain unchanged.
- R6 accepted run `41875703417` proved six native jobs and 21+8 independently reparsed TRX tests. This is **not** release or signature authority.

## R7 candidate, not a silent cutover

New `.github/workflows/devtool-dn03-r7-native-ci.yml` runs on `pull_request` and `push` for `main` and on manual dispatch. The YAML is a **reviewed R7 adaptation** of R6, not the unmodified output of the DN03-R4 generator. It must not be passed to the generated-R6 `verify` mode as though identical.

1. `r7_plan` checks out the *exact event commit*, installs immutable Devtool, compiles the real `dn03_r5/native_r6` graph, admits the six known owned Linux `.NET` jobs, and exposes its SHA-256 graph digest.
2. Each original native node continues using the current exact Git source, locked SDK/runtime and existing R3 report verification. A new direct `needs: r7_plan` dependency binds every original graph SHA use to the reviewed plan result. Native files still come from the original frozen six-node DAG; no shadow executor is introduced.
3. The R4 job includes `r7_plan` as a dependency, requires all seven dependencies to succeed, and deliberately scopes the `DN03_R4_NEEDS_JSON` value back to the exact six native nodes before running the unchanged R4 verifier. A missing, failed, cancelled or skipped native node cannot pass.
4. R4 independently validates same-run source, host receipts, outputs and TRX bytes. An additional R7 gate asserts the verified R4 receipt: 6 nodes, 2 test nodes, 29 cases (21 PureTests, 8 AnimalNotes), exact report digests, no failed/skipped results, and **no** remote authentication or release qualification.

This adaptation permits source changes that the original **static** R6 graph digest would rightly refuse. It does not imply Devtool can yet generate an equivalently dynamic workflow natively. File ownership and source eligibility are still checked on every run.

## Rollout / branch protection

1. Merge only after the R7 PR's full hosted graph succeeds on PR commit.
2. Verify the first `push` to `main` succeeds with the same R7 gate and expected source-bound receipt.
3. Qualify one intentional change to an owned `.NET` input on a separate PR, proving the graph digest changes and the full R7 pipeline validates the new source; also qualify deliberate malformed/missing report refusal.
4. Only then make **`DN03-R7 / verify 29 TRX tests (non-release)`** a required branch-protection check in GitHub Settings. Keep R5 and R6 dispatch workflows as rollback paths. No signed release, remote authentication, Windows executor, or EOL runtime transition is implied.
5. Because this pilot asserts **exactly** 29 tests, test additions/removals require a reviewed gate-count update and new acceptance. This is a strict transitional policy, not a long-term changing test-count oracle.

## Rollback

Disable the R7 required status check (if enabled); revert only the R7 candidate workflow, source contract, and this document on a normal branch/PR. Never rewrite Git history, delete R5/R6 workflows, or lower the R3/R4 test gates. Restore R5 as the required gate only if separately accepted for that purpose; do not mistake its prior `NOT_RUN` typed test evidence for R6-quality verification.

## Outstanding

- Current R7 hosted PR and post-merge source validation **pending**.
- Tests covering source change and failure injection **pending**.
- Branch-protection enabling is explicitly deferred.
- Independent GitHub artifact provenance (R8), coverage and Windows/multi-target qualification (R9), and separate signing/device release gates remain open.
