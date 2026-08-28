#!/usr/bin/env python3
"""Fail-closed static verifier for the transactional-bus evidence package."""

from __future__ import annotations

import hashlib
import gzip
import json
import subprocess
from pathlib import Path


ROOT = Path.cwd().resolve()
EVIDENCE = ROOT / "evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/TRANSACTIONAL-BUS-CAPABILITY-SPLIT"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def commit_blob(commit: str, path: str) -> bytes:
    return subprocess.run(
        ["git", "show", f"{commit}:{path}"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    ).stdout


manifest = read_json(EVIDENCE / "MUTATION_MANIFEST.json")
technical = manifest["technicalCommit"]
require(technical == "1e8d0b4c6ad2e4b10cf90f8cfef9c3d16fc70407", "unexpected technical commit")
require(
    subprocess.run(["git", "show", "-s", "--format=%T", technical], cwd=ROOT, check=True, capture_output=True, text=True).stdout.strip()
    == manifest["technicalTree"],
    "technical tree mismatch",
)
require(
    subprocess.run(["git", "show", "-s", "--format=%P", technical], cwd=ROOT, check=True, capture_output=True, text=True).stdout.strip()
    == manifest["technicalParent"],
    "technical parent mismatch",
)

mutations = manifest["mutations"]
require(len(mutations) == 17, "mutation count is not 17")
require(len({item["id"] for item in mutations}) == 17, "mutation ids are not unique")

for item in mutations:
    original = commit_blob(technical, item["targetPath"])
    require(digest(original) == item["baselineSha256"], f"{item['id']} baseline hash mismatch")
    require(item["postRestoreSha256"] == item["baselineSha256"], f"{item['id']} restore hash mismatch")

    old = "\n".join(item["oldText"]).encode()
    new = "\n".join(item["newText"]).encode()
    positions: list[int] = []
    start = 0
    while True:
        position = original.find(old, start)
        if position < 0:
            break
        positions.append(position)
        start = position + len(old)
    require(len(positions) == item["occurrenceCount"], f"{item['id']} occurrence count mismatch")
    index = item["occurrenceIndex"]
    require(0 <= index < len(positions), f"{item['id']} occurrence index is invalid")
    position = positions[index]
    mutant = original[:position] + new + original[position + len(old):]
    require(digest(mutant) == item["mutantSha256"], f"{item['id']} mutant hash mismatch")

    build_log = EVIDENCE / item["buildLog"]
    build_binlog = EVIDENCE / item["buildEvidence"]
    result_path = EVIDENCE / item["testEvidence"]
    require(build_log.stat().st_size > 0, f"{item['id']} build log is empty")
    require(build_binlog.stat().st_size > 1024, f"{item['id']} binlog is not credible")
    result = read_json(result_path)["results"]["summary"]
    require(result["tests"] == item["maximum"], f"{item['id']} test count mismatch")
    require(result["failed"] >= 1, f"{item['id']} did not fail")
    require(result["skipped"] == result["pending"] == result["other"] == 0, f"{item['id']} has nonexecuting results")


def aggregate(directory: Path, pattern: str) -> tuple[int, int, int, int]:
    files = sorted(directory.glob(pattern))
    total = failed = skipped = 0
    for result_path in files:
        summary = read_json(result_path)["results"]["summary"]
        total += summary["tests"]
        failed += summary["failed"]
        skipped += summary["skipped"] + summary["pending"] + summary["other"]
    return len(files), total, failed, skipped


require(aggregate(EVIDENCE / "positive/unit-results", "*.ctrf") == (19, 2267, 0, 0), "unit aggregate mismatch")
require(aggregate(EVIDENCE / "positive/local-results", "*.ctrf") == (7, 244, 0, 0), "local aggregate mismatch")
require(aggregate(EVIDENCE / "positive/transaction-results", "*.json") == (1, 35, 0, 0), "transaction aggregate mismatch")
require(aggregate(EVIDENCE / "positive/ef-conflict-results", "*.json") == (1, 4, 0, 0), "EF conflict aggregate mismatch")

fixture = read_json(EVIDENCE / "positive/local-fixture/fixture-findings.json")
require(fixture["findings"] == [], "fixture cleanup has findings")
require(fixture["brokers"] == ["postgres", "azurite", "localstack", "activemq", "artemis"], "fixture broker set mismatch")
for broker, expected in fixture["logs"].items():
    with gzip.open(EVIDENCE / f"positive/local-fixture/{broker}-broker.log.gz", "rb") as broker_log:
        require(digest(broker_log.read()) == expected, f"{broker} log hash mismatch")

with gzip.open(EVIDENCE / "positive/engineering-build.log.gz", "rt", encoding="utf-8") as build_stream:
    build_log = build_stream.read()
require("0 Warnung(en)" in build_log and "0 Fehler" in build_log, "engineering build summary mismatch")

checksum_path = EVIDENCE / "SHA256SUMS"
require(checksum_path.is_file(), "SHA256SUMS is missing")
listed: set[str] = set()
for line in checksum_path.read_text(encoding="utf-8").splitlines():
    expected, relative = line.split("  ", 1)
    require(relative not in listed, f"duplicate checksum entry {relative}")
    listed.add(relative)
    require(digest((EVIDENCE / relative).read_bytes()) == expected, f"checksum mismatch {relative}")

inventory = {
    path.relative_to(EVIDENCE).as_posix()
    for path in EVIDENCE.rglob("*")
    if path.is_file() and path.name != "SHA256SUMS"
}
require(listed == inventory, f"checksum inventory mismatch missing={sorted(inventory - listed)} extra={sorted(listed - inventory)}")

print(
    "PASS transactional-bus evidence: "
    f"{len(inventory)} hashed files; 17/17 mutants killed; "
    "2267 unit, 244 local, 35 focused transaction and 4 EF ownership cases green"
)
