#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Hostile unit fixtures for the generated-artifact gate."""

from __future__ import annotations

import hashlib
import io
import sys
import tempfile
import types
import unittest
import uuid
import zipfile
from pathlib import Path

from artifact_gate import scan_artifacts, scan_nupkg
from identity_rules import FORMER_PASCAL, contains_former_identity_bytes


ROOT = Path(__file__).resolve().parents[2]
STOPPED_PACKAGE = ROOT / "src/ViciOne.ServiceBus/bin/Release/ViciOne.ServiceBus.1.0.0.nupkg"
STOPPED_PACKAGE_SHA256 = "91427c55ff06b29199422505eda1f1576f2af6f865bc501c174ab6934bac9e10"


def package(
    entries: dict[str, bytes],
    *,
    archive_comment: bytes = b"",
    entry_comments: dict[str, bytes] | None = None,
) -> bytes:
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, "w") as archive:
        for path, data in entries.items():
            info = zipfile.ZipInfo(path)
            info.comment = (entry_comments or {}).get(path, b"")
            archive.writestr(info, data)
        archive.comment = archive_comment
    return buffer.getvalue()


def load_mutant(
    mutant_id: str,
    replacements: list[tuple[str, str]],
) -> tuple[str, types.ModuleType]:
    source_path = Path(__file__).with_name("artifact_gate.py")
    source = source_path.read_text(encoding="utf-8")
    for before, after in replacements:
        occurrences = source.count(before)
        if occurrences != 1:
            raise AssertionError(
                f"mutant {mutant_id} expected one source anchor, found {occurrences}"
            )
        source = source.replace(before, after, 1)

    module_name = f"_artifact_gate_mutant_{mutant_id.casefold()}_{uuid.uuid4().hex}"
    module = types.ModuleType(module_name)
    module.__file__ = str(source_path)
    sys.modules[module_name] = module
    try:
        exec(compile(source, str(source_path), "exec"), module.__dict__)
    except Exception:
        sys.modules.pop(module_name, None)
        raise
    return module_name, module


class ArtifactGateTests(unittest.TestCase):
    def stopped_package_bytes(self) -> bytes:
        data = STOPPED_PACKAGE.read_bytes()
        self.assertEqual(STOPPED_PACKAGE_SHA256, hashlib.sha256(data).hexdigest())
        return data

    def test_rejects_former_identity_in_outer_package_path(self) -> None:
        _, findings = scan_nupkg(
            f"{FORMER_PASCAL}.nupkg",
            package({"lib/net9.0/ViciOne.ServiceBus.dll": b"safe"}),
        )
        self.assertTrue(any(item.gate == "nuget-path" for item in findings))

    def test_rejects_former_identity_in_entry_path(self) -> None:
        _, findings = scan_nupkg(
            "target.nupkg", package({f"lib/net9.0/{FORMER_PASCAL}.dll": b"safe"})
        )
        self.assertTrue(any(item.gate == "nuget-path" for item in findings))

    def test_rejects_former_identity_in_decompressed_entry_content(self) -> None:
        _, findings = scan_nupkg(
            "target.nupkg",
            package({"lib/net9.0/ViciOne.ServiceBus.dll": b"\xff\0" + FORMER_PASCAL.encode("ascii") + b"\0"}),
        )
        self.assertTrue(any(item.gate == "nuget-content" for item in findings))

    def test_accepts_target_only_package(self) -> None:
        _, findings = scan_nupkg(
            "target.nupkg",
            package({"lib/net9.0/ViciOne.ServiceBus.dll": b"ViciOne.ServiceBus"}),
        )
        self.assertEqual([], findings)

    def test_allows_exact_legal_provenance_entry_only(self) -> None:
        records, findings = scan_nupkg(
            "target.nupkg", package({"README.md": b"Upstream: " + FORMER_PASCAL.encode("ascii")})
        )
        self.assertEqual([], findings)
        self.assertTrue(records[0]["legalProvenanceException"])

    def test_rejects_provenance_content_in_nested_legal_basename(self) -> None:
        records, findings = scan_nupkg(
            "target.nupkg",
            package({"docs/README.md": b"Upstream: " + FORMER_PASCAL.encode("ascii")}),
        )
        self.assertFalse(records[0]["legalProvenanceException"])
        self.assertTrue(any(item.gate == "nuget-content" for item in findings))

    def test_rejects_former_identity_in_archive_comment(self) -> None:
        _, findings = scan_nupkg(
            "target.nupkg",
            package(
                {"lib/net9.0/ViciOne.ServiceBus.dll": b"safe"},
                archive_comment=FORMER_PASCAL.encode("ascii"),
            ),
        )
        self.assertTrue(any(item.gate == "nuget-archive-comment" for item in findings))

    def test_rejects_former_identity_in_entry_comment(self) -> None:
        entry = "README.md"
        _, findings = scan_nupkg(
            "target.nupkg",
            package(
                {entry: b"Upstream: " + FORMER_PASCAL.encode("ascii")},
                entry_comments={entry: FORMER_PASCAL.encode("ascii")},
            ),
        )
        self.assertTrue(any(item.gate == "nuget-entry-comment" for item in findings))

    def test_rejects_invalid_zip(self) -> None:
        records, findings = scan_nupkg("target.nupkg", b"not a ZIP archive")
        self.assertEqual([], records)
        self.assertTrue(any(item.gate == "nuget" for item in findings))

    def test_accepts_exact_stopped_package_without_scanning_raw_container_bytes(self) -> None:
        data = self.stopped_package_bytes()
        self.assertTrue(contains_former_identity_bytes(data))
        records, findings = scan_nupkg(STOPPED_PACKAGE.relative_to(ROOT).as_posix(), data)
        self.assertGreater(len(records), 0)
        self.assertEqual([], findings)

    def test_scan_artifacts_keeps_raw_container_bytes_opaque(self) -> None:
        data = self.stopped_package_bytes()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            project = root / "src/Probe/Probe.csproj"
            project.parent.mkdir(parents=True)
            project.write_text("<Project />\n", encoding="utf-8")
            target = project.parent / "bin/Release/Probe.1.0.0.nupkg"
            target.parent.mkdir(parents=True)
            target.write_bytes(data)
            result = scan_artifacts(root)

        self.assertEqual("PASS", result["status"])
        self.assertEqual(1, result["counts"]["nupkg"])
        self.assertEqual([], result["findings"])

    def test_scan_artifacts_rejects_null_artifact_discovery(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            result = scan_artifacts(Path(directory))
        self.assertEqual("FAIL", result["status"])
        self.assertTrue(any(item["gate"] == "null-artifact" for item in result["findings"]))

    def test_all_five_required_nupkg_scanner_mutants_are_killed(self) -> None:
        mutants: dict[str, list[tuple[str, str]]] = {
            "RAW_CONTAINER_SCAN_REINTRODUCED": [
                (
                    "    findings: list[ArtifactFinding] = []\n    if contains_former_identity(path):",
                    "    findings: list[ArtifactFinding] = []\n"
                    "    findings.extend(scan_bytes(path, data, \"nupkg-binary\"))\n"
                    "    if contains_former_identity(path):",
                )
            ],
            "OUTER_PACKAGE_PATH_CHECK_REMOVED": [
                (
                    "    if contains_former_identity(path):",
                    "    if False and contains_former_identity(path):",
                )
            ],
            "ENTRY_PATH_CHECK_REMOVED": [
                (
                    "            if contains_former_identity(info.filename):",
                    "            if False and contains_former_identity(info.filename):",
                )
            ],
            "DECOMPRESSED_CONTENT_CHECK_REMOVED": [
                (
                    "            if payload is not None and not legal:",
                    "            if False and payload is not None and not legal:",
                )
            ],
            "COMMENT_CHECKS_REMOVED": [
                (
                    "        findings.extend(\n"
                    "            scan_bytes(\n"
                    "                f\"{path}!/<archive-comment>\",\n"
                    "                package.comment,\n"
                    "                \"nuget-archive-comment\",\n"
                    "                semantic_path=\"nupkg-archive-comment\",\n"
                    "            )\n"
                    "        )\n",
                    "",
                ),
                (
                    "            entry_findings.extend(\n"
                    "                scan_bytes(\n"
                    "                    f\"{entry_path}#comment\",\n"
                    "                    info.comment,\n"
                    "                    \"nuget-entry-comment\",\n"
                    "                    semantic_path=\"nupkg-entry-comment\",\n"
                    "                )\n"
                    "            )\n",
                    "",
                ),
            ],
        }
        self.assertEqual(5, len(mutants), "null mutation inventory is forbidden")
        killed: list[str] = []
        stopped_data = self.stopped_package_bytes()

        for mutant_id, replacements in mutants.items():
            with self.subTest(mutant=mutant_id):
                module_name, mutant = load_mutant(mutant_id, replacements)
                try:
                    if mutant_id == "RAW_CONTAINER_SCAN_REINTRODUCED":
                        _, findings = mutant.scan_nupkg(
                            STOPPED_PACKAGE.relative_to(ROOT).as_posix(), stopped_data
                        )
                        mutant_violation = bool(findings)
                    elif mutant_id == "OUTER_PACKAGE_PATH_CHECK_REMOVED":
                        _, findings = mutant.scan_nupkg(
                            f"{FORMER_PASCAL}.nupkg",
                            package({"lib/net9.0/ViciOne.ServiceBus.dll": b"safe"}),
                        )
                        mutant_violation = not findings
                    elif mutant_id == "ENTRY_PATH_CHECK_REMOVED":
                        _, findings = mutant.scan_nupkg(
                            "target.nupkg",
                            package({f"lib/net9.0/{FORMER_PASCAL}.dll": b"safe"}),
                        )
                        mutant_violation = not findings
                    elif mutant_id == "DECOMPRESSED_CONTENT_CHECK_REMOVED":
                        _, findings = mutant.scan_nupkg(
                            "target.nupkg",
                            package({"lib/net9.0/ViciOne.ServiceBus.dll": FORMER_PASCAL.encode("ascii")}),
                        )
                        mutant_violation = not findings
                    else:
                        _, findings = mutant.scan_nupkg(
                            "target.nupkg",
                            package(
                                {"lib/net9.0/ViciOne.ServiceBus.dll": b"safe"},
                                archive_comment=FORMER_PASCAL.encode("ascii"),
                            ),
                        )
                        mutant_violation = not findings
                finally:
                    sys.modules.pop(module_name, None)
                if mutant_violation:
                    killed.append(mutant_id)

        self.assertEqual(sorted(mutants), sorted(killed))


if __name__ == "__main__":
    unittest.main()
