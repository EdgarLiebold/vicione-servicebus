#!/usr/bin/env python3
"""Hostile contract tests for the complete deliverable freeze manifest."""

from __future__ import annotations

import copy
import unittest

from freeze_manifest import derived_counts, validate_manifest


class FreezeManifestContractTests(unittest.TestCase):
    @staticmethod
    def clean_manifest() -> dict[str, object]:
        files: list[dict[str, object]] = [
            {
                "gitStatus": "UNCHANGED",
                "baselineKey": "baseline-key-unchanged",
                "targetPath": "baseline-unchanged.txt",
            },
            {
                "gitStatus": "M",
                "baselineKey": "baseline-key-modified",
                "targetPath": "baseline-modified.txt",
            },
            {
                "gitStatus": "A",
                "baselineKey": None,
                "targetPath": "new-deliverable.txt",
            },
        ]
        return {
            "manifestIntegrityStatus": "PASS",
            "overallStatus": "INCOMPLETE_NOT_GREEN",
            "gateSummary": {"overallStatus": "INCOMPLETE_NOT_GREEN"},
            "counts": derived_counts(files),
            "files": files,
        }

    def validate(self, manifest: dict[str, object]) -> list[str]:
        return validate_manifest(
            manifest,
            expected_baseline_keys={"baseline-key-unchanged", "baseline-key-modified"},
            expected_new_paths={"new-deliverable.txt"},
        )

    def test_accepts_complete_manifest(self) -> None:
        self.assertEqual([], self.validate(self.clean_manifest()))

    def test_rejects_testagent_scratch_inclusion(self) -> None:
        manifest = self.clean_manifest()
        files = manifest["files"]
        self.assertIsInstance(files, list)
        files.append(
            {
                "gitStatus": "A",
                "baselineKey": None,
                "targetPath": ".testagent/binlogs/build.binlog",
            }
        )
        manifest["counts"] = derived_counts(files)

        findings = validate_manifest(
            manifest,
            expected_baseline_keys={"baseline-key-unchanged", "baseline-key-modified"},
            expected_new_paths={"new-deliverable.txt", ".testagent/binlogs/build.binlog"},
        )

        self.assertIn("scratch .testagent path included in deliverable", findings)

    def test_rejects_missing_unchanged_baseline_key(self) -> None:
        manifest = self.clean_manifest()
        files = manifest["files"]
        self.assertIsInstance(files, list)
        manifest["files"] = [
            item for item in files if item["gitStatus"] != "UNCHANGED"
        ]
        manifest["counts"] = derived_counts(manifest["files"])

        findings = self.validate(manifest)

        self.assertTrue(any("baseline key set mismatch" in item for item in findings))

    def test_rejects_null_baseline_key_for_modified_record(self) -> None:
        manifest = self.clean_manifest()
        files = manifest["files"]
        self.assertIsInstance(files, list)
        modified = next(item for item in files if item["gitStatus"] == "M")
        modified["baselineKey"] = None
        manifest["counts"] = derived_counts(files)

        findings = self.validate(manifest)

        self.assertIn("M record must have non-null baselineKey", findings)

    def test_rejects_ambiguous_pass_beside_incomplete_overall_status(self) -> None:
        manifest = self.clean_manifest()
        manifest["status"] = "PASS"

        findings = self.validate(manifest)

        self.assertIn("ambiguous generic status is forbidden in freeze manifest", findings)

    def test_rejects_count_drift(self) -> None:
        manifest = copy.deepcopy(self.clean_manifest())
        counts = manifest["counts"]
        self.assertIsInstance(counts, dict)
        counts["baselineChanged"] = 999

        findings = self.validate(manifest)

        self.assertIn("manifest count drift from file records", findings)


if __name__ == "__main__":
    unittest.main()
