"""R02b execution-policy tests. Pure Python; NEVER run dotnet."""
from __future__ import annotations

from pathlib import Path
import sys
import tomllib
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "scripts"))
from devtool_host_policy import HOST_TARGETS, issues, migrate_source


def _fixture() -> bytes:
    prefix = """[execution]
environment = "native-termux"

[targets.nestcore]
runner = "dotnet"

[targets.nestcore.env]
GamePath = "/data/data/com.termux/files/home/stardew/game"

"""
    for name in HOST_TARGETS:
        if name == "nestcore":
            continue
        prefix += f"""[targets.{name}.env]
GamePath = "/data/data/com.termux/files/home/stardew/game"
DOTNET_ROLL_FORWARD = "Major"

"""
    return prefix.encode("utf-8")


class HostPolicyTests(unittest.TestCase):
    def test_migrates_without_changing_other_target_environments(self):
        before = _fixture()
        after = migrate_source(before)
        cfg = tomllib.loads(after.decode("utf-8"))
        self.assertEqual(issues(cfg), [])
        self.assertNotIn("env", cfg["targets"]["nestcore"])
        for name in HOST_TARGETS:
            self.assertNotIn("GamePath", cfg["targets"][name].get("env", {}))
            if name != "nestcore":
                self.assertEqual(cfg["targets"][name]["env"]["DOTNET_ROLL_FORWARD"], "Major")

    def test_rejects_wrong_base_execution(self):
        with self.assertRaisesRegex(ValueError, "Expected native-termux"):
            migrate_source(_fixture().replace(b'environment = "native-termux"', b'environment = "desktop-linux"'))

    def test_rejects_mismatched_game_path(self):
        with self.assertRaisesRegex(ValueError, "Unexpected environment or GamePath"):
            migrate_source(_fixture().replace(b'game"', b'other"', 1))

    def test_rejects_missing_target(self):
        c = tomllib.loads(migrate_source(_fixture()).decode("utf-8"))
        c["targets"].pop("eggsinwild")
        self.assertTrue(any("Missing R02b target" in x for x in issues(c)))

    def test_detects_regression_to_hardcoded_game_path(self):
        c = tomllib.loads(migrate_source(_fixture()).decode("utf-8"))
        c["targets"]["svmods"]["env"]["GamePath"] = "/path/to/game"
        self.assertTrue(any("GamePath" in x for x in issues(c)))

    def test_detects_regression_to_termux_default(self):
        c = tomllib.loads(migrate_source(_fixture()).decode("utf-8"))
        c["execution"]["environment"] = "native-termux"
        self.assertTrue(any("execution.environment" in x for x in issues(c)))

    def test_real_repository_policy(self):
        config = ROOT / ".devtool.toml"
        if not config.is_file():
            self.skipTest("Source checkout not available in isolated builder")
        c = tomllib.loads(config.read_text(encoding="utf-8"))
        self.assertEqual(issues(c), [])
        self.assertEqual(c["project"]["runner"], "dotnet")
        self.assertEqual(c["workspace"]["default_target"], "svmods")
        self.assertEqual(c["dotnet"]["configuration"], "Release")


if __name__ == "__main__":
    unittest.main()
