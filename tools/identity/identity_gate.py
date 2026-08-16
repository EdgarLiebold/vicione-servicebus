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
from typing import Iterable

from identity_rules import (
    BASELINE_COMMIT,
    COMMENTLESS_OR_BINARY_EXCEPTIONS,
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
_BASELINE_ARCHIVES: dict[Path, dict[str, bytes]] = {}
_BASELINE_TREE_ENTRIES: dict[Path, dict[str, tuple[str, str]]] = {}


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
        "README.md": (
            f"Dieses Repository ist ein vollständiger Fork von **{FORMER_PASCAL} 8.5.10**, Upstream-Commit `62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, aus dem Projekt [{FORMER_PASCAL}]({upstream_url}). Der übernommene und geänderte Bestand steht unter der **Apache License 2.0**; siehe [LICENSE](LICENSE), [NOTICE](NOTICE), [COPYRIGHT](COPYRIGHT) und [MODIFICATIONS.md](MODIFICATIONS.md).",
            f"This repository is a complete fork of **{FORMER_PASCAL} 8.5.10**, upstream commit `62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, from the [{FORMER_PASCAL}]({upstream_url}) project. The retained and modified code is licensed under the **Apache License 2.0**; see [LICENSE](LICENSE), [NOTICE](NOTICE), [COPYRIGHT](COPYRIGHT), and [MODIFICATIONS.md](MODIFICATIONS.md).",
        ),
        "NOTICE": (
            f"{FORMER_PASCAL}\nCopyright 2007-2024 Chris Patterson",
            f"This repository is a full fork of {FORMER_PASCAL} 8.5.10 at upstream commit",
        ),
        "COPYRIGHT": (),
        "MODIFICATIONS.md": (
            f"The baseline is the complete {FORMER_PASCAL} 8.5.10 source tree at upstream commit `62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, imported into the local fork baseline commit `1de4bf6eb45c406da3cd6f26bdab6ed6d5aeefbc` (tree `2b09d4e2b2e14289f06ba112ce1ae52e326a0307`).",
        ),
        "LICENSE": (),
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
        masked = masked.replace(context, " " * len(context), 1)
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


def scan_tree(root: Path) -> list[Finding]:
    findings: list[Finding] = []
    for candidate in commit_candidate_files(root):
        path = candidate.relative_to(root).as_posix()
        findings.extend(scan_entry(path, candidate.read_bytes()))
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
    for source in baseline_paths(root):
        target = map_path(source)
        if target in seen_targets:
            findings.append(
                Finding("path-bijection", target, f"target also mapped from {seen_targets[target]}")
            )
        seen_targets[target] = source
        target_path = root / target
        exists = target_path.exists() or target_path.is_symlink()
        if not exists:
            findings.append(Finding("baseline-census", target, f"missing target for baseline path {source}"))
        source_data = baseline_bytes(root, source)
        record: dict[str, object] = {
            **baseline_binding(root, source),
            "targetPath": target,
            "pathDisposition": "RENAMED" if source != target else "UNCHANGED",
            "baselineSha256": sha256(source_data),
            "targetExists": exists,
        }
        if exists and target_path.is_file():
            record["targetSha256"] = sha256(target_path.read_bytes())
        records.append(record)
    return records, findings


def derive_change_notices(root: Path) -> tuple[list[dict[str, object]], list[Finding]]:
    records: list[dict[str, object]] = []
    findings: list[Finding] = []
    for source in baseline_paths(root):
        target = map_path(source)
        target_path = root / target
        if not target_path.exists() or not target_path.is_file():
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
                "mechanism": "ROOT_CHANGE_LIST",
                "effective": True,
            }
        )
    return records, findings


def derive_refactor_conformance(root: Path) -> list[Finding]:
    """Prove every ordinary baseline text is only identity-mapped plus noticed."""
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
        expected = map_text(before_text)
        # Path.read_text performs universal-newline translation. Compare the
        # decoded bytes so CRLF baselines remain byte-for-byte auditable.
        actual = target_path.read_bytes().decode("utf-8")
        if actual != expected:
            findings.append(Finding("deterministic-refactor", target, "content differs from closed mapping"))
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
        target_text = (root / target).read_text(encoding="utf-8") if (root / target).exists() else ""
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
            if not present:
                findings.append(Finding("public-api", target, f"mapped declaration missing for {source}:{line}"))
            declaration_sha = sha256(declaration.encode("utf-8"))
            target_declaration_sha = sha256(mapped.encode("utf-8"))
            api_key = sha256(
                "\0".join((str(binding["baselineKey"]), str(line), declaration_sha)).encode("ascii")
            )
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
        artifacts: list[dict[str, str]] = []
        for artifact in sorted(project.parent.glob("bin/Release/**/*")):
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
    }


def validate_format_exceptions(
    root: Path,
    exceptions: Iterable[str] = COMMENTLESS_OR_BINARY_EXCEPTIONS,
) -> list[Finding]:
    findings: list[Finding] = []
    target_sources = {map_path(source): source for source in baseline_paths(root)}
    for target in sorted(set(exceptions)):
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


def validate_legal_documents(root: Path) -> list[Finding]:
    findings = validate_format_exceptions(root)
    notice = (root / "NOTICE").read_text(encoding="utf-8")
    modifications = (root / "MODIFICATIONS.md").read_text(encoding="utf-8")
    notice_lines = notice.splitlines()
    for path in sorted(COMMENTLESS_OR_BINARY_EXCEPTIONS):
        if notice_lines.count(path) != 1:
            findings.append(Finding("legal-exception-list", "NOTICE", f"exact exception occurrence count for {path} is not 1"))
        if modifications.count(f"`{path}`") != 1:
            findings.append(Finding("legal-exception-list", "MODIFICATIONS.md", f"exact exception occurrence count for {path} is not 1"))
    unchanged_container = ".devcontainer/devcontainer.json"
    if unchanged_container in notice or unchanged_container in modifications:
        findings.append(Finding("legal-exception-list", unchanged_container, "unchanged commentable file is listed as an exception"))
    if baseline_bytes(root, "LICENSE") != (root / "LICENSE").read_bytes():
        findings.append(Finding("legal", "LICENSE", "LICENSE is not byte-identical"))
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


def validate_persisted_evidence(
    evidence: Path,
    mapping: list[dict[str, object]],
    notices: list[dict[str, object]],
    api: list[dict[str, object]],
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
    return findings


def run_gate(root: Path, output: Path | None) -> int:
    mapping, mapping_findings = derive_baseline_mapping(root)
    notices, notice_findings = derive_change_notices(root)
    api, api_findings = derive_public_api_mapping(root)
    scan_findings = scan_tree(root)
    conformance_findings = derive_refactor_conformance(root)
    license_equal = baseline_bytes(root, "LICENSE") == (root / "LICENSE").read_bytes()
    legal_findings = validate_legal_documents(root)
    persisted_findings = validate_persisted_evidence(
        root / "evidence" / "WP-F2-SERVICEBUS-IDENTITY",
        mapping,
        notices,
        api,
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
    result = {
        "status": "PASS" if not (mapping_findings + notice_findings + api_findings) else "FAIL",
        "findings": [
            item.as_dict() for item in mapping_findings + notice_findings + api_findings
        ],
    }
    write_json(evidence / "generation-result.json", result)
    scan_exit = run_gate(root, evidence / "SOURCE_IDENTITY_GATE.json")
    return 0 if result["status"] == "PASS" and scan_exit == 0 else 1


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("scan", "evidence"))
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    root = args.root.resolve(strict=True)
    if args.command == "scan":
        return run_gate(root, args.output)
    evidence = args.output or root / "evidence" / "WP-F2-SERVICEBUS-IDENTITY"
    return generate_evidence(root, evidence)


if __name__ == "__main__":
    sys.exit(main())
