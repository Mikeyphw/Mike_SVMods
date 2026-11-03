#!/usr/bin/env python3
"""R06b: verify real hosted native Devtool DAG plus both TRX receipts.

No .NET process is ever started here. A successful CI run requires a real native
machine result, the entire expected DAG, and two nonempty passing test receipts.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

from verify_dotnet_trx import TrxError, inspect_trx

SHA = re.compile(r'[0-9a-f]{40}\Z')
EXPECTED_NODES = ('ci-source', 'r06b_restore_pure', 'r06b_test_pure',
                  'r06b_restore_animal', 'r06b_test_animal')
TRX_FILES = ('r06b-pure.trx', 'r06b-animalnotes.trx')


class EvidenceError(ValueError):
    pass


def read_json(path: Path) -> dict:
    if path.is_symlink() or not path.is_file():
        raise EvidenceError('Evidence missing/symlinked: '+path.name)
    try:
        value = json.loads(path.read_text(encoding='utf-8'))
    except (UnicodeError, ValueError) as exc:
        raise EvidenceError('Malformed evidence JSON: '+path.name) from exc
    if not isinstance(value, dict) or not value:
        raise EvidenceError('Empty or nonobject evidence JSON: '+path.name)
    return value


def verify(root: Path, evidence: Path, consumer_sha: str, devtool_sha: str) -> dict:
    if not SHA.fullmatch(consumer_sha) or not SHA.fullmatch(devtool_sha):
        raise EvidenceError('Source IDs must be full lowercase Git SHAs')
    if evidence.is_symlink() or not evidence.is_dir() or root.is_symlink() or not root.is_dir():
        raise EvidenceError('Missing/symlinked workspace or evidence directory')
    plan = read_json(evidence/'plan.json')
    preflight = read_json(evidence/'preflight.json')
    invoke = read_json(evidence/'invoke.json')
    if invoke.get('ok') is not True or invoke.get('exit_code') != 0:
        raise EvidenceError('Devtool native invoke not successful')
    totals = invoke.get('totals')
    if (not isinstance(totals, dict) or totals.get('failed') != 0 or
            totals.get('errors') != 0 or totals.get('skipped') != 0):
        raise EvidenceError('Native run has failures, errors or skipped nodes')
    results = invoke.get('results')
    if not isinstance(results, list) or len(results) < len(EXPECTED_NODES):
        raise EvidenceError('Native run did not execute its complete DAG')
    if any(not isinstance(r, dict) or r.get('exit_code') != 0 or
           r.get('status') in ('failed', 'skipped') for r in results):
        raise EvidenceError('Native machine result failed or skipped')
    graph = invoke.get('workflow')
    nodes = graph.get('nodes') if isinstance(graph, dict) else None
    if not isinstance(nodes, list) or len(nodes) != len(EXPECTED_NODES):
        raise EvidenceError('Native graph must contain exactly five executable nodes')
    observed: dict[str, dict] = {}
    for node in nodes:
        if not isinstance(node, dict) or not isinstance(node.get('job'), dict):
            raise EvidenceError('Malformed native job identity')
        job = node['job']
        if job.get('target') != 'svmods' or job.get('target_local') is not True:
            raise EvidenceError('Native DAG contains a foreign/unscoped job')
        name = job.get('local_name')
        if name not in EXPECTED_NODES or name in observed:
            raise EvidenceError('Unexpected/duplicated native job '+str(name))
        if node.get('state') not in ('passed', 'success', 'completed', 'ok'):
            raise EvidenceError('Native node not successful: '+str(name))
        if int(node.get('result_count', 0)) < 1:
            raise EvidenceError('Native node has no execution results: '+str(name))
        observed[name] = node
    if set(observed) != set(EXPECTED_NODES):
        raise EvidenceError('Missing native locked job(s)')
    try:
        summary = {name: inspect_trx(root/'.ci-results/r06b'/name, 1)
                   for name in TRX_FILES}
    except TrxError as exc:
        raise EvidenceError('Invalid real hosted test receipt: '+str(exc)) from exc
    return {
        'schema_version': 1, 'status': 'HOSTED_NATIVE_LOCKED_DOTNET_PASS',
        'consumer_sha': consumer_sha, 'devtool_sha': devtool_sha,
        'native_nodes': list(EXPECTED_NODES), 'node_count': len(nodes),
        'result_count': len(results), 'test_receipts': summary,
        'plan_sha256': hashlib.sha256((evidence/'plan.json').read_bytes()).hexdigest(),
        'preflight_sha256': hashlib.sha256((evidence/'preflight.json').read_bytes()).hexdigest(),
        'invoke_sha256': hashlib.sha256((evidence/'invoke.json').read_bytes()).hexdigest(),
        'hosted_net6_tests_via_sdk10': True,
        'game_dependent_tests': 'NOT_QUALIFIED',
        'native_dotnet_provider_rm09': 'NOT_QUALIFIED_COMMAND_JOBS_ONLY',
        'release_qualified': False,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--repo', type=Path, required=True)
    parser.add_argument('--evidence', type=Path, required=True)
    parser.add_argument('--consumer-sha', required=True)
    parser.add_argument('--devtool-sha', required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args()
    try:
        root = args.repo.resolve()
        evidence = args.evidence.resolve()
        if args.receipt.resolve() != (evidence/'receipt.json').resolve():
            raise EvidenceError('Receipt must remain within evidence directory')
        report = verify(root, evidence, args.consumer_sha, args.devtool_sha)
        args.receipt.write_text(json.dumps(report, indent=2, sort_keys=True)+'\n', encoding='utf-8')
    except (OSError, EvidenceError, TypeError, ValueError) as exc:
        print(json.dumps({'status':'FAIL', 'error':str(exc)}, sort_keys=True), file=sys.stderr)
        return 1
    print(json.dumps(report, sort_keys=True))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
