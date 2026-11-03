#!/usr/bin/env python3
"""Stage/verify GitHub-hosted NuGet test-project locks. No .NET commands here."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import sys
import xml.etree.ElementTree as ET

PROJECTS = (
    'Tests/RoosterControl.PureTests',
    'Tests/EggsInWild.AnimalNotes.Tests',
)


class LockError(ValueError):
    pass


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def project_dependencies(csproj: Path) -> set[str]:
    try:
        root = ET.fromstring(csproj.read_bytes())
    except (OSError, ET.ParseError) as exc:
        raise LockError(f'Cannot parse project: {csproj}: {exc}') from exc
    packages: set[str] = set()
    for element in root.iter():
        if element.tag.split('}')[-1] == 'PackageReference':
            name = element.attrib.get('Include', '').strip().lower()
            if name:
                packages.add(name)
    if not packages:
        raise LockError(f'Expected explicit PackageReference entries: {csproj}')
    return packages


def verify_lock(path: Path, csproj: Path) -> dict:
    if path.is_symlink() or not path.is_file():
        raise LockError(f'Lock file absent or symlinked: {path}')
    raw = path.read_bytes()
    try:
        obj = json.loads(raw)
    except (ValueError, UnicodeDecodeError) as exc:
        raise LockError(f'Lock JSON invalid: {path}: {exc}') from exc
    if not isinstance(obj, dict) or not isinstance(obj.get('version'), int) or obj['version'] < 1:
        raise LockError(f'Invalid lock schema version: {path}')
    groups = obj.get('dependencies')
    if not isinstance(groups, dict) or not groups:
        raise LockError(f'Empty lock dependency groups: {path}')
    direct = set()
    hashed = 0
    count = 0
    for framework, dependencies in groups.items():
        if not isinstance(framework, str) or not isinstance(dependencies, dict) or not dependencies:
            raise LockError(f'Invalid dependency group: {path}')
        for name, entry in dependencies.items():
            if not isinstance(name, str) or not isinstance(entry, dict):
                raise LockError(f'Malformed lock package: {path}: {name}')
            if not isinstance(entry.get('resolved'), str) or not entry['resolved'].strip():
                raise LockError(f'Package missing resolved version: {path}: {name}')
            kind = str(entry.get('type', '')).lower()
            if kind not in {'direct', 'transitive', 'project', 'centraltransitive'}:
                raise LockError(f'Unknown lock entry type: {path}: {name}: {kind}')
            if kind == 'direct':
                direct.add(name.lower())
            if kind != 'project':
                ch = entry.get('contentHash')
                if not isinstance(ch, str) or not ch.strip():
                    raise LockError(f'Package missing contentHash: {path}: {name}')
                hashed += 1
            count += 1
    expected = project_dependencies(csproj)
    if not expected.issubset(direct):
        raise LockError(f'Direct dependencies absent from lock {path}: {sorted(expected - direct)}')
    if hashed == 0:
        raise LockError(f'Lock has no content-hashed dependencies: {path}')
    return {'sha256': digest(raw), 'bytes': len(raw), 'resolved_entries': count,
            'hashed_packages': hashed, 'direct_packages': sorted(direct)}


def run(mode: str, repo: Path, output: Path, source_sha: str, sdk_version: str) -> dict:
    if len(source_sha) != 40 or not all(c in '0123456789abcdefABCDEF' for c in source_sha):
        raise LockError('Source SHA must be a 40-character hexadecimal commit ID')
    if not sdk_version.startswith('10.0.'):
        raise LockError('Source is not qualified by .NET 10 SDK')
    if output.resolve().is_relative_to(repo.resolve()) is False:
        raise LockError('Evidence output must be inside source checkout')
    evidence = output / 'qualified'
    manifest_path = evidence / 'manifest.json'
    if mode == 'verify':
        if not manifest_path.is_file():
            raise LockError('Qualified manifest missing')
        old = json.loads(manifest_path.read_bytes())
        if old.get('source_sha') != source_sha or old.get('sdk_version') != sdk_version:
            raise LockError('Hosted provenance changed between restores')
        if sorted(old.get('projects', {})) != sorted(PROJECTS):
            raise LockError('Manifest project inventory mismatch')
    else:
        old = None
    results: dict[str, dict] = {}
    for project in PROJECTS:
        project_file = repo / project / (Path(project).name + '.csproj')
        source_lock = repo / project / 'packages.lock.json'
        info = verify_lock(source_lock, project_file)
        target_lock = evidence / 'locks' / project / 'packages.lock.json'
        if mode == 'stage':
            target_lock.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source_lock, target_lock)
        staged_info = verify_lock(target_lock, project_file)
        if info != staged_info:
            raise LockError(f'Candidate lock changed: {project}')
        if old and old['projects'].get(project) != info:
            raise LockError(f'Lock changed across clean locked restore: {project}')
        results[project] = info
    receipt = {'schema_version': 1, 'purpose': 'R05B_HOSTED_NUGET_LOCK_CANDIDATE',
               'status': 'CANDIDATE_NOT_COMMITTED', 'source_sha': source_sha,
               'sdk_version': sdk_version, 'projects': results,
               'locked_restore_verified': mode == 'verify'}
    if mode == 'stage':
        evidence.mkdir(parents=True, exist_ok=True)
        manifest_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n', encoding='utf-8')
    else:
        # This file is written only after the second locked restore + file-hash checks succeed.
        manifest_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n', encoding='utf-8')
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=('stage', 'verify'))
    parser.add_argument('--repo', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--source-sha', required=True)
    parser.add_argument('--sdk-version', required=True)
    args = parser.parse_args()
    try:
        report = run(args.mode, args.repo.resolve(), args.output.resolve(), args.source_sha, args.sdk_version)
    except (LockError, OSError, ValueError) as exc:
        print(json.dumps({'status': 'FAIL', 'error': str(exc)}), file=sys.stderr)
        return 1
    print(json.dumps(report, sort_keys=True))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
