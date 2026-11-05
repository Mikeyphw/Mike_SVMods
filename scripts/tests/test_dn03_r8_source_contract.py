"""Source-only contract regressions for the staged R8 workflow."""
import pathlib
import tempfile
import unittest
from shutil import copyfile
from importlib.util import spec_from_file_location, module_from_spec

HERE = pathlib.Path(__file__).resolve()
ROOT = HERE.parents[2]
FILE = ROOT / 'scripts/dn03_r8_source_contract.py'
spec = spec_from_file_location('r8_contract', FILE)
module = module_from_spec(spec)
spec.loader.exec_module(module)


class R8SourceTests(unittest.TestCase):
    def test_active_source_contract(self):
        module.check(ROOT)

    def test_detect_missing_signing(self):
        with tempfile.TemporaryDirectory() as td:
            path = pathlib.Path(td)
            for f in (module.R8, module.R7, module.R6, module.R5,
                      'docs/ci/DN03_R8_GITHUB_PROVENANCE.md'):
                x = path / f
                x.parent.mkdir(parents=True, exist_ok=True)
                copyfile(ROOT / f, x)
            wf = path / module.R8
            wf.write_text(wf.read_text().replace('attestations: write', 'attestations: read'))
            with self.assertRaises(AssertionError):
                module.check(path)

    def test_detect_mutable_pin(self):
        with tempfile.TemporaryDirectory() as td:
            path = pathlib.Path(td)
            for f in (module.R8, module.R7, module.R6, module.R5,
                      'docs/ci/DN03_R8_GITHUB_PROVENANCE.md'):
                x = path / f
                x.parent.mkdir(parents=True, exist_ok=True)
                copyfile(ROOT / f, x)
            wf = path / module.R8
            wf.write_text(wf.read_text().replace('R8_DEVTOOL_SHA: ', 'R8_DEVTOOL_SHA_OLD: '))
            with self.assertRaises(AssertionError):
                module.check(path)

if __name__ == '__main__':
    unittest.main()
