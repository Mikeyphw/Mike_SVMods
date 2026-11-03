"""R05b negative policy fixtures; no .NET executed."""
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from check_nuget_candidate_policy import ROOT, check_sources


class TestR05bPolicy(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = (ROOT / '.github/workflows/r05b-nuget-lock-candidate.yml').read_text()
        cls.ci = (ROOT / '.github/workflows/ci.yml').read_text()
        cls.sdk = (ROOT / '.github/r05-sdk10/global.json').read_text()

    def problems(self, workflow=None, ci=None, sdk=None):
        return check_sources(self.workflow if workflow is None else workflow,
                             self.ci if ci is None else ci,
                             self.sdk if sdk is None else sdk)

    def test_pass(self):
        self.assertEqual([], self.problems())

    def test_dispatch_required(self):
        self.assertTrue(self.problems(workflow=self.workflow.replace('  workflow_dispatch:', '  push:')))

    def test_no_write_token(self):
        self.assertTrue(self.problems(workflow=self.workflow.replace('contents: read', 'contents: write')))

    def test_no_local_sdk_guess(self):
        self.assertTrue(self.problems(workflow=self.workflow.replace('working-directory: .github/r05-sdk10', 'working-directory: .')))

    def test_no_job_runner_context(self):
        self.assertTrue(self.problems(workflow=self.workflow.replace('${{ github.workspace }}', '${{ runner.temp }}')))

    def test_no_no_lockfile_generation(self):
        self.assertTrue(self.problems(workflow=self.workflow.replace('--use-lock-file', '--no-cache')))

    def test_locked_mode_required(self):
        self.assertTrue(self.problems(workflow=self.workflow.replace('--locked-mode', '--force-evaluate')))

    def test_second_cache_required(self):
        self.assertTrue(self.problems(workflow=self.workflow.replace('nuget-second', 'nuget-first')))

    def test_no_qualified_output_on_failure(self):
        self.assertTrue(self.problems(workflow=self.workflow.replace('if: success()', 'if: always()')))

    def test_baseline_source_gate(self):
        self.assertTrue(self.problems(ci=self.ci.replace('run: python3 scripts/check_nuget_candidate_policy.py', 'run: echo bypass')))

    def test_sdk_pinned(self):
        self.assertTrue(self.problems(sdk=self.sdk.replace('10.0.100', '8.0.100')))

    def test_both_test_projects_needed(self):
        self.assertTrue(self.problems(workflow=self.workflow.replace('EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj', 'OTHER/OTHER.csproj')))


if __name__ == '__main__':
    unittest.main()
