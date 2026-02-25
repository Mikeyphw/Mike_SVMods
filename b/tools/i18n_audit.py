#!/usr/bin/env python3
"""i18n audit for Mike_SVMods (in-game UI strings).

This tool catches two classes of problems:
1) Missing translation keys referenced in code (GetString/TryGet/Translation.Get).
2) Hardcoded player-facing string literals in GMCM/UI hotspots.

It writes a markdown report to Reports/i18n_audit.md and exits non-zero if issues found.
"""

from __future__ import annotations

import argparse
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


KEY_CALL_PATTERNS = [
    # NestCore helper
    re.compile(r'\.GetString\(\s*"(?P<key>[^"]+)"', re.MULTILINE),
    re.compile(r'\.TryGet\(\s*"(?P<key>[^"]+)"', re.MULTILINE),
    # SMAPI Translation helper in GMCM-heavy mods
    re.compile(r'\.Translation\.Get\(\s*"(?P<key>[^"]+)"', re.MULTILINE),
]

# Heuristic: GMCM/UI methods where a raw string literal is likely user-facing.
UI_CALL_HINT = re.compile(
    r'\.(Section|SectionLabel|SectionLabelDynamic|Paragraph|ParagraphDynamic|AddParagraph|AddSectionTitle|AddTextOption|AddBoolOption|AddNumberOption|AddKeybind|AddPageLink|AddParagraphLink|Link|Page)\s*\('
)

# Captures string literals (simple; good enough for audit).
STRING_LITERAL = re.compile(r'"(?P<lit>(?:[^"\\]|\\.)*)"')

# Detect legacy fallback patterns: Translate(i18n, "key", "fallback")
FALLBACK_CALL = re.compile(r'\b(Translate|T)\s*\([^\)]*,\s*"[^"]+"\s*,\s*"(?P<fallback>[^"]+)"', re.MULTILINE)


@dataclass(frozen=True)
class Finding:
    kind: str
    file: Path
    line: int
    detail: str


def iter_cs_files(root: Path) -> Iterable[Path]:
    for p in root.rglob("*.cs"):
        # Skip generated/obj/bin if present
        parts = {x.lower() for x in p.parts}
        if {"bin", "obj", ".git"} & parts:
            continue
        yield p


def load_default_keys(mod_root: Path) -> set[str]:
    default_json = mod_root / "i18n" / "default.json"
    if not default_json.exists():
        return set()
    try:
        data = json.loads(default_json.read_text(encoding="utf-8"))
        return set(data.keys())
    except Exception:
        return set()


def guess_mod_root(file: Path) -> Path | None:
    # mod root is immediate child of repo root (NestCore, EggsInWild, etc)
    for parent in [file] + list(file.parents):
        if (parent / "i18n" / "default.json").exists():
            return parent
    return None


def collect_key_usages(text: str) -> set[str]:
    keys: set[str] = set()
    for pat in KEY_CALL_PATTERNS:
        for m in pat.finditer(text):
            keys.add(m.group("key"))
    return keys


def find_hardcoded_ui_literals(file: Path, lines: list[str]) -> list[Finding]:
    findings: list[Finding] = []
    is_ui_hotspot = any(
        token in str(file).lower()
        for token in ["gmcm", "integrations/gmcm", ".gmcm."]
    )

    for idx, line in enumerate(lines, start=1):
        if not is_ui_hotspot and not UI_CALL_HINT.search(line):
            continue

        # flag fallback patterns immediately
        fb = FALLBACK_CALL.search(line)
        if fb and fb.group("fallback").strip():
            findings.append(
                Finding(
                    kind="fallback-literal",
                    file=file,
                    line=idx,
                    detail=f"Fallback literal detected: \"{fb.group('fallback')}\"",
                )
            )

        if not UI_CALL_HINT.search(line):
            continue

        for m in STRING_LITERAL.finditer(line):
            lit = m.group("lit")
            if not lit:
                continue

            # ignore keys, asset names, IDs
            if "." in lit and " " not in lit:
                continue

            # user-facing heuristic: has letters and spaces
            if re.search(r"[A-Za-z]", lit) and (" " in lit or ":" in lit):
                findings.append(
                    Finding(
                        kind="hardcoded-ui-literal",
                        file=file,
                        line=idx,
                        detail=f"Literal: \"{lit}\"",
                    )
                )

    return findings


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", type=Path, default=Path.cwd(), help="repo root")
    ap.add_argument("--report", type=Path, default=None, help="report path (default: Reports/i18n_audit.md)")
    args = ap.parse_args()

    root: Path = args.root.resolve()
    report_path = args.report or (root / "Reports" / "i18n_audit.md")
    report_path.parent.mkdir(parents=True, exist_ok=True)

    findings: list[Finding] = []
    missing_keys: list[str] = []

    # Cache mod key sets
    mod_keys: dict[Path, set[str]] = {}

    for cs in iter_cs_files(root):
        text = cs.read_text(encoding="utf-8", errors="replace")
        lines = text.splitlines()

        mod_root = guess_mod_root(cs)
        if mod_root and mod_root not in mod_keys:
            mod_keys[mod_root] = load_default_keys(mod_root)

        used_keys = collect_key_usages(text)
        if mod_root:
            known = mod_keys.get(mod_root, set())
            for k in sorted(used_keys):
                # ignore non-i18n keys (rare) but keep config/specialOrder etc
                if k.startswith(("config.", "specialOrder.", "object.", "mail.", "quest.", "hud.", "message.")):
                    if k not in known:
                        findings.append(Finding("missing-key", cs, 1, k))

        findings.extend(find_hardcoded_ui_literals(cs, lines))

    # Write report
    by_kind: dict[str, list[Finding]] = {}
    for f in findings:
        by_kind.setdefault(f.kind, []).append(f)

    md = []
    md.append(f"# i18n Audit Report\n\nRoot: `{root}`\n")
    for kind in sorted(by_kind.keys()):
        md.append(f"## {kind}\n")
        for f in sorted(by_kind[kind], key=lambda x: (str(x.file), x.line, x.detail)):
            rel = f.file.relative_to(root)
            md.append(f"- `{rel}:{f.line}` — {f.detail}")
        md.append("")

    report_path.write_text("\n".join(md).rstrip() + "\n", encoding="utf-8")

    return 1 if findings else 0


if __name__ == "__main__":
    raise SystemExit(main())
