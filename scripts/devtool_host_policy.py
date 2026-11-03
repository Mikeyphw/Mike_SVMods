#!/usr/bin/env python3
"""R02b: portable, source-only Devtool execution policy (no .NET invocation)."""
from __future__ import annotations

from copy import deepcopy
from pathlib import Path
import re
import tomllib

HOST_TARGETS = ("nestcore", "letmehatch", "eggsinwild", "roostercontrol", "svmods")
TERMUX_GAME_PATH = 'GamePath = "/data/data/com.termux/files/home/stardew/game"'


def issues(config: dict) -> list[str]:
    problems: list[str] = []
    if config.get("execution", {}).get("environment") != "auto":
        problems.append('Devtool execution.environment must be "auto"')
    targets = config.get("targets", {})
    for name in HOST_TARGETS:
        if name not in targets:
            problems.append(f"Missing R02b target: {name}")
            continue
        env = targets[name].get("env", {})
        if "GamePath" in env:
            problems.append(f"{name}: do not pin GamePath in the shared Devtool configuration")
        for key, value in env.items():
            if isinstance(value, str) and value.startswith("/data/data/com.termux/"):
                problems.append(f"{name}.{key}: absolute Termux path is not portable")
    return problems


def migrate_source(source: bytes) -> bytes:
    """Fail closed unless only the intended five paths and one environment change occur."""
    if not source.endswith(b"\n"):
        raise ValueError("Expected LF-terminated baseline TOML")
    before = tomllib.loads(source.decode("utf-8"))
    if before.get("execution", {}).get("environment") != "native-termux":
        raise ValueError("Expected native-termux base execution policy")
    old_env = b'environment = "native-termux"'
    old_game = TERMUX_GAME_PATH.encode("utf-8")
    if source.count(old_env) != 1 or source.count(old_game) != len(HOST_TARGETS):
        raise ValueError("Unexpected environment or GamePath source occurrences")
    for name in HOST_TARGETS:
        actual = before["targets"][name].get("env", {}).get("GamePath")
        if actual != "/data/data/com.termux/files/home/stardew/game":
            raise ValueError(f"Unexpected {name} GamePath")

    updated = source.replace(old_env, b'environment = "auto"').replace(old_game + b"\n", b"")
    # Drop the now-empty table, leaving all non-GamePath target environment keys intact.
    empty = b"[targets.nestcore.env]\n\n"
    if updated.count(empty) != 1:
        raise ValueError("Unexpected NestCore environment table shape")
    updated = updated.replace(empty, b"", 1)
    changed = tomllib.loads(updated.decode("utf-8"))
    expected = deepcopy(before)
    expected["execution"]["environment"] = "auto"
    for name in HOST_TARGETS:
        env = expected["targets"][name].get("env", {})
        env.pop("GamePath", None)
        if not env:
            expected["targets"][name].pop("env", None)
    if expected != changed or issues(changed):
        raise ValueError("Unexpected configuration change during R02b migration")
    return updated


def assert_current_repo(root: Path) -> None:
    config = tomllib.loads((root / ".devtool.toml").read_text(encoding="utf-8"))
    failures = issues(config)
    if failures:
        raise AssertionError("R02B_HOST_POLICY_FAIL: " + "; ".join(failures))


if __name__ == "__main__":
    assert_current_repo(Path(__file__).resolve().parents[1])
    print("R02B_HOST_POLICY_PASS")
