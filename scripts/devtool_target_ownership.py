#!/usr/bin/env python3
"""R03: deterministic Devtool target-ownership migration and source-only checks.

No .NET commands or game files are needed. The real Devtool CLI remains the
source of truth for runtime affected selection; these are regression contracts.
"""
from __future__ import annotations

from copy import deepcopy
from pathlib import Path
import fnmatch
import json
import posixpath
import re
import sys
import tomllib

MODS = ("nestcore", "letmehatch", "eggsinwild", "roostercontrol")
ROOTS = {
    "svmods": "Tests",
    "nestcore": "NestCore",
    "letmehatch": "LetMeHatch",
    "eggsinwild": "EggsInWild",
    "roostercontrol": "RoosterControl",
    "roostercontrol_pure": "RoosterControl.Pure",
    "pure": "Tests/RoosterControl.PureTests",  # retained compatibility alias
    "embed_tool": "tools/EmbedTextAsCode",
}
SHARED = ["../.devtool.toml", "../Directory.Build.props", "../Directory.Build.targets", "../.github/workflows/**"]
INPUTS = {
    "svmods": ["../.devtool.toml", "../Directory.Build.props", "../Directory.Build.targets", "../Mike_SVMods.sln", "../scripts/**", "../.github/workflows/**"],
    "nestcore": SHARED,
    "letmehatch": [*SHARED, "../Tests/LetMeHatch.Core.Tests/**", "../Tests/LetMeHatch.Tests/**"],
    "eggsinwild": [*SHARED, "../Tests/EggsInWild.AnimalNotes.Tests/**", "../Tests/EggsInWild.Tests/**", "../Tests/EggsInWild.Phase7.Tests/**"],
    "roostercontrol": [*SHARED, "../Tests/RoosterControl.Descriptors.Tests/**", "../Tests/RoosterControl.Tests/**"],
    "roostercontrol_pure": [*SHARED, "../Tests/RoosterControl.PureTests/**", "../RoosterControl/Application/Catalogs/**", "../RoosterControl/Framework/Config/**", "../RoosterControl/Constants.cs", "../RoosterControl/Enums.cs"],
    "pure": ["../../.devtool.toml", "../../Directory.Build.props", "../../Directory.Build.targets", "../../.github/workflows/**", "../../RoosterControl.Pure/**", "../../RoosterControl/Application/Catalogs/**", "../../RoosterControl/Framework/Config/**", "../../RoosterControl/Constants.cs", "../../RoosterControl/Enums.cs"],
    "embed_tool": ["../../.devtool.toml", "../../Directory.Build.props", "../../Directory.Build.targets", "../../.github/workflows/**"],
}
OUTPUTS = {
    "svmods": ["**/bin/**", "**/obj/**"],
    "nestcore": ["bin/**", "obj/**"],
    "letmehatch": ["bin/**", "obj/**", "../Tests/LetMeHatch.Core.Tests/bin/**", "../Tests/LetMeHatch.Core.Tests/obj/**", "../Tests/LetMeHatch.Tests/bin/**", "../Tests/LetMeHatch.Tests/obj/**"],
    "eggsinwild": ["bin/**", "obj/**", "../Tests/EggsInWild.AnimalNotes.Tests/bin/**", "../Tests/EggsInWild.AnimalNotes.Tests/obj/**", "../Tests/EggsInWild.Tests/bin/**", "../Tests/EggsInWild.Tests/obj/**", "../Tests/EggsInWild.Phase7.Tests/bin/**", "../Tests/EggsInWild.Phase7.Tests/obj/**"],
    "roostercontrol": ["bin/**", "obj/**", "../Tests/RoosterControl.Descriptors.Tests/bin/**", "../Tests/RoosterControl.Descriptors.Tests/obj/**", "../Tests/RoosterControl.Tests/bin/**", "../Tests/RoosterControl.Tests/obj/**"],
    "roostercontrol_pure": ["bin/**", "obj/**", "../Tests/RoosterControl.PureTests/bin/**", "../Tests/RoosterControl.PureTests/obj/**"],
    "pure": ["bin/**", "obj/**"],
    "embed_tool": ["bin/**", "obj/**"],
}
# Cross-target dependencies are declared by *source-only* workflows. The real
# build/test workflows stay unchanged for R03 (R04/R06 will host them in CI).
DEPENDENCIES = {
    "svmods": ["nestcore", "letmehatch", "eggsinwild", "roostercontrol", "roostercontrol_pure", "pure", "embed_tool"],
    "nestcore": [],
    "letmehatch": ["nestcore", "embed_tool"],
    "eggsinwild": ["nestcore", "embed_tool"],
    "roostercontrol": ["nestcore", "embed_tool"],
    "roostercontrol_pure": [],
    "pure": ["roostercontrol_pure"],
    "embed_tool": [],
}

def _render_array(key: str, values: list[str]) -> str:
    return f'{key} = [\n' + ''.join(f'    "{item}",\n' for item in values) + ']\n'


def _edit_section(source: str, header: str, edit) -> str:
    marker = f'[{header}]\n'
    if source.count(marker) != 1:
        raise ValueError(f'R03 expected one section {header}')
    offset = source.index(marker) + len(marker)
    next_header = re.search(r'^\[', source[offset:], flags=re.MULTILINE)
    end = offset + next_header.start() if next_header else len(source)
    return source[:offset] + edit(source[offset:end]) + source[end:]


def _replace_exact_field(body: str, key: str, old: str, new: str) -> str:
    pattern = re.compile(r'^' + re.escape(key) + r' = ' + re.escape(old) + r'$', re.MULTILINE)
    text, count = pattern.subn(lambda _: f'{key} = {new}', body)
    if count != 1:
        raise ValueError(f'R03 expected one {key} = {old}; got {count}')
    return text


def _replace_list(body: str, key: str, values: list[str]) -> str:
    regex = re.compile(r'^' + re.escape(key) + r' = \[\n.*?^\]\n', flags=re.MULTILINE | re.DOTALL)
    result, count = regex.subn(lambda _: _render_array(key, values), body)
    if count != 1:
        raise ValueError(f'R03 expected one {key} list; got {count}')
    return result


def _graph(name: str) -> str:
    deps = DEPENDENCIES[name]
    if not deps:
        return '[{id = "source", ref = "job:target-ownership-contract", depends_on = []}]'
    steps = []
    for target in deps:
        workflow = "ownership-probe" if not DEPENDENCIES[target] else "ownership-graph"
        steps.append(f'  {{id = "dep_{target}", ref = "target:{target}#{workflow}", depends_on = []}},')
    return '[\n' + '\n'.join(steps) + '\n]'


def migrate_source(source: bytes) -> bytes:
    """Perform only reviewed, schema-preserving changes to an exact R02b base."""
    original = source.decode('utf-8')
    if '\r' in original or not original.endswith('\n'):
        raise ValueError('R03 requires the LF-terminated .devtool.toml baseline')
    before = tomllib.loads(original)
    if before.get('execution', {}).get('environment') != 'auto':
        raise ValueError('R02b must be applied first')
    if set(before.get('targets', {})) != set(ROOTS):
        raise ValueError('unexpected target set; refusing to rebase blindly')
    if any(before['targets'][name].get('root') != '.' for name in ROOTS):
        raise ValueError('target root already changed from audited base')

    text = original
    for name in ROOTS:
        def update(body: str, current: str = name) -> str:
            body = _replace_exact_field(body, 'root', '"."', f'"{ROOTS[current]}"')
            body = _replace_list(body, 'inputs', INPUTS[current])
            body = _replace_list(body, 'outputs', OUTPUTS[current])
            if current == 'svmods':
                body = _replace_exact_field(body, 'test_command', '"./Tests/run-tests.sh"', '"./run-tests.sh"')
            return body
        text = _edit_section(text, f'targets.{name}', update)

    # Keep repository-relative source job command and all pre-existing tests.
    text = _edit_section(text, 'targets.svmods.jobs.ci-source', lambda body: 'root = ".."\n' + body)
    for name in ('svmods', 'letmehatch', 'eggsinwild', 'roostercontrol'):
        text = _edit_section(text, f'targets.{name}.workflows',
                             lambda body, n=name: body.rstrip('\n') + f'\nownership-graph = {_graph(n)}\n\n')
    for name in ('nestcore', 'roostercontrol_pure', 'pure', 'embed_tool'):
        if f'[targets.{name}.workflows]' in text:
            raise ValueError(f'unexpected {name}.workflows already present')
        text += f'\n[targets.{name}.workflows]\n{"ownership-probe" if not DEPENDENCIES[name] else "ownership-graph"} = {_graph(name)}\n'
    if '[jobs.target-ownership-contract]' in text:
        raise ValueError('existing contract job would be overwritten')
    text += ('\n[jobs.target-ownership-contract]\n'
             'runner = "command"\n'
             'root = "."\n'
             'command = ["python3", "scripts/devtool_target_ownership.py", "--json"]\n'
             'inputs = [".devtool.toml", "scripts/devtool_target_ownership.py"]\n'
             'outputs = []\n'
             '\n[jobs.target-ownership-contract.metadata]\nread_only = true\n')

    new = tomllib.loads(text)
    expected = deepcopy(before)
    for name in ROOTS:
        target = expected['targets'][name]
        target['root'] = ROOTS[name]
        target['inputs'] = INPUTS[name]
        target['outputs'] = OUTPUTS[name]
        if name == 'svmods':
            target['test_command'] = './run-tests.sh'
            target['jobs']['ci-source']['root'] = '..'
        target.setdefault('workflows', {})['ownership-probe' if not DEPENDENCIES[name] else 'ownership-graph'] = [
            {'id': 'source', 'ref': 'job:target-ownership-contract', 'depends_on': []}
        ] if not DEPENDENCIES[name] else [
            {'id': f'dep_{dep}', 'ref': f'target:{dep}#{"ownership-probe" if not DEPENDENCIES[dep] else "ownership-graph"}', 'depends_on': []}
            for dep in DEPENDENCIES[name]
        ]
    expected['jobs']['target-ownership-contract'] = {
        'runner': 'command', 'root': '.',
        'command': ['python3', 'scripts/devtool_target_ownership.py', '--json'],
        'inputs': ['.devtool.toml', 'scripts/devtool_target_ownership.py'], 'outputs': [],
        'metadata': {'read_only': True},
    }
    if expected != new:
        raise ValueError('R03 transformation changed unintended TOML keys')
    issues = validate(new)
    if issues:
        raise ValueError('R03 ownership contract failed: ' + '; '.join(issues))
    return text.encode('utf-8')


def _normalize(root: str, pattern: str) -> str:
    normalized = posixpath.normpath(posixpath.join(root, pattern))
    if normalized in ('.', '..') or normalized.startswith('../') or normalized.startswith('/'):
        raise ValueError('invalid owner pattern: ' + root + '/' + pattern)
    return normalized


def _deps(config: dict) -> dict[str, set[str]]:
    targets = config['targets']
    dependency_map = {name: set() for name in targets}
    for name, target in targets.items():
        for workflow in target.get('workflows', {}).values():
            for step in workflow:
                ref = step.get('ref', '') if isinstance(step, dict) else step
                if isinstance(ref, str) and ref.startswith('target:'):
                    other = ref[7:].split('#', 1)[0]
                    dependency_map[name].add(other)
    return dependency_map


def select_affected(config: dict, paths: list[str]) -> set[str]:
    """Independent conservative fixture model (not a substitute for Devtool CLI)."""
    targets = config['targets']
    directly_owned: set[str] = set()
    uncertain = False
    for path in paths:
        matched: set[str] = set()
        for name, target in targets.items():
            root = target['root'].strip('/')
            if target.get('affected_ownership', 'root-and-inputs') == 'root-and-inputs' and (path == root or path.startswith(root + '/')):
                matched.add(name)
            for raw in target.get('inputs', []):
                pattern = _normalize(root, raw)
                if fnmatch.fnmatchcase(path, pattern) or path == pattern or path.startswith(pattern.rstrip('/') + '/'):
                    matched.add(name)
        if not matched:
            uncertain = True
        directly_owned |= matched
    if uncertain:
        return set(targets)
    deps = _deps(config)
    changed = set(directly_owned)
    while True:
        expanded = changed | {target for target, needed in deps.items() if needed & changed}
        if changed == expanded:
            return changed
        changed = expanded


EXPECTED_PATHS = {
    'EggsInWild/ModEntry.cs': {'eggsinwild', 'svmods'},
    'LetMeHatch/ModEntry.cs': {'letmehatch', 'svmods'},
    'NestCore/NestCore.csproj': {'nestcore', 'letmehatch', 'eggsinwild', 'roostercontrol', 'svmods'},
    'tools/EmbedTextAsCode/Program.cs': {'embed_tool', 'letmehatch', 'eggsinwild', 'roostercontrol', 'svmods'},
    'Tests/RoosterControl.PureTests/CatalogApplyModeTests.cs': {'svmods', 'pure', 'roostercontrol_pure', 'dn03_r5'},
    'RoosterControl/ModEntry.cs': {'roostercontrol', 'svmods'},
}


def validate(config: dict) -> list[str]:
    errors: list[str] = []
    targets = config.get('targets', {})
    for name, expected_root in ROOTS.items():
        target = targets.get(name)
        if target is None:
            errors.append(f'missing target {name}')
            continue
        if target.get('root') != expected_root:
            errors.append(f'{name}: root should be {expected_root}')
        if target.get('inputs') != INPUTS[name]:
            errors.append(f'{name}: ownership inputs drifted')
        if target.get('outputs') != OUTPUTS[name]:
            errors.append(f'{name}: output scopes drifted')
        for pattern in (*target.get('inputs', []), *target.get('outputs', [])):
            try:
                _normalize(expected_root, pattern)
            except ValueError as exc:
                errors.append(str(exc))
    if 'dn03_r5' in targets:
        d = targets['dn03_r5']
        if d.get('root') != '.' or d.get('affected_ownership') != 'inputs-only' or not d.get('inputs'):
            errors.append('dn03_r5 must declare nonempty inputs-only ownership')
    job = config.get('jobs', {}).get('target-ownership-contract', {})
    if job.get('command') != ['python3', 'scripts/devtool_target_ownership.py', '--json'] or job.get('metadata', {}).get('read_only') is not True:
        errors.append('target-ownership-contract must be Python-only and read-only')
    if targets.get('svmods', {}).get('jobs', {}).get('ci-source', {}).get('root') != '..':
        errors.append('svmods source job root must resolve to repo root')
    if targets.get('svmods', {}).get('test_command') != './run-tests.sh':
        errors.append('svmods test command must be relative to Tests root')
    deps = _deps(config)
    for name, expected in DEPENDENCIES.items():
        if deps.get(name) != set(expected):
            errors.append(f'{name}: wrong graph dependencies: {sorted(deps.get(name, set()))}')
    if not errors:
        for path, expected in EXPECTED_PATHS.items():
            # R03 migration fixtures intentionally contain the original target
            # set; real DN03-R5 consumer checkouts contain the additional target.
            # Never expect a nonexistent target in an R03-only fixture, and
            # never discard dn03_r5 when it is actually configured.
            expected_for_config = (expected if 'dn03_r5' in targets
                                   else expected - {'dn03_r5'})
            actual = select_affected(config, [path])
            if actual != expected_for_config:
                errors.append(f'{path}: selected {sorted(actual)} != expected {sorted(expected_for_config)}')
        if select_affected(config, ['docs/unknown-unowned-file.md']) != set(targets):
            errors.append('unknown paths must fall back conservatively')
        # The original R03 target set still owns this shared file.
        # DN03-R5 must opt into the same ownership explicitly before
        # this fixture can claim the new target is affected here.
        if select_affected(config, ['Directory.Build.targets']) != set(ROOTS):
            errors.append('shared MSBuild metadata must select every target')
    return sorted(set(errors))


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    config = tomllib.loads((root / '.devtool.toml').read_text(encoding='utf-8'))
    issues = validate(config)
    report = {'status': 'PASS' if not issues else 'FAIL', 'issues': issues,
              'selection_model': 'source-fixture-only', 'devtool_runtime_affected': 'NOT_RUN',
              'dotnet_local_execution': 'NOT_RUN'}
    print(json.dumps(report, sort_keys=True))
    return 1 if issues else 0


if __name__ == '__main__':
    raise SystemExit(main())
