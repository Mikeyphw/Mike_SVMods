"""DN03-FINAL static source contract for the portable matrix (no .NET required)."""
from __future__ import annotations
from pathlib import Path
import re
import sys

WORKFLOW = '.github/workflows/devtool-dn03-final-cross-platform.yml'
LEGACY = ('.github/workflows/devtool-dn03-r7-native-ci.yml',
          '.github/workflows/devtool-dn03-r8-provenance.yml')
PIN_RE = re.compile(r"CI_DEVTOOL_SHA: '([0-9a-f]{40})'")


def check(root: Path) -> None:
    for rel in (*LEGACY, WORKFLOW):
        path = root / rel
        assert path.is_file() and not path.is_symlink(), f'missing or linked: {rel}'
    s = (root / WORKFLOW).read_text(encoding='utf-8')
    assert len(s.encode()) < 20_000, 'oversized workflow'
    pins = PIN_RE.findall(s)
    assert len(pins) == 2 and pins[0] == pins[1], 'two identical immutable Devtool pins required'
    assert '__FINAL_DEVTOOL_SHA__' not in s
    assert s.count('    runs-on: ${{ matrix.host }}') == 1
    assert 'host: [ubuntu-24.04, windows-2022]' in s
    assert '      fail-fast: false' in s
    assert '      - name: Fail-closed hosted source contract' in s
    assert '      - name: Verify SDK, runtime, source and pinned Devtool' in s
    assert '10.0.100' in s and '6.0.428' in s and '6\\.0\\.36' in s
    assert s.count('      - name: Pin exact SDK selection in ephemeral checkout') == 1, 'ephemeral SDK pin missing'
    assert "Join-Path $env:GITHUB_WORKSPACE 'global.json'" in s, 'SDK pin must live only in checkout'
    assert '"sdk":{"version":"10.0.100","rollForward":"disable","allowPrerelease":false}' in s, 'SDK selection must be exact'
    assert 'if (Test-Path -LiteralPath $globalJson)' in s, 'do not overwrite global.json'
    assert 'dotnet restore $t.project --locked-mode --no-http-cache' in s
    assert '--no-restore' in s
    assert 'actions_ci_qualification.py' in s
    assert 'review --source $env:GITHUB_SHA' in s
    assert 'aggregate \\' in s
    assert s.count('DN03_FINAL_58_OF_58_CROSS_PLATFORM_GATE_PASS') == 1
    assert '    if: always()' in s
    assert "test \"$CI_MATRIX_RESULT\" = success" in s
    assert '--profiles ubuntu-24.04 windows-2022' in s
    assert '--test pure Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj 21' in s
    assert '--test animal Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj 8' in s
    assert '      - name: Upload original host-local test receipts (not remote authentication)' in s
    assert '      - name: Upload cross-platform reviewed evidence (non-release)' in s
    assert '          persist-credentials: false' in s
    assert '  contents: read' in s
    assert not re.search(r'\b(?:id-token|attestations|packages): write\b', s), 'unnecessary signing permissions'
    assert 'continue-on-error' not in s, 'cannot ignore failing tests'
    assert not re.search(r'(?m)^\s*(?:NUGET_PACKAGES|CI_DEVTOOL_PATH):\s*\$\{\{ runner\.',s), \
        'runner expression forbidden in job env'
    assert all((root / x).is_file() for x in (
        'Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj',
        'Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj'))
    ci = (root/'.github/workflows/ci.yml').read_text(encoding='utf-8')
    assert ci.count('run: python3 scripts/dn03_final_source_contract.py') == 1, 'CI source check missing'


if __name__ == '__main__':
    try:
        check(Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path.cwd())
    except (AssertionError, OSError) as err:
        print('DN03_FINAL_SOURCE_CONTRACT_REFUSED: ' + str(err), file=sys.stderr)
        raise SystemExit(2)
    print('DN03_FINAL_SOURCE_CONTRACT_PASS')
