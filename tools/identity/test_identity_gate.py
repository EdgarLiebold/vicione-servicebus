#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Hostile-fixture and test-accounting tests for the identity gate."""

from __future__ import annotations

import unittest
import subprocess
import tempfile
from pathlib import Path

from identity_gate import (
    legal_identity_contexts,
    omitted_baseline_findings,
    scan_entry,
    scan_tree,
    test_sabotage_findings,
    validate_evidence_records,
    validate_format_exceptions,
    validate_test_run,
)
from identity_rules import FORMER_PASCAL


OLD = FORMER_PASCAL


class IdentityGateHostileFixtureTests(unittest.TestCase):
    def assert_rejected(self, path: str, content: str | bytes) -> None:
        data = content if isinstance(content, bytes) else content.encode("utf-8")
        self.assertTrue(scan_entry(path, data), f"fixture unexpectedly passed: {path}")

    def test_rejects_old_namespace(self) -> None:
        self.assert_rejected("src/Example.cs", f"namespace {OLD}.Example;")

    def test_rejects_old_package_name(self) -> None:
        self.assert_rejected("src/Example.csproj", f"<PackageId>{OLD}.Example</PackageId>")

    def test_rejects_old_path(self) -> None:
        self.assert_rejected(f"src/{OLD}.Example/Example.cs", "namespace Example;")

    def test_rejects_old_wire_header(self) -> None:
        self.assert_rejected("src/Header.cs", "M" + "T-Host-Info")

    def test_rejects_the_superseded_wire_header(self) -> None:
        # Never released, so it is not a compatibility alias. It is a second forbidden prefix.
        self.assert_rejected("src/Header.cs", "ViciOne" + "-ServiceBus-Host-Info")

    def test_rejects_either_old_wire_header_in_mixed_case(self) -> None:
        self.assert_rejected("src/Header.cs", "m" + "T-hOsT-iNfO")
        self.assert_rejected("src/Header.cs", "vIcIoNe" + "-sErViCeBuS-hOsT-iNfO")

    def test_rejects_every_historic_root_under_both_old_prefixes(self) -> None:
        # Root removal must be visible as a detector that stops detecting, not only as a changed digest.
        for root in ("Host-Info", "Fault-Message", "Redelivery-Count", "Scheduling-TokenId", "Request-ClientId"):
            with self.subTest(root=root):
                self.assert_rejected("src/Header.cs", "M" + "T-" + root)
                self.assert_rejected("src/Header.cs", "ViciOne" + "-ServiceBus-" + root)

    def test_accepts_the_active_wire_header(self) -> None:
        data = b"public const string Info = \"VSB-Host-Info\";"
        self.assertFalse(scan_entry("src/Header.cs", data), "the active prefix must pass")

    def test_accepts_words_that_only_happen_to_contain_the_letters(self) -> None:
        # A detector that fires on "amount", "format" or "vsbuild" would be unusable.
        for word in (b"amount", b"format", b"empty", b"vsbuild", b"observable"):
            self.assertFalse(scan_entry("src/Words.cs", word), f"harmless word rejected: {word!r}")

    def test_rejects_old_activity_source(self) -> None:
        self.assert_rejected("src/Telemetry.cs", f'new ActivitySource("{OLD}")')

    def test_rejects_type_forwarder(self) -> None:
        self.assert_rejected("src/Forwarder.cs", "[assembly: Type" + "ForwardedTo(typeof(X))]")

    def test_rejects_alias_assembly(self) -> None:
        self.assert_rejected("src/Alias.cs", f"{'extern' + ' alias'} {OLD};")

    def test_rejects_hidden_binary_string(self) -> None:
        self.assert_rejected("asset.bin", b"\xff\x00" + OLD.encode("ascii") + b"\x00")

    def test_rejects_omitted_baseline_path(self) -> None:
        findings = omitted_baseline_findings({"src/A.cs", "src/B.cs"}, {"src/A.cs"})
        self.assertEqual("src/B.cs", findings[0].path)

    def test_invented_legal_exception_is_not_allowlisted(self) -> None:
        self.assert_rejected("src/NOTICE-copy.txt", f"derived from {OLD}")

    def assert_git_candidate_rejected(self, path: str, tracked: bool) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            candidate = root / path
            candidate.parent.mkdir(parents=True, exist_ok=True)
            candidate.write_text(f"forbidden={OLD}\n", encoding="utf-8")
            if tracked:
                subprocess.run(["git", "add", path], cwd=root, check=True)

            findings = scan_tree(root)

        self.assertTrue(any(item.path == path and item.gate == "text-scan" for item in findings))

    def test_rejects_former_identity_in_new_tool_file(self) -> None:
        self.assert_git_candidate_rejected("tools/identity/new_probe.txt", tracked=False)

    def test_rejects_former_identity_in_new_evidence_file(self) -> None:
        self.assert_git_candidate_rejected("evidence/WP-F2-SERVICEBUS-IDENTITY/new.json", tracked=False)

    def test_rejects_former_identity_in_existing_tool_file(self) -> None:
        self.assert_git_candidate_rejected("tools/identity/existing.py", tracked=True)

    def test_rejects_former_identity_in_existing_evidence_record(self) -> None:
        self.assert_git_candidate_rejected("evidence/WP-F2-SERVICEBUS-IDENTITY/existing.json", tracked=True)

    def assert_extra_legal_identity_rejected(self, path: str) -> None:
        authorized = "\n".join(legal_identity_contexts()[path])
        findings = scan_entry(path, f"{authorized}\nextra={OLD}\n".encode("utf-8"))
        self.assertTrue(any(item.gate in {"text-scan", "binary-scan"} for item in findings))

    def test_rejects_extra_former_identity_in_readme(self) -> None:
        self.assert_extra_legal_identity_rejected("README.md")

    def test_rejects_extra_former_identity_in_notice(self) -> None:
        self.assert_extra_legal_identity_rejected("NOTICE")

    def test_rejects_extra_former_identity_in_copyright(self) -> None:
        self.assert_extra_legal_identity_rejected("COPYRIGHT")

    def test_rejects_extra_former_identity_in_modifications(self) -> None:
        self.assert_extra_legal_identity_rejected("MODIFICATIONS.md")

    def test_rejects_extra_former_identity_in_license(self) -> None:
        self.assert_extra_legal_identity_rejected("LICENSE")

    def test_rejects_unchanged_commentable_format_exception(self) -> None:
        root = Path(__file__).resolve().parents[2]
        findings = validate_format_exceptions(root, {".devcontainer/devcontainer.json"})
        self.assertTrue(findings)
        self.assertTrue(all(item.gate == "legal-format-exception" for item in findings))

    def test_rejects_commentable_readme_format_exception(self) -> None:
        root = Path(__file__).resolve().parents[2]
        findings = validate_format_exceptions(root, {"README.md"})
        self.assertTrue(findings)
        self.assertTrue(all(item.gate == "legal-format-exception" for item in findings))

    def test_rejects_missing_baseline_evidence_record(self) -> None:
        expected = [{"baselineKey": "key-a", "targetPath": "a"}]
        findings = validate_evidence_records("mapping.json", [], expected, "baselineKey")
        self.assertTrue(any(item.gate == "persisted-evidence" for item in findings))

    def test_rejects_incorrect_baseline_evidence_binding(self) -> None:
        expected = [{"baselineKey": "key-a", "targetPath": "a"}]
        actual = [{"baselineKey": "key-a", "targetPath": "wrong"}]
        findings = validate_evidence_records("mapping.json", actual, expected, "baselineKey")
        self.assertTrue(any("incorrect binding" in item.reason for item in findings))

    def test_rejects_missing_api_evidence_line(self) -> None:
        expected = [{"apiBindingSha256": "api-a", "baselineLine": 10}]
        findings = validate_evidence_records("api.json", [], expected, "apiBindingSha256")
        self.assertTrue(any(item.gate == "persisted-evidence" for item in findings))

    def test_rejects_incorrect_api_evidence_line_binding(self) -> None:
        expected = [{"apiBindingSha256": "api-a", "baselineLine": 10}]
        actual = [{"apiBindingSha256": "api-a", "baselineLine": 11}]
        findings = validate_evidence_records("api.json", actual, expected, "apiBindingSha256")
        self.assertTrue(any("incorrect binding" in item.reason for item in findings))

    def test_rejects_test_sabotage(self) -> None:
        findings = test_sabotage_findings("[Test] void Runs() {}", "[Ignore] void Runs() {}", "T.cs")
        self.assertTrue(findings)

    def test_rejects_null_test_run(self) -> None:
        self.assertTrue(validate_test_run(discovered=0, executed=0, failed=0, skipped=0))

    def test_accepts_accounted_green_test_run(self) -> None:
        self.assertFalse(validate_test_run(discovered=10, executed=9, failed=0, skipped=1))


if __name__ == "__main__":
    unittest.main()
