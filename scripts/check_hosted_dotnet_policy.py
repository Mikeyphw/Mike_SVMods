#!/usr/bin/env python3
"""Source-only negative gate for the opt-in GitHub-hosted R05 SDK 10 pilot."""
from __future__ import annotations

import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
QUALIFICATION_WORKFLOW = '.github/workflows/r05-hosted-dotnet-sdk10.yml'
SDK_POLICY = '.github/r05-sdk10/global.json'


def check_sources(workflow: str, sdk_json: str, ci: str) -> list[str]:
    issues: list[str] = []
    try:
        policy = json.loads(sdk_json)
        sdk = policy['sdk']
        if sdk.get('version') != '10.0.100' or sdk.get('rollForward') != 'latestFeature' or sdk.get('allowPrerelease') is not False:
            issues.append('SDK10 global.json contract altered')
    except (ValueError, TypeError, KeyError):
        issues.append('SDK10 global.json invalid')
    if not re.search(r'(?m)^on:\s*\n\s+workflow_dispatch:\s*$', workflow):
        issues.append('R05 workflow must be explicitly dispatched')
    if not re.search(r'(?m)^permissions:\s*\n\s+contents:\s*read\s*$', workflow):
        issues.append('R05 workflow must use read-only permissions')
    if not re.search(r'(?m)^\s+working-directory:\s*\.github/r05-sdk10\s*$', workflow):
        issues.append('R05 workflow must run from SDK10 policy directory')
    for version in ('6.0.x', '10.0.x'):
        if not re.search(r'(?m)^\s+' + re.escape(version) + r'\s*$', workflow):
            issues.append('R05 must install SDK/runtime ' + version)
    if not re.search(r'(?m)^\s+if:\s*always\(\)\s*$', workflow):
        issues.append('R05 must upload logs even on failed runs')
    for project in ('RoosterControl.PureTests', 'EggsInWild.AnimalNotes.Tests'):
        if not re.search(r'(?m)^\s+dotnet restore .*' + re.escape(project) + r'/' + re.escape(project) + r'\.csproj\s*$', workflow):
            issues.append('Missing hosted restore for ' + project)
        if not re.search(r'(?m)^\s+dotnet test .*' + re.escape(project) + r'/' + re.escape(project) + r'\.csproj\s*\\\s*$', workflow):
            issues.append('Missing hosted test for ' + project)
    if workflow.count('python3 ../../scripts/verify_dotnet_trx.py') != 2:
        issues.append('R05 must verify both TRX receipts')
    if '--no-restore' not in workflow or "--logger 'trx;LogFileName=" not in workflow:
        issues.append('Missing no-restore or TRX logging')
    if "dotnet --list-runtimes | grep -Eq '^Microsoft\\.NETCore\\.App 6\\.'" not in workflow:
        issues.append('R05 must prove net6 runtime availability')
    if not re.search(r'(?m)^\s+run:\s*python3 scripts/check_hosted_dotnet_policy\.py\s*$', ci):
        issues.append('Baseline CI must check the R05 pilot policy')
    return issues


def main() -> int:
    try:
        workflow = (ROOT / QUALIFICATION_WORKFLOW).read_text('utf-8')
        sdk_json = (ROOT / SDK_POLICY).read_text('utf-8')
        ci = (ROOT / '.github/workflows/ci.yml').read_text('utf-8')
    except OSError as exc:
        print(json.dumps({'status': 'FAIL', 'errors': [str(exc)]}))
        return 1
    issues = check_sources(workflow, sdk_json, ci)
    print(json.dumps({'status': 'FAIL' if issues else 'PASS', 'errors': issues,
                      'hosted_sdk10_execution': 'NOT_RUN',
                      'game_dependent_integration': 'BLOCKED_UNTIL_HOSTED_GAME_RUNTIME_QUALIFIED',
                      'nuget_locked_restore': 'NOT_YET_QUALIFIED'}, sort_keys=True))
    return int(bool(issues))


if __name__ == '__main__':
    raise SystemExit(main())
