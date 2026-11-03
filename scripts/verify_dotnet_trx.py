#!/usr/bin/env python3
"""Fail-closed validation of real hosted VSTest TRX results. Does not run dotnet."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


class TrxError(ValueError):
    """Absent or insufficient test evidence."""


def _local(tag: str) -> str:
    return tag.rsplit('}', 1)[-1]


def inspect_trx(path: Path, min_executed: int = 1) -> dict[str, int | str]:
    if min_executed < 1:
        raise TrxError('min-executed must be at least one')
    if not path.is_file() or path.is_symlink():
        raise TrxError(f'TRX missing or unsafe: {path}')
    if path.stat().st_size < 32:
        raise TrxError(f'TRX empty or truncated: {path}')
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError) as exc:
        raise TrxError(f'TRX could not be parsed: {path}: {exc}') from exc
    if _local(root.tag) != 'TestRun':
        raise TrxError(f'TRX is not a VSTest TestRun: {path}')
    summaries = [x for x in root if _local(x.tag) == 'ResultSummary']
    if len(summaries) != 1:
        raise TrxError('TRX must contain exactly one ResultSummary')
    outcome = summaries[0].attrib.get('outcome', '').strip()
    if outcome not in ('Completed', 'Passed'):
        raise TrxError(f'Unexpected result summary outcome: {outcome or "missing"}')
    counters = [x for x in summaries[0] if _local(x.tag) == 'Counters']
    if len(counters) != 1:
        raise TrxError('TRX counters missing or duplicated')
    counts: dict[str, int] = {}
    for key in ('total', 'executed', 'passed', 'failed', 'error', 'timeout',
                'aborted', 'inconclusive', 'notExecuted'):
        try:
            counts[key] = int(counters[0].attrib[key])
        except (KeyError, ValueError) as exc:
            raise TrxError(f'Invalid or missing TRX counter: {key}') from exc
        if counts[key] < 0:
            raise TrxError(f'Negative TRX counter: {key}')
    if counts['executed'] < min_executed:
        raise TrxError(f'Insufficient executed tests: {counts["executed"]} < {min_executed}')
    if counts['total'] < counts['executed']:
        raise TrxError('TRX total cannot be smaller than executed')
    if counts['passed'] != counts['executed']:
        raise TrxError(f'Not every executed test passed: {counts}')
    for key in ('failed', 'error', 'timeout', 'aborted', 'inconclusive', 'notExecuted'):
        if counts[key] != 0:
            raise TrxError(f'TRX contains {key}={counts[key]}')
    results = [node for node in root.iter() if _local(node.tag) == 'UnitTestResult']
    if len(results) != counts['executed']:
        raise TrxError(f'TRX result rows ({len(results)}) differ from executed ({counts["executed"]})')
    if any(node.attrib.get('outcome') != 'Passed' for node in results):
        raise TrxError('TRX contains a non-passing UnitTestResult')
    return {'file': path.name, **counts}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--file', type=Path, required=True)
    parser.add_argument('--min-executed', type=int, default=1)
    args = parser.parse_args(argv)
    try:
        summary = inspect_trx(args.file, args.min_executed)
    except TrxError as exc:
        parser.exit(1, f'R05_TRX_FAIL: {exc}\n')
    print(json.dumps({'status': 'PASS', **summary}, sort_keys=True))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
