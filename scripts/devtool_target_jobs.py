#!/usr/bin/env python3
"""R04: migrate Devtool repository test jobs to target-local jobs, safely.

This source-only module never starts any .NET process. GitHub Actions owns .NET
execution; the migration runs as an after-extract declared-write preflight.
"""
from __future__ import annotations

import copy
import json
from pathlib import Path
import re
import sys
import tomllib

# Old repo-job -> (owner target, new local job). Preserve the old job bodies,
# command argv, inputs, outputs and env; only root and identity change.
MIGRATIONS = {
    "svmods-core-purity": ("svmods", "core_purity"),
    "svmods-tests-sln": ("svmods", "tests_sln"),
    "letmehatch-core-purity": ("letmehatch", "core_purity"),
    "letmehatch-core-tests": ("letmehatch", "core_tests"),
    "letmehatch-tests": ("letmehatch", "mod_tests"),
    "eggsinwild-animalnotes-tests": ("eggsinwild", "animalnotes"),
    "eggsinwild-tests": ("eggsinwild", "unit"),
    "eggsinwild-phase7-tests": ("eggsinwild", "phase7"),
    "roostercontrol-descriptor-tests": ("roostercontrol", "descriptors"),
    "roostercontrol-tests": ("roostercontrol", "unit"),
}
OWNERS = ("svmods", "letmehatch", "eggsinwild", "roostercontrol")
EXPECTED_EXEC = {
    "svmods": {"core_purity": "./LetMeHatch/Tools/validate-core.sh", "tests_sln": "dotnet"},
    "letmehatch": {"core_purity": "./LetMeHatch/Tools/validate-core.sh", "core_tests": "dotnet", "mod_tests": "dotnet"},
    "eggsinwild": {"animalnotes": "dotnet", "unit": "dotnet", "phase7": "dotnet"},
    "roostercontrol": {"descriptors": "dotnet", "unit": "dotnet"},
}
PURE_TEST_COMMAND = ["dotnet", "test", "Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj", "-c", "Release"]
PURE_JOB = {
    "runner": "command", "root": "..", "command": PURE_TEST_COMMAND,
    "inputs": ["RoosterControl.Pure/**", "RoosterControl/Application/Catalogs/**", "RoosterControl/Framework/Config/**", "Tests/RoosterControl.PureTests/**"],
    "outputs": ["RoosterControl.Pure/bin/**", "RoosterControl.Pure/obj/**", "Tests/RoosterControl.PureTests/bin/**", "Tests/RoosterControl.PureTests/obj/**"],
}


def _section(text: str, header: str) -> tuple[str, str]:
    """Return one exact TOML table and its body, rejecting duplicates."""
    pat = re.compile(r"(?m)^\[" + re.escape(header) + r"\]\n")
    matches = list(pat.finditer(text))
    if len(matches) != 1:
        raise ValueError(f"Expected exactly one [{header}] (found {len(matches)})")
    start = matches[0].start()
    tail = re.search(r"(?m)^\[", text[matches[0].end():])
    end = matches[0].end() + tail.start() if tail else len(text)
    return text[start:end], text[matches[0].end():end]


def _remove_section(text: str, header: str) -> tuple[str, str]:
    section, body = _section(text, header)
    return text.replace(section, "", 1), body


def _edit_section(text: str, header: str, transform) -> str:
    section, body = _section(text, header)
    updated = "[" + header + "]\n" + transform(body)
    return text.replace(section, updated, 1)


def _ref_changed(body: str, old: str, new: str, count: int) -> str:
    before = f'ref = "job:{old}"'
    after = f'ref = "job:{new}"'
    if body.count(before) != count or count < 1:
        raise ValueError(f"Expected {count} workflow refs {before}; found {body.count(before)}")
    return body.replace(before, after)


def _validate_input(before: dict) -> None:
    targets = before.get("targets", {})
    if any(name not in targets for name in (*OWNERS, "roostercontrol_pure", "pure")):
        raise ValueError("Missing owner or pure compatibility target")
    if before.get("execution", {}).get("environment") != "auto":
        raise ValueError("R02b host auto configuration missing")
    if before.get("jobs", {}).get("target-ownership-contract", {}).get("metadata", {}).get("read_only") is not True:
        raise ValueError("R03 source-only ownership job missing")
    for old, (owner, local) in MIGRATIONS.items():
        job = before.get("jobs", {}).get(old)
        if not isinstance(job, dict):
            raise ValueError(f"Missing R03 repository job: {old}")
        if job.get("root") != "." or job.get("runner") != "command":
            raise ValueError(f"Unexpected original job shape: {old}")
        if job.get("command", [""])[0] != EXPECTED_EXEC[owner][local]:
            raise ValueError(f"Unexpected existing job command: {old}")
        if local in targets[owner].get("jobs", {}):
            raise ValueError(f"Target-local job already exists: {owner}.{local}")
        workflow = targets[owner].get("workflows", {})
        if not any(s.get("ref") == f"job:{old}" for steps in workflow.values() for s in steps):
            raise ValueError(f"No workflow references old job: {old}")
    for owner in ("letmehatch", "eggsinwild", "roostercontrol"):
        if before["targets"][owner].get("test_command") != f"./Tests/devtool/validate-{owner}.sh":
            raise ValueError(f"Unexpected existing test_command for {owner}")
    if "validate" in targets["pure"]["workflows"] or "validate" in targets["roostercontrol_pure"]["workflows"]:
        raise ValueError("Pure validate workflow already exists")
    if "unit" in targets["roostercontrol_pure"].get("jobs", {}):
        raise ValueError("Pure test job already exists")


def migrate_source(source: bytes) -> bytes:
    raw = source.decode("utf-8")
    if "\r" in raw or not raw.endswith("\n"):
        raise ValueError("Expected LF-terminated R03 Devtool config")
    before = tomllib.loads(raw)
    _validate_input(before)
    expected = copy.deepcopy(before)
    text = raw
    # Translate each repository job to a local job. Explicit root=".."
    # resolves to the original repository-root working directory at runtime.
    for old, (owner, local) in MIGRATIONS.items():
        text, body = _remove_section(text, "jobs." + old)
        text, env_body = _remove_section(text, "jobs." + old + ".env")
        updated, count = re.subn(r'(?m)^root = "\."$', 'root = ".."', body)
        if count != 1:
            raise ValueError(f"Expected one root in job {old}")
        text += f"\n[targets.{owner}.jobs.{local}]\n" + updated.rstrip("\n") + "\n"
        text += f"\n[targets.{owner}.jobs.{local}.env]\n" + env_body.rstrip("\n") + "\n"
        ref_count = 0
        for workflow, steps in expected["targets"][owner]["workflows"].items():
            for step in steps:
                if step.get("ref") == "job:" + old:
                    step["ref"] = "job:" + local
                    ref_count += 1
        if not ref_count:
            raise ValueError(f"No workflow ref found for {old}")
        text = _edit_section(text, f"targets.{owner}.workflows",
                             lambda s, a=old, b=local, n=ref_count: _ref_changed(s, a, b, n))
        job = expected["jobs"].pop(old)
        job["root"] = ".."
        expected["targets"][owner].setdefault("jobs", {})[local] = job

    # test_command is evaluated in the target's scoped root, not repo root.
    for owner in ("letmehatch", "eggsinwild", "roostercontrol"):
        old = f'test_command = "./Tests/devtool/validate-{owner}.sh"'
        new = f'test_command = "../Tests/devtool/validate-{owner}.sh"'
        def edit(body: str, old_text: str = old, new_text: str = new) -> str:
            if body.count(old_text) != 1:
                raise ValueError(f"Cannot safely rebase {old_text}")
            return body.replace(old_text, new_text, 1)
        text = _edit_section(text, f"targets.{owner}", edit)
        expected["targets"][owner]["test_command"] = f"../Tests/devtool/validate-{owner}.sh"

    # The canonical PureTests target owns execution; 'pure' is a compatibility
    # alias that delegates instead of running the same .NET test twice.
    text += "\n[targets.roostercontrol_pure.jobs.unit]\n"
    text += ('runner = "command"\nroot = ".."\n'
             'command = ["dotnet", "test", "Tests/RoosterControl.PureTests/RoosterControl.PureTests.csproj", "-c", "Release"]\n'
             'inputs = ["RoosterControl.Pure/**", "RoosterControl/Application/Catalogs/**", "RoosterControl/Framework/Config/**", "Tests/RoosterControl.PureTests/**"]\n'
             'outputs = ["RoosterControl.Pure/bin/**", "RoosterControl.Pure/obj/**", "Tests/RoosterControl.PureTests/bin/**", "Tests/RoosterControl.PureTests/obj/**"]\n')
    expected["targets"]["roostercontrol_pure"].setdefault("jobs", {})["unit"] = copy.deepcopy(PURE_JOB)
    text = _edit_section(text, "targets.roostercontrol_pure.workflows",
                         lambda s: s.rstrip("\n") + '\nvalidate = [{id = "unit", ref = "job:unit", depends_on = []}]\n\n')
    text = _edit_section(text, "targets.pure.workflows",
                         lambda s: s.rstrip("\n") + '\nvalidate = [{id = "canonical", ref = "target:roostercontrol_pure#validate", depends_on = []}]\n\n')
    expected["targets"]["roostercontrol_pure"]["workflows"]["validate"] = [
        {"id": "unit", "ref": "job:unit", "depends_on": []}]
    expected["targets"]["pure"]["workflows"]["validate"] = [
        {"id": "canonical", "ref": "target:roostercontrol_pure#validate", "depends_on": []}]

    parsed = tomllib.loads(text)
    if parsed != expected:
        raise ValueError("R04 migration changed unrelated configuration fields")
    errors = validate(parsed)
    if errors:
        raise ValueError("R04 local job contract failed: " + "; ".join(errors))
    return text.encode("utf-8")


def validate(config: dict) -> list[str]:
    problems: list[str] = []
    targets = config.get("targets", {})
    globals_ = config.get("jobs", {})
    for old, (owner, local) in MIGRATIONS.items():
        if old in globals_:
            problems.append(f"Legacy global job still present: {old}")
        job = targets.get(owner, {}).get("jobs", {}).get(local, {})
        if job.get("root") != ".." or job.get("runner") != "command":
            problems.append(f"Wrong target-local job root/runner: {owner}.{local}")
        command = job.get("command", [])
        if not isinstance(command, list) or not command or command[0] != EXPECTED_EXEC[owner][local]:
            problems.append(f"Wrong target-local command: {owner}.{local}")
        if not isinstance(job.get("inputs"), list) or not isinstance(job.get("outputs"), list):
            problems.append(f"Missing job scopes: {owner}.{local}")
        if "env" not in job or job["env"].get("DOTNET_ROLL_FORWARD") != "Major":
            problems.append(f"Job environment drift: {owner}.{local}")
        referenced = [s.get("ref") for steps in targets.get(owner, {}).get("workflows", {}).values() for s in steps if isinstance(s, dict)]
        if f"job:{local}" not in referenced or f"job:{old}" in referenced:
            problems.append(f"Workflow does not resolve local job: {owner}.{local}")
    if set(globals_) != {"target-ownership-contract"}:
        problems.append("Shared global job set unexpectedly changed")
    if globals_.get("target-ownership-contract", {}).get("metadata", {}).get("read_only") is not True:
        problems.append("Shared ownership contract must remain read-only")
    if targets.get("svmods", {}).get("jobs", {}).get("ci-source", {}).get("metadata", {}).get("read_only") is not True:
        problems.append("Source-only CI job must remain read-only")
    for owner in ("letmehatch", "eggsinwild", "roostercontrol"):
        if targets.get(owner, {}).get("test_command") != f"../Tests/devtool/validate-{owner}.sh":
            problems.append(f"Unportable test command: {owner}")
    pure = targets.get("roostercontrol_pure", {})
    if pure.get("jobs", {}).get("unit") != PURE_JOB:
        problems.append("Canonical pure test job changed")
    if pure.get("workflows", {}).get("validate") != [{"id": "unit", "ref": "job:unit", "depends_on": []}]:
        problems.append("Canonical pure workflow changed")
    if targets.get("pure", {}).get("workflows", {}).get("validate") != [
        {"id": "canonical", "ref": "target:roostercontrol_pure#validate", "depends_on": []}]:
        problems.append("Pure compatibility alias must delegate to canonical test target")
    return sorted(set(problems))


def main() -> int:
    if sys.argv[1:] not in ([], ["--json"]):
        print("Usage: devtool_target_jobs.py [--json]", file=sys.stderr)
        return 2
    root = Path(__file__).resolve().parents[1]
    cfg = tomllib.loads((root / ".devtool.toml").read_text(encoding="utf-8"))
    issues = validate(cfg)
    print(json.dumps({"status": "FAIL" if issues else "PASS", "issues": issues,
                      "local_dotnet": "NOT_RUN", "hosted_devtool_gate": "R06_PENDING"}, sort_keys=True))
    return 1 if issues else 0


if __name__ == "__main__":
    raise SystemExit(main())
