#!/usr/bin/env python3
"""Independently reconstruct and validate the 27 final one-cause mutations."""

from __future__ import annotations

import csv
import hashlib
import json
from pathlib import Path


REPOSITORY = Path(__file__).resolve().parents[3]
EVIDENCE = Path(__file__).resolve().parents[1]


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def validate_cohort(name: str) -> tuple[int, int]:
    root = EVIDENCE / name
    manifest = json.loads((root / "MUTATION_MANIFEST.json").read_text(encoding="utf-8"))
    with (root / "MUTATION_EXECUTION.tsv").open(encoding="utf-8", newline="") as stream:
        executions = {row["mutation_id"]: row for row in csv.DictReader(stream, delimiter="\t")}

    failed_cases = 0
    for mutation in manifest["mutations"]:
        identifier = mutation["id"]
        target = REPOSITORY / mutation["targetPath"]
        baseline = target.read_bytes()
        actual_sha = sha256(baseline)
        assert actual_sha == mutation["baselineSha256"], (identifier, "baseline", actual_sha)
        assert actual_sha == mutation["postRestoreSha256"], (identifier, "restore", actual_sha)

        old = "\n".join(mutation["oldText"]).encode()
        new = "\n".join(mutation["newText"]).encode()
        assert baseline.count(old) == mutation["occurrenceCount"], (identifier, "occurrence")
        offset = -1
        for _ in range(mutation["occurrenceIndex"] + 1):
            offset = baseline.find(old, offset + 1)
        assert offset >= 0, (identifier, "index")
        mutant = baseline[:offset] + new + baseline[offset + len(old):]
        assert sha256(mutant) == mutation["mutantSha256"], (identifier, "mutant")

        execution = executions[identifier]
        assert int(execution["build_exit"]) == 0, (identifier, "build")
        assert int(execution["test_exit"]) == mutation["testExitCode"] == 2, (identifier, "test-exit")
        result_path = root / mutation["testEvidence"]
        assert execution["raw_result"] == mutation["testEvidence"], (identifier, "result-path")
        result = json.loads(result_path.read_text(encoding="utf-8"))["results"]["summary"]
        for field in ("tests", "passed", "failed", "skipped"):
            assert result[field] == int(execution[field]), (identifier, field)
        assert result["failed"] > 0 and result["passed"] == 0 and result["skipped"] == 0, identifier
        failed_cases += result["failed"]

        build = root / mutation["buildEvidence"]
        assert build.is_file() and build.stat().st_size > 1024, (identifier, "build-evidence")
        wrapper = root / mutation.get("wrapperLog", "")
        if mutation.get("wrapperLog"):
            assert wrapper.is_file() and wrapper.stat().st_size > 0, (identifier, "wrapper")
        fixture_name = mutation.get("fixtureEvidence")
        if fixture_name:
            fixture = root / fixture_name
            findings = json.loads((fixture / "fixture-findings.json").read_text(encoding="utf-8"))
            assert findings["findings"] == [], (identifier, "fixture-findings")
            for broker, digest in findings["logs"].items():
                assert sha256((fixture / f"{broker}-broker.log").read_bytes()) == digest, (identifier, broker)

    assert set(executions) == {item["id"] for item in manifest["mutations"]}
    return len(manifest["mutations"]), failed_cases


def main() -> None:
    asb_mutations, asb_failures = validate_cohort("AZURE-SERVICE-BUS")
    rabbit_mutations, rabbit_failures = validate_cohort("RABBITMQ")
    print(f"PASS mutations={asb_mutations + rabbit_mutations}/27")
    print(f"PASS azure-service-bus={asb_mutations}/12 causal-failures={asb_failures}")
    print(f"PASS rabbitmq={rabbit_mutations}/15 causal-failures={rabbit_failures}")
    print("PASS every baseline, occurrence, mutant and restored SHA-256 is exact")
    print("PASS every recorded build succeeded and every owner execution failed without skips")


if __name__ == "__main__":
    main()
