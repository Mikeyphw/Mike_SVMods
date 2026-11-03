"""R05a: independent negative verification of hosted SDK policy syntax."""
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from check_hosted_dotnet_policy import ROOT, check_sources


class PolicyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = (ROOT / '.github/workflows/r05-hosted-dotnet-sdk10.yml').read_text('utf-8')
        cls.sdk = (ROOT / '.github/r05-sdk10/global.json').read_text('utf-8')
        cls.ci = (ROOT / '.github/workflows/ci.yml').read_text('utf-8')

    def issues(self, *, workflow=None, sdk=None, ci=None):
        return check_sources(
            self.workflow if workflow is None else workflow,
            self.sdk if sdk is None else sdk,
            self.ci if ci is None else ci)

    def test_valid_policy(self):
        self.assertEqual([], self.issues())

    def test_no_sdk_version_drift(self):
        self.assertTrue(self.issues(sdk=self.sdk.replace('10.0.100', '8.0.100')))

    def test_no_prerelease(self):
        self.assertTrue(self.issues(sdk=self.sdk.replace('false', 'true')))

    def test_no_missing_net6_runtime(self):
        self.assertTrue(self.issues(workflow=self.workflow.replace('6.0.x', '8.0.x')))

    def test_no_missing_net6_runtime_check(self):
        self.assertTrue(self.issues(workflow=self.workflow.replace('Microsoft\\.NETCore\\.App 6\\.', 'Microsoft\\.NETCore\\.App 8\\.')))

    def test_no_undispatched_workflow(self):
        self.assertTrue(self.issues(workflow=self.workflow.replace('  workflow_dispatch:', '  pull_request:')))

    def test_no_write_privileges(self):
        self.assertTrue(self.issues(workflow=self.workflow.replace('contents: read', 'contents: write')))

    def test_no_test_removal(self):
        self.assertTrue(self.issues(workflow=self.workflow.replace('dotnet test ../../Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj', 'echo skip')))

    def test_no_missing_trx_verifier(self):
        self.assertTrue(self.issues(workflow=self.workflow.replace('python3 ../../scripts/verify_dotnet_trx.py', 'echo pass', 1)))

    def test_no_baseline_ci_bypass(self):
        self.assertTrue(self.issues(ci=self.ci.replace('run: python3 scripts/check_hosted_dotnet_policy.py', 'run: true')))

    def test_no_change_of_global_json_policy_directory(self):
        self.assertTrue(self.issues(workflow=self.workflow.replace('working-directory: .github/r05-sdk10', 'working-directory: .')))


if __name__ == '__main__':
    unittest.main()
