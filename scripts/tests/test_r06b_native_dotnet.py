"""R06b source and machine-evidence negative tests (no dotnet execution)."""
from __future__ import annotations

import copy
import json
from pathlib import Path
import sys
import tempfile
import tomllib
import unittest
from xml.etree import ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from r06b_target_config import DEVTOOL_SHA, job_specs, migration, steps, validate
from check_r06b_source_contract import check_sources
from verify_r06b_native_dotnet import EvidenceError, EXPECTED_NODES, verify

SRC = '''[execution]
environment = "auto"
[targets.svmods]
root = "Tests"
[targets.svmods.workflows]
ci-preflight = [{id = "source", ref = "job:ci-source", depends_on = []}]
[targets.svmods.jobs.ci-source]
runner = "command"
root = ".."
command = ["python3", "scripts/devtool_actions_contract.py"]
inputs = ["scripts/devtool_actions_contract.py"]
outputs = []
[targets.svmods.jobs.ci-source.metadata]
read_only = true
'''


class TestConfigMigration(unittest.TestCase):
    def setUp(self):
        self.src = SRC.encode()

    def test_migrates_and_keeps_prior_semantics(self):
        result = migration(self.src)
        before, after = tomllib.loads(SRC), tomllib.loads(result.decode())
        self.assertEqual([], validate(after))
        self.assertEqual(before['targets']['svmods']['jobs']['ci-source'],
                         after['targets']['svmods']['jobs']['ci-source'])
        self.assertEqual(steps(), after['targets']['svmods']['workflows']['ci-hosted-dotnet'])
        self.assertEqual(set(job_specs()), set(after['targets']['svmods']['jobs'])-{'ci-source'})
        for name in job_specs():
            self.assertEqual('..', after['targets']['svmods']['jobs'][name]['root'])

    def test_refuses_duplicate(self):
        with self.assertRaisesRegex(ValueError, 'already exists'):
            migration(migration(self.src))

    def test_refuses_bad_environment(self):
        with self.assertRaisesRegex(ValueError, 'Host-auto'):
            migration(self.src.replace(b'"auto"', b'"native-termux"'))

    def test_refuses_changed_target_root(self):
        with self.assertRaisesRegex(ValueError, 'scoped owner'):
            migration(self.src.replace(b'root = "Tests"', b'root = "."'))

    def test_refuses_disabled_source_job(self):
        with self.assertRaisesRegex(ValueError, 'read-only'):
            migration(self.src.replace(b'read_only = true', b'read_only = false'))

    def test_refuses_unexpected_workflow(self):
        with self.assertRaisesRegex(ValueError, 'source workflow'):
            migration(self.src.replace(b'job:ci-source', b'job:unreviewed'))

    def test_rejects_reordered_edges(self):
        cfg = tomllib.loads(migration(self.src).decode())
        cfg['targets']['svmods']['workflows']['ci-hosted-dotnet'][1]['depends_on'] = []
        self.assertTrue(validate(cfg))

    def test_rejects_no_locked_mode(self):
        cfg = tomllib.loads(migration(self.src).decode())
        cfg['targets']['svmods']['jobs']['r06b_restore_animal']['command'].remove('--locked-mode')
        self.assertTrue(validate(cfg))

    def test_rejects_unscoped_or_extra_command(self):
        cfg = tomllib.loads(migration(self.src).decode())
        cfg['targets']['svmods']['jobs']['r06b_test_pure']['command'].append('--no-build')
        self.assertTrue(validate(cfg))


class TestHostedSourcePolicy(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root = Path(__file__).resolve().parents[2]
        cls.wf = (root/'.github/workflows/r06b-native-devtool-dotnet.yml').read_text()
        cls.ci = (root/'.github/workflows/ci.yml').read_text()
        cls.cfg = tomllib.loads(migration(SRC.encode()).decode())

    def problems(self, wf=None, ci=None, cfg=None):
        return check_sources(self.wf if wf is None else wf,
                             self.ci if ci is None else ci,
                             self.cfg if cfg is None else cfg)

    def test_valid(self):
        self.assertEqual([], self.problems())

    def test_reject_write_token(self):
        self.assertTrue(self.problems(wf=self.wf.replace('contents: read', 'contents: write')))

    def test_reject_bad_devtool_pin(self):
        self.assertTrue(self.problems(wf=self.wf.replace(DEVTOOL_SHA, '0'*40)))

    def test_reject_job_runner_context(self):
        self.assertTrue(self.problems(wf=self.wf.replace('${{ github.workspace }}', '${{ runner.temp }}')))

    def test_reject_ci_gate_bypass(self):
        self.assertTrue(self.problems(ci=self.ci.replace('run: python3 scripts/check_r06b_source_contract.py', 'run: echo bypass')))

    def test_reject_undispatched(self):
        self.assertTrue(self.problems(wf=self.wf.replace('  workflow_dispatch:', '  push:')))

    def test_reject_missing_invoke(self):
        self.assertTrue(self.problems(wf=self.wf.replace('invoke --target svmods ci-hosted-dotnet', 'echo skip')))

    def test_reject_disabled_verifier(self):
        self.assertTrue(self.problems(wf=self.wf.replace('verify_r06b_native_dotnet.py', 'echo pass')))


class TestEvidence(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.root = Path(tmp.name)
        self.ev = self.root/'evidence'
        self.ev.mkdir()
        self.results = self.root/'.ci-results/r06b'
        self.results.mkdir(parents=True)
        self.machine = {
            'ok':True,'exit_code':0,'totals':{'failed':0,'errors':0,'skipped':0},
            'results':[{'exit_code':0,'status':'ok'} for _ in EXPECTED_NODES],
            'workflow':{'nodes':[{'job':{'target':'svmods','target_local':True,'local_name':name},
                                  'state':'passed','result_count':1} for name in EXPECTED_NODES]},
        }
        (self.ev/'plan.json').write_text(json.dumps({'targets':['svmods']}))
        (self.ev/'preflight.json').write_text(json.dumps({'checks':['dotnet']}))
        self.write_machine()
        self.write_trx('r06b-pure.trx')
        self.write_trx('r06b-animalnotes.trx')

    def write_machine(self):
        (self.ev/'invoke.json').write_text(json.dumps(self.machine))

    def write_trx(self, filename, passed=True, executed=1):
        root = ET.Element('TestRun')
        summary = ET.SubElement(root,'ResultSummary',{'outcome':'Completed'})
        ET.SubElement(summary,'Counters',{'total':str(executed),'executed':str(executed),
            'passed':str(executed if passed else 0),'failed':str(0 if passed else executed),
            'error':'0','timeout':'0','aborted':'0','inconclusive':'0','notExecuted':'0'})
        for _ in range(executed):
            ET.SubElement(root,'UnitTestResult',{'outcome':'Passed' if passed else 'Failed'})
        (self.results/filename).write_bytes(ET.tostring(root))

    def run_verifier(self):
        return verify(self.root,self.ev,'a'*40,DEVTOOL_SHA)

    def test_passes_realistic_receipts(self):
        v=self.run_verifier()
        self.assertEqual('HOSTED_NATIVE_LOCKED_DOTNET_PASS',v['status'])
        self.assertEqual(2,len(v['test_receipts']))
        self.assertFalse(v['release_qualified'])

    def test_rejects_skipped_nodes(self):
        self.machine['totals']['skipped']=1;self.write_machine()
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_missing_native_node(self):
        self.machine['workflow']['nodes'].pop();self.write_machine()
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_foreign_native_node(self):
        self.machine['workflow']['nodes'][0]['job']['target']='other';self.write_machine()
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_duplicate_node(self):
        self.machine['workflow']['nodes'][0]['job']['local_name']='r06b_test_animal';self.write_machine()
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_failed_exit_code(self):
        self.machine['results'][0]['exit_code']=1;self.write_machine()
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_no_executed_tests(self):
        self.write_trx('r06b-animalnotes.trx',executed=0)
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_failed_trx(self):
        self.write_trx('r06b-pure.trx',passed=False)
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_missing_trx(self):
        (self.results/'r06b-pure.trx').unlink()
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_missing_plan(self):
        (self.ev/'plan.json').unlink()
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_incomplete_run(self):
        self.machine['ok']=False;self.write_machine()
        with self.assertRaises(EvidenceError):self.run_verifier()

    def test_rejects_invalid_source_ids(self):
        with self.assertRaises(EvidenceError):verify(self.root,self.ev,'ABC',DEVTOOL_SHA)

    def test_rejects_symlinked_receipt(self):
        p=self.results/'r06b-pure.trx';p.unlink();p.symlink_to(self.results/'r06b-animalnotes.trx')
        with self.assertRaises(EvidenceError):self.run_verifier()


if __name__ == '__main__':
    unittest.main()
