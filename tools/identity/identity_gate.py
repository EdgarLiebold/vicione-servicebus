#!/usr/bin/env python3
"""Fail-closed source-tree and baseline-completeness gate for the identity fork."""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
import re
import stat
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
_GIT_OBJECT_FORMATS: dict[Path, str] = {}


@dataclass(frozen=True)
class Finding:
    gate: str
    path: str
    reason: str

    def as_dict(self) -> dict[str, str]:
        return {"gate": self.gate, "path": self.path, "reason": self.reason}


@dataclass(frozen=True)
class CSharpToken:
    text: str
    line: int


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

    text_contains_identity = bool(decoded and contains_former_identity(decoded))
    if text_contains_identity:
        findings.append(Finding("text-scan", path, "former technical identity in UTF-8 content"))
    # Successfully decoded UTF-8 without NUL bytes has no second UTF-16 ASCII view to discover: an
    # ASCII former identity is already visible in decoded. Keep all binary views for undecodable or
    # NUL-bearing data, where UTF-16 strings can genuinely be hidden from the UTF-8 text scan. This
    # avoids decoding large JSON/JSONL evidence four additional times without weakening the gate.
    if text_contains_identity or (
        (not decoded or b"\0" in scan_data) and contains_former_identity_bytes(scan_data)
    ):
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


def regular_candidate_bytes(
    root: Path,
    path: str,
    candidates: Mapping[str, tuple[str, str, bytes]] | None = None,
) -> bytes:
    snapshot = candidate_git_entries(root) if candidates is None else candidates
    entry = snapshot.get(path)
    if entry is None or entry[0] == "120000":
        raise ValueError(f"semantic input must be a regular Git candidate: {path}")
    return entry[2]


def load_historical_identity_policy(
    root: Path,
    candidates: Mapping[str, tuple[str, str, bytes]] | None = None,
) -> tuple[list[dict[str, object]], list[Finding]]:
    try:
        document = json.loads(regular_candidate_bytes(root, HISTORICAL_IDENTITY_POLICY_PATH, candidates).decode("utf-8"))
    except (ValueError, UnicodeDecodeError, json.JSONDecodeError) as error:
        return [], [Finding("historical-identity-policy", HISTORICAL_IDENTITY_POLICY_PATH, f"cannot load policy: {error}")]
    if not isinstance(document, dict) or document.get("schemaVersion") != HISTORICAL_IDENTITY_POLICY_SCHEMA_VERSION:
        return [], [Finding("historical-identity-policy", HISTORICAL_IDENTITY_POLICY_PATH, "unsupported schema")]
    records = document.get("entries")
    if not isinstance(records, list) or any(not isinstance(record, dict) for record in records):
        return [], [Finding("historical-identity-policy", HISTORICAL_IDENTITY_POLICY_PATH, "entries must be records")]
    return [dict(record) for record in records], []


def scan_tree(root: Path) -> list[Finding]:
    candidates = candidate_git_entries(root)
    candidate_bytes = {
        path: entry[2]
        for path, entry in candidates.items()
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
    records, load_findings = load_historical_identity_policy(root, candidates)
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


def git_object_format(root: Path) -> str:
    cached = _GIT_OBJECT_FORMATS.get(root)
    if cached is not None:
        return cached
    object_format = git(root, "rev-parse", "--show-object-format").decode("ascii").strip()
    if object_format not in {"sha1", "sha256"}:
        raise ValueError(f"unsupported Git object format: {object_format}")
    _GIT_OBJECT_FORMATS[root] = object_format
    return object_format


def git_blob_oid(root: Path, data: bytes) -> str:
    payload = f"blob {len(data)}\0".encode("ascii") + data
    return hashlib.new(git_object_format(root), payload).hexdigest()


def candidate_git_entry(root: Path, path: Path) -> tuple[str, str, bytes]:
    if path.is_symlink():
        data = os.readlink(path).encode("utf-8", errors="surrogateescape")
        mode = "120000"
    elif path.is_file():
        data = path.read_bytes()
        mode = "100755" if path.stat().st_mode & stat.S_IXUSR else "100644"
    else:
        raise ValueError(f"candidate is not a Git blob: {path.relative_to(root).as_posix()}")
    return mode, git_blob_oid(root, data), data


def candidate_git_entries(root: Path) -> dict[str, tuple[str, str, bytes]]:
    """Snapshot only tracked or non-ignored untracked blobs that Git could commit."""
    return {
        path.relative_to(root).as_posix(): candidate_git_entry(root, path)
        for path in commit_candidate_files(root)
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


def commit_tree(root: Path, commit: str) -> str:
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("commit must be a full lowercase Git object id")
    tree = git(root, "rev-parse", f"{commit}^{{tree}}").decode("ascii").strip()
    if not re.fullmatch(r"[0-9a-f]{40}", tree):
        raise ValueError("resolved tree must be a full lowercase Git object id")
    return tree


def validate_terminal_baseline_records(
    records: Iterable[Mapping[str, object]],
    expected_keys: set[str],
    actual_tree_entries: Mapping[str, tuple[str, str]],
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
        actual_entry = actual_tree_entries.get(target)
        exists = actual_entry is not None
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
            if any(field in record for field in ("targetSha256", "targetGitMode", "targetGitBlobOid")):
                findings.append(Finding("baseline-terminal", target, "retired target carries live blob identity"))
        else:
            expected_path_disposition = "UNCHANGED" if disposition == "MAPPED_EXISTING" else "RENAMED"
            if record.get("pathDisposition") != expected_path_disposition:
                findings.append(Finding("baseline-terminal", target, "live path disposition is inconsistent"))
            if any(field in record for field in ("retirementCommit", "retirementEvidence", "retirementTree")):
                findings.append(Finding("baseline-terminal", target, "live target carries retirement provenance"))
            if not exists or record.get("targetExists") is not True:
                findings.append(Finding("baseline-terminal", target, "live target is absent"))
            if actual_entry is not None:
                actual_mode, actual_blob_oid = actual_entry
                if record.get("targetGitMode") != actual_mode:
                    findings.append(Finding("baseline-terminal", target, "live target Git mode is stale"))
                if record.get("targetGitBlobOid") != actual_blob_oid:
                    findings.append(Finding("baseline-terminal", target, "live target Git blob oid is stale"))
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
    retirement_trees: dict[str, str] = {}
    actual_tree_entries: dict[str, tuple[str, str]] = {}
    candidates = candidate_git_entries(root)
    for source in baseline_paths(root):
        target = map_path(source)
        if target in seen_targets:
            findings.append(
                Finding("path-bijection", target, f"target also mapped from {seen_targets[target]}")
            )
        seen_targets[target] = source
        exists = target in candidates
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
        if exists:
            target_mode, target_blob_oid, target_data = candidates[target]
            actual_tree_entries[target] = (target_mode, target_blob_oid)
            record["targetSha256"] = sha256(target_data)
            record["targetGitMode"] = target_mode
            record["targetGitBlobOid"] = target_blob_oid
        elif not exists:
            deletion_commit = deletion_commits.get(target) or deletion_commits.get(source)
            if source not in deleted_sources:
                findings.append(Finding("baseline-retirement", target, f"CHANGELIST has no deletion for {source}"))
            if not deletion_commit:
                findings.append(Finding("baseline-retirement", target, f"Git history has no deletion for {source}"))
            retirement_tree = None
            if deletion_commit:
                if deletion_commit not in retirement_trees:
                    retirement_trees[deletion_commit] = commit_tree(root, deletion_commit)
                retirement_tree = retirement_trees[deletion_commit]
            record.update({
                "retirementCommit": deletion_commit,
                "retirementEvidence": "ROOT_CHANGE_LIST",
                "retirementTree": retirement_tree,
            })
        records.append(record)
    findings.extend(validate_terminal_baseline_records(
        records,
        expected_keys={str(baseline_binding(root, source)["baselineKey"]) for source in baseline_paths(root)},
        actual_tree_entries=actual_tree_entries,
    ))
    return records, findings


def derive_change_notices(root: Path) -> tuple[list[dict[str, object]], list[Finding]]:
    records: list[dict[str, object]] = []
    findings: list[Finding] = []
    candidates = candidate_git_entries(root)
    for source in baseline_paths(root):
        target = map_path(source)
        candidate = candidates.get(target)
        if candidate is None:
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
        after = candidate[2]
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
    candidates = candidate_git_entries(root)
    for source in baseline_paths(root):
        target = map_path(source)
        candidate = candidates.get(target)
        if candidate is None or candidate[0] == "120000" or source in LEGAL_OR_PROVENANCE_PATHS:
            continue
        before = baseline_bytes(root, source)
        try:
            before_text = before.decode("utf-8")
        except UnicodeDecodeError:
            continue
        actual = candidate[2].decode("utf-8")
        if source.startswith("tests/"):
            findings.extend(test_sabotage_findings(map_text(before_text), actual, target))
    return findings


CSHARP_DECLARATION_MODIFIERS = frozenset({
    "abstract", "async", "extern", "file", "new", "override", "partial", "readonly",
    "required", "sealed", "static", "unsafe", "virtual", "volatile",
    "public", "private", "protected", "internal",
})
CSHARP_RESTRICTED_INTERFACE_MODIFIERS = frozenset({"private", "protected", "internal"})
CSHARP_TYPE_KEYWORDS = frozenset({"class", "enum", "interface", "record", "struct"})
CSHARP_ACCESSOR_KEYWORDS = frozenset({"get", "set", "init", "add", "remove"})


def csharp_tokens(text: str) -> list[CSharpToken]:
    """Tokenize declaration-relevant C# without treating comments or literal contents as code."""
    tokens: list[CSharpToken] = []
    index = 0
    line = 1
    length = len(text)
    multi_character = (
        ">>>=", "<<=", ">>=", "??=", "=>", "::", "?.", "??", "++", "--", "&&", "||",
        "==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<", ">>", "..",
    )

    def consume_quoted(start: int, quote_index: int, quote: str, verbatim: bool) -> int:
        nonlocal line
        quote_count = 1
        if quote == '"':
            while quote_index + quote_count < length and text[quote_index + quote_count] == '"':
                quote_count += 1
        cursor = quote_index + quote_count
        if quote == '"' and quote_count == 2:
            return cursor
        if quote == '"' and quote_count >= 3:
            marker = '"' * quote_count
            closing = text.find(marker, cursor)
            if closing < 0:
                line += text[start:].count("\n")
                return length
            end = closing + quote_count
            line += text[start:end].count("\n")
            return end
        while cursor < length:
            character = text[cursor]
            if character == "\n":
                line += 1
            if character == quote:
                if verbatim and cursor + 1 < length and text[cursor + 1] == quote:
                    cursor += 2
                    continue
                return cursor + 1
            if character == "\\" and not verbatim:
                cursor += 2
            else:
                cursor += 1
        return length

    while index < length:
        character = text[index]
        if character.isspace():
            if character == "\n":
                line += 1
            index += 1
            continue
        if text.startswith("//", index):
            newline = text.find("\n", index + 2)
            index = length if newline < 0 else newline
            continue
        if text.startswith("/*", index):
            closing = text.find("*/", index + 2)
            end = length if closing < 0 else closing + 2
            line += text[index:end].count("\n")
            index = end
            continue

        literal_quote = -1
        if character == "'":
            literal_quote = index
        elif character == '"':
            literal_quote = index
        elif character in "@$":
            cursor = index
            while cursor < length and text[cursor] in "@$" and cursor - index < 3:
                cursor += 1
            if cursor < length and text[cursor] == '"':
                literal_quote = cursor
        if literal_quote >= 0:
            token_line = line
            verbatim = "@" in text[index:literal_quote]
            end = consume_quoted(index, literal_quote, text[literal_quote], verbatim)
            tokens.append(CSharpToken(text[index:end], token_line))
            index = end
            continue

        if character == "@" and index + 1 < length and (text[index + 1].isalpha() or text[index + 1] == "_"):
            end = index + 2
            while end < length and (text[end].isalnum() or text[end] == "_"):
                end += 1
            tokens.append(CSharpToken(text[index:end], line))
            index = end
            continue
        if character.isalpha() or character == "_":
            end = index + 1
            while end < length and (text[end].isalnum() or text[end] == "_"):
                end += 1
            tokens.append(CSharpToken(text[index:end], line))
            index = end
            continue
        if character.isdigit():
            end = index + 1
            while end < length and (text[end].isalnum() or text[end] in "._"):
                end += 1
            tokens.append(CSharpToken(text[index:end], line))
            index = end
            continue
        operator = next((item for item in multi_character if text.startswith(item, index)), character)
        tokens.append(CSharpToken(operator, line))
        index += len(operator)
    return tokens


def canonical_csharp_declaration(prefix: str, tokens: Iterable[CSharpToken]) -> str:
    return prefix + " " + " ".join(token.text for token in tokens).strip()


def csharp_declaration_prefix_start(tokens: list[CSharpToken], index: int) -> int:
    """Return the first token of contiguous modifiers, attributes, and attribute directives."""
    start = index
    included_attribute = False
    while start > 0:
        previous = start
        while start > 0 and tokens[start - 1].text in CSHARP_DECLARATION_MODIFIERS:
            start -= 1
        if start > 0 and tokens[start - 1].text == "]":
            depth = 1
            cursor = start - 2
            while cursor >= 0:
                if tokens[cursor].text == "]":
                    depth += 1
                elif tokens[cursor].text == "[":
                    depth -= 1
                    if depth == 0:
                        start = cursor
                        included_attribute = True
                        break
                cursor -= 1
        if start > 0:
            directive_line = tokens[start - 1].line
            line_start = start - 1
            while line_start > 0 and tokens[line_start - 1].line == directive_line:
                line_start -= 1
            if tokens[line_start].text == "#":
                attribute_before_directives = included_attribute
                lookbehind = line_start
                while not attribute_before_directives and lookbehind > 0:
                    if tokens[lookbehind - 1].text == "]":
                        attribute_before_directives = True
                        break
                    preceding_line = tokens[lookbehind - 1].line
                    preceding_start = lookbehind - 1
                    while preceding_start > 0 and tokens[preceding_start - 1].line == preceding_line:
                        preceding_start -= 1
                    if tokens[preceding_start].text != "#":
                        break
                    lookbehind = preceding_start
                if attribute_before_directives:
                    start = line_start
        if start == previous:
            break
    return start


def csharp_preprocessor_contexts(
    text: str,
    tokens: Iterable[CSharpToken] | None = None,
) -> dict[int, tuple[str, ...]]:
    """Bind every declaration to its lexical conditional-compilation context.

    The identity gate deliberately projects every branch instead of pretending to reproduce MSBuild and
    compiler evaluation. This is a conservative source contract: dormant declarations remain bound, and
    changing a directive, branch, or file-local define changes the declaration identity.
    """
    contexts: dict[int, tuple[str, ...]] = {}
    token_lines: dict[int, list[str]] = {}
    for token in tokens if tokens is not None else csharp_tokens(text):
        token_lines.setdefault(token.line, []).append(token.text)
    branches: list[list[str]] = []
    defines: set[str] = set()
    for line_number in range(1, len(text.splitlines()) + 1):
        line_tokens = token_lines.get(line_number, [])
        if len(line_tokens) >= 2 and line_tokens[0] == "#":
            kind = line_tokens[1]
            argument = " ".join(line_tokens[2:])
            if kind == "if":
                branches.append([f"if({argument})"])
            elif kind == "elif" and branches:
                branches[-1].append(f"elif({argument})")
            elif kind == "else" and branches:
                branches[-1].append("else")
            elif kind == "endif" and branches:
                branches.pop()
            elif kind == "define" and argument:
                defines.add(argument)
            elif kind == "undef" and argument:
                defines.discard(argument)
        contexts[line_number] = tuple(
            [
                *(f"define({name})" for name in sorted(defines)),
                *(" -> ".join(branch) for branch in branches),
            ]
        )
    return contexts


def csharp_public_declarations(text: str) -> list[tuple[str, int]]:
    """Project explicit and language-defined implicit public C# declarations deterministically."""
    tokens = csharp_tokens(text)
    preprocessor_contexts = csharp_preprocessor_contexts(text, tokens)
    declarations: list[tuple[str, int]] = []

    def bind_context(declaration: str, line: int, token_index: int) -> str:
        owner = owner_context(token_index)
        context = preprocessor_contexts.get(line, ())
        prefix = ("owner " + owner + " ") if owner else ""
        if context:
            prefix += "conditional " + " && ".join(context) + " "
        return prefix + declaration

    brace_stack: list[int] = []
    brace_pairs: dict[int, int] = {}
    has_conditional_directive = any(
        token.text == "#"
        and index + 1 < len(tokens)
        and tokens[index + 1].text in {"if", "elif", "else", "endif"}
        for index, token in enumerate(tokens)
    )
    branch_structure_unbalanced = False
    for index, token in enumerate(tokens):
        if token.text == "{":
            brace_stack.append(index)
        elif token.text == "}":
            if not brace_stack:
                if has_conditional_directive:
                    branch_structure_unbalanced = True
                continue
            brace_pairs[brace_stack.pop()] = index
    if brace_stack and has_conditional_directive:
        branch_structure_unbalanced = True

    owner_regions: list[tuple[int, int, str]] = []
    for keyword_index, token in enumerate(tokens):
        if token.text == "namespace":
            boundary = next(
                (
                    cursor
                    for cursor in range(keyword_index + 1, len(tokens))
                    if tokens[cursor].text in {"{", ";"}
                ),
                None,
            )
            if boundary is None:
                continue
            name = " ".join(item.text for item in tokens[keyword_index + 1:boundary])
            end = brace_pairs.get(boundary, len(tokens)) if tokens[boundary].text == "{" else len(tokens)
            owner_regions.append((boundary, end, "namespace " + name))
    seen_type_bodies: set[int] = set()
    for keyword_index, token in enumerate(tokens):
        if token.text not in CSHARP_TYPE_KEYWORDS:
            continue
        body_start = next(
            (
                cursor
                for cursor in range(keyword_index + 1, len(tokens))
                if tokens[cursor].text in {"{", ";"}
            ),
            None,
        )
        if (
            body_start is None
            or tokens[body_start].text != "{"
            or body_start not in brace_pairs
            or body_start in seen_type_bodies
        ):
            continue
        seen_type_bodies.add(body_start)
        header_start = keyword_index
        while header_start > 0 and tokens[header_start - 1].text not in {"{", "}", ";"}:
            header_start -= 1
        header = " ".join(item.text for item in tokens[header_start:body_start])
        owner_regions.append((body_start, brace_pairs[body_start], "type " + header))

    def owner_context(token_index: int) -> str:
        owners = [
            (start, end, name)
            for start, end, name in owner_regions
            if start < token_index < end
        ]
        owners.sort(key=lambda item: (-(item[1] - item[0]), item[0]))
        return " :: ".join(name for _, _, name in owners)

    def accessor_shape(body_start: int) -> str:
        body_end = brace_pairs.get(body_start, body_start)
        accessors: list[str] = []

        def accessor_identity(segment: list[CSharpToken], accessor: int) -> str:
            start = csharp_declaration_prefix_start(segment, accessor)
            context = preprocessor_contexts.get(segment[accessor].line, ())
            prefix = "conditional " + " && ".join(context) + " " if context else ""
            return prefix + " ".join(item.text for item in segment[start:accessor + 1])

        segment_start = body_start + 1
        cursor = segment_start
        while cursor < body_end:
            value = tokens[cursor].text
            if value == "{" and cursor in brace_pairs:
                segment = tokens[segment_start:cursor]
                accessor = next(
                    (index for index, item in enumerate(segment) if item.text in CSHARP_ACCESSOR_KEYWORDS),
                    None,
                )
                if accessor is not None:
                    accessors.append(accessor_identity(segment, accessor))
                cursor = brace_pairs[cursor] + 1
                segment_start = cursor
                continue
            if value in {";", "=>"}:
                segment = tokens[segment_start:cursor]
                accessor = next(
                    (index for index, item in enumerate(segment) if item.text in CSHARP_ACCESSOR_KEYWORDS),
                    None,
                )
                if accessor is not None:
                    accessors.append(accessor_identity(segment, accessor))
                segment_start = cursor + 1
            cursor += 1
        return " ; ".join(accessors)

    # Explicit externally visible declarations are token based, so modifier whitespace and line breaks
    # are irrelevant. Property/indexer/event accessor shape is part of the public contract, while method
    # and type bodies are deliberately excluded.
    for visibility_index, token in enumerate(tokens):
        if token.text not in {"public", "protected"}:
            continue
        start = csharp_declaration_prefix_start(tokens, visibility_index)
        modifier_end = visibility_index + 1
        while modifier_end < len(tokens) and tokens[modifier_end].text in CSHARP_DECLARATION_MODIFIERS:
            modifier_end += 1
        modifiers = {item.text for item in tokens[start:modifier_end]}
        if "private" in modifiers or (
            modifier_end < len(tokens) and tokens[modifier_end].text in CSHARP_ACCESSOR_KEYWORDS
        ):
            continue
        parentheses = 0
        brackets = 0
        angles = 0
        saw_parentheses = False
        end = len(tokens)
        property_body: int | None = None
        cursor = visibility_index + 1
        while cursor < len(tokens):
            value = tokens[cursor].text
            if value == "(":
                parentheses += 1
                saw_parentheses = True
            elif value == ")":
                if parentheses == 0 and not saw_parentheses:
                    end = cursor
                    break
                parentheses = max(0, parentheses - 1)
            elif value == "[":
                brackets += 1
            elif value == "]":
                brackets = max(0, brackets - 1)
            elif value == "<":
                angles += 1
            elif value == ">":
                angles = max(0, angles - 1)
            elif value == ">>":
                angles = max(0, angles - 2)
            elif parentheses == 0 and brackets == 0 and value == "{" and cursor in brace_pairs:
                signature_values = {item.text for item in tokens[start:cursor]}
                if "=" in signature_values:
                    cursor = brace_pairs[cursor]
                else:
                    end = cursor
                    if not saw_parentheses and not signature_values.intersection(CSHARP_TYPE_KEYWORDS):
                        property_body = cursor
                    break
            elif parentheses == 0 and brackets == 0 and value in {";", "=>"}:
                end = cursor
                break
            cursor += 1
        signature = tokens[start:end]
        if signature:
            declaration = canonical_csharp_declaration("explicit", signature)
            if property_body is not None:
                declaration += " { " + accessor_shape(property_body) + " }"
            declarations.append((bind_context(declaration, token.line, visibility_index), token.line))

    raw_regions: list[dict[str, object]] = []
    for keyword_index, token in enumerate(tokens):
        if token.text not in CSHARP_TYPE_KEYWORDS:
            continue
        body_start = next(
            (
                cursor for cursor in range(keyword_index + 1, len(tokens))
                if tokens[cursor].text in {"{", ";"}
            ),
            None,
        )
        if body_start is None or tokens[body_start].text != "{" or body_start not in brace_pairs:
            continue
        header_start = keyword_index
        while header_start > 0 and tokens[header_start - 1].text not in {"{", "}", ";"}:
            header_start -= 1
        raw_regions.append({
            "kind": token.text,
            "keyword": keyword_index,
            "bodyStart": body_start,
            "bodyEnd": brace_pairs[body_start],
            "headerStart": header_start,
            "headerTokens": tokens[header_start:body_start],
            "public": False,
        })

    raw_regions.sort(key=lambda region: int(region["bodyStart"]))
    for region in raw_regions:
        keyword_index = int(region["keyword"])
        parents = [
            candidate for candidate in raw_regions
            if int(candidate["bodyStart"]) < keyword_index < int(candidate["bodyEnd"])
        ]
        parent = min(parents, key=lambda candidate: int(candidate["bodyEnd"]) - int(candidate["bodyStart"]), default=None)
        header_values = {token.text for token in region["headerTokens"]}  # type: ignore[index]
        implicit_public = (
            parent is not None
            and parent["kind"] == "interface"
            and parent["public"] is True
            and not header_values.intersection(CSHARP_RESTRICTED_INTERFACE_MODIFIERS)
        )
        region["public"] = (
            "public" in header_values
            or ("protected" in header_values and "private" not in header_values)
            or implicit_public
        )

    def implicit_interface_members(region: Mapping[str, object]) -> None:
        cursor = int(region["bodyStart"]) + 1
        body_end = int(region["bodyEnd"])
        segment_start = cursor
        while cursor < body_end:
            value = tokens[cursor].text
            if value == "{" and cursor in brace_pairs:
                segment = tokens[segment_start:cursor]
                values = {item.text for item in segment}
                if (
                    segment
                    and "public" not in values
                    and not values.intersection(CSHARP_RESTRICTED_INTERFACE_MODIFIERS)
                ):
                    declaration = canonical_csharp_declaration("implicit-interface", segment)
                    accessors = accessor_shape(cursor)
                    if accessors:
                        declaration += " { " + accessors + " }"
                    declarations.append((bind_context(declaration, segment[0].line, segment_start), segment[0].line))
                cursor = brace_pairs[cursor] + 1
                segment_start = cursor
                continue
            if value == ";":
                segment = tokens[segment_start:cursor]
                values = {item.text for item in segment}
                if (
                    segment
                    and "public" not in values
                    and not values.intersection(CSHARP_RESTRICTED_INTERFACE_MODIFIERS)
                ):
                    declaration = canonical_csharp_declaration("implicit-interface", segment)
                    declarations.append((bind_context(declaration, segment[0].line, segment_start), segment[0].line))
                segment_start = cursor + 1
            cursor += 1

    def implicit_enum_members(region: Mapping[str, object]) -> None:
        cursor = int(region["bodyStart"]) + 1
        body_end = int(region["bodyEnd"])
        segment_start = cursor
        parentheses = 0
        brackets = 0
        braces = 0
        while cursor <= body_end:
            value = tokens[cursor].text if cursor < body_end else ","
            if value == "(":
                parentheses += 1
            elif value == ")":
                parentheses = max(0, parentheses - 1)
            elif value == "[":
                brackets += 1
            elif value == "]":
                brackets = max(0, brackets - 1)
            elif value == "{":
                braces += 1
            elif value == "}":
                braces = max(0, braces - 1)
            elif value == "," and parentheses == 0 and brackets == 0 and braces == 0:
                segment = tokens[segment_start:cursor]
                if segment:
                    declaration = canonical_csharp_declaration("implicit-enum", segment)
                    declarations.append((bind_context(declaration, segment[0].line, segment_start), segment[0].line))
                segment_start = cursor + 1
            cursor += 1

    for region in raw_regions:
        if region["public"] is not True:
            continue
        if region["kind"] == "interface":
            implicit_interface_members(region)
        elif region["kind"] == "enum":
            implicit_enum_members(region)

    if branch_structure_unbalanced:
        token_stream = " ".join(token.text for token in tokens)
        declarations.append(("all-branch-structure-sha256 " + sha256(token_stream.encode("utf-8")), 1))

    # Preserve traversal order for declarations sharing a line. Sorting by declaration text here makes a
    # baseline rename capable of changing the zip pairing used by derive_public_api_mapping.
    return sorted(declarations, key=lambda item: item[1])


def current_product_public_declarations(
    root: Path,
) -> tuple[list[dict[str, object]], list[Finding]]:
    records: list[dict[str, object]] = []
    findings: list[Finding] = []
    for target, (mode, _, data) in candidate_git_entries(root).items():
        if not target.startswith("src/") or not target.endswith(".cs"):
            continue
        if mode == "120000":
            findings.append(Finding("public-api-current", target, "C# source cannot be a symbolic link"))
            continue
        try:
            text = data.decode("utf-8")
        except UnicodeDecodeError:
            findings.append(Finding("public-api-current", target, "current C# source is not UTF-8"))
            continue
        occurrences: Counter[str] = Counter()
        for declaration, line in csharp_public_declarations(text):
            declaration_sha = sha256(declaration.encode("utf-8"))
            occurrence = occurrences[declaration_sha]
            occurrences[declaration_sha] += 1
            records.append(
                {
                    "targetPath": target,
                    "targetLine": line,
                    "targetDeclarationSha256": declaration_sha,
                    "targetDeclarationOccurrence": occurrence,
                }
            )
    return records, findings


def public_target_key(record: Mapping[str, object]) -> tuple[str, str, int] | None:
    path = record.get("targetPath")
    declaration_sha = record.get("targetDeclarationSha256")
    occurrence = record.get("targetDeclarationOccurrence")
    if (
        not isinstance(path, str)
        or not isinstance(declaration_sha, str)
        or not re.fullmatch(r"[0-9a-f]{64}", declaration_sha)
        or not isinstance(occurrence, int)
        or occurrence < 0
    ):
        return None
    return path, declaration_sha, occurrence


def validate_current_public_api_records(
    records: Iterable[Mapping[str, object]],
    expected_current_keys: set[tuple[str, str, int]],
) -> list[Finding]:
    bound_keys: list[tuple[str, str, int]] = []
    findings: list[Finding] = []
    for record in records:
        disposition = record.get("apiDisposition")
        target = str(record.get("targetPath") or "")
        if disposition == "CURRENT_ADDED":
            if record.get("targetPresent") is not True or not target.startswith("src/"):
                findings.append(Finding("public-api-current", target, "current-added declaration is not live product API"))
            if any(
                field in record
                for field in ("baselineKey", "baselineLine", "baselineDeclarationSha256")
            ):
                findings.append(Finding("public-api-current", target, "current-added declaration carries baseline identity"))
        if target.startswith("src/") and record.get("targetPresent") is True:
            if disposition not in {"MAPPED_PRESENT", "CURRENT_ADDED"}:
                findings.append(Finding("public-api-current", target, "live product declaration has invalid disposition"))
            key = public_target_key(record)
            if key is None:
                findings.append(Finding("public-api-current", target, "live product declaration key is invalid"))
            else:
                bound_keys.append(key)
    if len(bound_keys) != len(set(bound_keys)):
        findings.append(Finding("public-api-current", "PUBLIC_API_MAPPING.json", "duplicate current declaration binding"))
    if set(bound_keys) != expected_current_keys:
        findings.append(Finding("public-api-current", "PUBLIC_API_MAPPING.json", "missing or invented current declaration binding"))
    return findings


def derive_public_api_mapping(root: Path) -> tuple[list[dict[str, object]], list[Finding]]:
    records: list[dict[str, object]] = []
    findings: list[Finding] = []
    target_keys: set[tuple[str, str, int]] = set()
    candidates = candidate_git_entries(root)
    for source in baseline_paths(root):
        if not source.endswith(".cs"):
            continue
        data = baseline_bytes(root, source)
        try:
            text = data.decode("utf-8")
        except UnicodeDecodeError:
            continue
        target = map_path(source)
        target_candidate = candidates.get(target)
        target_exists = target_candidate is not None
        try:
            target_text = target_candidate[2].decode("utf-8") if target_candidate is not None else ""
        except UnicodeDecodeError:
            target_text = ""
        target_declarations: dict[str, list[int]] = {}
        for target_declaration, target_line in csharp_public_declarations(target_text):
            target_declarations.setdefault(target_declaration, []).append(
                target_line
            )
        baseline_declarations = csharp_public_declarations(text)
        mapped_declarations = csharp_public_declarations(map_text(text))
        if len(baseline_declarations) != len(mapped_declarations):
            findings.append(Finding(
                "public-api-mapping",
                source,
                "identity mapping changed the number of projected declarations",
            ))
            continue
        declaration_occurrences: Counter[str] = Counter()
        binding = baseline_binding(root, source)
        for (declaration, line), (mapped, mapped_line) in zip(
            baseline_declarations,
            mapped_declarations,
        ):
            if mapped_line != line:
                findings.append(Finding(
                    "public-api-mapping",
                    source,
                    "identity mapping changed a declaration source line",
                ))
                continue
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
    current_declarations, current_findings = current_product_public_declarations(root)
    findings.extend(current_findings)
    current_keys: set[tuple[str, str, int]] = set()
    for declaration in current_declarations:
        key = public_target_key(declaration)
        if key is None:
            findings.append(Finding("public-api-current", str(declaration.get("targetPath") or ""), "current declaration key is invalid"))
            continue
        current_keys.add(key)
        if key in target_keys:
            continue
        target = str(declaration["targetPath"])
        target_line = int(declaration["targetLine"])
        declaration_sha = str(declaration["targetDeclarationSha256"])
        occurrence = int(declaration["targetDeclarationOccurrence"])
        api_key = sha256(
            "\0".join(("CURRENT_ADDED", target, str(target_line), declaration_sha, str(occurrence))).encode(
                "utf-8", errors="surrogateescape"
            )
        )
        records.append(
            {
                "apiBindingSha256": api_key,
                "targetPath": target,
                "targetLine": target_line,
                "targetDeclarationSha256": declaration_sha,
                "targetDeclarationOccurrence": occurrence,
                "targetPresent": True,
                "apiDisposition": "CURRENT_ADDED",
            }
        )
    findings.extend(validate_current_public_api_records(records, current_keys))
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
    candidates = candidate_git_entries(root)
    for project_path, (mode, _, data) in sorted(candidates.items()):
        if not project_path.endswith(".csproj") or any(
            part in BUILD_OUTPUT_PARTS for part in Path(project_path).parts
        ):
            continue
        if mode == "120000":
            raise ValueError(f"project source must be a regular Git candidate: {project_path}")
        project = root / project_path
        tree = ET.fromstring(data.decode("utf-8"))
        project_name = project.stem
        assembly = xml_property(tree, "AssemblyName") or project_name
        package_id = xml_property(tree, "PackageId") or assembly
        root_namespace = xml_property(tree, "RootNamespace") or assembly
        is_test = project.relative_to(root).as_posix().startswith("tests/")
        is_packable = (xml_property(tree, "IsPackable") or ("false" if is_test else "true")).casefold() == "true"
        records.append(
            {
                "project": project.relative_to(root).as_posix(),
                "assemblyName": assembly,
                "packageId": package_id,
                "rootNamespace": root_namespace,
                "isPackable": is_packable,
                "targetArtifactPattern": f"{assembly}.dll",
                # Ignored build outputs are covered by ARTIFACT_GATE.json and its build-bound evidence.
                # This source inventory is intentionally reproducible from Git-candidate bytes alone.
                "artifacts": [],
            }
        )
    return records


def identity_disposition(root: Path) -> dict[str, object]:
    policy_bytes = regular_candidate_bytes(root, HISTORICAL_IDENTITY_POLICY_PATH)
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
    candidates = candidate_git_entries(root)
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
        target_entry = candidates.get(target)
        if source is None or target_entry is None or target_entry[0] == "120000":
            findings.append(Finding("legal-format-exception", target, "exception has no baseline-to-target file binding"))
            continue
        changed = source != target or baseline_bytes(root, source) != target_entry[2]
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
    candidates = candidate_git_entries(root)

    def legal_text(path: str) -> str:
        try:
            return regular_candidate_bytes(root, path, candidates).decode("utf-8")
        except (ValueError, UnicodeDecodeError) as error:
            findings.append(Finding("legal", path, str(error)))
            return ""

    notice = legal_text("NOTICE")
    modifications = legal_text("MODIFICATIONS.md")
    try:
        license_bytes = regular_candidate_bytes(root, "LICENSE.txt", candidates)
    except ValueError as error:
        findings.append(Finding("legal", "LICENSE.txt", str(error)))
        license_bytes = b""
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
    if baseline_bytes(root, "LICENSE") != license_bytes:
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
    license_equal = baseline_bytes(root, "LICENSE") == regular_candidate_bytes(root, "LICENSE.txt")
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
            "liveBaselineTargetsWithGitIdentity": sum(
                1
                for record in mapping
                if record.get("targetExists") is True
                and record.get("targetGitMode")
                and record.get("targetGitBlobOid")
            ),
            "currentProductPublicDeclarations": sum(
                1
                for record in api
                if record.get("targetPresent") is True
                and str(record.get("targetPath") or "").startswith("src/")
            ),
            "currentAddedPublicDeclarations": sum(
                1 for record in api if record.get("apiDisposition") == "CURRENT_ADDED"
            ),
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
