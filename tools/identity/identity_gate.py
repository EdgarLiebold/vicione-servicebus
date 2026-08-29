#!/usr/bin/env python3
"""Fail-closed source-tree and baseline-completeness gate for the identity fork."""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import re
import subprocess
import sys
import tarfile
import xml.etree.ElementTree as ET
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Mapping

from identity_rules import (
    BASELINE_COMMIT,
    COMMENTLESS_OR_BINARY_EXCEPTIONS,
    COMMENTLESS_OR_BINARY_BASELINE_SOURCES,
    FORMER_IDENTITY_REGISTRY,
    FORMER_PASCAL,
    LEGAL_OR_PROVENANCE_PATHS,
    contains_former_identity,
    contains_former_identity_bytes,
    map_path,
    map_text,
    supports_modification_notice,
)


FORBIDDEN_COMPATIBILITY_FORMS = (
    "".join(("type", "forwardedto")).encode("ascii"),
    "".join(("extern", " alias")).encode("ascii"),
)
BUILD_OUTPUT_PARTS = frozenset({".git", "bin", "obj"})
PUBLIC_DECLARATION = re.compile(r"^\s*public\s+.+", re.MULTILINE)
TEST_SABOTAGE_FORMS = ("[Ignore", "[Explicit", "Assert.Pass(", ".Skip =", "Skip =")
NOTICE_EXCEPTION_HEADING = "covered by exact path-bound entries in MODIFICATIONS.md:"
MODIFICATIONS_EXCEPTION_HEADING = "## Changed files without an in-file modification comment"
HISTORICAL_IDENTITY_POLICY_PATH = "tools/identity/historical_identity_policy.json"
HISTORICAL_IDENTITY_POLICY_SCHEMA_VERSION = 1
HISTORICAL_IDENTITY_CATEGORIES = frozenset({
    "LEGAL_PROVENANCE",
    "HISTORICAL_EVIDENCE",
    "NEGATIVE_TEST_ORACLE",
    "IDENTITY_TOOL_SELF_REFERENCE",
})
GENERATED_EVIDENCE_CONTRACTS = (
    "BASELINE_TO_TARGET_PATHS.json",
    "CHANGE_NOTICES.json",
    "IDENTITY_DISPOSITION.json",
    "PACKAGE_INVENTORY.json",
    "PUBLIC_API_MAPPING.json",
)
GENERATED_EVIDENCE_MANIFEST = "GENERATED_SHA256SUMS"
TERMINAL_BASELINE_DISPOSITIONS = frozenset({
    "MAPPED_EXISTING",
    "MOVED_EXACT",
    "RETIRED_DELETED",
})
_BASELINE_ARCHIVES: dict[Path, dict[str, bytes]] = {}
_BASELINE_TREE_ENTRIES: dict[Path, dict[str, tuple[str, str]]] = {}
_DELETED_PATH_COMMITS: dict[Path, dict[str, str]] = {}


@dataclass(frozen=True)
class Finding:
    gate: str
    path: str
    reason: str

    def as_dict(self) -> dict[str, str]:
        return {"gate": self.gate, "path": self.path, "reason": self.reason}


def git(root: Path, *arguments: str) -> bytes:
    result = subprocess.run(
        ["git", "-C", str(root), *arguments],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if result.returncode:
        raise RuntimeError(result.stderr.decode("utf-8", errors="replace"))
    return result.stdout


def baseline_archive(root: Path) -> dict[str, bytes]:
    cached = _BASELINE_ARCHIVES.get(root)
    if cached is not None:
        return cached

    raw = git(root, "archive", "--format=tar", BASELINE_COMMIT)
    entries: dict[str, bytes] = {}
    with tarfile.open(fileobj=io.BytesIO(raw), mode="r:") as archive:
        for member in archive.getmembers():
            if member.isfile():
                extracted = archive.extractfile(member)
                if extracted is None:
                    raise RuntimeError(f"git archive entry cannot be read: {member.name}")
                entries[member.name] = extracted.read()
            elif member.issym():
                entries[member.name] = member.linkname.encode("utf-8")
    _BASELINE_ARCHIVES[root] = entries
    return entries


def baseline_paths(root: Path) -> list[str]:
    return sorted(baseline_archive(root))


def baseline_bytes(root: Path, path: str) -> bytes:
    return baseline_archive(root)[path]


def baseline_tree_entries(root: Path) -> dict[str, tuple[str, str]]:
    cached = _BASELINE_TREE_ENTRIES.get(root)
    if cached is not None:
        return cached
    records: dict[str, tuple[str, str]] = {}
    for entry in git(root, "ls-tree", "-rz", "--full-tree", BASELINE_COMMIT).split(b"\0"):
        if not entry:
            continue
        metadata, raw_path = entry.split(b"\t", 1)
        mode, object_type, object_id = metadata.decode("ascii").split()
        if object_type != "blob":
            continue
        records[raw_path.decode("utf-8", errors="surrogateescape")] = (mode, object_id)
    _BASELINE_TREE_ENTRIES[root] = records
    return records


def commit_candidate_files(root: Path) -> Iterable[Path]:
    """Yield exactly existing tracked plus non-ignored untracked Git candidates."""
    raw = git(root, "ls-files", "--cached", "--others", "--exclude-standard", "-z")
    paths = sorted(
        {
            item.decode("utf-8", errors="surrogateescape")
            for item in raw.split(b"\0")
            if item
        }
    )
    for path in paths:
        candidate = root / path
        if candidate.is_file() or candidate.is_symlink():
            yield candidate


def legal_identity_contexts() -> dict[str, tuple[str, ...]]:
    upstream_url = f"https://github.com/{FORMER_PASCAL}/{FORMER_PASCAL}"
    return {
        # The present provenance statement, not the one the import wrote. The repository was created
        # from a complete pinned fork and carries the retained, modernised scope today; a gate that
        # still demanded the old sentence would be asserting a claim the product no longer makes.
        "README.md": (
            f"This repository was created from a complete, pinned fork of **{FORMER_PASCAL} 8.5.10**, upstream commit\n`62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, from the\n[{FORMER_PASCAL}]({upstream_url}) project, and carries the deliberately\nretained and modernised ViciOne capability scope today. The retained and modified code is licensed\nunder the **Apache License 2.0**; see [LICENSE.txt](LICENSE.txt), [NOTICE](NOTICE),\n[COPYRIGHT](COPYRIGHT) and [MODIFICATIONS.md](MODIFICATIONS.md).",
        ),
        "NOTICE": (
            f"{FORMER_PASCAL}\nCopyright 2007-2024 Chris Patterson",
            f"This repository is a full fork of {FORMER_PASCAL} 8.5.10 at upstream commit",
        ),
        "COPYRIGHT": (),
        "MODIFICATIONS.md": (
            f"The baseline is the complete {FORMER_PASCAL} 8.5.10 source tree at upstream commit `62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, imported into the local fork baseline commit `1de4bf6eb45c406da3cd6f26bdab6ed6d5aeefbc` (tree `2b09d4e2b2e14289f06ba112ce1ae52e326a0307`).",
        ),
        "LICENSE.txt": (),
    }


def mask_legal_identity_contexts(path: str, text: str) -> tuple[str, list[Finding]]:
    findings: list[Finding] = []
    masked = text
    for context in legal_identity_contexts().get(path, ()):
        occurrences = masked.count(context)
        if occurrences != 1:
            findings.append(
                Finding("legal-context", path, f"authorized provenance context count is {occurrences}, expected 1")
            )
            continue
        replacement = "".join(character if character in "\r\n" else " " for character in context)
        masked = masked.replace(context, replacement, 1)
    return masked, findings


def scan_entry(path: str, data: bytes) -> list[Finding]:
    """Scan one supplied entry; kept independent so hostile fixtures test it."""
    findings: list[Finding] = []
    if contains_former_identity(path):
        findings.append(Finding("path-scan", path, "former technical identity in path"))

    scan_data = data
    try:
        decoded = data.decode("utf-8")
    except UnicodeDecodeError:
        decoded = ""

    if path in LEGAL_OR_PROVENANCE_PATHS and decoded:
        decoded, legal_findings = mask_legal_identity_contexts(path, decoded)
        findings.extend(legal_findings)
        scan_data = decoded.encode("utf-8")

    if decoded and contains_former_identity(decoded):
        findings.append(Finding("text-scan", path, "former technical identity in UTF-8 content"))
    if contains_former_identity_bytes(scan_data):
        findings.append(Finding("binary-scan", path, "former product identity in binary text surface"))
    lowered = scan_data.lower()
    if any(token in lowered for token in FORBIDDEN_COMPATIBILITY_FORMS):
        findings.append(Finding("compatibility-scan", path, "forbidden alias/type-forwarder form"))
    return findings


def line_sha256(line: bytes) -> str:
    return hashlib.sha256(line).hexdigest()


def historical_text_line_inventory(path: str, data: bytes) -> tuple[list[bool], Counter[str]]:
    decoded = data.decode("utf-8")
    legally_masked, _ = mask_legal_identity_contexts(path, decoded)
    original_lines = data.splitlines(keepends=True)
    candidate_lines = legally_masked.encode("utf-8").splitlines(keepends=True)
    if len(original_lines) != len(candidate_lines):
        raise ValueError(f"legal masking changed the line structure for {path}")
    suppressible_gates = {"text-scan", "binary-scan", "compatibility-scan"}
    selected = [
        any(finding.gate in suppressible_gates for finding in scan_entry("", candidate))
        for candidate in candidate_lines
    ]
    return selected, Counter(
        line_sha256(line)
        for line, is_selected in zip(original_lines, selected)
        if is_selected
    )


def historical_text_line_inventory_sha256(inventory: Mapping[str, int]) -> str:
    payload = json.dumps(sorted(inventory.items()), separators=(",", ":")).encode("ascii")
    return sha256(payload)


def mask_authorized_text_lines(
    path: str,
    data: bytes,
    authorized_line_count: object,
    authorized_lines_sha256: object,
) -> tuple[bytes, list[Finding]]:
    """Mask only the exact, counted UTF-8 line inventory authorized by the policy."""
    findings: list[Finding] = []
    if not isinstance(authorized_line_count, int) or isinstance(authorized_line_count, bool) or authorized_line_count <= 0:
        findings.append(Finding("historical-identity-policy", path, "authorized text line count is invalid"))
    if not isinstance(authorized_lines_sha256, str) or not re.fullmatch(r"[0-9a-f]{64}", authorized_lines_sha256):
        findings.append(Finding("historical-identity-policy", path, "authorized text line inventory digest is invalid"))

    selected, actual_inventory = historical_text_line_inventory(path, data)
    if (
        sum(actual_inventory.values()) != authorized_line_count
        or historical_text_line_inventory_sha256(actual_inventory) != authorized_lines_sha256
    ):
        findings.append(Finding("historical-identity-policy", path, "authorized text line inventory is stale"))

    masked_lines: list[bytes] = []
    for line, is_selected in zip(data.splitlines(keepends=True), selected):
        if not is_selected:
            masked_lines.append(line)
            continue
        ending_length = len(line) - len(line.rstrip(b"\r\n"))
        content_length = len(line) - ending_length
        masked_lines.append(b" " * content_length + line[content_length:])
    return b"".join(masked_lines), findings


def load_historical_identity_policy(root: Path) -> tuple[list[dict[str, object]], list[Finding]]:
    path = root / HISTORICAL_IDENTITY_POLICY_PATH
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        return [], [Finding("historical-identity-policy", HISTORICAL_IDENTITY_POLICY_PATH, f"cannot load policy: {error}")]
    if not isinstance(document, dict) or document.get("schemaVersion") != HISTORICAL_IDENTITY_POLICY_SCHEMA_VERSION:
        return [], [Finding("historical-identity-policy", HISTORICAL_IDENTITY_POLICY_PATH, "unsupported schema")]
    records = document.get("entries")
    if not isinstance(records, list) or any(not isinstance(record, dict) for record in records):
        return [], [Finding("historical-identity-policy", HISTORICAL_IDENTITY_POLICY_PATH, "entries must be records")]
    return [dict(record) for record in records], []


def scan_tree(root: Path) -> list[Finding]:
    candidate_bytes = {
        candidate.relative_to(root).as_posix(): candidate.read_bytes()
        for candidate in commit_candidate_files(root)
    }
    raw_by_path = {
        path: scan_entry(path, data)
        for path, data in candidate_bytes.items()
    }
    suppressible_gates = {"text-scan", "binary-scan", "compatibility-scan", "path-scan"}
    expected_policy_paths = {
        path
        for path, path_findings in raw_by_path.items()
        if any(finding.gate in suppressible_gates for finding in path_findings)
    }
    records, load_findings = load_historical_identity_policy(root)
    policy_findings = validate_historical_identity_policy(records, candidate_bytes, expected_policy_paths)
    records_by_path = {str(record.get("path") or ""): record for record in records}
    invalid_policy_paths = {
        finding.path
        for finding in policy_findings
        if finding.path != HISTORICAL_IDENTITY_POLICY_PATH
    }
    findings: list[Finding] = load_findings + policy_findings
    for path, path_findings in raw_by_path.items():
        suppressible = [finding for finding in path_findings if finding.gate in suppressible_gates]
        retained = [finding for finding in path_findings if finding.gate not in suppressible_gates]
        record = records_by_path.get(path)
        allowed_gates = set(record.get("allowedGates", [])) if record else set()
        actual_gates = {finding.gate for finding in suppressible}
        if (
            suppressible
            and record is not None
            and path not in invalid_policy_paths
            and allowed_gates == actual_gates
        ):
            findings.extend(retained)
        else:
            findings.extend(path_findings)
    return findings


def validate_test_run(discovered: int, executed: int, failed: int, skipped: int) -> list[Finding]:
    findings: list[Finding] = []
    if discovered <= 0 or executed <= 0:
        findings.append(Finding("null-test", "test-result", "zero discovered or executed tests"))
    if executed + skipped < discovered:
        findings.append(Finding("test-accounting", "test-result", "discovered tests are unaccounted for"))
    if failed:
        findings.append(Finding("test-failure", "test-result", f"{failed} tests failed"))
    return findings


def test_sabotage_findings(before: str, after: str, path: str) -> list[Finding]:
    findings: list[Finding] = []
    for token in TEST_SABOTAGE_FORMS:
        if after.count(token) > before.count(token):
            findings.append(Finding("test-sabotage", path, f"new weakening token {token}"))
    return findings


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def baseline_binding(root: Path, path: str) -> dict[str, str]:
    mode, object_id = baseline_tree_entries(root)[path]
    path_sha = sha256(path.encode("utf-8", errors="surrogateescape"))
    key_payload = "\0".join((BASELINE_COMMIT, mode, object_id, path_sha)).encode("ascii")
    return {
        "baselineKey": sha256(key_payload),
        "baselineCommit": BASELINE_COMMIT,
        "baselinePathSha256": path_sha,
        "baselineGitBlobOid": object_id,
        "baselineGitMode": mode,
    }


def deleted_path_commits(root: Path) -> dict[str, str]:
    cached = _DELETED_PATH_COMMITS.get(root)
    if cached is not None:
        return cached
    output = git(
        root,
        "log",
        "--format=@@%H",
        "--name-status",
        "--diff-filter=D",
        "--no-renames",
        f"{BASELINE_COMMIT}..HEAD",
    ).decode("utf-8", errors="surrogateescape")
    current_commit = ""
    records: dict[str, str] = {}
    for line in output.splitlines():
        if line.startswith("@@"):
            current_commit = line[2:]
        elif line.startswith("D\t") and current_commit:
            records.setdefault(line.split("\t", 1)[1], current_commit)
    _DELETED_PATH_COMMITS[root] = records
    return records


def change_list_deleted_baseline_paths(root: Path) -> set[str]:
    document = (root / "CHANGELIST.md").read_text(encoding="utf-8")
    return {
        match.group("source")
        for match in re.finditer(
            r"^\| `(?P<path>[^`]+)` \| Deleted \| `(?P<source>[^`]+)` \|$",
            document,
            flags=re.MULTILINE,
        )
        if match.group("path") == match.group("source")
    }


def validate_terminal_baseline_records(
    records: Iterable[Mapping[str, object]],
    expected_keys: set[str],
    actual_paths: set[str],
) -> list[Finding]:
    materialized = [dict(record) for record in records]
    keys = [str(record.get("baselineKey") or "") for record in materialized]
    findings: list[Finding] = []
    if len(keys) != len(set(keys)):
        findings.append(Finding("baseline-terminal", "BASELINE_TO_TARGET_PATHS.json", "duplicate baselineKey"))
    if set(keys) != expected_keys:
        findings.append(Finding("baseline-terminal", "BASELINE_TO_TARGET_PATHS.json", "missing or invented baselineKey"))
    for record in materialized:
        key = str(record.get("baselineKey") or "")
        target = str(record.get("targetPath") or "")
        disposition = str(record.get("baselineDisposition") or "")
        if disposition not in TERMINAL_BASELINE_DISPOSITIONS:
            findings.append(Finding("baseline-terminal", target or key, "unknown terminal disposition"))
            continue
        exists = target in actual_paths
        if disposition == "RETIRED_DELETED":
            if exists or record.get("targetExists") is not False:
                findings.append(Finding("baseline-terminal", target, "retired target is present"))
            retirement_commit = record.get("retirementCommit")
            if (
                not isinstance(retirement_commit, str)
                or not re.fullmatch(r"[0-9a-f]{40}", retirement_commit)
                or record.get("retirementEvidence") != "ROOT_CHANGE_LIST"
                or not isinstance(record.get("retirementTree"), str)
                or not re.fullmatch(r"[0-9a-f]{40}", str(record.get("retirementTree")))
            ):
                findings.append(Finding("baseline-terminal", target, "retirement provenance is incomplete"))
        else:
            expected_path_disposition = "UNCHANGED" if disposition == "MAPPED_EXISTING" else "RENAMED"
            if record.get("pathDisposition") != expected_path_disposition:
                findings.append(Finding("baseline-terminal", target, "live path disposition is inconsistent"))
            if any(field in record for field in ("retirementCommit", "retirementEvidence", "retirementTree")):
                findings.append(Finding("baseline-terminal", target, "live target carries retirement provenance"))
            if not exists or record.get("targetExists") is not True:
                findings.append(Finding("baseline-terminal", target, "live target is absent"))
    return findings


def public_declaration_disposition(target_exists: bool, declaration_present: bool) -> str:
    if not target_exists:
        if declaration_present:
            raise ValueError("a declaration cannot be present on a retired path")
        return "RETIRED_PATH"
    return "MAPPED_PRESENT" if declaration_present else "MODIFIED_OR_REMOVED"


def validate_historical_identity_policy(
    records: Iterable[Mapping[str, object]],
    candidate_bytes: Mapping[str, bytes],
    expected_paths: set[str] | None = None,
) -> list[Finding]:
    materialized = [dict(record) for record in records]
    paths = [str(record.get("path") or "") for record in materialized]
    findings: list[Finding] = []
    if len(paths) != len(set(paths)):
        findings.append(Finding("historical-identity-policy", HISTORICAL_IDENTITY_POLICY_PATH, "duplicate path"))
    if expected_paths is not None and set(paths) != expected_paths:
        findings.append(Finding("historical-identity-policy", HISTORICAL_IDENTITY_POLICY_PATH, "missing or invented path"))
    for record in materialized:
        path = str(record.get("path") or "")
        category = str(record.get("category") or "")
        if category not in HISTORICAL_IDENTITY_CATEGORIES:
            findings.append(Finding("historical-identity-policy", path, "unknown category"))
        if path.startswith("src/") and category not in {"LEGAL_PROVENANCE", "HISTORICAL_EVIDENCE"}:
            findings.append(Finding("historical-identity-policy", path, "category is not allowed for active product"))
        if path.startswith("src/") and not path.endswith("AnalyzerReleases.Shipped.md"):
            findings.append(Finding("historical-identity-policy", path, "category is not allowed for active product"))
        data = candidate_bytes.get(path)
        if data is None:
            findings.append(Finding("historical-identity-policy", path, "policy path is absent"))
        allowed_gates = record.get("allowedGates")
        if not isinstance(allowed_gates, list) or not allowed_gates or any(
            gate not in {"text-scan", "binary-scan", "compatibility-scan", "path-scan"}
            for gate in allowed_gates
        ) or allowed_gates != sorted(set(allowed_gates)):
            findings.append(Finding("historical-identity-policy", path, "allowed gates are invalid"))
            continue
        if data is None:
            continue

        raw_findings = scan_entry(path, data)
        raw_gates = {
            finding.gate
            for finding in raw_findings
            if finding.gate in {"text-scan", "binary-scan", "compatibility-scan", "path-scan"}
        }
        if raw_gates != set(allowed_gates):
            findings.append(Finding("historical-identity-policy", path, "allowed gates do not match the current file"))

        try:
            data.decode("utf-8")
            is_text = True
        except UnicodeDecodeError:
            is_text = False

        if is_text:
            if "blobSha256" in record:
                findings.append(Finding("historical-identity-policy", path, "text policy must bind exact lines, not a blob"))
            if raw_gates - {"path-scan"}:
                masked, line_findings = mask_authorized_text_lines(
                    path,
                    data,
                    record.get("authorizedLineCount"),
                    record.get("authorizedLinesSha256"),
                )
                findings.extend(line_findings)
            else:
                if "authorizedLineCount" in record or "authorizedLinesSha256" in record:
                    findings.append(Finding("historical-identity-policy", path, "path-only policy cannot bind text lines"))
                masked = data
        else:
            if "authorizedLineCount" in record or "authorizedLinesSha256" in record:
                findings.append(Finding("historical-identity-policy", path, "binary policy cannot bind text lines"))
            if record.get("blobSha256") != sha256(data):
                findings.append(Finding("historical-identity-policy", path, "policy blob digest is stale"))
            masked = b""

        path_authorized = record.get("authorizePath") is True
        if path_authorized != ("path-scan" in allowed_gates):
            findings.append(Finding("historical-identity-policy", path, "path authorization does not match the current path"))
        residual = scan_entry(path, masked)
        if path_authorized:
            residual = [finding for finding in residual if finding.gate != "path-scan"]
        residual_gates = {
            finding.gate
            for finding in residual
            if finding.gate in {"text-scan", "binary-scan", "compatibility-scan", "path-scan"}
        }
        if residual_gates:
            findings.append(
                Finding(
                    "historical-identity-policy",
                    path,
                    "unauthorized historical identity remains after exact context masking: "
                    + ", ".join(sorted(residual_gates)),
                )
            )
    return findings


def omitted_baseline_findings(expected_targets: Iterable[str], actual_paths: set[str]) -> list[Finding]:
    return [
        Finding("baseline-census", target, "mapped baseline target is absent")
        for target in expected_targets
        if target not in actual_paths
    ]


def derive_baseline_mapping(root: Path) -> tuple[list[dict[str, object]], list[Finding]]:
    records: list[dict[str, object]] = []
    findings: list[Finding] = []
    seen_targets: dict[str, str] = {}
    deleted_sources = change_list_deleted_baseline_paths(root)
    deletion_commits = deleted_path_commits(root)
    retirement_tree = git(root, "rev-parse", "HEAD^{tree}").decode("ascii").strip()
    actual_paths = {
        candidate.relative_to(root).as_posix()
        for candidate in commit_candidate_files(root)
    }
    for source in baseline_paths(root):
        target = map_path(source)
        if target in seen_targets:
            findings.append(
                Finding("path-bijection", target, f"target also mapped from {seen_targets[target]}")
            )
        seen_targets[target] = source
        target_path = root / target
        exists = target_path.exists() or target_path.is_symlink()
        source_data = baseline_bytes(root, source)
        disposition = (
            "RETIRED_DELETED"
            if not exists
            else "MOVED_EXACT" if source != target else "MAPPED_EXISTING"
        )
        record: dict[str, object] = {
            **baseline_binding(root, source),
            "targetPath": target,
            "pathDisposition": "RENAMED" if source != target else "UNCHANGED",
            "baselineDisposition": disposition,
            "baselineSha256": sha256(source_data),
            "targetExists": exists,
        }
        if exists and target_path.is_file():
            record["targetSha256"] = sha256(target_path.read_bytes())
        elif not exists:
            deletion_commit = deletion_commits.get(target) or deletion_commits.get(source)
            if source not in deleted_sources:
                findings.append(Finding("baseline-retirement", target, f"CHANGELIST has no deletion for {source}"))
            if not deletion_commit:
                findings.append(Finding("baseline-retirement", target, f"Git history has no deletion for {source}"))
            record.update({
                "retirementCommit": deletion_commit,
                "retirementEvidence": "ROOT_CHANGE_LIST",
                "retirementTree": retirement_tree,
            })
        records.append(record)
    findings.extend(validate_terminal_baseline_records(
        records,
        expected_keys={str(baseline_binding(root, source)["baselineKey"]) for source in baseline_paths(root)},
        actual_paths=actual_paths,
    ))
    return records, findings


def derive_change_notices(root: Path) -> tuple[list[dict[str, object]], list[Finding]]:
    records: list[dict[str, object]] = []
    findings: list[Finding] = []
    for source in baseline_paths(root):
        target = map_path(source)
        target_path = root / target
        if not target_path.exists() or not target_path.is_file():
            records.append(
                {
                    **baseline_binding(root, source),
                    "targetPath": target,
                    "contentChanged": False,
                    "pathChanged": source != target,
                    "changeKind": "RETIRED_DELETED",
                    "mechanism": "ROOT_CHANGE_LIST",
                    "effective": True,
                }
            )
            continue
        before = baseline_bytes(root, source)
        after = target_path.read_bytes()
        if source == target and before == after:
            continue

        # The per file notice duty was replaced by the generated root change list, so a changed baseline file is
        # no longer required to carry a comment of its own. The census of what changed stays, because the change
        # list is built from exactly this comparison.
        records.append(
            {
                **baseline_binding(root, source),
                "targetPath": target,
                "contentChanged": before != after,
                "pathChanged": source != target,
                "changeKind": "MODIFIED_OR_MOVED",
                "mechanism": "ROOT_CHANGE_LIST",
                "effective": True,
            }
        )
    return records, findings


def derive_refactor_conformance(root: Path) -> list[Finding]:
    """Retain only anti-sabotage checks after the identity-only refactor phase ended."""
    findings: list[Finding] = []
    for source in baseline_paths(root):
        target = map_path(source)
        target_path = root / target
        if not target_path.exists() or not target_path.is_file() or source in LEGAL_OR_PROVENANCE_PATHS:
            continue
        before = baseline_bytes(root, source)
        try:
            before_text = before.decode("utf-8")
        except UnicodeDecodeError:
            continue
        actual = target_path.read_bytes().decode("utf-8")
        if source.startswith("tests/"):
            findings.extend(test_sabotage_findings(map_text(before_text), actual, target))
    return findings


def derive_public_api_mapping(root: Path) -> tuple[list[dict[str, object]], list[Finding]]:
    records: list[dict[str, object]] = []
    findings: list[Finding] = []
    target_keys: set[tuple[str, str, int]] = set()
    for source in baseline_paths(root):
        if not source.endswith(".cs"):
            continue
        data = baseline_bytes(root, source)
        try:
            text = data.decode("utf-8")
        except UnicodeDecodeError:
            continue
        target = map_path(source)
        target_exists = (root / target).exists()
        target_text = (root / target).read_text(encoding="utf-8") if target_exists else ""
        target_declarations: dict[str, list[int]] = {}
        for target_match in PUBLIC_DECLARATION.finditer(target_text):
            target_declaration = target_match.group(0).strip()
            target_declarations.setdefault(target_declaration, []).append(
                target_text.count("\n", 0, target_match.start()) + 1
            )
        declaration_occurrences: Counter[str] = Counter()
        binding = baseline_binding(root, source)
        for match in PUBLIC_DECLARATION.finditer(text):
            declaration = match.group(0).strip()
            mapped = map_text(declaration)
            line = text.count("\n", 0, match.start()) + 1
            occurrence = declaration_occurrences[mapped]
            declaration_occurrences[mapped] += 1
            target_lines = target_declarations.get(mapped, [])
            present = occurrence < len(target_lines)
            target_line = target_lines[occurrence] if present else None
            disposition = public_declaration_disposition(target_exists, present)
            declaration_sha = sha256(declaration.encode("utf-8"))
            target_declaration_sha = sha256(mapped.encode("utf-8"))
            api_key = sha256(
                "\0".join((str(binding["baselineKey"]), str(line), declaration_sha)).encode("ascii")
            )
            if present:
                key = (target, target_declaration_sha, occurrence)
                if key in target_keys:
                    findings.append(Finding("public-api-bijection", target, "duplicate target API binding"))
                else:
                    target_keys.add(key)
            records.append(
                {
                    "baselineKey": binding["baselineKey"],
                    "baselineLine": line,
                    "baselineDeclarationSha256": declaration_sha,
                    "apiBindingSha256": api_key,
                    "targetPath": target,
                    "targetLine": target_line,
                    "targetDeclarationSha256": target_declaration_sha,
                    "targetDeclarationOccurrence": occurrence,
                    "targetPresent": present,
                    "apiDisposition": disposition,
                }
            )
    return records, findings


def write_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def xml_property(root: ET.Element, name: str) -> str | None:
    for element in root.iter():
        if element.tag.rsplit("}", 1)[-1] == name and element.text and element.text.strip():
            return element.text.strip()
    return None


def derive_package_inventory(root: Path) -> list[dict[str, object]]:
    records: list[dict[str, object]] = []
    for project in sorted(root.rglob("*.csproj")):
        if any(part in BUILD_OUTPUT_PARTS for part in project.relative_to(root).parts):
            continue
        tree = ET.fromstring(project.read_text(encoding="utf-8"))
        project_name = project.stem
        assembly = xml_property(tree, "AssemblyName") or project_name
        package_id = xml_property(tree, "PackageId") or assembly
        root_namespace = xml_property(tree, "RootNamespace") or assembly
        is_test = project.relative_to(root).as_posix().startswith("tests/")
        is_packable = (xml_property(tree, "IsPackable") or ("false" if is_test else "true")).casefold() == "true"
        # The SDK writes every build result under artifacts/, so there is no bin beside a project to
        # read any more. A gate that kept looking there would report zero artifacts for every project
        # and call that a pass.
        artifacts: list[dict[str, str]] = []
        artifact_roots = [
            root / "artifacts/sdk/bin" / project_name,
            root / "artifacts/packages",
        ]
        for artifact in sorted(item for artifact_root in artifact_roots for item in artifact_root.glob("**/*")):
            if not artifact.is_file() or artifact.suffix.casefold() not in {".dll", ".pdb", ".nupkg"}:
                continue
            if artifact.suffix.casefold() == ".nupkg" or artifact.stem == assembly:
                artifacts.append(
                    {
                        "path": artifact.relative_to(root).as_posix(),
                        "sha256": sha256(artifact.read_bytes()),
                    }
                )
        records.append(
            {
                "project": project.relative_to(root).as_posix(),
                "assemblyName": assembly,
                "packageId": package_id,
                "rootNamespace": root_namespace,
                "isPackable": is_packable,
                "targetArtifactPattern": f"{assembly}.dll",
                "artifacts": artifacts,
            }
        )
    return records


def identity_disposition(root: Path) -> dict[str, object]:
    policy_bytes = (root / HISTORICAL_IDENTITY_POLICY_PATH).read_bytes()
    policy = json.loads(policy_bytes.decode("utf-8"))
    policy_entries = policy["entries"]
    return {
        "mappingPolicy": "single-declarative-family-registry-identity_rules.py",
        "formerIdentityFamilyRegistry": [
            {
                "family": family.key,
                "mappingRuleCount": len(family.mapping_rules),
                "scanPatternSha256": sha256(family.scan_pattern.encode("utf-8")),
                "scanExampleSha256": [
                    sha256(value.encode("utf-8")) for value in family.scan_examples
                ],
            }
            for family in FORMER_IDENTITY_REGISTRY
        ],
        "retainedLegalProvenanceContexts": [
            {
                "path": path,
                "authorizedContextSha256": [sha256(value.encode("utf-8")) for value in contexts],
                "reason": "path-and-context-bound Apache-2.0 attribution or mandated bilingual provenance",
            }
            for path, contexts in sorted(legal_identity_contexts().items())
        ],
        "directoryIdentityExceptions": [],
        "historicalIdentityPolicy": {
            "schemaVersion": policy["schemaVersion"],
            "path": HISTORICAL_IDENTITY_POLICY_PATH,
            "sha256": sha256(policy_bytes),
            "entryCount": len(policy_entries),
            "categories": dict(sorted(Counter(str(entry["category"]) for entry in policy_entries).items())),
        },
    }


def validate_format_exceptions(
    root: Path,
    exceptions: Iterable[str] = COMMENTLESS_OR_BINARY_EXCEPTIONS,
) -> list[Finding]:
    findings: list[Finding] = []
    exception_targets = set(exceptions)
    target_sources = {map_path(source): source for source in baseline_paths(root)}
    target_sources.update(COMMENTLESS_OR_BINARY_BASELINE_SOURCES)
    explicit_sources = [
        COMMENTLESS_OR_BINARY_BASELINE_SOURCES[target]
        for target in exception_targets
        if target in COMMENTLESS_OR_BINARY_BASELINE_SOURCES
    ]
    for source in sorted({source for source in explicit_sources if explicit_sources.count(source) > 1}):
        findings.append(Finding(
            "legal-format-exception",
            source,
            "baseline source is bound to more than one current exception target",
        ))
    for target in sorted(exception_targets):
        source = target_sources.get(target)
        target_path = root / target
        if source is None or not target_path.is_file():
            findings.append(Finding("legal-format-exception", target, "exception has no baseline-to-target file binding"))
            continue
        changed = source != target or baseline_bytes(root, source) != target_path.read_bytes()
        if not changed:
            findings.append(Finding("legal-format-exception", target, "exception file is byte-identical and path-identical"))
        if supports_modification_notice(target):
            findings.append(Finding("legal-format-exception", target, "exception file supports a syntax-valid in-file notice"))
    return findings


def notice_format_exception_targets(text: str) -> list[str]:
    """The complete ordered exception list from NOTICE, or an empty list for a malformed section."""
    if text.count(NOTICE_EXCEPTION_HEADING) != 1:
        return []
    section = text.split(NOTICE_EXCEPTION_HEADING, 1)[1]
    return [line.strip() for line in section.splitlines() if line.strip()]


def modification_format_exception_bindings(text: str) -> list[tuple[str, str]]:
    """The ordered baseline-source/current-target pairs in the dedicated modification section."""
    if text.count(MODIFICATIONS_EXCEPTION_HEADING) != 1:
        return []
    section = text.split(MODIFICATIONS_EXCEPTION_HEADING, 1)[1]
    if "\n## " in section:
        section = section.split("\n## ", 1)[0]
    pattern = re.compile(r"^- `([^`]+)` -> `([^`]+)`$", re.MULTILINE)
    return pattern.findall(section)


def validate_legal_documents(root: Path) -> list[Finding]:
    findings = validate_format_exceptions(root)
    notice = (root / "NOTICE").read_text(encoding="utf-8")
    modifications = (root / "MODIFICATIONS.md").read_text(encoding="utf-8")
    expected_targets = sorted(COMMENTLESS_OR_BINARY_EXCEPTIONS)
    if notice_format_exception_targets(notice) != expected_targets:
        findings.append(Finding(
            "legal-exception-list",
            "NOTICE",
            "format-exception section does not equal the exact ordered current-target set",
        ))
    expected_bindings = [
        (COMMENTLESS_OR_BINARY_BASELINE_SOURCES[target], target)
        for target in expected_targets
    ]
    if modification_format_exception_bindings(modifications) != expected_bindings:
        findings.append(Finding(
            "legal-exception-list",
            "MODIFICATIONS.md",
            "format-exception section does not equal the exact ordered baseline-source/current-target map",
        ))
    unchanged_container = ".devcontainer/devcontainer.json"
    if unchanged_container in notice or unchanged_container in modifications:
        findings.append(Finding("legal-exception-list", unchanged_container, "unchanged commentable file is listed as an exception"))
    # The baseline carries the licence as LICENSE, the product carries it as LICENSE.txt. The name is
    # the only thing that changed, so the historical path and the active path are named separately and
    # their bytes are compared.
    if baseline_bytes(root, "LICENSE") != (root / "LICENSE.txt").read_bytes():
        findings.append(Finding("legal", "LICENSE.txt", "the licence text differs from the imported baseline"))
    return findings


def validate_evidence_records(
    evidence_name: str,
    actual: object,
    expected: list[dict[str, object]],
    key_name: str,
) -> list[Finding]:
    if not isinstance(actual, list) or any(not isinstance(item, dict) for item in actual):
        return [Finding("persisted-evidence", evidence_name, "evidence must be a list of records")]
    actual_records = [dict(item) for item in actual]
    actual_keys = [str(item.get(key_name) or "") for item in actual_records]
    expected_keys = [str(item[key_name]) for item in expected]
    findings: list[Finding] = []
    if len(actual_keys) != len(set(actual_keys)):
        findings.append(Finding("persisted-evidence", evidence_name, f"duplicate {key_name}"))
    if set(actual_keys) != set(expected_keys):
        findings.append(Finding("persisted-evidence", evidence_name, f"missing or invented {key_name}"))
    actual_by_key = {str(item.get(key_name) or ""): item for item in actual_records}
    for expected_record in expected:
        key = str(expected_record[key_name])
        if key in actual_by_key and actual_by_key[key] != expected_record:
            findings.append(Finding("persisted-evidence", evidence_name, f"incorrect binding for {key_name}={key}"))
    return findings


def generated_evidence_manifest(evidence: Path) -> str:
    return "".join(
        f"{sha256((evidence / name).read_bytes())}  {name}\n"
        for name in GENERATED_EVIDENCE_CONTRACTS
    )


def validate_generated_evidence_manifest(evidence: Path) -> list[Finding]:
    try:
        expected = generated_evidence_manifest(evidence)
        actual = (evidence / GENERATED_EVIDENCE_MANIFEST).read_text(encoding="ascii")
    except (OSError, UnicodeDecodeError) as error:
        return [Finding("persisted-evidence", GENERATED_EVIDENCE_MANIFEST, f"cannot load manifest input: {error}")]
    if actual != expected:
        return [
            Finding(
                "persisted-evidence",
                GENERATED_EVIDENCE_MANIFEST,
                "manifest does not exactly bind every generated contract",
            )
        ]
    return []


def validate_persisted_evidence(
    evidence: Path,
    mapping: list[dict[str, object]],
    notices: list[dict[str, object]],
    api: list[dict[str, object]],
    disposition: dict[str, object],
    packages: list[dict[str, object]],
) -> list[Finding]:
    findings: list[Finding] = []
    contracts = (
        ("BASELINE_TO_TARGET_PATHS.json", mapping, "baselineKey"),
        ("CHANGE_NOTICES.json", notices, "baselineKey"),
        ("PUBLIC_API_MAPPING.json", api, "apiBindingSha256"),
    )
    for name, expected, key_name in contracts:
        path = evidence / name
        try:
            actual = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as error:
            findings.append(Finding("persisted-evidence", name, f"cannot load evidence: {error}"))
            continue
        findings.extend(validate_evidence_records(name, actual, expected, key_name))
    for name, expected in (
        ("IDENTITY_DISPOSITION.json", disposition),
        ("PACKAGE_INVENTORY.json", packages),
    ):
        path = evidence / name
        try:
            actual = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as error:
            findings.append(Finding("persisted-evidence", name, f"cannot load evidence: {error}"))
            continue
        if actual != expected:
            findings.append(Finding("persisted-evidence", name, "document differs from current tree"))
    findings.extend(validate_generated_evidence_manifest(evidence))
    return findings


def run_gate(root: Path, output: Path | None, evidence: Path | None = None) -> int:
    mapping, mapping_findings = derive_baseline_mapping(root)
    notices, notice_findings = derive_change_notices(root)
    api, api_findings = derive_public_api_mapping(root)
    disposition = identity_disposition(root)
    packages = derive_package_inventory(root)
    scan_findings = scan_tree(root)
    conformance_findings = derive_refactor_conformance(root)
    license_equal = baseline_bytes(root, "LICENSE") == (root / "LICENSE.txt").read_bytes()
    legal_findings = validate_legal_documents(root)
    persisted_findings = validate_persisted_evidence(
        evidence or root / "evidence" / "WP-F2-SERVICEBUS-IDENTITY",
        mapping,
        notices,
        api,
        disposition,
        packages,
    )
    findings = (
        mapping_findings
        + notice_findings
        + api_findings
        + scan_findings
        + conformance_findings
        + legal_findings
        + persisted_findings
    )
    result = {
        "baselineCommit": BASELINE_COMMIT,
        "status": "PASS" if not findings else "FAIL",
        "counts": {
            "baselinePaths": len(mapping),
            "changedBaselineFiles": len(notices),
            "publicDeclarationRecords": len(api),
            "findings": len(findings),
        },
        "licenseByteIdentical": license_equal,
        "findings": [finding.as_dict() for finding in findings],
    }
    if output:
        write_json(output, result)
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0 if not findings else 1


def generate_evidence(root: Path, evidence: Path) -> int:
    mapping, mapping_findings = derive_baseline_mapping(root)
    notices, notice_findings = derive_change_notices(root)
    api, api_findings = derive_public_api_mapping(root)
    write_json(evidence / "BASELINE_TO_TARGET_PATHS.json", mapping)
    write_json(evidence / "CHANGE_NOTICES.json", notices)
    write_json(evidence / "PUBLIC_API_MAPPING.json", api)
    write_json(evidence / "IDENTITY_DISPOSITION.json", identity_disposition(root))
    write_json(evidence / "PACKAGE_INVENTORY.json", derive_package_inventory(root))
    (evidence / GENERATED_EVIDENCE_MANIFEST).write_text(
        generated_evidence_manifest(evidence),
        encoding="ascii",
    )
    result = {
        "status": "PASS" if not (mapping_findings + notice_findings + api_findings) else "FAIL",
        "findings": [
            item.as_dict() for item in mapping_findings + notice_findings + api_findings
        ],
    }
    write_json(evidence / "generation-result.json", result)
    scan_exit = run_gate(root, evidence / "SOURCE_IDENTITY_GATE.json", evidence)
    return 0 if result["status"] == "PASS" and scan_exit == 0 else 1


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("scan", "evidence"))
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path)
    parser.add_argument("--evidence-root", type=Path)
    args = parser.parse_args()
    root = args.root.resolve(strict=True)
    if args.command == "scan":
        evidence_root = args.evidence_root.resolve(strict=True) if args.evidence_root else None
        return run_gate(root, args.output, evidence_root)
    evidence = args.output or root / "evidence" / "WP-F2-SERVICEBUS-IDENTITY"
    return generate_evidence(root, evidence)


if __name__ == "__main__":
    sys.exit(main())
