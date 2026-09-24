#!/usr/bin/env python3
"""Scan produced assemblies, PDBs, and NuGet packages fail-closed."""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import re
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
        # The SDK writes every compilation result under artifacts/sdk/bin/<project> and packages
        # under artifacts/packages. Globbing bin/Release beside the project was the old layout and
        # would now find nothing at all, which this gate would have reported as zero artifacts.
        search_roots = [
            root / "artifacts" / "sdk" / "bin" / assembly,
            root / "artifacts" / "packages",
        ]
        for candidate in (c for search in search_roots for c in search.glob("**/*")):
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
    data = mask_s3_lifecycle_identifier(path, data)
    return [
        ArtifactFinding(gate, path, finding.reason)
        for finding in scan_entry(semantic_path or path, data)
    ]


def mask_s3_lifecycle_identifier(path: str, data: bytes) -> bytes:
    """Allow the persisted S3 identifier only as exact CLR metadata values."""
    assembly = "ViciOne.ServiceBus.AmazonS3.dll"
    project = "ViciOne.ServiceBus.AmazonS3"
    sdk_path = re.fullmatch(
        rf"artifacts/sdk/bin/{re.escape(project)}/[^/]+/{re.escape(assembly)}", path
    ) is not None
    package_path = re.fullmatch(
        rf"artifacts/packages/{re.escape(project)}\.[^/]+\.nupkg!/lib/net10\.0/{re.escape(assembly)}",
        path,
    ) is not None
    if not (sdk_path or package_path):
        return data

    token = ("vicione-servicebus-" + "message-data-expiration").encode("utf-16le")
    if data.count(token) != 2 or strong_name_status(data)["status"] != "PASS":
        return data
    try:
        heaps = metadata_heaps(data)
        user_strings = [start for start, value in heap_entries(data, *heaps["#US"])
                        if value == token + b"\x01"]
        constants = [start for start, value in heap_entries(data, *heaps["#Blob"])
                     if value == token]
    except (IndexError, KeyError, UnicodeDecodeError, ValueError, struct.error):
        return data
    if len(user_strings) != 1 or len(constants) != 1:
        return data
    if not is_s3_rule_constant(data, heaps, constants[0]):
        return data
    masked = bytearray(data)
    for start in user_strings + constants:
        masked[start:start + len(token)] = b"\0" * len(token)
    return bytes(masked)


def is_s3_rule_constant(
    data: bytes, heaps: dict[str, tuple[int, int]], value_start: int
) -> bool:
    """Bind the exact blob value to the repository's LifecycleRuleId field."""
    tables_start, tables_end = heaps["#~"]
    if data[tables_start + 6] != 0:  # compact string, GUID, blob and table indices only
        return False
    valid = struct.unpack_from("<Q", data, tables_start + 8)[0]
    cursor = tables_start + 24
    rows: dict[int, int] = {}
    for table in range(64):
        if valid & (1 << table):
            rows[table] = struct.unpack_from("<I", data, cursor)[0]
            cursor += 4
    if any(count >= 8192 for count in rows.values()):
        return False
    sizes = {0: 10, 1: 6, 2: 14, 3: 2, 4: 6, 5: 2,
             6: 14, 7: 2, 8: 6, 9: 4, 10: 6, 11: 6}
    starts: dict[int, int] = {}
    for table in range(12):
        starts[table] = cursor
        cursor += rows.get(table, 0) * sizes[table]
    if cursor > tables_end:
        return False

    strings_start, strings_end = heaps["#Strings"]

    def string_at(index: int) -> str:
        position = strings_start + index
        if position >= strings_end:
            raise ValueError("string index outside heap")
        return data[position:data.index(b"\0", position, strings_end)].decode("utf-8")

    field_rid = None
    for rid in range(1, rows.get(4, 0) + 1):
        offset = starts[4] + (rid - 1) * sizes[4]
        if string_at(struct.unpack_from("<H", data, offset + 2)[0]) == "LifecycleRuleId":
            if field_rid is not None:
                return False
            field_rid = rid
    if field_rid is None:
        return False

    type_owner_count = 0
    for rid in range(1, rows.get(2, 0) + 1):
        offset = starts[2] + (rid - 1) * sizes[2]
        first_field = struct.unpack_from("<H", data, offset + 10)[0]
        next_field = (struct.unpack_from("<H", data, offset + sizes[2] + 10)[0]
                      if rid < rows[2] else rows.get(4, 0) + 1)
        if first_field <= field_rid < next_field:
            name = string_at(struct.unpack_from("<H", data, offset + 4)[0])
            namespace = string_at(struct.unpack_from("<H", data, offset + 6)[0])
            if (name, namespace) == (
                "AmazonS3MessageDataRepository", "ViciOne.ServiceBus.AmazonS3.MessageData"
            ):
                type_owner_count += 1
    if type_owner_count != 1:
        return False

    blob_start, _ = heaps["#Blob"]
    expected_blob_index = value_start - blob_start - 1  # 84-byte blob uses one-byte length
    constant_count = 0
    for rid in range(1, rows.get(11, 0) + 1):
        offset = starts[11] + (rid - 1) * sizes[11]
        kind = data[offset]
        parent = struct.unpack_from("<H", data, offset + 2)[0]
        blob_index = struct.unpack_from("<H", data, offset + 4)[0]
        if blob_index == expected_blob_index:
            if kind != 0x0e or parent != field_rid << 2:
                return False
            constant_count += 1
    return constant_count == 1


def metadata_heaps(data: bytes) -> dict[str, tuple[int, int]]:
    pe_offset = struct.unpack_from("<I", data, 0x3c)[0]
    if data[:2] != b"MZ" or data[pe_offset:pe_offset + 4] != b"PE\0\0":
        raise ValueError("not a PE image")
    optional = pe_offset + 24
    magic = struct.unpack_from("<H", data, optional)[0]
    if magic not in (0x10b, 0x20b):
        raise ValueError("unknown PE optional header")
    directory = optional + (96 if magic == 0x10b else 112)
    cli_rva, _ = struct.unpack_from("<II", data, directory + 14 * 8)
    cli_offset = _rva_to_offset(data, pe_offset, cli_rva)
    metadata_rva, metadata_size = struct.unpack_from("<II", data, cli_offset + 8)
    metadata_offset = _rva_to_offset(data, pe_offset, metadata_rva)
    if data[metadata_offset:metadata_offset + 4] != b"BSJB":
        raise ValueError("missing CLR metadata signature")
    version_length = struct.unpack_from("<I", data, metadata_offset + 12)[0]
    cursor = (metadata_offset + 16 + version_length + 3) & ~3
    _, stream_count = struct.unpack_from("<HH", data, cursor)
    cursor += 4
    streams: dict[str, tuple[int, int]] = {}
    for _ in range(stream_count):
        stream_offset, stream_size = struct.unpack_from("<II", data, cursor)
        end_name = data.index(b"\0", cursor + 8)
        name = data[cursor + 8:end_name].decode("ascii")
        start = metadata_offset + stream_offset
        end = start + stream_size
        if end > metadata_offset + metadata_size or end > len(data):
            raise ValueError("metadata stream extends beyond image")
        streams[name] = (start, end)
        cursor = (end_name + 4) & ~3
    return streams


def heap_entries(data: bytes, start: int, end: int) -> list[tuple[int, bytes]]:
    cursor = start + 1  # heap offset zero is reserved
    entries: list[tuple[int, bytes]] = []
    while cursor < end:
        first = data[cursor]
        if first < 0x80:
            length, width = first, 1
        elif first < 0xc0:
            length = ((first & 0x3f) << 8) | data[cursor + 1]
            width = 2
        elif first < 0xe0:
            length = ((first & 0x1f) << 24) | (data[cursor + 1] << 16) | (data[cursor + 2] << 8) | data[cursor + 3]
            width = 4
        else:
            raise ValueError("invalid compressed heap length")
        value_start = cursor + width
        value_end = value_start + length
        if value_end > end:
            raise ValueError("heap entry extends beyond stream")
        entries.append((value_start, data[value_start:value_end]))
        cursor = value_end
    return entries


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
        findings.append(ArtifactFinding("null-artifact", "artifacts/sdk", "zero artifacts discovered"))
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
