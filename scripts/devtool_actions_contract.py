#!/usr/bin/env python3
"""Fail-closed source/CI interface audit; does not qualify hosted Devtool execution."""
from __future__ import annotations

import json
from pathlib import Path
import re
import tomllib

from validate_mod_manifests import validate as validate_manifests
from devtool_target_jobs import validate as validate_local_jobs

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = (
    "python3 scripts/validate_mod_manifests.py",
    "python3 -m unittest discover -s scripts/tests -p 'test_*.py'",
    "python3 scripts/devtool_target_ownership.py --json",
    "python3 scripts/devtool_target_jobs.py --json",
    "python3 scripts/devtool_actions_contract.py",
)


def job_body(yaml_text: str, job: str) -> str:
    """Extract a two-space-indented CI job; avoids matching comments/other jobs."""
    match = re.search(
        r"(?ms)^  " + re.escape(job) + r":\s*\n(.*?)(?=^  [A-Za-z0-9_-]+:\s*\n|\Z)",
        yaml_text,
    )
    return match.group(1) if match else ""


def check_ci_source(yaml_text: str) -> list[str]:
    issues: list[str] = []
    job = job_body(yaml_text, "source-contracts")
    if not job:
        return ["CI source-contracts job missing"]
    if re.search(r"(?m)^\s*continue-on-error:\s*true\s*$", job):
        issues.append("CI source contract cannot ignore failures")
    if re.search(r"(?m)^\s*if:\s*(?:false|\$\{\{\s*false\s*\}\})\s*$", job):
        issues.append("CI source contract cannot be disabled")
    for command in COMMANDS:
        if not re.search(r"(?m)^\s*(?:-\s*)?run:\s*" + re.escape(command) + r"\s*$", job):
            issues.append(f"CI does not execute: {command}")
    if not job_body(yaml_text, "pure-tests"):
        issues.append("CI must retain baseline pure-tests job until R06")
    return issues


def check_devtool(root: Path, cfg: dict) -> list[str]:
    issues: list[str] = []
    wrapper = cfg.get("wrapper", {})
    targets = cfg.get("targets", {})
    if wrapper.get("schema") != 2:
        issues.append("Devtool wrapper schema 2 required")
    for name, item in wrapper.get("commands", {}).items():
        if not name.startswith("ci-"):
            continue
        target = targets.get(item.get("target"), {})
        if item.get("workflow") not in target.get("workflows", {}):
            issues.append(f"{name}: unresolved target/workflow")
    for name, target in targets.items():
        # Devtool resolves solution/build_target/test_target from the repo root,
        # independently of target.root (which scopes execution/ownership).
        # Check the same paths the actual config parser resolves.
        for key in ("solution", "build_target", "test_target"):
            path = target.get(key)
            if path and not (root / path).is_file():
                issues.append(f"{name}: missing {key} {path}")
        for workflow, steps in target.get("workflows", {}).items():
            if not steps:
                issues.append(f"{name}/{workflow}: empty workflow")
            ids: set[str] = set()
            for step in steps:
                if not isinstance(step, dict):
                    continue
                sid = step.get("id")
                if not sid or sid in ids:
                    issues.append(f"{name}/{workflow}: duplicate or missing step ID")
                if any(dep not in ids for dep in step.get("depends_on", [])):
                    issues.append(f"{name}/{workflow}: {sid} references future/unknown predecessor")
                ref = step.get("ref", "")
                if ref.startswith("job:"):
                    jname = ref[4:]
                    if jname not in cfg.get("jobs", {}) and jname not in target.get("jobs", {}):
                        issues.append(f"{name}/{workflow}: undefined {ref}")
                ids.add(sid)
    source_job = targets.get("svmods", {}).get("jobs", {}).get("ci-source", {})
    if source_job.get("command") != ["python3", "scripts/devtool_actions_contract.py"]:
        issues.append("svmods source-contract job changed or missing")
    issues.extend(validate_local_jobs(cfg))
    return issues


def main() -> int:
    issues = validate_manifests(ROOT)
    try:
        cfg = tomllib.loads((ROOT / ".devtool.toml").read_text(encoding="utf-8"))
        issues.extend(check_devtool(ROOT, cfg))
    except (OSError, ValueError) as error:
        issues.append(f"cannot load Devtool config: {error}")
    try:
        ci = (ROOT / ".github/workflows/ci.yml").read_text(encoding="utf-8")
        issues.extend(check_ci_source(ci))
    except OSError as error:
        issues.append(f"cannot read CI workflow: {error}")
    result = {
        "status": "FAIL" if issues else "PASS",
        "errors": sorted(set(issues)),
        "hosted_devtool_execution": "NOT_RUN",
        "hosted_devtool_gate": "DEFERRED_TO_R06",
    }
    print(json.dumps(result, sort_keys=True))
    return 1 if issues else 0


if __name__ == "__main__":
    raise SystemExit(main())
