#!/usr/bin/env python3
"""Fail-closed source-only gate for R05b hosted lockfile generation."""
from __future__ import annotations

import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW_PATH = '.github/workflows/r05b-nuget-lock-candidate.yml'


def check_sources(workflow: str, ci: str, sdk_json: str) -> list[str]:
    errors: list[str] = []
    try:
        sdk = json.loads(sdk_json)['sdk']
        if sdk['version'] != '10.0.100' or sdk['rollForward'] != 'latestFeature' or sdk['allowPrerelease'] is not False:
            errors.append('SDK10 compatibility pin drifted')
    except (ValueError, TypeError, KeyError):
        errors.append('SDK10 policy invalid')
    if not re.search(r'^on:\s*\n  workflow_dispatch:\s*$', workflow, re.M):
        errors.append('R05b must require explicit workflow_dispatch')
    if not re.search(r'^permissions:\s*\n  contents: read\s*$', workflow, re.M):
        errors.append('R05b must use read-only GITHUB_TOKEN')
    if not re.search(r'^    runs-on: ubuntu-24\.04\s*$', workflow, re.M):
        errors.append('R05b must run on hosted Ubuntu')
    if not re.search(r'^        working-directory: \.github/r05-sdk10\s*$', workflow, re.M):
        errors.append('R05b SDK pin working directory was changed')
    for v in ('6.0.x', '10.0.x'):
        if not re.search(r'^            ' + re.escape(v) + r'\s*$', workflow, re.M):
            errors.append(f'R05b must provision {v}')
    match = re.search(r'^    env:\s*\n(.*?)(?=^    steps:)', workflow, re.M | re.S)
    if not match:
        errors.append('R05b job environment missing')
    else:
        env = match.group(1)
        if '${{ runner.' in env:
            errors.append('R05b job env cannot use runner context')
        if not re.search(r'^      NUGET_PACKAGES: \$\{\{ github\.workspace \}\}/\.ci-results/r05b/nuget-first\s*$', env, re.M):
            errors.append('R05b isolated first NuGet cache missing')
    for project in ('RoosterControl.PureTests', 'EggsInWild.AnimalNotes.Tests'):
        p = f'../../Tests/{project}/{project}.csproj'
        if workflow.count(p) != 2:
            errors.append(f'R05b requires two restore passes for {project}')
    for frag in ('--use-lock-file', '--force-evaluate', '--locked-mode', '--force --no-http-cache',
                 'nuget-second', 'scripts/verify_nuget_locks.py stage',
                 'scripts/verify_nuget_locks.py verify', 'if: success()',
                 'if-no-files-found: error', 'persist-credentials: false'):
        if frag not in workflow:
            errors.append(f'R05b missing hosted gate: {frag}')
    if workflow.count('dotnet restore') != 4:
        errors.append('R05b must perform two generated and two locked restores')
    if '${{ github.run_id }}-${{ github.run_attempt }}' not in workflow:
        errors.append('R05b artifact identity is not run-scoped')
    if not re.search(r'^        run: python3 scripts/check_nuget_candidate_policy\.py\s*$', ci, re.M):
        errors.append('Baseline CI must verify hosted R05b policy')
    return errors


def main() -> int:
    try:
        w = (ROOT / WORKFLOW_PATH).read_text(encoding='utf-8')
        ci = (ROOT / '.github/workflows/ci.yml').read_text(encoding='utf-8')
        sdk = (ROOT / '.github/r05-sdk10/global.json').read_text(encoding='utf-8')
    except OSError as exc:
        print(json.dumps({'status': 'FAIL', 'errors': [str(exc)]}))
        return 1
    problems = check_sources(w, ci, sdk)
    print(json.dumps({'status': 'FAIL' if problems else 'PASS', 'errors': problems,
                      'hosted_lock_candidate_run': 'NOT_RUN',
                      'enforced_locked_restore_in_baseline_ci': 'NOT_YET_ENABLED'}, sort_keys=True))
    return int(bool(problems))


if __name__ == '__main__':
    raise SystemExit(main())
