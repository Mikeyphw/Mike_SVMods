#!/usr/bin/env python3
"""
tools/debug_knob_audit.py

Heuristic scanner for "debug knobs" (hardcoded tuning values) in C# mods:
- finds lines with numeric literals + likely tuning keywords/ops (chance, cooldown, ticks, clamp, Next, etc)
- skips bin/obj/Tests and some common non-tuning files by default

Usage:
  python tools/debug_knob_audit.py [root_dir] [--max-per-file N]
"""

from __future__ import annotations

import argparse
import os
import re
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


NUM_TOKEN = re.compile(r"(?<![\w.])(?:0x[0-9A-Fa-f]+|\d+\.\d+|\d+)(?![\w.])")
KEYWORDS = re.compile(
    r"\b(chance|fraction|interval|ticks|radius|distance|attempt|max|min|multiplier|weight|cap|"
    r"cooldown|delay|timeout|guard|wave|spawn|loot|drop|luck|vision|range|heal|damage)\b",
    re.IGNORECASE,
)
OPS = re.compile(r"NextDouble\(|Next\(|Math\.Clamp|Math\.Round|Math\.Min|Math\.Max|%|/|\*")

SKIP_PATH = re.compile(
    r"(?:^|[\\/])(bin|obj|Tests)(?:[\\/]|$)|"
    r"EnumsAndStrings\.cs|Gmcm|Framework[\\/]Config|Infrastructure[\\/]Commands|"
    r"SaveKeys|Ids|LegacyConfigAdapter",
    re.IGNORECASE,
)


@dataclass(frozen=True)
class Hit:
    rel_path: str
    line_no: int
    line: str


def is_code_line(line: str) -> bool:
    s = line.strip()
    if not s:
        return False
    return not (s.startswith("//") or s.startswith("///"))


def iter_cs_files(root: Path) -> Iterable[Path]:
    for p in root.rglob("*.cs"):
        if SKIP_PATH.search(str(p)):
            continue
        yield p


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("root_dir", nargs="?", default=".", help="Repo root (default: .)")
    parser.add_argument("--max-per-file", type=int, default=25, help="Max hits per file")
    args = parser.parse_args()

    root = Path(args.root_dir).resolve()
    if not root.exists():
        print(f"Root dir not found: {root}")
        return 2

    hits_by_file: dict[str, list[Hit]] = defaultdict(list)

    for fp in iter_cs_files(root):
        rel = fp.relative_to(root).as_posix()
        try:
            lines = fp.read_text(encoding="utf-8").splitlines()
        except UnicodeDecodeError:
            continue

        for idx, line in enumerate(lines, start=1):
            if not is_code_line(line):
                continue
            if not NUM_TOKEN.search(line):
                continue
            if not (KEYWORDS.search(line) or OPS.search(line)):
                continue

            # Ignore FNV constants (common deterministic hash impl detail)
            if "2166136261" in line or "16777619" in line:
                continue

            hits_by_file[rel].append(Hit(rel, idx, line.rstrip()))
            if len(hits_by_file[rel]) >= args.max_per_file:
                break

    if not hits_by_file:
        print("✅ No likely debug knobs found (with current heuristics).")
        return 0

    print("❌ Likely debug knobs (heuristic):\n")

    file_order = Counter({k: len(v) for k, v in hits_by_file.items()}).most_common()
    for rel, count in file_order:
        print(f"[{rel}] ({count} hits)")
        for h in hits_by_file[rel]:
            print(f"  {h.line_no:4d}: {h.line}")
        print()

    return 1


if __name__ == "__main__":
    raise SystemExit(main())
