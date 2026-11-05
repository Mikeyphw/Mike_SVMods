"""Source-only regression tests for portable CI matrix admission."""
import importlib.util
from pathlib import Path
import tempfile
import unittest

SOURCE = Path(__file__).resolve().parents[1] / 'dn03_final_source_contract.py'
spec = importlib.util.spec_from_file_location('final_contract', SOURCE)
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)
WF = mod.WORKFLOW
DEV = 'e' * 40


class SourceTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        base=Path(__file__).resolve().parents[2]
        installed=base/'.github/workflows/devtool-dn03-final-cross-platform.yml'
        packaged=base/'.github/workflows/devtool-dn03-final-cross-platform.yml.tmpl'
        template=(installed if installed.is_file() else packaged).read_text()
        template=template.replace('__FINAL_DEVTOOL_SHA__',DEV)
        for path in (*mod.LEGACY, WF, '.github/workflows/ci.yml',
                     'Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj',
                     'Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj'):
            f=self.root/path
            f.parent.mkdir(parents=True,exist_ok=True)
            f.write_text(template if path==WF else
                         'jobs:\n  source-contracts:\n    steps:\n      - run: python3 scripts/dn03_final_source_contract.py\n'
                         if path.endswith('ci.yml') else 'baseline')
        self.wf=self.root/WF

    def bad(self, text):
        self.wf.write_text(text)
        with self.assertRaises(AssertionError):
            mod.check(self.root)

    def test_positive(self):
        mod.check(self.root)

    def test_missing_windows(self):
        self.bad(self.wf.read_text().replace('ubuntu-24.04, windows-2022',
                                             'ubuntu-24.04'))

    def test_drifting_devtool_pin(self):
        # The installed workflow has a real Git SHA, not the fixture's
        # synthetic 'eeee...' SHA. Mutate whichever immutable pin is present.
        before = self.wf.read_text()
        pins = mod.PIN_RE.findall(before)
        self.assertEqual(len(pins), 2)
        self.assertEqual(pins[0], pins[1])
        altered = 'a' * 40 if pins[0] != 'a' * 40 else 'b' * 40
        original_field = f"CI_DEVTOOL_SHA: '{pins[0]}'"
        altered_field = f"CI_DEVTOOL_SHA: '{altered}'"
        mutated = before.replace(original_field, altered_field, 1)
        self.assertNotEqual(mutated, before, 'test must really mutate the SHA')
        self.bad(mutated)

    def test_missing_ephemeral_sdk_pin(self):
        self.bad(self.wf.read_text().replace('Pin exact SDK selection in ephemeral checkout',
                                             'SDK policy removed'))

    def test_sdk_roll_forward_forbidden(self):
        self.bad(self.wf.read_text().replace('"rollForward":"disable"',
                                             '"rollForward":"latestMajor"'))

    def test_sdk_version_drift_forbidden(self):
        self.bad(self.wf.read_text().replace('"version":"10.0.100"',
                                             '"version":"10.0.101"'))

    def test_no_raw_trx_review(self):
        self.bad(self.wf.read_text().replace('review --source $env:GITHUB_SHA',
                                             'none --source $env:GITHUB_SHA'))

    def test_no_aggregate_gate(self):
        self.bad(self.wf.read_text().replace('DN03_FINAL_58_OF_58_CROSS_PLATFORM_GATE_PASS','REMOVED'))

    def test_no_locked_mode(self):
        self.bad(self.wf.read_text().replace('--locked-mode --no-http-cache',
                                             '--no-http-cache'))

    def test_forged_signing(self):
        self.bad(self.wf.read_text() + '\n      id-token: write\n')

    def test_continue_on_error(self):
        self.bad(self.wf.read_text() + '\ncontinue-on-error: true\n')

    def test_missing_legacy(self):
        (self.root/mod.LEGACY[0]).unlink()
        with self.assertRaises(AssertionError):
            mod.check(self.root)

    def test_missing_ci_gate(self):
        (self.root/'.github/workflows/ci.yml').write_text('jobs: {}\n')
        with self.assertRaises(AssertionError):
            mod.check(self.root)


if __name__=='__main__':
    unittest.main()
