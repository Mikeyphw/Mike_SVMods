#!/usr/bin/env python3
"""R06b deterministic source-only migration: host-only Devtool .NET workflow.

The migration never executes dotnet.  Its caller must enforce the source commit
and original blob identity before writing the declared .devtool.toml path.
"""
from __future__ import annotations

import copy
import json
import re
import tomllib
from pathlib import Path

WORKFLOW = 'ci-hosted-dotnet'
DEVTOOL_SHA = 'b164463db51e2cffd12f753043b9e55a08cc43ae'
PROJECTS = {
    'pure': 'Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj',
    'animal': 'Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj',
}
NAMES = ('r06b_restore_pure', 'r06b_test_pure', 'r06b_restore_animal', 'r06b_test_animal')


def job_specs() -> dict[str, dict]:
    pure, animal = PROJECTS['pure'], PROJECTS['animal']
    return {
        'r06b_restore_pure': {
            'runner': 'command', 'root': '..',
            'command': ['dotnet', 'restore', pure, '--locked-mode', '--no-http-cache'],
            'inputs': ['.devtool.toml', 'Directory.Build.props', pure,
                       'Tests/RoosterControl.PureTests/packages.lock.json', 'RoosterControl.Pure/**'],
            'outputs': ['RoosterControl.Pure/obj/**', 'Tests/RoosterControl.PureTests/obj/**'],
        },
        'r06b_test_pure': {
            'runner': 'command', 'root': '..',
            'command': ['dotnet', 'test', pure, '-c', 'Release', '--no-restore',
                        '--logger', 'trx;LogFileName=r06b-pure.trx', '--results-directory', '.ci-results/r06b'],
            'inputs': ['.devtool.toml', 'Directory.Build.props', pure,
                       'RoosterControl.Pure/**', 'Tests/RoosterControl.PureTests/**'],
            'outputs': ['RoosterControl.Pure/bin/**', 'RoosterControl.Pure/obj/**',
                        'Tests/RoosterControl.PureTests/bin/**', 'Tests/RoosterControl.PureTests/obj/**',
                        '.ci-results/r06b/r06b-pure.trx'],
        },
        'r06b_restore_animal': {
            'runner': 'command', 'root': '..',
            'command': ['dotnet', 'restore', animal, '--locked-mode', '--no-http-cache'],
            'inputs': ['.devtool.toml', 'Directory.Build.props', animal,
                       'Tests/EggsInWild.AnimalNotes.Tests/packages.lock.json',
                       'EggsInWild/Application/AnimalNotes/**'],
            'outputs': ['Tests/EggsInWild.AnimalNotes.Tests/obj/**'],
        },
        'r06b_test_animal': {
            'runner': 'command', 'root': '..',
            'command': ['dotnet', 'test', animal, '-c', 'Release', '--no-restore',
                        '--logger', 'trx;LogFileName=r06b-animalnotes.trx',
                        '--results-directory', '.ci-results/r06b'],
            'inputs': ['.devtool.toml', 'Directory.Build.props', animal,
                       'EggsInWild/Application/AnimalNotes/**', 'Tests/EggsInWild.AnimalNotes.Tests/**'],
            'outputs': ['Tests/EggsInWild.AnimalNotes.Tests/bin/**',
                        'Tests/EggsInWild.AnimalNotes.Tests/obj/**',
                        '.ci-results/r06b/r06b-animalnotes.trx'],
        },
    }


def steps() -> list[dict]:
    return [
        {'id': 'source', 'ref': 'job:ci-source', 'depends_on': []},
        {'id': 'restore_pure', 'ref': 'job:r06b_restore_pure', 'depends_on': ['source']},
        {'id': 'test_pure', 'ref': 'job:r06b_test_pure', 'depends_on': ['restore_pure']},
        {'id': 'restore_animal', 'ref': 'job:r06b_restore_animal', 'depends_on': ['test_pure']},
        {'id': 'test_animal', 'ref': 'job:r06b_test_animal', 'depends_on': ['restore_animal']},
    ]


def _toml_array(items: list[str]) -> str:
    return '[' + ', '.join(json.dumps(x) for x in items) + ']'


def _job_table(name: str, job: dict) -> str:
    return (f'\n[targets.svmods.jobs.{name}]\n'
            'runner = "command"\nroot = ".."\n'
            f'command = {_toml_array(job["command"])}\n'
            f'inputs = {_toml_array(job["inputs"])}\n'
            f'outputs = {_toml_array(job["outputs"])}\n')


def migration(source: bytes) -> bytes:
    """Inject just R06b workflow and 4 jobs; prove all prior TOML data is intact."""
    text = source.decode('utf-8')
    if not text.endswith('\n') or '\r' in text:
        raise ValueError('R06b expects normalized LF source configuration')
    before = tomllib.loads(text)
    target = before.get('targets', {}).get('svmods')
    if not isinstance(target, dict):
        raise ValueError('Missing svmods target')
    if before.get('execution', {}).get('environment') != 'auto':
        raise ValueError('Host-auto policy must remain enabled')
    if target.get('root') != 'Tests':
        raise ValueError('R03 scoped owner not present')
    if target.get('jobs', {}).get('ci-source', {}).get('metadata', {}).get('read_only') is not True:
        raise ValueError('Missing read-only native ci-source job')
    if target.get('workflows', {}).get('ci-preflight') != [
        {'id': 'source', 'ref': 'job:ci-source', 'depends_on': []}
    ]:
        raise ValueError('R06a native source workflow was changed')
    if WORKFLOW in target.get('workflows', {}):
        raise ValueError('R06b workflow already exists; refusing duplicate application')
    if any(k in target.get('jobs', {}) for k in NAMES):
        raise ValueError('R06b job already exists; refusing duplicate application')
    marker = re.compile(r'(?m)^\[targets\.svmods\.workflows\]\n')
    matches = list(marker.finditer(text))
    if len(matches) != 1:
        raise ValueError('Expected one svmods workflow TOML table')
    pos = matches[0].end()
    later = re.search(r'(?m)^\[', text[pos:])
    end = pos + later.start() if later else len(text)
    wf = '\n' + WORKFLOW + ' = [\n'
    for step in steps():
        deps = _toml_array(step['depends_on'])
        wf += f'  {{id = "{step["id"]}", ref = "{step["ref"]}", depends_on = {deps}}},\n'
    wf += ']\n'
    text = text[:end] + wf + text[end:]
    for key, job in job_specs().items():
        text += _job_table(key, job)
    actual = tomllib.loads(text)
    expected = copy.deepcopy(before)
    expected['targets']['svmods'].setdefault('workflows', {})[WORKFLOW] = steps()
    expected['targets']['svmods'].setdefault('jobs', {}).update(job_specs())
    if actual != expected:
        raise ValueError('R06b migration unexpectedly changed prior TOML settings')
    if validate(actual):
        raise ValueError('R06b result violates contract')
    return text.encode('utf-8')


def validate(cfg: dict) -> list[str]:
    errors: list[str] = []
    if cfg.get('execution', {}).get('environment') != 'auto':
        errors.append('Host auto environment changed')
    target = cfg.get('targets', {}).get('svmods', {})
    if target.get('root') != 'Tests':
        errors.append('Scoped svmods root changed')
    if target.get('workflows', {}).get(WORKFLOW) != steps():
        errors.append('R06b chain/graph changed')
    if target.get('jobs', {}).get('ci-source', {}).get('metadata', {}).get('read_only') is not True:
        errors.append('Source job no longer read-only')
    for name, spec in job_specs().items():
        if target.get('jobs', {}).get(name) != spec:
            errors.append(f'R06b target-local job drift: {name}')
    if cfg.get('wrapper', {}).get('commands', {}).get(WORKFLOW):
        errors.append('R06b hosted .NET workflow must not be exposed as local wrapper command')
    return errors


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    try:
        cfg = tomllib.loads((root / '.devtool.toml').read_text())
        errors = validate(cfg)
    except (OSError, ValueError) as exc:
        errors = [str(exc)]
    print(json.dumps({'status': 'FAIL' if errors else 'PASS', 'errors': errors,
                      'local_dotnet_execution': 'NOT_RUN'}, sort_keys=True))
    return int(bool(errors))


if __name__ == '__main__':
    raise SystemExit(main())
