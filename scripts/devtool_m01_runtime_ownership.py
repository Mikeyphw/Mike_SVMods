"""M01 Devtool runtime selector verification (no dotnet or game execution)."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys
import tomllib


def verify(repo: Path, devtool: Path) -> dict:
    repo = repo.expanduser().resolve()
    devtool = devtool.expanduser().resolve()
    if not (devtool / 'devtool' / 'affected.py').is_file():
        raise ValueError('a real Devtool source checkout is required')
    data = tomllib.loads((repo / '.devtool.toml').read_text(encoding='utf-8'))
    dn03 = data['targets']['dn03_r5']
    if dn03.get('root') != '.' or dn03.get('affected_ownership') != 'inputs-only':
        raise ValueError('dn03_r5 must use explicit inputs-only ownership')
    if not dn03['inputs']:
        raise ValueError('dn03_r5 explicit inputs missing')
    sys.path.insert(0, str(devtool))
    from devtool.config import load_wrapper_project_config
    from devtool.affected import analyze_affected_targets, _target_match_reasons
    cfg = load_wrapper_project_config(str(repo), str(repo / '.devtool.toml'))
    if cfg.targets['dn03_r5'].affected_ownership != 'inputs-only':
        raise ValueError('real Devtool loader did not honor dn03_r5 ownership')
    cases = {
        'mod-unrelated': ('LetMeHatch/ModEntry.cs', False),
        'pure-test': ('Tests/RoosterControl.PureTests/CatalogApplyModeTests.cs', True),
        'animal-test': ('Tests/EggsInWild.AnimalNotes.Tests/AnimalNoteIdsTests.cs', True),
        'shared-lock': ('global.json', True),
    }
    results = {}
    for label, (path, expected) in cases.items():
        actual = bool(_target_match_reasons(cfg, 'dn03_r5', path))
        if actual != expected:
            raise ValueError(f'{label}: expected direct {expected}, observed {actual}')
        selection = analyze_affected_targets(cfg, changed_paths=[path])
        if ('dn03_r5' in selection.direct_targets) != expected:
            raise ValueError(f'{label}: affected selector disagrees with direct reasons')
        results[label] = {'path': path, 'direct_owner': actual}
    # Unowned sentinel must invoke the conservative candidate-wide fallback.
    unknown = 'm01-unowned-unknown/fixture.bin'
    result = analyze_affected_targets(cfg, changed_paths=[unknown])
    if unknown not in result.uncertain_paths or set(result.targets) != set(cfg.targets):
        raise ValueError('unowned source did not conservatively select all targets')
    return {'status': 'PASS', 'source_only': True, 'target_count': len(cfg.targets),
            'scenarios': results, 'unknown_file': 'workspace-wide'}


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[1])
    p.add_argument('--devtool', type=Path, default=Path.home() / 'Code/devtool')
    args = p.parse_args()
    try:
        report = verify(args.repo, args.devtool)
    except (OSError, ValueError, KeyError, SystemExit) as exc:
        print(f'MIKE_SVMODS_M01_RUNTIME_REFUSED: {exc}', file=sys.stderr)
        return 2
    print(json.dumps(report, indent=2, sort_keys=True))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
