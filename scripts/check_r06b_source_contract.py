#!/usr/bin/env python3
"""Source-only hosted R06b workflow gate. Never executes .NET locally."""
from __future__ import annotations

import json
from pathlib import Path
import re
import tomllib

from r06b_target_config import DEVTOOL_SHA, validate as validate_jobs

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = '.github/workflows/r06b-native-devtool-dotnet.yml'
COMMAND = 'python3 scripts/check_r06b_source_contract.py'


def check_sources(workflow: str, ci: str, config: dict) -> list[str]:
    issues = validate_jobs(config)
    def require(regex: str, message: str):
        if not re.search(regex, workflow, re.M):
            issues.append(message)
    require(r'^on:\s*\n  workflow_dispatch:\s*$', 'R06b must require explicit dispatch')
    require(r'^permissions:\s*\n  contents: read\s*$', 'R06b GitHub token must be read only')
    require(r'^    runs-on: ubuntu-24\.04\s*$', 'R06b needs hosted Ubuntu 24.04')
    require(r'^      DEVTOOL_SOURCE_SHA: '+DEVTOOL_SHA+r'\s*$', 'Devtool SHA drift')
    require(r'^      NUGET_PACKAGES: \$\{\{ github\.workspace \}\}/\.ci-results/r06b/nuget\s*$',
            'R06b must have isolated NuGet package cache')
    for token in (
        'checkout@11d5960a326750d5838078e36cf38b85af677262',
        'upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02',
        'actions/setup-dotnet@v4', '6.0.x', '10.0.x',
        'persist-credentials: false', '"$(git rev-parse HEAD)" = "$GITHUB_SHA"',
        'checkout --detach "$DEVTOOL_SOURCE_SHA"',
        '"$(git -C "$RUNNER_TEMP/devtool-source" rev-parse HEAD)" = "$DEVTOOL_SOURCE_SHA"',
        'plan ci-hosted-dotnet --target svmods --json',
        'preflight --target svmods --workflow ci-hosted-dotnet --json',
        'invoke --target svmods ci-hosted-dotnet',
        '--machine-file "$RUNNER_TEMP/r06b-evidence/invoke.json"',
        'verify_r06b_native_dotnet.py', 'if: always()',
        'if-no-files-found: error',
        'r06b-native-dotnet-${{ github.run_id }}-${{ github.run_attempt }}',
        "'1'", 'r06b-pure.trx', 'r06b-animalnotes.trx',
    ):
        if token not in workflow:
            issues.append('R06b missing control: '+token)
    if re.search(r'^\s*continue-on-error:\s*true\s*$', workflow, re.M):
        issues.append('R06b cannot ignore errors')
    job_env = re.search(r'^    env:\s*\n(.*?)(?=^    steps:)', workflow, re.M | re.S)
    if job_env is None or '${{ runner.' in job_env.group(1):
        issues.append('Job-level runner context prohibited')
    if any(re.search(r'^\s*run:\s*dotnet (restore|test|pack|build)\b', workflow, re.M) for _ in (0,)):
        issues.append('R06b .NET restore/test must be Devtool native job commands, not workflow shell steps')
    if not re.search(r'^\s+run: '+re.escape(COMMAND)+r'\s*$', ci, re.M):
        issues.append('Baseline CI source policy must check R06b')
    if 'run: python3 scripts/check_r06_source_contract.py' not in ci:
        issues.append('R06a baseline gate must remain enabled')
    if 'run: python3 scripts/check_committed_nuget_locks.py' not in ci:
        issues.append('R05c committed lock gate must remain enabled')
    return sorted(set(issues))


def main() -> int:
    try:
        cfg = tomllib.loads((ROOT/'.devtool.toml').read_text(encoding='utf-8'))
        ci = (ROOT/'.github/workflows/ci.yml').read_text(encoding='utf-8')
        workflow = (ROOT/WORKFLOW).read_text(encoding='utf-8')
        issues = check_sources(workflow, ci, cfg)
    except (OSError, UnicodeError, ValueError) as exc:
        issues = [str(exc)]
    print(json.dumps({'status':'FAIL' if issues else 'PASS', 'errors':issues,
                      'r06b_hosted_execution':'NOT_RUN_BY_SOURCE_CHECK',
                      'local_dotnet':'NOT_RUN'}, sort_keys=True))
    return int(bool(issues))


if __name__ == '__main__':
    raise SystemExit(main())
