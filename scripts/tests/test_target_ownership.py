"""R03 negative tests: source-only ownership fixtures and migration invariants."""
from __future__ import annotations

from copy import deepcopy
from pathlib import Path
import sys
import tomllib
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from devtool_target_ownership import (DEPENDENCIES, EXPECTED_PATHS, INPUTS, MODS,
                                       OUTPUTS, ROOTS, migrate_source,
                                       select_affected, validate)


def baseline_source() -> bytes:
    """Minimal reviewed R02b shape with the same target/table structure."""
    parts = ['[execution]\nenvironment = "auto"\n']
    for name in ROOTS:
        parts += [f'[targets.{name}]\n', 'runner = "dotnet"\n', 'root = "."\n']
        if name == 'svmods':
            parts += ['test_command = "./Tests/run-tests.sh"\n']
        parts += [f'inputs = [\n    "OLD/{name}/**",\n]\n',
                  f'outputs = [\n    "OLD/{name}/bin/**",\n]\n']
        if name == 'svmods':
            parts += ['[targets.svmods.jobs.ci-source]\nrunner = "command"\ncommand = ["python3", "scripts/devtool_actions_contract.py"]\n']
        if name in ('svmods', *MODS[1:]):
            parts += [f'[targets.{name}.workflows]\nvalidate = [{{id = "main", ref = "job:existing", depends_on = []}}]\n']
    parts += ['[jobs.existing]\nrunner = "command"\ncommand = ["python3", "noop.py"]\n']
    return '\n'.join(parts).encode('utf-8') + b'\n'


class OwnershipTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.raw = migrate_source(baseline_source())
        cls.cfg = tomllib.loads(cls.raw.decode())

    def test_migration_has_reviewed_invariants(self):
        self.assertEqual(validate(self.cfg), [])
        self.assertEqual(self.cfg['execution']['environment'], 'auto')
        self.assertEqual(set(self.cfg['targets']), set(ROOTS))
        self.assertIn('existing', self.cfg['jobs'])
        self.assertEqual(self.cfg['targets']['svmods']['jobs']['ci-source']['command'],
                         ['python3', 'scripts/devtool_actions_contract.py'])

    def test_baseline_migration_rejects_second_application(self):
        with self.assertRaisesRegex(ValueError, 'root already changed'):
            migrate_source(self.raw)

    def test_unknown_or_additional_target_refused(self):
        with self.assertRaisesRegex(ValueError, 'unexpected target set'):
            migrate_source(baseline_source() + b'[targets.unexpected]\nroot = "."\n')

    def test_wrong_host_policy_refused(self):
        raw = baseline_source().replace(b'environment = "auto"', b'environment = "native-termux"')
        with self.assertRaisesRegex(ValueError, 'R02b'):
            migrate_source(raw)

    def test_breaking_ownership_root_is_caught(self):
        cfg = deepcopy(self.cfg)
        cfg['targets']['eggsinwild']['root'] = '.'
        self.assertTrue(any('eggsinwild: root' in x for x in validate(cfg)))

    def test_breaking_dependency_is_caught(self):
        cfg = deepcopy(self.cfg)
        cfg['targets']['letmehatch']['workflows']['ownership-graph'] = []
        self.assertTrue(any('letmehatch: wrong graph' in x for x in validate(cfg)))

    def test_generator_owner_is_correct(self):
        self.assertEqual(self.cfg['targets']['embed_tool']['root'], 'tools/EmbedTextAsCode')
        self.assertEqual(self.cfg['targets']['embed_tool']['outputs'], ['bin/**', 'obj/**'])

    def test_fixtures_select_without_hiding_dependencies(self):
        # This source-only fixture deliberately models the original R03 target
        # set. The new dn03_r5 target is asserted in the real consumer contract,
        # not added retroactively to an R03-only migration snapshot.
        self.assertEqual(set(self.cfg['targets']), set(ROOTS))
        self.assertNotIn('dn03_r5', self.cfg['targets'])
        for changed, expected in EXPECTED_PATHS.items():
            with self.subTest(path=changed):
                self.assertEqual(select_affected(self.cfg, [changed]),
                                 expected - {'dn03_r5'})

    def test_unknown_path_broadens_all(self):
        self.assertEqual(select_affected(self.cfg, ['docs/completely-unowned.md']), set(ROOTS))

    def test_shared_build_file_selects_all(self):
        self.assertEqual(select_affected(self.cfg, ['Directory.Build.targets']), set(ROOTS))

    def test_mod_source_is_not_owned_by_every_target(self):
        selected = select_affected(self.cfg, ['EggsInWild/ModEntry.cs'])
        self.assertEqual(selected, {'eggsinwild', 'svmods'})
        self.assertNotIn('letmehatch', selected)
        self.assertNotIn('roostercontrol', selected)

    def test_rooster_pure_alias_retained_explicitly(self):
        target = self.cfg['targets']['pure']
        self.assertEqual(target['root'], 'Tests/RoosterControl.PureTests')
        self.assertEqual(self.cfg['targets']['roostercontrol_pure']['root'], 'RoosterControl.Pure')

    def test_no_local_dotnet_in_contract_job(self):
        command = self.cfg['jobs']['target-ownership-contract']['command']
        self.assertEqual(command[0], 'python3')
        self.assertTrue(self.cfg['jobs']['target-ownership-contract']['metadata']['read_only'])


if __name__ == '__main__':
    unittest.main()
