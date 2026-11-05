"""Fail-closed source-only R8 workflow contract (no .NET or remote access)."""
from pathlib import Path
import re
import sys

R8 = '.github/workflows/devtool-dn03-r8-provenance.yml'
R7 = '.github/workflows/devtool-dn03-r7-native-ci.yml'
R6 = '.github/workflows/devtool-dn03r4-dn03_r5-native_r6.yml'
R5 = '.github/workflows/dn03-r5-source-qualification.yml'


def check(root: Path) -> None:
    for p in (R8, R7, R6, R5):
        f = root / p
        assert f.is_file() and not f.is_symlink(), f'workflow missing or linked: {p}'
    data = (root / R8).read_text(encoding='utf8')
    assert '__R8_DEVTOOL_SHA__' not in data
    match = re.search(r"R8_DEVTOOL_SHA: '([0-9a-f]{40})'", data)
    assert match and match.group(1) != '0' * 40
    assert "workflows: ['DN03-R7 native .NET CI (29 tests, non-release)']" in data
    assert '    types: [completed]' in data
    assert '    branches: [main]' in data
    assert '    if:' in data and "github.event_name == 'workflow_run'" in data
    assert '      id-token: write' in data and '      attestations: write' in data
    assert data.count('actions/attest@1e69f48acb82d1966a394da916b4c1698aa569d6') == 1
    assert 'create-storage-record: false' in data
    assert data.count('dn03-r8-provenance-observation') == 2
    assert 'DN03_R8_SIGSTORE_REPORT_ATTESTATION_PASS' in data
    assert "test \"$R8_TRIGGER_EVENT\" = 'push'" in data
    assert "test \"$R8_TRIGGER_BRANCH\" = 'main'" in data
    assert '      - name: Recheck remote R7 run and all fifteen GitHub artifact archives' in data
    assert '      - name: Upload independent GitHub API observation' in data
    assert '      - name: Independently verify report attestation' in data
    assert (root / 'docs/ci/DN03_R8_GITHUB_PROVENANCE.md').is_file()
    assert len(data.encode()) < 20_000


if __name__ == '__main__':
    check(Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path.cwd())
    print('DN03_R8_SOURCE_CONTRACT_PASS')
