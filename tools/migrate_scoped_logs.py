#!/usr/bin/env python3
"""
Migrate C# log call sites to scoped BOAF logging helper calls.

Default behavior is dry-run (no file writes). Use --apply to write changes.

Rewrites:
  - <target>.Log(...)
  - <target>.VerboseLog(...)
  - <target>.TestingLog(...)

Targets:
  - Monitor / _monitor -> ScopedLogMigration.LogMonitor(...)
  - _log / log         -> ScopedLogMigration.Log|Verbose|Testing(...)

Resulting calls are fully-qualified to avoid adding using directives.
"""

from __future__ import annotations

import argparse
import dataclasses
import pathlib
import re
import sys
from typing import Iterable, List, Sequence, Tuple


LOG_TARGET_RE = re.compile(
    r"\b(?P<target>Monitor|_monitor|_log|log)\.(?P<method>Log|VerboseLog|TestingLog)\s*\(",
    re.MULTILINE,
)


MOD_TAGS = {
    "BirdsOfAFeather": "BOAF",
    "RoosterAddon": "RoosterAddon",
    "WildEggsHatching": "WE",
    "LetMeHatchThisEgg": "LTHTE",
}

HELPER_REL_PATH = pathlib.Path("BirdsOfAFeather/Helpers/Logging/ScopedLogMigration.cs")
HELPER_TEMPLATE = """using Mikew.BirdsOfAFeather.Framework;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Helpers.Logging;

public static class ScopedLogMigration
{
    public static void Log(ILog log, string mod, string category, string message)
        => Log(log, mod, category, message, LogLevel.Trace);

    public static void Log(ILog log, string mod, string category, string message, LogLevel level)
    {
        if (log is SmapiLog slog)
            slog.LogScoped(mod, category, message, level);
        else
            log.Log(message, level);
    }

    public static void Verbose(ILog log, string mod, string category, string message)
        => Verbose(log, mod, category, message, LogLevel.Debug);

    public static void Verbose(ILog log, string mod, string category, string message, LogLevel level)
    {
        if (log is SmapiLog slog)
            slog.LogScoped(mod, category, message, level);
        else
            log.VerboseLog(message, level);
    }

    public static void Testing(ILog log, string mod, string category, string message)
        => Testing(log, mod, category, message, LogLevel.Debug);

    public static void Testing(ILog log, string mod, string category, string message, LogLevel level)
    {
        if (log is SmapiLog slog)
            slog.LogScoped(mod, category, message, level);
        else
            log.TestingLog(message, level);
    }

    public static void LogMonitor(IMonitor monitor, string mod, string category, string message)
        => LogMonitor(monitor, mod, category, message, LogLevel.Trace);

    public static void LogMonitor(IMonitor monitor, string mod, string category, string message, LogLevel level)
    {
        var helper = new SmapiLog(monitor, configProvider: () => ModEntry.ModCfg, defaultMod: mod, defaultCategory: category);
        helper.LogScoped(mod, category, message, level);
    }
}
"""


@dataclasses.dataclass
class Rewrite:
    start: int
    end: int
    original: str
    replacement: str
    line: int


def parse_args(argv: Sequence[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Migrate log call sites to scoped logging helper calls.")
    parser.add_argument("--root", default=".", help="Repository root (default: current directory).")
    parser.add_argument(
        "--mods",
        nargs="*",
        default=list(MOD_TAGS.keys()),
        choices=list(MOD_TAGS.keys()),
        help="Top-level mod folders to process.",
    )
    parser.add_argument("--apply", action="store_true", help="Write changes to files.")
    parser.add_argument("--verbose", action="store_true", help="Print each rewritten call.")
    parser.add_argument(
        "--emit-helper",
        action="store_true",
        help="Create/overwrite BirdsOfAFeather/Helpers/Logging/ScopedLogMigration.cs helper.",
    )
    return parser.parse_args(argv)


def normalize_path_parts(path: pathlib.Path) -> List[str]:
    return [p.lower() for p in path.parts]


def infer_category(mod_root: str, rel_path: pathlib.Path) -> str:
    parts = normalize_path_parts(rel_path)
    path_text = "/".join(parts)

    # WildEggsHatching
    if mod_root == "WildEggsHatching":
        if "application/animalnotes" in path_text:
            return "animal-notes"
        if "application/spawning" in path_text:
            return "spawning"
        if "application/progression" in path_text:
            return "progression"
        if "deserttomb" in path_text:
            return "desert-tomb"
        if "infrastructure/commands" in path_text:
            return "commands"
        if "infrastructure/diagnostics" in path_text:
            return "verify"
        if "infrastructure/content" in path_text:
            return "content"
        if "savedata" in path_text:
            return "save-data"
        if "mods/" in path_text or "/mods/" in path_text or "integrations" in path_text:
            return "integrations"
        return "general"

    # RoosterAddon
    if mod_root == "RoosterAddon":
        if "catalog" in path_text:
            return "catalogs"
        if "planning" in path_text:
            return "planning"
        if "hatching" in path_text:
            return "hatching"
        if "farmanimal" in path_text:
            return "farmanimals"
        if "gmcm" in path_text:
            return "gmcm"
        if "assets" in path_text:
            return "assets"
        if "command" in path_text:
            return "commands"
        if "savedata" in path_text:
            return "save-data"
        if "integrations" in path_text:
            return "integrations"
        return "general"

    # LetMeHatchThisEgg
    if mod_root == "LetMeHatchThisEgg":
        if "cloning" in path_text:
            return "cloning"
        if "breeding" in path_text:
            return "breeding"
        if "gmcm" in path_text:
            return "gmcm"
        if "command" in path_text:
            return "commands"
        if "integrations" in path_text:
            return "integrations"
        if "infrastructure/content" in path_text:
            return "content-pipeline"
        if "asset" in path_text:
            return "assets"
        if "coordination" in path_text or "runtime" in path_text:
            return "runtime"
        return "general"

    # BirdsOfAFeather
    if mod_root == "BirdsOfAFeather":
        if "gmcm" in path_text:
            return "gmcm"
        if "helpers/config" in path_text:
            return "config"
        if "helpers/content" in path_text:
            return "content"
        if "helpers/savedata" in path_text:
            return "save-data"
        if "integrations" in path_text:
            return "integrations"
        if "modentry" in path_text:
            return "runtime"
        return "general"

    return "general"


def index_to_line(text: str, idx: int) -> int:
    return text.count("\n", 0, idx) + 1


def find_matching_paren(text: str, open_idx: int) -> int:
    # open_idx must point at '('
    depth = 0
    i = open_idx
    in_string = False
    in_char = False
    verbatim = False
    while i < len(text):
        ch = text[i]
        nxt = text[i + 1] if i + 1 < len(text) else ""

        if in_string:
            if verbatim:
                if ch == '"' and nxt == '"':
                    i += 2
                    continue
                if ch == '"':
                    in_string = False
                    verbatim = False
            else:
                if ch == "\\":
                    i += 2
                    continue
                if ch == '"':
                    in_string = False
            i += 1
            continue

        if in_char:
            if ch == "\\":
                i += 2
                continue
            if ch == "'":
                in_char = False
            i += 1
            continue

        if ch == "@" and nxt == '"':
            in_string = True
            verbatim = True
            i += 2
            continue
        if ch == '"':
            in_string = True
            verbatim = False
            i += 1
            continue
        if ch == "'":
            in_char = True
            i += 1
            continue

        if ch == "(":
            depth += 1
        elif ch == ")":
            depth -= 1
            if depth == 0:
                return i
        i += 1

    raise ValueError("Unmatched '(' in source.")


def split_top_level_args(args_text: str) -> List[str]:
    args: List[str] = []
    start = 0
    depth_paren = 0
    depth_brace = 0
    depth_bracket = 0
    i = 0
    in_string = False
    in_char = False
    verbatim = False

    while i < len(args_text):
        ch = args_text[i]
        nxt = args_text[i + 1] if i + 1 < len(args_text) else ""

        if in_string:
            if verbatim:
                if ch == '"' and nxt == '"':
                    i += 2
                    continue
                if ch == '"':
                    in_string = False
                    verbatim = False
            else:
                if ch == "\\":
                    i += 2
                    continue
                if ch == '"':
                    in_string = False
            i += 1
            continue

        if in_char:
            if ch == "\\":
                i += 2
                continue
            if ch == "'":
                in_char = False
            i += 1
            continue

        if ch == "@" and nxt == '"':
            in_string = True
            verbatim = True
            i += 2
            continue
        if ch == '"':
            in_string = True
            i += 1
            continue
        if ch == "'":
            in_char = True
            i += 1
            continue

        if ch == "(":
            depth_paren += 1
        elif ch == ")":
            depth_paren -= 1
        elif ch == "{":
            depth_brace += 1
        elif ch == "}":
            depth_brace -= 1
        elif ch == "[":
            depth_bracket += 1
        elif ch == "]":
            depth_bracket -= 1
        elif ch == "," and depth_paren == 0 and depth_brace == 0 and depth_bracket == 0:
            args.append(args_text[start:i].strip())
            start = i + 1
        i += 1

    tail = args_text[start:].strip()
    if tail:
        args.append(tail)
    return args


def build_replacement(
    target: str,
    method: str,
    args: List[str],
    mod_tag: str,
    category: str,
) -> str:
    helper_prefix = "Mikew.BirdsOfAFeather.Helpers.Logging.ScopedLogMigration"
    qmod = f"\"{mod_tag}\""
    qcat = f"\"{category}\""

    if target in {"Monitor", "_monitor"}:
        helper_method = "LogMonitor"
        payload = ", ".join([target, qmod, qcat] + args)
        return f"{helper_prefix}.{helper_method}({payload});"

    if method == "Log":
        helper_method = "Log"
    elif method == "VerboseLog":
        helper_method = "Verbose"
    else:
        helper_method = "Testing"

    payload = ", ".join([target, qmod, qcat] + args)
    return f"{helper_prefix}.{helper_method}({payload});"


def find_rewrites(text: str, mod_tag: str, category: str) -> List[Rewrite]:
    rewrites: List[Rewrite] = []
    for m in LOG_TARGET_RE.finditer(text):
        target = m.group("target")
        method = m.group("method")
        open_paren = m.end() - 1

        try:
            close_paren = find_matching_paren(text, open_paren)
        except ValueError:
            continue

        semi = close_paren + 1
        while semi < len(text) and text[semi].isspace():
            semi += 1
        if semi >= len(text) or text[semi] != ";":
            continue

        stmt_end = semi + 1
        original = text[m.start():stmt_end]
        args_text = text[open_paren + 1:close_paren]
        args = split_top_level_args(args_text)
        replacement = build_replacement(target, method, args, mod_tag, category)

        rewrites.append(
            Rewrite(
                start=m.start(),
                end=stmt_end,
                original=original,
                replacement=replacement,
                line=index_to_line(text, m.start()),
            )
        )

    return rewrites


def apply_rewrites(text: str, rewrites: Sequence[Rewrite]) -> str:
    if not rewrites:
        return text
    out: List[str] = []
    cursor = 0
    for rw in sorted(rewrites, key=lambda r: r.start):
        out.append(text[cursor:rw.start])
        out.append(rw.replacement)
        cursor = rw.end
    out.append(text[cursor:])
    return "".join(out)


def iter_cs_files(root: pathlib.Path, mods: Iterable[str]) -> Iterable[Tuple[str, pathlib.Path]]:
    for mod in mods:
        mod_dir = root / mod
        if not mod_dir.exists():
            continue
        for file in mod_dir.rglob("*.cs"):
            yield mod, file


def main(argv: Sequence[str]) -> int:
    ns = parse_args(argv)
    root = pathlib.Path(ns.root).resolve()

    if ns.emit_helper:
        helper_path = root / HELPER_REL_PATH
        helper_path.parent.mkdir(parents=True, exist_ok=True)
        helper_path.write_text(HELPER_TEMPLATE, encoding="utf-8")
        print(f"[INFO] wrote helper: {helper_path}")

    total_files = 0
    changed_files = 0
    total_calls = 0

    for mod_root, file in iter_cs_files(root, ns.mods):
        total_files += 1
        rel_path = file.relative_to(root / mod_root)
        category = infer_category(mod_root, rel_path)
        mod_tag = MOD_TAGS[mod_root]

        text = file.read_text(encoding="utf-8")
        rewrites = find_rewrites(text, mod_tag, category)
        if not rewrites:
            continue

        changed_files += 1
        total_calls += len(rewrites)

        if ns.verbose:
            print(f"\n{file}")
            for rw in rewrites:
                print(f"  L{rw.line}: {rw.original.strip()} -> {rw.replacement.strip()}")

        if ns.apply:
            new_text = apply_rewrites(text, rewrites)
            if new_text != text:
                file.write_text(new_text, encoding="utf-8")

    mode = "APPLY" if ns.apply else "DRY-RUN"
    print(f"[{mode}] scanned={total_files} changed_files={changed_files} rewritten_calls={total_calls}")
    if not ns.apply:
        print("Run with --apply to write files.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
