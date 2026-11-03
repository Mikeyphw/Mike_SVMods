"""R05c negative policy and provenance fixtures; Python-only; no real .NET."""
from __future__ import annotations
import json
from pathlib import Path
import shutil
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from check_committed_nuget_locks import ROOT, check_ci, check_source, PROVENANCE
from verify_nuget_locks import PROJECTS


class R05cTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        self.prov = self.root / PROVENANCE
        self.prov.parent.mkdir(parents=True)
        self.prov.write_bytes((ROOT / PROVENANCE).read_bytes())
        self.ci_path = self.root / '.github/workflows/ci.yml'
        self.ci_path.parent.mkdir(parents=True)
        self.ci_path.write_bytes((ROOT / '.github/workflows/ci.yml').read_bytes())
        for project in PROJECTS:
            folder=self.root/project
            folder.mkdir(parents=True)
            (folder/'packages.lock.json').write_bytes((ROOT / project / 'packages.lock.json').read_bytes())
            # Exact direct dependency set with a ProjectReference for the pure test.
            projects = '<ProjectReference Include="../../RoosterControl.Pure/RoosterControl.Pure.csproj" />' if 'RoosterControl' in project else ''
            name=Path(project).name
            (folder/(name+'.csproj')).write_text('<Project><ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" /><PackageReference Include="xunit" Version="2.7.0" /><PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />'+projects+'</ItemGroup></Project>')

    def test_genuine_hosted_provenance_and_ci_accept(self):
        self.assertEqual([], check_source(self.root))

    def test_reject_missing_lockfile(self):
        (self.root/PROJECTS[0]/'packages.lock.json').unlink()
        self.assertTrue(check_source(self.root))

    def test_reject_byte_drift_even_if_json_semantically_unchanged(self):
        lock=self.root/PROJECTS[0]/'packages.lock.json'
        lock.write_bytes(lock.read_bytes()+b'\n')
        self.assertTrue(any('differs' in x for x in check_source(self.root)))

    def test_reject_missing_direct_package(self):
        lock=self.root/PROJECTS[0]/'packages.lock.json'
        doc=json.loads(lock.read_text())
        doc['dependencies']['net6.0'].pop('xunit')
        lock.write_text(json.dumps(doc))
        self.assertTrue(check_source(self.root))

    def test_reject_corrupted_nuget_contenthash(self):
        lock=self.root/PROJECTS[0]/'packages.lock.json'
        doc=json.loads(lock.read_text())
        doc['dependencies']['net6.0']['xunit']['contentHash']='bad!'
        lock.write_text(json.dumps(doc))
        self.assertTrue(check_source(self.root))

    def test_reject_missing_project_reference(self):
        lock=self.root/PROJECTS[0]/'packages.lock.json'
        doc=json.loads(lock.read_text())
        doc['dependencies']['net6.0'].pop('roostercontrol.pure',None)
        lock.write_text(json.dumps(doc))
        self.assertTrue(check_source(self.root))

    def test_reject_changed_artifact_source(self):
        doc=json.loads(self.prov.read_text())
        doc['source_commit']='b'*40
        self.prov.write_text(json.dumps(doc))
        self.assertTrue(check_source(self.root))

    def test_reject_changed_artifact_checksum(self):
        doc=json.loads(self.prov.read_text())
        doc['candidate_artifact_sha256']='f'*64
        self.prov.write_text(json.dumps(doc))
        self.assertTrue(check_source(self.root))

    def test_reject_false_hosted_locked_attestation(self):
        doc=json.loads(self.prov.read_text())
        doc['hosted_locked_restore_verified']=False
        self.prov.write_text(json.dumps(doc))
        self.assertTrue(check_source(self.root))

    def test_reject_missing_manifest_project(self):
        doc=json.loads(self.prov.read_text())
        doc['lockfiles'].pop(f'{PROJECTS[0]}/packages.lock.json')
        self.prov.write_text(json.dumps(doc))
        self.assertTrue(check_source(self.root))

    def test_reject_unlocked_pure_restore(self):
        src=self.ci_path.read_text()
        self.assertTrue(check_ci(src.replace(' --locked-mode --no-http-cache','',1)))

    def test_reject_unlocked_animalnotes_restore(self):
        src=self.ci_path.read_text()
        self.assertTrue(check_ci(src.replace(' --locked-mode --no-http-cache','',1).replace(' --locked-mode --no-http-cache','',1)))

    def test_reject_missing_animalnotes_job(self):
        src=self.ci_path.read_text()
        prefix,tail=src.split('  animalnotes-locked:\n',1)
        _,suffix=tail.split('  source-contracts:\n',1)
        self.assertTrue(check_ci(prefix+'  source-contracts:\n'+suffix))

    def test_reject_missing_source_gate(self):
        src=self.ci_path.read_text()
        self.assertTrue(check_ci(src.replace('run: python3 scripts/check_committed_nuget_locks.py','run: echo bypass')))

    def test_reject_disabled_job(self):
        src=self.ci_path.read_text()
        self.assertTrue(check_ci(src.replace('  animalnotes-locked:\n','  animalnotes-locked:\n    if: false\n')))

    def test_reject_silent_job_failure(self):
        src=self.ci_path.read_text()
        self.assertTrue(check_ci(src.replace('  animalnotes-locked:\n','  animalnotes-locked:\n    continue-on-error: true\n')))

    def test_reject_without_trx_verifier(self):
        src=self.ci_path.read_text()
        self.assertTrue(check_ci(src.replace('python3 ../../scripts/verify_dotnet_trx.py','echo skip')))

    def test_reject_force_evaluate_in_locked_jobs(self):
        src=self.ci_path.read_text()
        self.assertTrue(check_ci(src.replace(' --locked-mode --no-http-cache',' --locked-mode --force-evaluate',1)))

if __name__=='__main__':
    unittest.main()
