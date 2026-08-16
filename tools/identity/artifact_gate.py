#!/usr/bin/env python3
"""Scan produced assemblies, PDBs, and NuGet packages fail-closed."""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import struct
import sys
import zipfile
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from identity_gate import scan_entry
from identity_rules import contains_former_identity


ARTIFACT_SUFFIXES = frozenset({".dll", ".pdb", ".nupkg"})
LEGAL_PACKAGE_ENTRIES = frozenset(
    {"readme.md", "license", "license.txt", "notice", "copyright", "modifications.md"}
)


@dataclass(frozen=True)
class ArtifactFinding:
    gate: str
    path: str
    reason: str

    def as_dict(self) -> dict[str, str]:
        return {"gate": self.gate, "path": self.path, "reason": self.reason}


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def collect_artifacts(root: Path) -> list[tuple[Path, bool]]:
    artifacts: dict[Path, bool] = {}
    for project in root.rglob("*.csproj"):
        relative = project.relative_to(root)
        if "bin" in relative.parts or "obj" in relative.parts:
            continue
        assembly = project.stem
        project_xml = ET.fromstring(project.read_text(encoding="utf-8"))
        expects_strong_name = any(
            element.tag.rsplit("}", 1)[-1] == "Import"
            and Path(element.attrib.get("Project", "")).name == "signing.props"
            for element in project_xml.iter()
        )
        for candidate in project.parent.glob("bin/Release/**/*"):
            if not candidate.is_file() or candidate.suffix.casefold() not in ARTIFACT_SUFFIXES:
                continue
            if candidate.suffix.casefold() == ".nupkg" or candidate.stem == assembly:
                artifacts[candidate] = artifacts.get(candidate, False) or expects_strong_name
    return sorted(artifacts.items())


def scan_bytes(
    path: str,
    data: bytes,
    gate: str,
    *,
    semantic_path: str | None = None,
) -> list[ArtifactFinding]:
    return [
        ArtifactFinding(gate, path, finding.reason)
        for finding in scan_entry(semantic_path or path, data)
    ]


def scan_nupkg(path: str, data: bytes) -> tuple[list[dict[str, object]], list[ArtifactFinding]]:
    records: list[dict[str, object]] = []
    findings: list[ArtifactFinding] = []
    if contains_former_identity(path):
        findings.append(
            ArtifactFinding("nuget-path", path, "former technical identity in outer package path")
        )
    try:
        package = zipfile.ZipFile(io.BytesIO(data))
    except (OSError, RuntimeError, ValueError, NotImplementedError, EOFError, zipfile.BadZipFile):
        findings.append(ArtifactFinding("nuget", path, "invalid NuGet ZIP archive"))
        return records, findings

    with package:
        findings.extend(
            scan_bytes(
                f"{path}!/<archive-comment>",
                package.comment,
                "nuget-archive-comment",
                semantic_path="nupkg-archive-comment",
            )
        )
        for info in sorted(package.infolist(), key=lambda item: item.filename):
            entry_path = f"{path}!/{info.filename}"
            legal = info.filename.casefold() in LEGAL_PACKAGE_ENTRIES
            entry_findings: list[ArtifactFinding] = []
            if contains_former_identity(info.filename):
                entry_findings.append(
                    ArtifactFinding("nuget-path", entry_path, "former technical identity in package path")
                )
            entry_findings.extend(
                scan_bytes(
                    f"{entry_path}#comment",
                    info.comment,
                    "nuget-entry-comment",
                    semantic_path="nupkg-entry-comment",
                )
            )
            try:
                payload = package.read(info)
            except (OSError, RuntimeError, ValueError, NotImplementedError, EOFError, zipfile.BadZipFile) as error:
                payload = None
                entry_findings.append(
                    ArtifactFinding("nuget-read", entry_path, f"cannot decompress package entry: {error}")
                )
            if payload is not None and not legal:
                entry_findings.extend(
                    scan_bytes(
                        entry_path,
                        payload,
                        "nuget-content",
                        semantic_path="nupkg-entry-content",
                    )
                )
            findings.extend(entry_findings)
            records.append(
                {
                    "path": info.filename,
                    "sha256": sha256(payload) if payload is not None else None,
                    "size": len(payload) if payload is not None else None,
                    "commentSha256": sha256(info.comment),
                    "commentSize": len(info.comment),
                    "legalProvenanceException": legal,
                    "status": "PASS" if not entry_findings else "FAIL",
                }
            )
    return records, findings


def _rva_to_offset(data: bytes, pe_offset: int, rva: int) -> int:
    section_count = struct.unpack_from("<H", data, pe_offset + 6)[0]
    optional_size = struct.unpack_from("<H", data, pe_offset + 20)[0]
    section_offset = pe_offset + 24 + optional_size
    for index in range(section_count):
        offset = section_offset + index * 40
        virtual_size, virtual_address, raw_size, raw_offset = struct.unpack_from(
            "<IIII", data, offset + 8
        )
        span = max(virtual_size, raw_size)
        if virtual_address <= rva < virtual_address + span:
            return raw_offset + (rva - virtual_address)
    raise ValueError(f"RVA 0x{rva:x} does not belong to a PE section")


def strong_name_status(data: bytes) -> dict[str, object]:
    """Read the CLI strong-name flag and signature directory from a managed PE."""
    try:
        if data[:2] != b"MZ":
            raise ValueError("missing MZ header")
        pe_offset = struct.unpack_from("<I", data, 0x3C)[0]
        if data[pe_offset : pe_offset + 4] != b"PE\0\0":
            raise ValueError("missing PE header")
        optional = pe_offset + 24
        magic = struct.unpack_from("<H", data, optional)[0]
        directory_offset = optional + (96 if magic == 0x10B else 112 if magic == 0x20B else 0)
        if directory_offset == optional:
            raise ValueError(f"unknown optional-header magic 0x{magic:x}")
        cli_rva, cli_size = struct.unpack_from("<II", data, directory_offset + 14 * 8)
        if not cli_rva or cli_size < 40:
            raise ValueError("missing CLI header")
        cli_offset = _rva_to_offset(data, pe_offset, cli_rva)
        flags = struct.unpack_from("<I", data, cli_offset + 16)[0]
        signature_rva, signature_size = struct.unpack_from("<II", data, cli_offset + 32)
        signature = b""
        if signature_rva and signature_size:
            signature_offset = _rva_to_offset(data, pe_offset, signature_rva)
            signature = data[signature_offset : signature_offset + signature_size]
        flag_set = bool(flags & 0x8)
        signature_present = bool(signature) and any(signature)
        return {
            "managedPe": True,
            "strongNameFlag": flag_set,
            "signatureSize": signature_size,
            "signaturePresent": signature_present,
            "status": "PASS" if flag_set and signature_present else "FAIL",
        }
    except (IndexError, struct.error, ValueError) as error:
        return {"managedPe": False, "status": "FAIL", "reason": str(error)}


def scan_artifacts(root: Path) -> dict[str, object]:
    artifacts: list[dict[str, object]] = []
    findings: list[ArtifactFinding] = []
    strong_names: list[dict[str, object]] = []
    paths = collect_artifacts(root)
    for artifact, expects_strong_name in paths:
        relative = artifact.relative_to(root).as_posix()
        data = artifact.read_bytes()
        artifact_findings: list[ArtifactFinding] = []
        package_entries: list[dict[str, object]] = []
        if artifact.suffix.casefold() == ".nupkg":
            package_entries, package_findings = scan_nupkg(relative, data)
            artifact_findings.extend(package_findings)
        else:
            artifact_findings.extend(scan_bytes(relative, data, f"{artifact.suffix[1:]}-binary"))
        if artifact.suffix.casefold() == ".dll":
            status = strong_name_status(data)
            strong_record = {"path": relative, "expected": expects_strong_name, **status}
            strong_names.append(strong_record)
            if expects_strong_name and status["status"] != "PASS":
                artifact_findings.append(
                    ArtifactFinding("strong-name", relative, str(status.get("reason", "not signed")))
                )
        findings.extend(artifact_findings)
        artifacts.append(
            {
                "path": relative,
                "kind": artifact.suffix[1:].casefold(),
                "sha256": sha256(data),
                "size": len(data),
                "packageEntries": package_entries,
                "status": "PASS" if not artifact_findings else "FAIL",
                "findings": [finding.as_dict() for finding in artifact_findings],
            }
        )
    if not paths:
        findings.append(ArtifactFinding("null-artifact", "bin/Release", "zero artifacts discovered"))
    return {
        "status": "PASS" if not findings else "FAIL",
        "counts": {
            "artifacts": len(artifacts),
            "dll": sum(item["kind"] == "dll" for item in artifacts),
            "pdb": sum(item["kind"] == "pdb" for item in artifacts),
            "nupkg": sum(item["kind"] == "nupkg" for item in artifacts),
            "embeddedPdbAssemblies": sum(item["kind"] == "dll" for item in artifacts),
            "findings": len(findings),
        },
        "artifacts": artifacts,
        "strongNames": strong_names,
        "findings": [finding.as_dict() for finding in findings],
    }


def write_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    root = args.root.resolve(strict=True)
    result = scan_artifacts(root)
    if args.output:
        write_json(args.output, result)
    print(json.dumps({"status": result["status"], "counts": result["counts"]}, indent=2))
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
