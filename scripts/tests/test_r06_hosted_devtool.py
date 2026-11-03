"""R06a negative fixtures: no real .NET or GitHub access in this test suite."""
from __future__ import annotations
import copy, json, sys, tempfile, unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from check_r06_source_contract import ROOT, check_source
from verify_r06_native_evidence import EvidenceError,verify
import tomllib

SHA='a'*40
DEV='b'*40

class R06SourceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow=(ROOT/'.github/workflows/r06a-native-devtool-source.yml').read_text()
        cls.ci=(ROOT/'.github/workflows/ci.yml').read_text()
        cls.cfg=tomllib.loads((ROOT/'.devtool.toml').read_text())
    def issues(self,work=None,ci=None,cfg=None):
        return check_source(self.workflow if work is None else work,
             self.ci if ci is None else ci, self.cfg if cfg is None else cfg)
    def test_valid(self):self.assertEqual([],self.issues())
    def test_dispatch_only(self):self.assertTrue(self.issues(work=self.workflow.replace('  workflow_dispatch:','  push:')))
    def test_no_write_permission(self):self.assertTrue(self.issues(work=self.workflow.replace('contents: read','contents: write')))
    def test_no_source_pin_drift(self):self.assertTrue(self.issues(work=self.workflow.replace('b164463db51e2cffd12f753043b9e55a08cc43ae','a'*40)))
    def test_no_invoke_removal(self):self.assertTrue(self.issues(work=self.workflow.replace('invoke --target svmods ci-preflight','echo skipped')))
    def test_no_bypass(self):self.assertTrue(self.issues(work=self.workflow+'\n    continue-on-error: true\n'))
    def test_no_baseline_bypass(self):self.assertTrue(self.issues(ci=self.ci.replace('run: python3 scripts/check_r06_source_contract.py','run: echo skipped')))
    def test_no_unreviewed_job(self):
        cfg=copy.deepcopy(self.cfg);cfg['targets']['svmods']['jobs']['ci-source']['command']=['dotnet','test']
        self.assertTrue(self.issues(cfg=cfg))
    def test_no_unsafe_job_context(self):self.assertTrue(self.issues(work=self.workflow.replace('DEVTOOL_SOURCE_SHA:','DEVTOOL_SOURCE_SHA: ${{ runner.temp }} #')))

class R06EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup)
        self.folder=Path(self.tmp.name)
        for f in ('plan.json','preflight.json'):(self.folder/f).write_text('{"schema_version":1}')
        self.receipt={'ok':True,'exit_code':0,'totals':{'failed':0,'errors':0},
            'results':[{'name':'source','status':'ok','exit_code':0}],
            'workflow':{'nodes':[{'key':'source','state':'passed','job':{'target':'svmods','local_name':'ci-source'}}]}}
        self.save()
    def save(self):(self.folder/'invoke.json').write_text(json.dumps(self.receipt))
    def test_valid_evidence(self):
        data=verify(self.folder,SHA,DEV)
        self.assertEqual('HOSTED_NATIVE_SOURCE_PASS',data['status'])
    def test_missing_plan(self):
        (self.folder/'plan.json').unlink()
        with self.assertRaises(EvidenceError):verify(self.folder,SHA,DEV)
    def test_invalid_machine(self):
        (self.folder/'invoke.json').write_text('not-json')
        with self.assertRaises(EvidenceError):verify(self.folder,SHA,DEV)
    def test_failed_status(self):
        self.receipt['ok']=False;self.save()
        with self.assertRaises(EvidenceError):verify(self.folder,SHA,DEV)
    def test_failed_totals(self):
        self.receipt['totals']['failed']=1;self.save()
        with self.assertRaises(EvidenceError):verify(self.folder,SHA,DEV)
    def test_zero_results(self):
        self.receipt['results']=[];self.save()
        with self.assertRaises(EvidenceError):verify(self.folder,SHA,DEV)
    def test_missing_native_node(self):
        self.receipt['workflow']['nodes']=[];self.save()
        with self.assertRaises(EvidenceError):verify(self.folder,SHA,DEV)
    def test_wrong_job_identity(self):
        self.receipt['workflow']['nodes'][0]['job']['local_name']='other';self.save()
        with self.assertRaises(EvidenceError):verify(self.folder,SHA,DEV)
    def test_failed_node(self):
        self.receipt['workflow']['nodes'][0]['state']='failed';self.save()
        with self.assertRaises(EvidenceError):verify(self.folder,SHA,DEV)
    def test_short_source_sha(self):
        with self.assertRaises(EvidenceError):verify(self.folder,'abc',DEV)
    def test_no_symlink_evidence(self):
        link=self.folder.parent/(self.folder.name+'-link');link.symlink_to(self.folder,target_is_directory=True)
        with self.assertRaises(EvidenceError):verify(link,SHA,DEV)

if __name__=='__main__':unittest.main()
