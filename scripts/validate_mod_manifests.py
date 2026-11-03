#!/usr/bin/env python3
"""Read-only SMAPI manifest source validation (no game or SMAPI binaries needed)."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

MODS = ("NestCore", "LetMeHatch", "RoosterControl", "EggsInWild")
RETIRED_INTERNAL_IDS = {"mikew.letmehatch.smapi": "Mikew.lmh"}
VERSION = re.compile(r"^[0-9]+(?:\.[0-9]+){1,3}(?:[-+][A-Za-z0-9.-]+)?$")
API_VERSION = re.compile(r"^[0-9]+(?:\.[0-9]+){1,3}$")
UPDATE_KEY = re.compile(r"^[A-Za-z][A-Za-z0-9]*:[^\s:]+$")
NEXUS_KEY = re.compile(r"^Nexus:[1-9][0-9]*$", re.I)


def api_parts(value: object) -> tuple[int, int, int, int] | None:
    if not isinstance(value, str) or not API_VERSION.fullmatch(value):
        return None
    components = [int(part) for part in value.split(".")]
    return tuple((components + [0] * 4)[:4])  # type: ignore[return-value]


def project_dll(project: Path) -> str | None:
    try:
        tree = ET.parse(project)
    except (ET.ParseError, OSError):
        return None
    name = tree.findtext(".//AssemblyName") or project.stem
    return name + ".dll"


def validate(root: Path) -> list[str]:
    """Validate exactly the four owned mods; return deterministic failures."""
    root = root.resolve()
    errors: list[str] = []
    known: dict[str, str] = {}
    manifests: dict[str, dict] = {}

    for mod in MODS:
        path = root / mod / "manifest.json"
        if not path.is_file() or path.is_symlink():
            errors.append(f"{mod}: missing or non-regular manifest.json")
            continue
        try:
            obj = json.loads(path.read_text(encoding="utf-8-sig"))
        except (ValueError, OSError) as exc:
            errors.append(f"{mod}: invalid JSON ({type(exc).__name__})")
            continue
        if not isinstance(obj, dict):
            errors.append(f"{mod}: manifest must be an object")
            continue
        manifests[mod] = obj
        for field in ("Name", "Author", "Description", "UniqueID"):
            if not isinstance(obj.get(field), str) or not obj[field].strip():
                errors.append(f"{mod}: missing or empty {field}")
        uid = obj.get("UniqueID")
        if isinstance(uid, str) and uid.strip():
            norm = uid.casefold()
            if norm in known:
                errors.append(f"{mod}: duplicate UniqueID {uid} (also {known[norm]})")
            else:
                known[norm] = mod
            if not re.fullmatch(r"[A-Za-z0-9_-]+(?:\.[A-Za-z0-9_-]+)+", uid):
                errors.append(f"{mod}: malformed UniqueID {uid}")
        if not isinstance(obj.get("Version"), str) or not VERSION.fullmatch(obj["Version"]):
            errors.append(f"{mod}: invalid Version")
        if api_parts(obj.get("MinimumApiVersion")) is None:
            errors.append(f"{mod}: invalid or missing MinimumApiVersion")
        dll = obj.get("EntryDll")
        expected = project_dll(root / mod / f"{mod}.csproj")
        if not isinstance(dll, str) or not re.fullmatch(r"[A-Za-z0-9_.-]+\.dll", dll):
            errors.append(f"{mod}: missing or unsafe EntryDll")
        elif expected is None or dll != expected:
            errors.append(f"{mod}: EntryDll {dll} differs from project {expected}")
        keys = obj.get("UpdateKeys", [])
        if not isinstance(keys, list):
            errors.append(f"{mod}: UpdateKeys must be a list")
        else:
            for key in keys:
                if not isinstance(key, str) or not UPDATE_KEY.fullmatch(key):
                    errors.append(f"{mod}: invalid UpdateKeys {key!r}")
                elif key.casefold().startswith("nexus:") and not NEXUS_KEY.fullmatch(key):
                    errors.append(f"{mod}: invalid Nexus key {key!r}")

    for mod, obj in manifests.items():
        deps = obj.get("Dependencies", [])
        if not isinstance(deps, list):
            errors.append(f"{mod}: Dependencies must be a list")
            continue
        seen: set[str] = set()
        for i, dep in enumerate(deps):
            if not isinstance(dep, dict) or not isinstance(dep.get("UniqueID"), str) or not dep["UniqueID"].strip():
                errors.append(f"{mod}: invalid dependency #{i}")
                continue
            uid = dep["UniqueID"]
            norm = uid.casefold()
            if norm in seen:
                errors.append(f"{mod}: duplicate dependency {uid}")
            seen.add(norm)
            if norm == str(obj.get("UniqueID", "")).casefold():
                errors.append(f"{mod}: depends on itself")
            if norm in RETIRED_INTERNAL_IDS:
                errors.append(f"{mod}: stale dependency {uid}; use {RETIRED_INTERNAL_IDS[norm]}")
            if "IsRequired" in dep and not isinstance(dep["IsRequired"], bool):
                errors.append(f"{mod}: dependency {uid} has invalid IsRequired")
            if "MinimumVersion" in dep and not isinstance(dep["MinimumVersion"], str):
                errors.append(f"{mod}: dependency {uid} has invalid MinimumVersion")
            owner = known.get(norm)
            if owner and dep.get("IsRequired", True):
                own_min = api_parts(obj.get("MinimumApiVersion"))
                other_min = api_parts(manifests[owner].get("MinimumApiVersion"))
                if own_min and other_min and own_min < other_min:
                    errors.append(f"{mod}: MinimumApiVersion lower than required {owner}")
    return sorted(set(errors))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)
    problems = validate(args.repo)
    verdict = "FAIL" if problems else "PASS"
    if args.json:
        print(json.dumps({"status": verdict, "mod_count": len(MODS), "errors": problems}, sort_keys=True))
    else:
        print(f"SMAPI_MANIFEST_CONTRACT_{verdict}: {len(MODS)} mods")
        for problem in problems:
            print(f"  - {problem}", file=sys.stderr)
    return int(bool(problems))


if __name__ == "__main__":
    raise SystemExit(main())
