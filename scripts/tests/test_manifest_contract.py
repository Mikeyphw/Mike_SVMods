"""Negative fixture regressions for SMAPI source-only manifest validation."""
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from validate_mod_manifests import MODS, validate


class ManifestTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        for mod in MODS:
            directory = self.root / mod
            directory.mkdir()
            (directory / f"{mod}.csproj").write_text(
                '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                '<TargetFramework>net6.0</TargetFramework></PropertyGroup></Project>',
                encoding="utf-8",
            )
            obj = {"Name": mod, "Author": "Test", "Description": "Test fixture",
                   "Version": "1.0.0", "UniqueID": f"Test.{mod}",
                   "EntryDll": f"{mod}.dll", "MinimumApiVersion": "4.0.0",
                   "UpdateKeys": [], "Dependencies": []}
            (directory / "manifest.json").write_text(json.dumps(obj), encoding="utf-8")

    def edit(self, mod, **fields):
        path = self.root / mod / "manifest.json"
        obj = json.loads(path.read_text(encoding="utf-8"))
        obj.update(fields)
        path.write_text(json.dumps(obj), encoding="utf-8")

    def assert_error(self, needle):
        problems = validate(self.root)
        self.assertTrue(any(needle in x for x in problems), problems)

    def test_valid(self):
        self.assertEqual(validate(self.root), [])

    def test_duplicate_id(self):
        self.edit("LetMeHatch", UniqueID="test.nestcore")
        self.assert_error("duplicate UniqueID")

    def test_stale_internal_id(self):
        self.edit("RoosterControl", Dependencies=[{"UniqueID": "Mikew.letmehatch.smapi", "IsRequired": False}])
        self.assert_error("stale dependency")

    def test_old_nexus_key(self):
        self.edit("RoosterControl", UpdateKeys=["22555"])
        self.assert_error("invalid UpdateKeys")

    def test_unsafe_entry_dll(self):
        self.edit("LetMeHatch", EntryDll="../LetMeHatch.dll")
        self.assert_error("unsafe EntryDll")

    def test_wrong_entry_dll(self):
        self.edit("EggsInWild", EntryDll="other.dll")
        self.assert_error("differs from project")

    def test_missing_manifest(self):
        (self.root / "NestCore" / "manifest.json").unlink()
        self.assert_error("missing or non-regular")

    def test_invalid_json(self):
        (self.root / "NestCore" / "manifest.json").write_text("{", encoding="utf-8")
        self.assert_error("invalid JSON")

    def test_internal_dependency_api_floor(self):
        self.edit("NestCore", MinimumApiVersion="4.1.0")
        self.edit("EggsInWild", Dependencies=[{"UniqueID": "Test.NestCore", "IsRequired": True}])
        self.assert_error("MinimumApiVersion lower")

    def test_external_optional_dependency(self):
        self.edit("LetMeHatch", Dependencies=[{"UniqueID": "External.Mod", "IsRequired": False}])
        self.assertEqual(validate(self.root), [])


if __name__ == "__main__":
    unittest.main()
