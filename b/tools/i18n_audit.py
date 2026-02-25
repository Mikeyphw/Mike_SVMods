#!/usr/bin/env python3
"""i18n audit for Mike_SVMods (in-game UI keys).

This audits *translation key usage* in C# sources:
- Extracts string-literal keys passed to I18TranslationHelper.GetString/TryGet.
- Reports keys missing from i18n/default.json per mod.

It intentionally ignores console/log strings and does not attempt to detect every possible UI literal.
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[1]
REPORTS_DIR = ROOT / "Reports"
REPORT_PATH = REPORTS_DIR / "i18n_audit.md"


KEY_CALL_RE = re.compile(
    r"""(?x)
    \b(?:GetString|TryGet)\(
        \s*
        \"(?P<key>[^\"]+)\"
    """
)

@dataclass(frozen=True)
class Finding:
    mod: str
    key: str
    file: str
    line: int


def iter_mod_dirs(root: Path) -> Iterable[Path]:
    for child in root.iterdir():
        if not child.is_dir():
            continue
        if (child / "manifest.json").exists():
            yield child


def load_default_keys(mod_dir: Path) -> set[str]:
    default_path = mod_dir / "i18n" / "default.json"
    if not default_path.exists():
        return set()
    try:
        data = json.loads(default_path.read_text(encoding="utf-8"))
        return {str(k) for k in data.keys()}
    except Exception:
        return set()


def scan_cs_files(mod_dir: Path, default_keys: set[str]) -> list[Finding]:
    findings: list[Finding] = []
    for cs in mod_dir.rglob("*.cs"):
        text = cs.read_text(encoding="utf-8", errors="ignore")
        for match in KEY_CALL_RE.finditer(text):
            key = match.group("key")
            if key not in default_keys:
                # compute line number
                line = text.count("\n", 0, match.start()) + 1
                findings.append(
                    Finding(
                        mod=mod_dir.name,
                        key=key,
                        file=str(cs.relative_to(ROOT)),
                        line=line,
                    )
                )
    return findings


def write_report(findings: list[Finding]) -> None:
    REPORTS_DIR.mkdir(parents=True, exist_ok=True)

    by_mod: dict[str, list[Finding]] = {}
    for f in findings:
        by_mod.setdefault(f.mod, []).append(f)

    lines: list[str] = []
    lines.append("# i18n audit (key usage)")
    lines.append("")
    lines.append("This report lists *string-literal* translation keys used in C# which are missing from `i18n/default.json`.")
    lines.append("")
    if not findings:
        lines.append("✅ No missing keys found.")
        lines.append("")
        REPORT_PATH.write_text("\n".join(lines), encoding="utf-8")
        return

    for mod, items in sorted(by_mod.items()):
        lines.append(f"## {mod}")
        lines.append("")
        for f in sorted(items, key=lambda x: (x.key, x.file, x.line)):
            lines.append(f"- `{f.key}` at `{f.file}:{f.line}`")
        lines.append("")

    REPORT_PATH.write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    all_findings: list[Finding] = []

    for mod_dir in iter_mod_dirs(ROOT):
        default_keys = load_default_keys(mod_dir)
        if not default_keys:
            continue
        all_findings.extend(scan_cs_files(mod_dir, default_keys))

    write_report(all_findings)
    print(f"Wrote: {REPORT_PATH.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
