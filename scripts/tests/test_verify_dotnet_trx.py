"""R05a: source-only tests for TRX failure/zero-discovery receipts."""
import copy
from pathlib import Path
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_dotnet_trx import TrxError, inspect_trx


NS = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'


def trx_bytes(*, executed=2, passed=2, total=2, failed=0, not_executed=0,
              error=0, inconclusive=0, outcome='Completed', result_outcome='Passed'):
    ET.register_namespace('', NS)
    root = ET.Element(f'{{{NS}}}TestRun')
    result = ET.SubElement(root, f'{{{NS}}}Results')
    for i in range(executed):
        ET.SubElement(result, f'{{{NS}}}UnitTestResult', {'testName': str(i), 'outcome': result_outcome})
    summary = ET.SubElement(root, f'{{{NS}}}ResultSummary', {'outcome': outcome})
    ET.SubElement(summary, f'{{{NS}}}Counters', {
        'total': str(total), 'executed': str(executed), 'passed': str(passed),
        'failed': str(failed), 'error': str(error), 'timeout': '0',
        'aborted': '0', 'inconclusive': str(inconclusive), 'notExecuted': str(not_executed)
    })
    return ET.tostring(root, encoding='utf-8', xml_declaration=True)


class TrxTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.file = Path(self._tmp.name, 'result.trx')

    def write(self, data):
        self.file.write_bytes(data)
        return self.file

    def assert_rejected(self, **kwargs):
        with self.assertRaises(TrxError):
            inspect_trx(self.write(trx_bytes(**kwargs)))

    def test_valid_namespace_and_counts(self):
        result = inspect_trx(self.write(trx_bytes()), min_executed=1)
        self.assertEqual(result['passed'], 2)

    def test_missing_file_rejected(self):
        with self.assertRaisesRegex(TrxError, 'missing'):
            inspect_trx(self.file)

    def test_empty_rejected(self):
        with self.assertRaises(TrxError):
            inspect_trx(self.write(b''))

    def test_malformed_rejected(self):
        with self.assertRaises(TrxError):
            inspect_trx(self.write(b'<TestRun>' * 40))

    def test_zero_tests_rejected(self):
        self.assert_rejected(executed=0, passed=0, total=0)

    def test_min_count_enforced(self):
        with self.assertRaises(TrxError):
            inspect_trx(self.write(trx_bytes()), min_executed=3)

    def test_failed_rejected(self):
        self.assert_rejected(failed=1, passed=1)

    def test_skipped_rejected(self):
        self.assert_rejected(total=3, not_executed=1)

    def test_inconclusive_rejected(self):
        self.assert_rejected(inconclusive=1, passed=1)

    def test_bad_summary_rejected(self):
        self.assert_rejected(outcome='Failed')

    def test_per_row_failure_rejected(self):
        self.assert_rejected(result_outcome='Failed')

    def test_missing_counters_rejected(self):
        root = ET.fromstring(trx_bytes())
        summary = next(x for x in root if x.tag.endswith('ResultSummary'))
        summary.clear()
        self.write(ET.tostring(root))
        with self.assertRaises(TrxError):
            inspect_trx(self.file)

    def test_total_less_than_executed_rejected(self):
        self.assert_rejected(total=1)

    def test_symlink_rejected(self):
        other = Path(self._tmp.name, 'other.trx')
        self.write(trx_bytes())
        other.symlink_to(self.file)
        with self.assertRaises(TrxError):
            inspect_trx(other)


if __name__ == '__main__':
    unittest.main()
