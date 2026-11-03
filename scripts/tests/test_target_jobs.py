"""R04 source-only migration regressions, without invoking dotnet."""
from __future__ import annotations

import copy
from pathlib import Path
import sys
import tomllib
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from devtool_target_jobs import MIGRATIONS, PURE_JOB, migrate_source, validate
from devtool_target_ownership import ROOTS, INPUTS, OUTPUTS, DEPENDENCIES, validate as validate_r03


def source_fixture() -> bytes:
    """R03-shaped TOML with the ten old global jobs and their real owner refs."""
    lines = ['[execution]', 'environment = "auto"', '']
    names = ("svmods", "letmehatch", "eggsinwild", "roostercontrol", "roostercontrol_pure", "pure")
    for name in names:
        lines.extend([f'[targets.{name}]', f'root = "{name}"'])
        if name in ("letmehatch", "eggsinwild", "roostercontrol"):
            lines.append(f'test_command = "./Tests/devtool/validate-{name}.sh"')
        lines.append('')
        if name == "svmods":
            lines.extend(['[targets.svmods.jobs.ci-source]', 'root = ".."', 'runner = "command"',
                          'command = ["python3", "scripts/devtool_actions_contract.py"]',
                          '[targets.svmods.jobs.ci-source.metadata]', 'read_only = true', ''])
        steps = [old for old, (owner, _) in MIGRATIONS.items() if owner == name]
        lines.append(f'[targets.{name}.workflows]')
        if steps:
            ordered = ', '.join('{id = "step%d", ref = "job:%s", depends_on = []}' % (i, old)
                                for i, old in enumerate(steps))
            lines.append('validate = [' + ordered + ']')
            if name == 'svmods':
                lines.append('ci-local = [' + ordered + ']')
        else:
            lines.append('ownership-graph = [{id = "source", ref = "job:target-ownership-contract", depends_on = []}]')
        lines.append('')
    for old, (owner, local) in MIGRATIONS.items():
        command = '["./LetMeHatch/Tools/validate-core.sh"]' if local == "core_purity" else (
            '["dotnet", "test", "Tests/Tests.sln", "-c", "Release"]')
        lines += [f'[jobs.{old}]', 'runner = "command"', 'root = "."',
                  f'command = {command}', 'inputs = ["Tests/**"]',
                  'outputs = ["Tests/**/bin/**"]', '', f'[jobs.{old}.env]',
                  'DOTNET_ROLL_FORWARD = "Major"', '']
    lines += ['[jobs.target-ownership-contract]', 'runner = "command"', 'root = "."',
              'command = ["python3", "scripts/devtool_target_ownership.py", "--json"]',
              'inputs = [".devtool.toml"]', 'outputs = []',
              '[jobs.target-ownership-contract.metadata]', 'read_only = true', '']
    return ('\n'.join(lines)).encode()


class TargetLocalJobTests(unittest.TestCase):
    def test_migrates_all_ten_jobs_and_preserves_unrelated_tables(self):
        before = tomllib.loads(source_fixture().decode())
        after = tomllib.loads(migrate_source(source_fixture()).decode())
        self.assertEqual(validate(after), [])
        self.assertEqual(after['jobs']['target-ownership-contract'], before['jobs']['target-ownership-contract'])
        self.assertEqual(after['targets']['svmods']['jobs']['ci-source'], before['targets']['svmods']['jobs']['ci-source'])
        self.assertEqual(len(after['jobs']), 1)
        self.assertEqual([s['ref'] for s in after['targets']['svmods']['workflows']['ci-local']],
                         [s['ref'] for s in after['targets']['svmods']['workflows']['validate']])
        for old, (owner, local) in MIGRATIONS.items():
            original = copy.deepcopy(before['jobs'][old]); original['root'] = '..'
            self.assertEqual(after['targets'][owner]['jobs'][local], original)

    def test_second_migration_is_rejected(self):
        migrated = migrate_source(source_fixture())
        with self.assertRaisesRegex(ValueError, 'Missing R03 repository job'):
            migrate_source(migrated)

    def test_missing_old_job_is_rejected(self):
        broken = source_fixture().replace(b'[jobs.eggsinwild-tests]', b'[jobs.renamed-tests]')
        with self.assertRaisesRegex(ValueError, 'Unexpected original job shape'):
            migrate_source(broken)

    def test_legacy_root_change_is_rejected(self):
        broken = source_fixture().replace(b'[jobs.letmehatch-tests]\nrunner = "command"\nroot = "."',
                                          b'[jobs.letmehatch-tests]\nrunner = "command"\nroot = "tmp"')
        with self.assertRaisesRegex(ValueError, 'Unexpected original job shape'):
            migrate_source(broken)

    def test_command_drift_is_rejected(self):
        broken = source_fixture().replace(b'command = ["dotnet", "test"',
                                          b'command = ["pwsh", "test"', 1)
        with self.assertRaisesRegex(ValueError, 'Unexpected existing job command'):
            migrate_source(broken)

    def test_unexpected_owner_script_is_rejected(self):
        broken = source_fixture().replace(b'./Tests/devtool/validate-eggsinwild.sh', b'./wrong.sh')
        with self.assertRaisesRegex(ValueError, 'Unexpected existing test_command'):
            migrate_source(broken)

    def test_host_policy_is_required(self):
        broken = source_fixture().replace(b'environment = "auto"', b'environment = "native-termux"')
        with self.assertRaisesRegex(ValueError, 'host auto'):
            migrate_source(broken)

    def test_shared_ownership_job_is_required(self):
        broken = source_fixture().replace(b'[jobs.target-ownership-contract.metadata]\nread_only = true',
                                          b'[jobs.target-ownership-contract.metadata]\nread_only = false')
        with self.assertRaisesRegex(ValueError, 'ownership job missing'):
            migrate_source(broken)

    def test_pure_alias_delegates(self):
        changed = tomllib.loads(migrate_source(source_fixture()).decode())
        self.assertEqual(changed['targets']['roostercontrol_pure']['jobs']['unit'], PURE_JOB)
        self.assertEqual(changed['targets']['pure']['workflows']['validate'][0]['ref'],
                         'target:roostercontrol_pure#validate')

    def test_canonical_pure_job_drift_fails(self):
        cfg = tomllib.loads(migrate_source(source_fixture()).decode())
        cfg['targets']['roostercontrol_pure']['jobs']['unit']['command'] = ['echo', 'skip']
        self.assertTrue(any('Canonical pure test job' in error for error in validate(cfg)))

    def test_global_test_job_reintroduced_fails(self):
        cfg = tomllib.loads(migrate_source(source_fixture()).decode())
        cfg['jobs']['roostercontrol-tests'] = {'runner': 'command', 'command': ['dotnet', 'test']}
        self.assertTrue(any('Legacy global job' in error for error in validate(cfg)))

    def test_local_command_cannot_be_silently_skipped(self):
        cfg = tomllib.loads(migrate_source(source_fixture()).decode())
        cfg['targets']['eggsinwild']['jobs']['unit']['command'] = ['true']
        self.assertTrue(any('Wrong target-local command' in error for error in validate(cfg)))

    def test_extra_env_vars_preserved(self):
        changed = source_fixture().replace(b'[jobs.svmods-tests-sln.env]\nDOTNET_ROLL_FORWARD = "Major"',
                                            b'[jobs.svmods-tests-sln.env]\nDOTNET_ROLL_FORWARD = "Major"\nCUSTOM = "safe"')
        after = tomllib.loads(migrate_source(changed).decode())
        self.assertEqual(after['targets']['svmods']['jobs']['tests_sln']['env']['CUSTOM'], 'safe')

    def test_r03_affected_graph_still_valid_after_migration(self):
        cfg = tomllib.loads(migrate_source(source_fixture()).decode())
        targets = cfg['targets']
        for name, root in ROOTS.items():
            target = targets.setdefault(name, {})
            target.update(root=root, inputs=copy.deepcopy(INPUTS[name]), outputs=copy.deepcopy(OUTPUTS[name]))
            if name == 'svmods':
                target['test_command'] = './run-tests.sh'
            if DEPENDENCIES[name]:
                steps = [
                    {'id': f'dep_{dep}', 'ref': 'target:' + dep + '#' +
                     ('ownership-graph' if DEPENDENCIES[dep] else 'ownership-probe'),
                     'depends_on': []}
                    for dep in DEPENDENCIES[name]
                ]
                target.setdefault('workflows', {})['ownership-graph'] = steps
            else:
                target.setdefault('workflows', {})['ownership-probe'] = [
                    {'id': 'source', 'ref': 'job:target-ownership-contract', 'depends_on': []}]
        cfg['jobs']['target-ownership-contract']['command'] = [
            'python3', 'scripts/devtool_target_ownership.py', '--json']
        self.assertEqual(validate_r03(cfg), [])

    def test_real_repo_contract_if_full_checkout_present(self):
        repo = Path(__file__).resolve().parents[2]
        conf = repo / '.devtool.toml'
        if not conf.exists():
            self.skipTest('Source-only staging fixture lacks full checkout')
        self.assertEqual(validate(tomllib.loads(conf.read_text(encoding='utf-8'))), [])


if __name__ == '__main__':
    unittest.main()
