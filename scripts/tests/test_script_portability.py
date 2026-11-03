"""R02a portability regression checks: Python/Bash syntax only, no .NET execution."""
from __future__ import annotations

from pathlib import Path
import re
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]
MOD_SCRIPTS = {
    "validate-eggsinwild.sh": [
        "Tests/EggsInWild.AnimalNotes.Tests/EggsInWild.AnimalNotes.Tests.csproj",
        "Tests/EggsInWild.Tests/EggsInWild.Tests.csproj",
        "Tests/EggsInWild.Phase7.Tests/EggsInWild.Phase7.Tests.csproj",
    ],
    "validate-letmehatch.sh": [
        "Tests/LetMeHatch.Core.Tests/LetMeHatch.Core.Tests.csproj",
        "Tests/LetMeHatch.Tests/LetMeHatch.Tests.csproj",
    ],
    "validate-roostercontrol.sh": [
        "Tests/RoosterControl.Descriptors.Tests/RoosterControl.Descriptors.Tests.csproj",
        "Tests/RoosterControl.Tests/RoosterControl.Tests.csproj",
    ],
}


class ScriptPortabilityContracts(unittest.TestCase):
    def test_shell_scripts_have_portable_shebang_and_valid_syntax(self):
        for name in (*MOD_SCRIPTS,):
            with self.subTest(script=name):
                script = ROOT / "Tests" / "devtool" / name
                data = script.read_text(encoding="utf-8")
                self.assertTrue(data.startswith("#!/usr/bin/env bash\n"))
                self.assertNotIn("/data/data/com.termux/", data)
                result = subprocess.run(["bash", "-n", str(script)], capture_output=True, text=True, check=False)
                self.assertEqual(result.returncode, 0, result.stderr)

    def test_script_root_is_derived_from_script_not_caller(self):
        for name in MOD_SCRIPTS:
            with self.subTest(script=name):
                source = (ROOT / "Tests" / "devtool" / name).read_text(encoding="utf-8")
                self.assertIn('${BASH_SOURCE[0]}', source)
                self.assertIn('cd "$REPO_ROOT"', source)
                self.assertIn('set -euo pipefail', source)
                self.assertIn('DOTNET_ROLL_FORWARD', source)

    def test_all_test_project_commands_retained_in_order(self):
        for name, projects in MOD_SCRIPTS.items():
            with self.subTest(script=name):
                source = (ROOT / "Tests" / "devtool" / name).read_text(encoding="utf-8")
                commands = [line for line in source.splitlines() if line.startswith("dotnet test ")]
                self.assertEqual(commands, [f"dotnet test {p} -c Release" for p in projects])

    def test_letmehatch_core_check_runs_from_any_cwd(self):
        source = (ROOT / "Tests/devtool/validate-letmehatch.sh").read_text(encoding="utf-8")
        self.assertIn('bash "$REPO_ROOT/LetMeHatch/Tools/validate-core.sh"', source)
        self.assertLess(source.index('bash "$REPO_ROOT/LetMeHatch/Tools/validate-core.sh"'), source.index("dotnet test "))

    def test_canonical_bash_runner_requires_core_gate(self):
        script = ROOT / "Tests/run-tests.sh"
        source = script.read_text(encoding="utf-8")
        self.assertIn('bash "${CORE_VALIDATOR}"', source)
        self.assertNotIn('[[ ! -x "${CORE_VALIDATOR}" ]]', source)
        self.assertIn('if [[ ! -f "${CORE_VALIDATOR}" ]]', source)
        self.assertLess(source.index('bash "${CORE_VALIDATOR}"'), source.index('dotnet test '))
        check = subprocess.run(["bash", "-n", str(script)], capture_output=True, text=True, check=False)
        self.assertEqual(check.returncode, 0, check.stderr)

    def test_powershell_runner_fails_closed(self):
        source = (ROOT / "Tests/run-tests.ps1").read_text(encoding="utf-8")
        self.assertIn("Test-Path -LiteralPath $validator -PathType Leaf", source)
        self.assertIn('throw "Required Core validator is missing:', source)
        self.assertIn('if ($LASTEXITCODE -ne 0)', source)
        self.assertIn('& dotnet test', source)
        self.assertNotIn('Write-Warning "Validate-Core.ps1 not found; skipping."', source)


if __name__ == "__main__":
    unittest.main()
