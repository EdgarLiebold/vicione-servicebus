#!/usr/bin/env python3
"""Hostile-fixture and test-accounting tests for the identity gate."""

from __future__ import annotations

import contextlib
import io
import json
import unittest
import subprocess
import tempfile
import hashlib
from pathlib import Path
from unittest.mock import patch

from identity_gate import (
    GENERATED_EVIDENCE_CONTRACTS,
    GENERATED_EVIDENCE_MANIFEST,
    generated_evidence_manifest,
    candidate_git_entry,
    candidate_git_entries,
    commit_tree,
    csharp_public_declarations,
    derive_baseline_mapping,
    derive_package_inventory,
    derive_public_api_mapping,
    legal_identity_contexts,
    map_text,
    modification_format_exception_bindings,
    notice_format_exception_targets,
    omitted_baseline_findings,
    scan_entry,
    scan_tree,
    test_sabotage_findings,
    public_declaration_disposition,
    run_gate,
    validate_historical_identity_policy,
    validate_current_public_api_records,
    validate_terminal_baseline_records,
    validate_evidence_records,
    validate_format_exceptions,
    validate_generated_evidence_manifest,
    validate_legal_documents,
    validate_test_run,
)
from identity_rules import (
    COMMENTLESS_OR_BINARY_BASELINE_SOURCES,
    COMMENTLESS_OR_BINARY_EXCEPTIONS,
    FORMER_PASCAL,
)


OLD = FORMER_PASCAL
EXPECTED_FORMAT_EXCEPTION_BINDINGS = {
    "ViciOne.ServiceBus.slnx": "MassTransit.sln",
    "ViciOne.ServiceBus.snk": "MassTransit.snk",
    "tests/Transports/ViciOne.ServiceBus.EventHubIntegration.Tests/config.json":
        "tests/MassTransit.EventHubIntegration.Tests/config.json",
    "tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/client.p12":
        "tests/MassTransit.RabbitMqTransport.Tests/client.p12",
}


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

    # The complete root contract, written out here and not read from the product. A test that iterates the
    # product list shrinks with it: delete a root and the loop simply runs one case fewer, still green. This
    # list is the independent side of the comparison, so any removal or rename shows up as a difference.
    EXPECTED_HEADER_ROOTS = (
        "activity", "fail", "fault", "forwarder", "hangfire", "host", "initiating", "initiator", "jobid",
        "message", "original", "quartz", "reason", "redelivery", "request", "response", "routing", "scheduling",
        "server", "source",
    )

    def test_the_product_detector_carries_exactly_the_expected_roots(self) -> None:
        from identity_rules import _HEADER_ROOTS

        self.assertEqual(sorted(self.EXPECTED_HEADER_ROOTS), sorted(_HEADER_ROOTS),
                         "a root was added, removed or renamed in the product detector")

    def test_rejects_every_historic_root_under_both_old_prefixes(self) -> None:
        # Root removal must be visible as a detector that stops detecting, not only as a changed digest.
        for root in self.EXPECTED_HEADER_ROOTS:
            with self.subTest(root=root):
                self.assert_rejected("src/Header.cs", "M" + "T-" + root + "-Info")
                self.assert_rejected("src/Header.cs", "ViciOne" + "-ServiceBus-" + root + "-Info")

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

    def test_scan_uses_the_symlink_blob_without_dereferencing_an_ignored_target(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            (root / ".gitignore").write_text("ignored/\n", encoding="utf-8")
            ignored = root / "ignored/Outside.cs"
            ignored.parent.mkdir(parents=True)
            ignored.write_text(f"namespace {OLD}.Hidden;\n", encoding="utf-8")
            source = root / "src/Linked.cs"
            source.parent.mkdir(parents=True)
            source.symlink_to(ignored)
            subprocess.run(["git", "add", ".gitignore", "src/Linked.cs"], cwd=root, check=True)

            findings = scan_tree(root)

        self.assertFalse(any(item.path == "src/Linked.cs" for item in findings))

    def test_historical_policy_must_be_a_regular_git_candidate(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            (root / ".gitignore").write_text("ignored/\n", encoding="utf-8")
            ignored = root / "ignored/policy.json"
            ignored.parent.mkdir(parents=True)
            ignored.write_text('{"schemaVersion": 1, "entries": []}\n', encoding="utf-8")
            policy = root / "tools/identity/historical_identity_policy.json"
            policy.parent.mkdir(parents=True)
            policy.symlink_to(ignored)
            subprocess.run(
                ["git", "add", ".gitignore", "tools/identity/historical_identity_policy.json"],
                cwd=root,
                check=True,
            )

            findings = scan_tree(root)

        self.assertTrue(any(
            finding.gate == "historical-identity-policy"
            and "regular Git candidate" in finding.reason
            for finding in findings
        ))

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
        self.assert_extra_legal_identity_rejected("LICENSE.txt")

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

    def test_ignored_legal_exception_file_cannot_supply_a_git_binding(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            target = "ViciOne.ServiceBus.snk"
            (root / ".gitignore").write_text(target + "\n", encoding="utf-8")
            (root / target).write_bytes(b"ignored ghost")
            subprocess.run(["git", "add", ".gitignore"], cwd=root, check=True)
            with patch("identity_gate.baseline_paths", return_value=[]):
                findings = validate_format_exceptions(root, {target})

        self.assertTrue(any("no baseline-to-target file binding" in item.reason for item in findings))

    def test_legal_authority_documents_must_be_regular_git_candidates(self) -> None:
        for authority_path in ("NOTICE", "MODIFICATIONS.md", "LICENSE.txt"):
            with self.subTest(path=authority_path), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                subprocess.run(["git", "init", "-q"], cwd=root, check=True)
                (root / ".gitignore").write_text("ignored/\n", encoding="utf-8")
                ignored = root / "ignored/authority"
                ignored.parent.mkdir(parents=True)
                ignored.write_bytes(b"license" if authority_path == "LICENSE.txt" else b"outside authority\n")
                for path, data in (
                    ("NOTICE", b"\n"),
                    ("MODIFICATIONS.md", b"\n"),
                    ("LICENSE.txt", b"license"),
                ):
                    candidate = root / path
                    if path == authority_path:
                        candidate.symlink_to(ignored)
                    else:
                        candidate.write_bytes(data)
                subprocess.run(
                    ["git", "add", ".gitignore", "NOTICE", "MODIFICATIONS.md", "LICENSE.txt"],
                    cwd=root,
                    check=True,
                )
                with contextlib.ExitStack() as stack:
                    stack.enter_context(patch("identity_gate.validate_format_exceptions", return_value=[]))
                    stack.enter_context(patch("identity_gate.baseline_bytes", return_value=b"license"))
                    findings = validate_legal_documents(root)

            self.assertTrue(any(
                finding.gate == "legal"
                and finding.path == authority_path
                and "regular Git candidate" in finding.reason
                for finding in findings
            ))

    def test_active_legal_documents_and_format_exceptions_are_consistent(self) -> None:
        root = Path(__file__).resolve().parents[2]
        self.assertEqual([], validate_legal_documents(root))

    def test_format_exception_map_matches_the_independent_exact_contract(self) -> None:
        self.assertEqual(EXPECTED_FORMAT_EXCEPTION_BINDINGS, COMMENTLESS_OR_BINARY_BASELINE_SOURCES)
        self.assertEqual(set(EXPECTED_FORMAT_EXCEPTION_BINDINGS), set(COMMENTLESS_OR_BINARY_EXCEPTIONS))
        self.assertEqual(
            len(EXPECTED_FORMAT_EXCEPTION_BINDINGS.values()),
            len(set(EXPECTED_FORMAT_EXCEPTION_BINDINGS.values())),
        )

    def test_notice_parser_exposes_an_additional_stale_exception(self) -> None:
        root = Path(__file__).resolve().parents[2]
        notice = (root / "NOTICE").read_text(encoding="utf-8") + "stale/deleted.txt\n"
        self.assertNotEqual(
            sorted(EXPECTED_FORMAT_EXCEPTION_BINDINGS),
            notice_format_exception_targets(notice),
        )

    def test_modifications_parser_exposes_an_incorrect_baseline_source(self) -> None:
        root = Path(__file__).resolve().parents[2]
        modifications = (root / "MODIFICATIONS.md").read_text(encoding="utf-8")
        modifications = modifications.replace(
            "- `MassTransit.sln` -> `ViciOne.ServiceBus.slnx`",
            "- `MassTransit.snk` -> `ViciOne.ServiceBus.slnx`",
            1,
        )
        expected = [
            (source, target)
            for target, source in sorted(EXPECTED_FORMAT_EXCEPTION_BINDINGS.items())
        ]
        self.assertNotEqual(expected, modification_format_exception_bindings(modifications))

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


class IdentityGateTerminalDispositionTests(unittest.TestCase):
    EXISTING = {
        "key-a": {
            "baselineKey": "key-a",
            "targetPath": "src/A.cs",
            "baselineDisposition": "MAPPED_EXISTING",
            "pathDisposition": "UNCHANGED",
            "targetExists": True,
            "targetGitMode": "100644",
            "targetGitBlobOid": "1" * 40,
        },
        "key-b": {
            "baselineKey": "key-b",
            "targetPath": "src/B.cs",
            "baselineDisposition": "MOVED_EXACT",
            "pathDisposition": "RENAMED",
            "targetExists": True,
            "targetGitMode": "100644",
            "targetGitBlobOid": "2" * 40,
        },
        "key-c": {
            "baselineKey": "key-c",
            "targetPath": "removed/C.cs",
            "baselineDisposition": "RETIRED_DELETED",
            "targetExists": False,
            "retirementCommit": "c" * 40,
            "retirementEvidence": "ROOT_CHANGE_LIST",
            "retirementTree": "d" * 40,
        },
    }

    def findings(
        self,
        records: list[dict[str, object]],
        entries: dict[str, tuple[str, str]] | None = None,
    ):
        return validate_terminal_baseline_records(
            records,
            expected_keys={"key-a", "key-b", "key-c"},
            actual_tree_entries=entries if entries is not None else {
                "src/A.cs": ("100644", "1" * 40),
                "src/B.cs": ("100644", "2" * 40),
            },
        )

    def test_accepts_one_exact_terminal_state_per_baseline_key(self) -> None:
        self.assertEqual([], self.findings(list(self.EXISTING.values())))

    def test_rejects_missing_duplicate_and_invented_baseline_keys(self) -> None:
        variants = (
            list(self.EXISTING.values())[:-1],
            list(self.EXISTING.values()) + [dict(self.EXISTING["key-a"])],
            list(self.EXISTING.values()) + [{
                "baselineKey": "invented",
                "targetPath": "invented.cs",
                "baselineDisposition": "MAPPED_EXISTING",
                "targetExists": True,
            }],
        )
        for records in variants:
            with self.subTest(records=len(records)):
                self.assertTrue(self.findings(records))

    def test_rejects_resurrected_retired_and_missing_live_targets(self) -> None:
        self.assertTrue(self.findings(list(self.EXISTING.values()), {
            "src/A.cs": ("100644", "1" * 40),
            "src/B.cs": ("100644", "2" * 40),
            "removed/C.cs": ("100644", "3" * 40),
        }))
        self.assertTrue(self.findings(list(self.EXISTING.values()), {
            "src/A.cs": ("100644", "1" * 40),
        }))

    def test_rejects_retirement_without_git_and_change_list_provenance(self) -> None:
        for field in ("retirementCommit", "retirementEvidence", "retirementTree"):
            records = [dict(item) for item in self.EXISTING.values()]
            records[-1].pop(field)
            with self.subTest(field=field):
                findings = self.findings(records)
                self.assertTrue(any("retirement provenance" in item.reason for item in findings))

    def test_rejects_coordinated_live_disposition_drift_and_invented_retirement_metadata(self) -> None:
        wrong_path_state = [dict(item) for item in self.EXISTING.values()]
        wrong_path_state[0]["baselineDisposition"] = "MOVED_EXACT"
        self.assertTrue(any("path disposition" in item.reason for item in self.findings(wrong_path_state)))

        invented_retirement = [dict(item) for item in self.EXISTING.values()]
        invented_retirement[0]["retirementCommit"] = "e" * 40
        self.assertTrue(any("carries retirement" in item.reason for item in self.findings(invented_retirement)))

    def test_rejects_missing_stale_or_invented_live_git_identity(self) -> None:
        for field in ("targetGitMode", "targetGitBlobOid"):
            records = [dict(item) for item in self.EXISTING.values()]
            records[0].pop(field)
            with self.subTest(field=field):
                self.assertTrue(self.findings(records))

        wrong_mode = {
            "src/A.cs": ("100755", "1" * 40),
            "src/B.cs": ("100644", "2" * 40),
        }
        self.assertTrue(any("Git mode is stale" in item.reason for item in self.findings(
            list(self.EXISTING.values()), wrong_mode
        )))

        wrong_blob = {
            "src/A.cs": ("100644", "3" * 40),
            "src/B.cs": ("100644", "2" * 40),
        }
        self.assertTrue(any("Git blob oid is stale" in item.reason for item in self.findings(
            list(self.EXISTING.values()), wrong_blob
        )))

    def test_candidate_git_entry_distinguishes_regular_executable_and_symlink_modes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            regular = root / "regular.txt"
            regular.write_text("payload\n", encoding="utf-8")
            regular.chmod(0o644)
            normal_mode, normal_oid, _ = candidate_git_entry(root, regular)
            regular.chmod(0o755)
            executable_mode, executable_oid, _ = candidate_git_entry(root, regular)
            link = root / "link.txt"
            link.symlink_to("regular.txt")
            link_mode, link_oid, link_data = candidate_git_entry(root, link)

        self.assertEqual("100644", normal_mode)
        self.assertEqual("100755", executable_mode)
        self.assertEqual(normal_oid, executable_oid)
        self.assertEqual("120000", link_mode)
        self.assertEqual(b"regular.txt", link_data)
        self.assertNotEqual(normal_oid, link_oid)

    def test_ignored_existing_target_is_not_a_live_git_candidate_or_baseline_target(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            (root / ".gitignore").write_text("retired.cs\n", encoding="utf-8")
            (root / "retired.cs").write_text("public sealed class Resurrected { }\n", encoding="utf-8")
            self.assertNotIn("retired.cs", candidate_git_entries(root))
            binding = {
                "baselineKey": "1" * 64,
                "baselineCommit": "2" * 40,
                "baselinePathSha256": "3" * 64,
                "baselineGitBlobOid": "4" * 40,
                "baselineGitMode": "100644",
            }
            with (
                patch("identity_gate.baseline_paths", return_value=["retired.cs"]),
                patch("identity_gate.baseline_bytes", return_value=b"baseline\n"),
                patch("identity_gate.baseline_binding", return_value=binding),
                patch("identity_gate.change_list_deleted_baseline_paths", return_value={"retired.cs"}),
                patch("identity_gate.deleted_path_commits", return_value={"retired.cs": "a" * 40}),
                patch("identity_gate.commit_tree", return_value="b" * 40),
            ):
                records, findings = derive_baseline_mapping(root)

        self.assertEqual([], findings)
        self.assertEqual("RETIRED_DELETED", records[0]["baselineDisposition"])
        self.assertIs(records[0]["targetExists"], False)
        self.assertNotIn("targetGitBlobOid", records[0])

    def test_retirement_tree_resolves_from_the_deletion_commit_not_current_head(self) -> None:
        deletion_commit = "a" * 40
        deletion_tree = "b" * 40
        with patch("identity_gate.git", return_value=(deletion_tree + "\n").encode("ascii")) as resolve:
            self.assertEqual(deletion_tree, commit_tree(Path("/repository"), deletion_commit))
        resolve.assert_called_once_with(Path("/repository"), "rev-parse", f"{deletion_commit}^{{tree}}")

    def test_retirement_tree_rejects_symbolic_or_malformed_commit_identity(self) -> None:
        for commit in ("HEAD", "a" * 39, "A" * 40):
            with self.subTest(commit=commit), self.assertRaisesRegex(ValueError, "full lowercase"):
                commit_tree(Path("/repository"), commit)


class IdentityGateHistoricalContextTests(unittest.TestCase):
    def setUp(self) -> None:
        self.files = {
            "evidence/history.json": f'{{"former":"{OLD}"}}\n'.encode("utf-8"),
            "tests2/NegativeTests.cs": f'const string Former = "{OLD}";\n'.encode("utf-8"),
            "evidence/history.bin": b"\xff" + OLD.encode("ascii"),
        }
        self.records = [
            {
                "path": path,
                "category": category,
                "authorizedLineCount": 1,
                "authorizedLinesSha256": hashlib.sha256(
                    json.dumps(
                        [(hashlib.sha256(self.files[path]).hexdigest(), 1)],
                        separators=(",", ":"),
                    ).encode("ascii")
                ).hexdigest(),
                "allowedGates": ["binary-scan", "text-scan"],
            }
            for path, category in (
                ("evidence/history.json", "HISTORICAL_EVIDENCE"),
                ("tests2/NegativeTests.cs", "NEGATIVE_TEST_ORACLE"),
            )
        ]
        self.records.append(
            {
                "path": "evidence/history.bin",
                "category": "HISTORICAL_EVIDENCE",
                "blobSha256": hashlib.sha256(self.files["evidence/history.bin"]).hexdigest(),
                "allowedGates": ["binary-scan"],
            }
        )

    def test_accepts_only_exact_path_category_context_and_gate_bindings(self) -> None:
        self.assertEqual([], validate_historical_identity_policy(self.records, self.files))

    def test_unrelated_text_changes_do_not_expand_or_stale_an_authorized_context(self) -> None:
        files = dict(self.files)
        files["evidence/history.json"] += b'{"current":"ViciOne.ServiceBus"}\n'
        self.assertEqual([], validate_historical_identity_policy(self.records, files))

    def test_new_identity_line_is_not_covered_by_an_existing_exact_context(self) -> None:
        files = dict(self.files)
        files["evidence/history.json"] += f'{{"invented":"{OLD}"}}\n'.encode("utf-8")
        findings = validate_historical_identity_policy(self.records, files)
        self.assertTrue(any("inventory is stale" in item.reason for item in findings))

    def test_binary_digest_and_path_authorization_are_independent_fail_closed_axes(self) -> None:
        stale_binary = [dict(record) for record in self.records]
        stale_binary[-1] = dict(stale_binary[-1], blobSha256="0" * 64)
        self.assertTrue(validate_historical_identity_policy(stale_binary, self.files))

        path = f"evidence/{OLD}/history.txt"
        files = {path: b"current identity only\n"}
        record = {
            "path": path,
            "category": "HISTORICAL_EVIDENCE",
            "allowedGates": ["path-scan"],
            "authorizePath": True,
        }
        self.assertEqual([], validate_historical_identity_policy([record], files, {path}))
        self.assertTrue(validate_historical_identity_policy([{key: value for key, value in record.items() if key != "authorizePath"}], files, {path}))

    def test_rejects_duplicate_or_unsorted_gate_authority(self) -> None:
        for allowed_gates in (["text-scan", "binary-scan"], ["binary-scan", "binary-scan", "text-scan"]):
            records = [dict(record) for record in self.records]
            records[0] = dict(records[0], allowedGates=allowed_gates)
            with self.subTest(allowed_gates=allowed_gates):
                self.assertTrue(validate_historical_identity_policy(records, self.files))

    def test_rejects_duplicate_missing_stale_unknown_and_wrong_binding_policy_entries(self) -> None:
        stale = [dict(record) for record in self.records]
        stale[0] = dict(stale[0], authorizedLinesSha256="0" * 64)
        text_blob = [dict(record) for record in self.records]
        text_blob[0] = dict(text_blob[0], blobSha256=hashlib.sha256(self.files["evidence/history.json"]).hexdigest())
        variants = (
            self.records + [dict(self.records[0])],
            self.records[:-1],
            stale,
            [dict(self.records[0], category="ANY_DIRECTORY"), *self.records[1:]],
            text_blob,
        )
        expected_paths = set(self.files)
        for records in variants:
            with self.subTest(records=records):
                self.assertTrue(validate_historical_identity_policy(records, self.files, expected_paths))

    def test_rejects_policy_that_would_suppress_an_active_product_copy(self) -> None:
        product = b"namespace " + OLD.encode("ascii") + b".Leaked;"
        record = {
            "path": "src/Leaked.cs",
            "category": "HISTORICAL_EVIDENCE",
            "authorizedLineCount": 1,
            "authorizedLinesSha256": hashlib.sha256(
                json.dumps(
                    [(hashlib.sha256(product).hexdigest(), 1)],
                    separators=(",", ":"),
                ).encode("ascii")
            ).hexdigest(),
            "allowedGates": ["binary-scan", "text-scan"],
        }
        findings = validate_historical_identity_policy([record], {"src/Leaked.cs": product})
        self.assertTrue(any("category is not allowed for active product" in item.reason for item in findings))


class IdentityGatePublicProjectionTests(unittest.TestCase):
    def test_classifies_present_modified_and_retired_declarations_independently(self) -> None:
        self.assertEqual("MAPPED_PRESENT", public_declaration_disposition(True, True))
        self.assertEqual("MODIFIED_OR_REMOVED", public_declaration_disposition(True, False))
        self.assertEqual("RETIRED_PATH", public_declaration_disposition(False, False))

    def test_rejects_impossible_present_declaration_on_retired_path(self) -> None:
        with self.assertRaisesRegex(ValueError, "retired path"):
            public_declaration_disposition(False, True)

    def test_csharp_projection_covers_multiline_modifiers_and_implicit_public_members(self) -> None:
        source = '''
const string NotApi = "public class Hidden";
// public class AlsoHidden { }
public
sealed class Added<T, U> : BaseType, IFirst, ISecond { }
sealed public class Reordered { }
public int First, Second;
public int Property { get; private set; }
protected internal virtual void Hook() { }
private protected void HiddenProtected() { }
public interface IFoo
{
    void Added();
    string Name { get; set; }
    private void Hidden();
}
public enum State
{
    None,
    Ready = 2,
}
'''
        declarations = {declaration for declaration, _ in csharp_public_declarations(source)}

        self.assertIn(
            "explicit public sealed class Added < T , U > : BaseType , IFirst , ISecond",
            declarations,
        )
        self.assertIn("explicit sealed public class Reordered", declarations)
        self.assertIn("explicit public int First , Second", declarations)
        self.assertIn("explicit public int Property { get ; private set }", declarations)
        self.assertIn("explicit protected internal virtual void Hook ( )", declarations)
        self.assertIn("owner type public interface IFoo implicit-interface void Added ( )", declarations)
        self.assertIn("owner type public interface IFoo implicit-interface string Name { get ; set }", declarations)
        self.assertIn("owner type public enum State implicit-enum None", declarations)
        self.assertIn("owner type public enum State implicit-enum Ready = 2", declarations)
        self.assertFalse(any("AlsoHidden" in declaration for declaration in declarations))
        self.assertFalse(any("NotApi" in declaration for declaration in declarations))
        self.assertFalse(any("private void Hidden" in declaration for declaration in declarations))
        self.assertFalse(any("HiddenProtected" in declaration for declaration in declarations))

    def test_accessor_and_secondary_base_type_changes_have_distinct_public_identities(self) -> None:
        with_secondary_base = csharp_public_declarations(
            "public class Added : BaseType, ISecond { public int Value { get; private set; } }"
        )
        without_secondary_base = csharp_public_declarations(
            "public class Added : BaseType { public int Value { get; set; } }"
        )

        self.assertNotEqual(with_secondary_base, without_secondary_base)
        self.assertIn(
            "explicit public class Added : BaseType , ISecond",
            {declaration for declaration, _ in with_secondary_base},
        )
        self.assertIn(
            "owner type public class Added : BaseType , ISecond explicit public int Value { get ; private set }",
            {declaration for declaration, _ in with_secondary_base},
        )

    def test_conditional_compilation_context_is_part_of_the_declaration_identity(self) -> None:
        unconditional = csharp_public_declarations("public void Added() { }\n")
        conditional = csharp_public_declarations("#if false\npublic void Added() { }\n#endif\n")

        self.assertNotEqual(unconditional, conditional)
        self.assertEqual(
            "conditional if(false) explicit public void Added ( )",
            conditional[0][0],
        )

    def test_owner_namespace_and_declaration_attributes_are_part_of_identity(self) -> None:
        namespace_one = csharp_public_declarations(
            "namespace N1 { [Obsolete] public class Contract { public void Execute() { } } }"
        )
        namespace_two = csharp_public_declarations(
            "namespace N2 { [Obsolete] public class Contract { public void Execute() { } } }"
        )
        no_attribute = csharp_public_declarations(
            "namespace N1 { public class Contract { public void Execute() { } } }"
        )
        moved_member = csharp_public_declarations(
            "namespace N1 { public class Other { public void Execute() { } } }"
        )

        self.assertNotEqual(namespace_one, namespace_two)
        self.assertNotEqual(namespace_one, no_attribute)
        self.assertNotEqual(no_attribute, moved_member)
        self.assertTrue(any("explicit [ Obsolete ] public class Contract" in item[0] for item in namespace_one))
        self.assertTrue(any("owner namespace N1 :: type" in item[0] for item in namespace_one))

    def test_identity_mapping_happens_before_owner_tokenization(self) -> None:
        baseline = "namespace MassTransit { public class Contract { public void Execute() { } } }"
        target = "namespace ViciOne.ServiceBus { public class Contract { public void Execute() { } } }"

        self.assertEqual(
            csharp_public_declarations(target),
            csharp_public_declarations(map_text(baseline)),
        )

    def test_else_elif_nested_and_commented_directive_provenance_is_unambiguous(self) -> None:
        else_a = csharp_public_declarations(
            "#if A\ninternal class Hidden { }\n#else\npublic class Contract { }\n#endif\n"
        )
        else_b = csharp_public_declarations(
            "#if B\ninternal class Hidden { }\n#else\npublic class Contract { }\n#endif\n"
        )
        elif_a = csharp_public_declarations(
            "#if A\ninternal class Hidden { }\n#elif SHARED\npublic class Contract { }\n#endif\n"
        )
        elif_b = csharp_public_declarations(
            "#if B\ninternal class Hidden { }\n#elif SHARED\npublic class Contract { }\n#endif\n"
        )
        nested = csharp_public_declarations(
            "#if OUTER\n#if INNER\npublic class Contract { }\n#endif\n#endif\n"
        )
        commented_a = csharp_public_declarations("/* #if A */\npublic class Contract { }\n")
        commented_b = csharp_public_declarations("/* #if B */\npublic class Contract { }\n")

        self.assertNotEqual(else_a, else_b)
        self.assertNotEqual(elif_a, elif_b)
        self.assertIn("conditional if(OUTER) && if(INNER)", nested[0][0])
        self.assertEqual(commented_a, commented_b)

    def test_new_implicit_interface_member_is_a_current_added_terminal_record(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            source = root / "src/New/IFoo.cs"
            source.parent.mkdir(parents=True)
            source.write_text("public interface IFoo { void Added(); }\n", encoding="utf-8")
            subprocess.run(["git", "add", "src/New/IFoo.cs"], cwd=root, check=True)
            with patch("identity_gate.baseline_paths", return_value=[]):
                records, findings = derive_public_api_mapping(root)

        self.assertEqual([], findings)
        self.assertEqual(2, len(records))
        self.assertTrue(all(record["apiDisposition"] == "CURRENT_ADDED" for record in records))

    def test_symbolic_link_source_is_rejected_without_reading_its_target_as_api(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            (root / ".gitignore").write_text("ignored/\nartifacts/\n", encoding="utf-8")
            ignored = root / "ignored/Outside.cs"
            ignored.parent.mkdir(parents=True)
            ignored.write_text("public sealed class NotCommitted { }\n", encoding="utf-8")
            source = root / "src/New/Linked.cs"
            source.parent.mkdir(parents=True)
            source.symlink_to(ignored)
            subprocess.run(["git", "add", ".gitignore", "src/New/Linked.cs"], cwd=root, check=True)
            with patch("identity_gate.baseline_paths", return_value=[]):
                records, findings = derive_public_api_mapping(root)

        self.assertEqual([], records)
        self.assertTrue(any("symbolic link" in finding.reason for finding in findings))

    def test_ignored_project_is_not_part_of_the_git_candidate_package_inventory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            (root / ".gitignore").write_text("ignored/\n", encoding="utf-8")
            ignored = root / "ignored/Phantom.csproj"
            ignored.parent.mkdir(parents=True)
            ignored.write_text("<Project Sdk=\"Microsoft.NET.Sdk\" />\n", encoding="utf-8")
            real = root / "src/Real/Real.csproj"
            real.parent.mkdir(parents=True)
            real.write_text("<Project Sdk=\"Microsoft.NET.Sdk\" />\n", encoding="utf-8")
            artifact = root / "artifacts/sdk/bin/Real/debug/Real.dll"
            artifact.parent.mkdir(parents=True)
            artifact.write_bytes(b"ignored build output")
            subprocess.run(["git", "add", ".gitignore", "src/Real/Real.csproj"], cwd=root, check=True)

            records = derive_package_inventory(root)

        self.assertEqual(["src/Real/Real.csproj"], [record["project"] for record in records])
        self.assertEqual([], records[0]["artifacts"])

    def test_project_source_symlink_is_rejected_without_dereferencing_ignored_bytes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            (root / ".gitignore").write_text("ignored/\n", encoding="utf-8")
            ignored = root / "ignored/Phantom.csproj"
            ignored.parent.mkdir(parents=True)
            ignored.write_text("<Project Sdk=\"Microsoft.NET.Sdk\" />\n", encoding="utf-8")
            linked = root / "src/Linked/Linked.csproj"
            linked.parent.mkdir(parents=True)
            linked.symlink_to(ignored)
            subprocess.run(
                ["git", "add", ".gitignore", "src/Linked/Linked.csproj"],
                cwd=root,
                check=True,
            )

            with self.assertRaisesRegex(ValueError, "project source must be a regular Git candidate"):
                derive_package_inventory(root)

    def test_current_product_declaration_without_baseline_is_explicitly_terminal(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            source = root / "src/New/Added.cs"
            source.parent.mkdir(parents=True)
            source.write_text("public sealed class Added { }\n", encoding="utf-8")
            subprocess.run(["git", "add", "src/New/Added.cs"], cwd=root, check=True)
            with patch("identity_gate.baseline_paths", return_value=[]):
                records, findings = derive_public_api_mapping(root)

        self.assertEqual([], findings)
        self.assertEqual(1, len(records))
        self.assertEqual("CURRENT_ADDED", records[0]["apiDisposition"])
        self.assertEqual("src/New/Added.cs", records[0]["targetPath"])
        self.assertIs(records[0]["targetPresent"], True)
        self.assertNotIn("baselineKey", records[0])

    def test_current_product_projection_rejects_missing_duplicate_and_invented_bindings(self) -> None:
        declaration_sha = hashlib.sha256(b"public sealed class Added { }").hexdigest()
        key = ("src/New/Added.cs", declaration_sha, 0)
        valid = {
            "apiBindingSha256": "a" * 64,
            "targetPath": key[0],
            "targetLine": 1,
            "targetDeclarationSha256": key[1],
            "targetDeclarationOccurrence": key[2],
            "targetPresent": True,
            "apiDisposition": "CURRENT_ADDED",
        }
        invented = dict(valid, targetDeclarationSha256="b" * 64)
        for records in ([], [valid, dict(valid)], [invented]):
            with self.subTest(records=len(records)):
                self.assertTrue(validate_current_public_api_records(records, {key}))

    def test_current_added_projection_rejects_baseline_identity_or_non_product_path(self) -> None:
        declaration_sha = "c" * 64
        for record in (
            {
                "baselineKey": "d" * 64,
                "targetPath": "src/New.cs",
                "targetDeclarationSha256": declaration_sha,
                "targetDeclarationOccurrence": 0,
                "targetPresent": True,
                "apiDisposition": "CURRENT_ADDED",
            },
            {
                "targetPath": "tests2/NewTests.cs",
                "targetDeclarationSha256": declaration_sha,
                "targetDeclarationOccurrence": 0,
                "targetPresent": True,
                "apiDisposition": "CURRENT_ADDED",
            },
        ):
            with self.subTest(path=record["targetPath"]):
                self.assertTrue(validate_current_public_api_records([record], set()))


class IdentityGateGeneratedEvidenceManifestTests(unittest.TestCase):
    def create_contracts(self, root: Path) -> str:
        lines: list[str] = []
        for name in (
            "BASELINE_TO_TARGET_PATHS.json",
            "CHANGE_NOTICES.json",
            "IDENTITY_DISPOSITION.json",
            "PACKAGE_INVENTORY.json",
            "PUBLIC_API_MAPPING.json",
        ):
            data = (name + "\n").encode("ascii")
            (root / name).write_bytes(data)
            lines.append(f"{hashlib.sha256(data).hexdigest()}  {name}\n")
        return "".join(lines)

    def test_manifest_binds_the_exact_independently_named_contract_set(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            expected = self.create_contracts(root)
            self.assertEqual(
                (
                    "BASELINE_TO_TARGET_PATHS.json",
                    "CHANGE_NOTICES.json",
                    "IDENTITY_DISPOSITION.json",
                    "PACKAGE_INVENTORY.json",
                    "PUBLIC_API_MAPPING.json",
                ),
                GENERATED_EVIDENCE_CONTRACTS,
            )
            self.assertEqual(expected, generated_evidence_manifest(root))
            (root / GENERATED_EVIDENCE_MANIFEST).write_text(expected, encoding="ascii")
            self.assertEqual([], validate_generated_evidence_manifest(root))

    def test_manifest_fails_closed_for_changed_missing_and_extra_bindings(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            expected = self.create_contracts(root)
            manifest = root / GENERATED_EVIDENCE_MANIFEST
            wrong_hash = ("0" if expected[0] != "0" else "1") + expected[1:]
            for mutation in (wrong_hash, expected + "0" * 64 + "  EXTRA.json\n"):
                with self.subTest(mutation=mutation[-80:]):
                    manifest.write_text(mutation, encoding="ascii")
                    self.assertTrue(validate_generated_evidence_manifest(root))
            (root / GENERATED_EVIDENCE_CONTRACTS[0]).unlink()
            manifest.write_text(expected, encoding="ascii")
            self.assertTrue(validate_generated_evidence_manifest(root))

    def test_scan_uses_the_explicit_external_evidence_root(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "LICENSE.txt").write_bytes(b"license")
            evidence = root / "candidate-evidence"
            with contextlib.ExitStack() as stack:
                for target, value in (
                    ("identity_gate.derive_baseline_mapping", ([], [])),
                    ("identity_gate.derive_change_notices", ([], [])),
                    ("identity_gate.derive_public_api_mapping", ([], [])),
                    ("identity_gate.identity_disposition", {}),
                    ("identity_gate.derive_package_inventory", []),
                    ("identity_gate.scan_tree", []),
                    ("identity_gate.derive_refactor_conformance", []),
                    ("identity_gate.baseline_bytes", b"license"),
                    ("identity_gate.regular_candidate_bytes", b"license"),
                    ("identity_gate.validate_legal_documents", []),
                ):
                    stack.enter_context(patch(target, return_value=value))
                validate = stack.enter_context(
                    patch("identity_gate.validate_persisted_evidence", return_value=[])
                )
                stack.enter_context(contextlib.redirect_stdout(io.StringIO()))
                self.assertEqual(0, run_gate(root, None, evidence))
            self.assertEqual(evidence, validate.call_args.args[0])


if __name__ == "__main__":
    unittest.main()
