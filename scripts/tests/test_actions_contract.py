"""Negative CI-contract tests: no success for documentation-only wiring."""
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from devtool_actions_contract import check_ci_source, check_devtool

CI = """jobs:
  source-contracts:
    runs-on: ubuntu-24.04
    steps:
      - run: python3 scripts/validate_mod_manifests.py
      - run: python3 -m unittest discover -s scripts/tests -p 'test_*.py'
      - run: python3 scripts/devtool_actions_contract.py
  pure-tests:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test Tests/Tests.sln
"""


class ContractTests(unittest.TestCase):
    def test_valid_job(self):
        self.assertEqual(check_ci_source(CI), [])

    def test_job_missing(self):
        self.assertIn("missing", check_ci_source(CI.replace("source-contracts:", "source-checks:"))[0])

    def test_command_missing(self):
        self.assertTrue(any("does not execute" in issue for issue in
                            check_ci_source(CI.replace("run: python3 scripts/validate_mod_manifests.py", "run: echo skip"))))

    def test_continue_on_error_is_forbidden(self):
        altered = CI.replace("    steps:", "    continue-on-error: true\n    steps:", 1)
        self.assertTrue(any("ignore failures" in issue for issue in check_ci_source(altered)))

    def test_disabled_source_job_is_forbidden(self):
        altered = CI.replace("    steps:", "    if: false\n    steps:", 1)
        self.assertTrue(any("cannot be disabled" in issue for issue in check_ci_source(altered)))

    def test_cannot_remove_baseline_ci(self):
        self.assertTrue(any("pure-tests" in issue for issue in check_ci_source(CI.split("  pure-tests:")[0])))

    def test_unknown_workflow_predecessor(self):
        cfg = {"targets": {"svmods": {
            "root": ".",
            "workflows": {"broken": [{"id": "late", "ref": "job:unknown", "depends_on": ["early"]}]},
            "jobs": {"ci-source": {"command": ["python3", "scripts/devtool_actions_contract.py"]}},
        }}, "wrapper": {"schema": 2, "commands": {}}}
        issues = check_devtool(Path('.'), cfg)
        self.assertTrue(any("unknown predecessor" in s for s in issues))
        self.assertTrue(any("undefined" in s for s in issues))


if __name__ == "__main__":
    unittest.main()
