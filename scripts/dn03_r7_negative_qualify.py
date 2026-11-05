#!/usr/bin/env python3
"""DN03-R7 reproducible negative qualification against archived, real R7 main-push artifacts.

NON-RELEASE. The archived GHA context below is replayed, NOT authenticated.
No byte in the source checkout or published artifact is modified. All mutations
are applied to downloaded, runner-local temporary files, then restored.
"""
from __future__ import annotations

import argparse
from contextlib import contextmanager
from copy import deepcopy
import json
from pathlib import Path
import re
import subprocess
import sys
import textwrap
from typing import Callable, Iterator

REPO = "Mikeyphw/Mike_SVMods"
SOURCE = "5dc04f83acce009be8058e1ac5bab3d3aaaf6da2"
RUN = 42030323139
ATTEMPT = 1
DEVTOOL = "0ebd22c17ac3058704012a1e03ee3265328b0659"
GRAPH = "da038b45350a1a6bac31752e5e3cdb7de8348a3cbb0ca35497adb0b11ec25494"
MARKER = "DN03_R7_29_OF_29_TYPED_TRX_GATE_PASS"


def get_names():
    for i in range(1, 7):
        jid = f"node-{i:02}"
        yield (f"devtool-dn03_r5-native_r6-multi-{jid}-evidence", "evidence", jid)
        yield (f"dn03-host-{jid}-{RUN}-{ATTEMPT}", "host", jid)
        if i in (3, 6):
            yield (f"devtool-dn03_r5-native_r6-multi-{jid}-bytes", "bytes", jid)


def fetch_artifacts(evidence: Path) -> None:
    evidence.mkdir(parents=True, exist_ok=False)
    for segment in ("evidence", "host", "bytes"):
        (evidence / segment).mkdir()
    for name, group, jid in get_names():
        dest = evidence / group / jid
        dest.mkdir(parents=True, exist_ok=False)
        subprocess.run(
            ["gh", "run", "download", str(RUN), "-R", REPO, "-n", name, "-D", str(dest)],
            check=True,
        )
        if not any(dest.rglob("*")):
            raise RuntimeError(f"Artifact is empty: {name}")
    print("DN03_R7_NEGATIVE_MAIN_EVIDENCE_DOWNLOADED")


def evidence_env(repo_root: Path, evidence_root: Path, runner_temp: Path) -> dict[str, str]:
    needs = {f"node-{i:02}": {"result": "success", "outputs": {}} for i in range(1, 7)}
    return {
        "GITHUB_SHA": SOURCE,
        "GITHUB_REPOSITORY": REPO,
        "GITHUB_RUN_ID": str(RUN),
        "GITHUB_RUN_ATTEMPT": str(ATTEMPT),
        "GITHUB_ACTIONS": "true",  # context supplied to replay verifier, not a new remote attestation
        "GITHUB_WORKSPACE": str(repo_root),
        "RUNNER_OS": "Linux",
        "RUNNER_ARCH": "X64",
        "RUNNER_TEMP": str(runner_temp),
        "DN03_R4_NEEDS_JSON": json.dumps(needs, sort_keys=True),
        "DEVTOOL_SOURCE_SHA": DEVTOOL,
        "DN03_R4_GRAPH_SHA": GRAPH,
    }


def extract_r7_gate(source: Path) -> str:
    wf = (source / ".github/workflows/devtool-dn03-r7-native-ci.yml").read_text(encoding="utf-8")
    m = re.search(r"^\s*python3 - <<'R7_ACCEPT_PY'\s*\n(?P<body>.*?)^\s*R7_ACCEPT_PY\s*$",
                  wf, flags=re.M | re.S)
    if m is None or wf.count("R7_ACCEPT_PY") != 2:
        raise AssertionError("Actual R7 acceptance gate not found exactly once")
    code = textwrap.dedent(m.group("body"))
    compile(code, "committed-R7-acceptance-gate", "exec")
    if MARKER not in code:
        raise AssertionError("Actual R7 acceptance marker is missing")
    return code


@contextmanager
def replaced_file(path: Path, new: bytes | None) -> Iterator[None]:
    if path.is_symlink() or not path.is_file():
        raise AssertionError(f"Expected original file absent: {path}")
    original = path.read_bytes()
    try:
        if new is None:
            path.unlink()
        else:
            path.write_bytes(new)
        yield
    finally:
        path.write_bytes(original)


def one_trx(folder: Path) -> Path:
    matches = [p for p in folder.rglob("*.trx") if p.is_file() and not p.is_symlink()]
    if len(matches) != 1:
        raise AssertionError(f"Expected one real TRX in {folder}, found {len(matches)}")
    return matches[0]


def qualify(repo: Path, evidence: Path, runner_temp: Path, report: Path) -> None:
    from devtool.actions_dn03_r4_verify import R4Refused, verify_workflow

    repo = repo.resolve(strict=True)
    evidence = evidence.resolve(strict=True)
    runner_temp = runner_temp.resolve(strict=True)
    if evidence != runner_temp / "dn03-r4":
        raise AssertionError("Replay evidence must be under runner-temp/dn03-r4")
    head = subprocess.check_output(["git", "-C", str(repo), "rev-parse", "HEAD"], text=True).strip()
    if head != SOURCE:
        raise AssertionError(f"Replay checkout identity drift: {head}")
    env = evidence_env(repo, evidence, runner_temp)
    gate = extract_r7_gate(repo)
    result: list[dict[str, str]] = []

    def record(name: str, passed: bool, why: str = "") -> None:
        result.append({"case": name, "status": "PASS" if passed else "FAIL", "reason": why[:250]})
        print(f"{'PASS' if passed else 'FAIL'}: {name} {why[:160]}", flush=True)
        if not passed:
            raise AssertionError(f"Negative qualification case failed: {name}: {why}")

    def check_r4(*, context=None, digest=GRAPH, write=False):
        return verify_workflow(
            repo, target="dn03_r5", workflow="native_r6",
            devtool_sha=DEVTOOL, graph_sha=digest, evidence=evidence,
            environment=env if context is None else context, write=write,
        )

    # A genuine successful replay is mandatory. Negative-only samples cannot qualify.
    reference = check_r4(write=True)
    if (reference["consumer_source_sha"] != SOURCE or reference["graph_sha256"] != GRAPH
        or reference["node_count"] != 6 or reference["verified_test_node_count"] != 2
        or reference["verified_test_case_count"] != 29 or not reference["test_reports_independently_verified"]
        or reference["remote_authenticated"] is not False or reference["release_qualified"] is not False):
        raise AssertionError("Original R4 acceptance receipt failed expected identity/count assertions")
    record("positive_actual_R4_byte_replay", True)

    # Run the exact embedded gate from the original main-push R7 workflow.
    # No R7 verifier clone or weakened copy is introduced.
    acceptance_file = evidence / "acceptance.json"
    import os
    gate_env = dict(os.environ)
    gate_env.update(env)

    def run_gate() -> subprocess.CompletedProcess[str]:
        return subprocess.run([sys.executable, "-c", gate], env=gate_env,
                              stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)

    positive_gate = run_gate()
    record("positive_original_R7_inline_gate", positive_gate.returncode == 0 and MARKER in positive_gate.stdout,
           (positive_gate.stderr or positive_gate.stdout)[-180:])

    def r4_refused(name: str, *, changed_env=None, digest=GRAPH) -> None:
        try:
            check_r4(context=changed_env, digest=digest, write=False)
        except R4Refused as exc:
            record(name, True, str(exc))
        else:
            record(name, False, "R4 verifier accepted deliberately invalid evidence")

    for name, edit in (
        ("wrong_source_commit", {"GITHUB_SHA": "0" * 40}),
        ("wrong_run_id", {"GITHUB_RUN_ID": str(RUN + 1)}),
        ("wrong_attempt", {"GITHUB_RUN_ATTEMPT": "2"}),
    ):
        r4_refused(name, changed_env={**env, **edit})
    r4_refused("wrong_graph_digest", digest="0" * 64)
    for name, changes in (
        ("missing_native_node", lambda d: d.pop("node-06")),
        ("failed_native_node", lambda d: d["node-06"].update(result="failure")),
        ("skipped_native_node", lambda d: d["node-03"].update(result="skipped")),
    ):
        modified = json.loads(env["DN03_R4_NEEDS_JSON"])
        changes(modified)
        r4_refused(name, changed_env={**env, "DN03_R4_NEEDS_JSON": json.dumps(modified)})

    for jid in ("node-03", "node-06"):
        original = one_trx(evidence / "bytes" / jid)
        with replaced_file(original, original.read_bytes() + b"\n<!-- R7 negative mutation -->\n"):
            r4_refused(f"altered_{jid}_TRX_bytes")
        with replaced_file(original, None):
            r4_refused(f"missing_{jid}_TRX_report")

    for name, rel, new in (
        ("missing_node_receipt", "evidence/node-01/node-evidence.json", None),
        ("tampered_R3_receipt", "evidence/node-03/dn03-r3-node-verification.json", b"{}\n"),
        ("tampered_host_receipt", "host/node-04/host.json", b"{}\n"),
    ):
        with replaced_file(evidence / rel, new):
            r4_refused(name)

    # Contract-level tests must exercise the ACTUAL embedded 29/29 gate against
    # mutated copies of the verified acceptance receipt.
    def invalid_gate(name: str, edit: Callable[[dict], None]) -> None:
        altered = deepcopy(reference)
        edit(altered)
        raw = (json.dumps(altered, sort_keys=True, indent=2) + "\n").encode()
        with replaced_file(acceptance_file, raw):
            outcome = run_gate()
            record(name, outcome.returncode != 0 and MARKER not in outcome.stdout,
                   (outcome.stderr or "rejected")[-150:])

    gate_cases = (
        ("R7_wrong_source_claim", lambda d: d.update(consumer_source_sha="0" * 40)),
        ("R7_wrong_run_claim", lambda d: d.update(run_id=RUN+1)),
        ("R7_wrong_graph_claim", lambda d: d.update(graph_sha256="0" * 64)),
        ("R7_missing_native_node", lambda d: d["nodes"].pop()),
        ("R7_wrong_test_total", lambda d: d.update(verified_test_case_count=28)),
        ("R7_missing_test_report", lambda d: next(x for x in d["nodes"] if x["job_id"]=="node-06").update(verified_trx=None)),
        ("R7_TRX_digest_disagreement", lambda d: next(x for x in d["nodes"] if x["job_id"]=="node-03")["verified_trx"].update(sha256="0"*64)),
        ("R7_failed_test_count", lambda d: next(x for x in d["nodes"] if x["job_id"]=="node-06")["verified_trx"]["tests"].update(failed=1)),
        ("R7_skipped_test_count", lambda d: next(x for x in d["nodes"] if x["job_id"]=="node-06")["verified_trx"]["tests"].update(skipped=1)),
        ("R7_failed_native_DAG", lambda d: d.update(native_dag_passed=False)),
        ("R7_forged_remote_auth", lambda d: d.update(remote_authenticated=True)),
        ("R7_forged_release_claim", lambda d: d.update(release_qualified=True)),
    )
    for name, edit in gate_cases:
        invalid_gate(name, edit)
    with replaced_file(acceptance_file, None):
        outcome = run_gate()
        record("R7_absent_acceptance_receipt", outcome.returncode != 0 and MARKER not in outcome.stdout)
    with replaced_file(acceptance_file, b"{invalid-json"):
        outcome = run_gate()
        record("R7_malformed_acceptance_receipt", outcome.returncode != 0 and MARKER not in outcome.stdout)

    # Restore original receipt and test success again to detect mutation leakage.
    final = run_gate()
    record("positive_gate_after_all_mutations", final.returncode == 0 and MARKER in final.stdout)
    summary = {
        "kind": "dn03-r7-negative-replay", "consumer": REPO, "source_sha": SOURCE,
        "archived_run_id": RUN, "archived_run_attempt": ATTEMPT,
        "devtool_source_sha": DEVTOOL, "graph_sha256": GRAPH,
        "remote_authenticated": False, "release_qualified": False,
        "note": "Archived hosted evidence replayed with its archived context; not fresh remote authentication.",
        "cases": result, "passed": len(result), "failed": 0,
    }
    report.parent.mkdir(parents=True, exist_ok=True)
    report.write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"DN03_R7_NEGATIVE_QUALIFICATION_PASS cases={len(result)}", flush=True)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="cmd", required=True)
    fetch = sub.add_parser("fetch")
    fetch.add_argument("--evidence", required=True, type=Path)
    test = sub.add_parser("qualify")
    test.add_argument("--repo", required=True, type=Path)
    test.add_argument("--evidence", required=True, type=Path)
    test.add_argument("--runner-temp", required=True, type=Path)
    test.add_argument("--report", required=True, type=Path)
    args = parser.parse_args()
    if args.cmd == "fetch":
        fetch_artifacts(args.evidence)
    else:
        qualify(args.repo, args.evidence, args.runner_temp, args.report)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, AssertionError, subprocess.CalledProcessError) as e:
        print(f"DN03_R7_NEGATIVE_QUALIFICATION_REFUSED: {e}", file=sys.stderr)
        raise SystemExit(2)
