"""R05b lockfile artifact/provenance tests; all fixtures use JSON, no .NET."""
from pathlib import Path
import json
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_nuget_locks import PROJECTS, LockError, run, verify_lock

SHA = 'a' * 40
SDK = '10.0.101'


class TestNugetLocks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.repo = Path(self.temp.name)
        self.output = self.repo / '.ci-results' / 'r05b'
        self.lockpaths = []
        for project in PROJECTS:
            d = self.repo / project
            d.mkdir(parents=True)
            name = Path(project).name
            (d / (name + '.csproj')).write_text('<Project><ItemGroup><PackageReference Include="xunit" Version="2.7.0" /></ItemGroup></Project>')
            lock = d / 'packages.lock.json'
            lock.write_text(json.dumps({'version': 1, 'dependencies': {'net6.0': {
                'xunit': {'type': 'Direct', 'requested': '[2.7.0, )', 'resolved': '2.7.0', 'contentHash': 'sha-base64'}}}}))
            self.lockpaths.append(lock)

    def test_stage_and_verify(self):
        first = run('stage', self.repo, self.output, SHA, SDK)
        self.assertFalse(first['locked_restore_verified'])
        second = run('verify', self.repo, self.output, SHA, SDK)
        self.assertTrue(second['locked_restore_verified'])
        self.assertEqual(sorted(PROJECTS), sorted(second['projects']))

    def test_reject_missing_lock(self):
        self.lockpaths[0].unlink()
        with self.assertRaises(LockError):run('stage', self.repo, self.output, SHA, SDK)

    def test_reject_empty_packages(self):
        self.lockpaths[0].write_text(json.dumps({'version': 1, 'dependencies': {}}))
        with self.assertRaises(LockError):run('stage', self.repo, self.output, SHA, SDK)

    def test_reject_missing_direct_dependency(self):
        text = self.lockpaths[0].read_text().replace('xunit', 'MISSING')
        self.lockpaths[0].write_text(text)
        with self.assertRaises(LockError):run('stage', self.repo, self.output, SHA, SDK)

    def test_reject_missing_content_hash(self):
        text = self.lockpaths[0].read_text().replace('"contentHash": "sha-base64"', '"contentHash": ""')
        self.lockpaths[0].write_text(text)
        with self.assertRaises(LockError):run('stage', self.repo, self.output, SHA, SDK)

    def test_reject_changed_content_after_first_restore(self):
        run('stage', self.repo, self.output, SHA, SDK)
        self.lockpaths[0].write_text(self.lockpaths[0].read_text()+' ')
        with self.assertRaises(LockError):run('verify', self.repo, self.output, SHA, SDK)

    def test_reject_tampered_staged_copy(self):
        run('stage', self.repo, self.output, SHA, SDK)
        lock=self.output / 'qualified' / 'locks' / PROJECTS[0] / 'packages.lock.json'
        lock.write_text(lock.read_text()+'\n')
        with self.assertRaises(LockError):run('verify', self.repo, self.output, SHA, SDK)

    def test_reject_mismatched_sha(self):
        run('stage', self.repo, self.output, SHA, SDK)
        with self.assertRaises(LockError):run('verify', self.repo, self.output, 'b'*40, SDK)

    def test_reject_mismatched_sdk(self):
        run('stage', self.repo, self.output, SHA, SDK)
        with self.assertRaises(LockError):run('verify', self.repo, self.output, SHA, '10.0.200')

    def test_reject_missing_manifest(self):
        with self.assertRaises(LockError):run('verify', self.repo, self.output, SHA, SDK)

    def test_reject_invalid_source_sha(self):
        with self.assertRaises(LockError):run('stage', self.repo, self.output, 'abc', SDK)

    def test_reject_non_sdk10(self):
        with self.assertRaises(LockError):run('stage', self.repo, self.output, SHA, '6.0.428')

    def test_reject_output_outside_repo(self):
        with tempfile.TemporaryDirectory() as elsewhere:
            with self.assertRaises(LockError):run('stage', self.repo, Path(elsewhere), SHA, SDK)


if __name__ == '__main__':
    unittest.main()
