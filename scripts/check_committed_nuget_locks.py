#!/usr/bin/env python3
"""Fail-closed source-only check for committed R05c NuGet lockfiles and CI policy.

Does NOT invoke dotnet. The lockfile bytes originate from hosted R05b candidate
41663506662, which performed a second locked restore in a fresh NuGet cache.
"""
from __future__ import annotations

import json
from pathlib import Path
import re
import sys
from verify_nuget_locks import PROJECTS, LockError, verify_lock

ROOT = Path(__file__).resolve().parents[1]
PROVENANCE = 'docs/ci/R05C_LOCKFILE_PROVENANCE.json'
QUALIFIED_RUN = 41663506662
QUALIFIED_SOURCE = '24cee7e92111348ed834cad344f80f8d3c88a101'
QUALIFIED_ARTIFACT = '32573284145cac2e554ae6170790e3677edac88851c87430fb20cfe68a88469a'
QUALIFIED_ARTIFACT_ID = 13241868660


def _job(ci: str, name: str) -> str:
    pattern = r'^  ' + re.escape(name) + r':\s*\n(.*?)(?=^  [a-zA-Z0-9_-]+:\s*\n|\Z)'
    match = re.search(pattern, ci, flags=re.MULTILINE | re.DOTALL)
    return match.group(1) if match else ''


def check_ci(ci: str) -> list[str]:
    problems: list[str] = []
    pure = _job(ci, 'pure-tests')
    notes = _job(ci, 'animalnotes-locked')
    source = _job(ci, 'source-contracts')
    for name, body in (('pure-tests', pure), ('animalnotes-locked', notes), ('source-contracts', source)):
        if not body:
            problems.append(f'R05c required CI job absent: {name}')
        elif re.search(r'^\s*continue-on-error:\s*true\s*$', body, re.M):
            problems.append(f'R05c job cannot ignore failures: {name}')
        elif re.search(r'^\s*if:\s*false\s*$', body, re.M):
            problems.append(f'R05c job cannot be disabled: {name}')

    pure_restore = 'dotnet restore Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj --locked-mode --no-http-cache'
    if not re.search(r'^\s*run:\s*' + re.escape(pure_restore) + r'\s*$', pure, re.M):
        problems.append('Baseline pure-tests must restore pinned lock with --locked-mode')
    if not re.search(r'^\s*run:\s*dotnet test Tests/RoosterControl\.PureTests/RoosterControl\.PureTests\.csproj -c Release --no-restore\s*$', pure, re.M):
        problems.append('Baseline pure-tests must test without re-restoring')

    notes_restore = 'dotnet restore ../../Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj --locked-mode --no-http-cache'
    if not re.search(r'^\s*run:\s*' + re.escape(notes_restore) + r'\s*$', notes, re.M):
        problems.append('AnimalNotes must restore pinned lock with --locked-mode')
    for expected in ('runs-on: ubuntu-24.04', 'working-directory: .github/r05-sdk10',
                     '6.0.x', '10.0.x', 'dotnet test ../../Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj',
                     '--no-restore', 'python3 ../../scripts/verify_dotnet_trx.py', '--min-executed 1'):
        if expected not in notes:
            problems.append(f'R05c AnimalNotes hosted qualification missing: {expected}')
    if not re.search(r'^\s*run:\s*python3 scripts/check_committed_nuget_locks\.py\s*$', source, re.M):
        problems.append('Baseline CI source job must verify committed lockfiles')
    if '--force-evaluate' in pure or '--force-evaluate' in notes:
        problems.append('Locked CI must not force-evaluate dependency graph')
    if '${{ runner.' in ci:
        problems.append('CI YAML must not use job-level runner context')
    return problems


def check_source(root: Path) -> list[str]:
    issues: list[str] = []
    try:
        record = json.loads((root / PROVENANCE).read_text(encoding='utf-8'))
    except (OSError, ValueError) as exc:
        return [f'R05c provenance missing/invalid: {exc}']
    if record.get('schema_version') != 1 or record.get('purpose') != 'R05C_COMMITTED_NUGET_LOCKS':
        issues.append('R05c provenance schema/purpose changed')
    if record.get('source_commit') != QUALIFIED_SOURCE:
        issues.append('R05c source must match qualified hosted source')
    if record.get('candidate_run_id') != QUALIFIED_RUN or record.get('candidate_artifact_id') != QUALIFIED_ARTIFACT_ID:
        issues.append('R05c run/artifact identity not qualified')
    if record.get('candidate_artifact_sha256') != QUALIFIED_ARTIFACT:
        issues.append('R05c artifact checksum changed')
    if not str(record.get('hosted_sdk_version', '')).startswith('10.0.'):
        issues.append('R05c not qualified on SDK 10')
    if record.get('hosted_locked_restore_verified') is not True:
        issues.append('R05c hosted locked restore not verified')
    expected = {f'{project}/packages.lock.json' for project in PROJECTS}
    if set(record.get('lockfiles', {})) != expected:
        issues.append('R05c lockfile inventory does not cover both projects exactly')
    for project in PROJECTS:
        rel = f'{project}/packages.lock.json'
        meta = record.get('lockfiles', {}).get(rel)
        if not isinstance(meta, dict):
            issues.append(f'R05c missing lockfile provenance: {rel}')
            continue
        try:
            project_file = root / project / (Path(project).name + '.csproj')
            lockinfo = verify_lock(root / rel, project_file)
        except (LockError, OSError, ValueError) as exc:
            issues.append(f'R05c invalid lock {rel}: {exc}')
            continue
        if lockinfo != meta:
            issues.append(f'R05c lockfile differs from hosted artifact: {rel}')
    try:
        ci = (root / '.github/workflows/ci.yml').read_text(encoding='utf-8')
        issues.extend(check_ci(ci))
    except OSError as exc:
        issues.append(f'R05c missing CI policy: {exc}')
    return issues


def main() -> int:
    errors = check_source(ROOT)
    print(json.dumps({
        'status': 'PASS' if not errors else 'FAIL',
        'errors': sorted(set(errors)),
        'hosted_locked_restore_source_run': QUALIFIED_RUN,
        'r05c_ci_dotnet_execution': 'GITHUB_ACTIONS_ONLY',
        'net6_and_sdk10_ci_qualifications': 'PENDING_NEW_PUSH',
    }, sort_keys=True))
    return bool(errors)


if __name__ == '__main__':
    sys.exit(main())
