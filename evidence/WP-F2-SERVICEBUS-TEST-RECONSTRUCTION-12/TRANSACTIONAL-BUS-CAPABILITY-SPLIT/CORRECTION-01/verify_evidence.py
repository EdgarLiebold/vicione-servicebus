#!/usr/bin/env python3
"""Read-only verifier for the transactional-bus correction evidence."""

from __future__ import annotations

import csv
import gzip
import hashlib
import json
import shlex
import subprocess
import tempfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
EVIDENCE = Path(__file__).resolve().parent
MANIFEST = json.loads((EVIDENCE / "MUTATION_MANIFEST.json").read_text(encoding="utf-8"))
RESULTS = json.loads((EVIDENCE / "FINAL_RESULTS.json").read_text(encoding="utf-8"))


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_file(path: Path) -> str:
    return sha256_bytes(path.read_bytes())


def git(*arguments: str) -> str:
    return subprocess.run(
        ["git", *arguments], cwd=ROOT, check=True, text=True, capture_output=True
    ).stdout.strip()


def summary(path: Path) -> dict[str, int]:
    report = json.loads(path.read_text(encoding="utf-8"))
    return report["results"]["summary"]


def aggregate(paths: list[Path]) -> dict[str, int]:
    totals = {key: 0 for key in ("tests", "passed", "failed", "skipped", "pending", "other")}
    for path in paths:
        current = summary(path)
        for key in totals:
            totals[key] += int(current[key])
    return totals


def verify_checksums() -> None:
    checksum_path = EVIDENCE / "SHA256SUMS"
    lines = [line for line in checksum_path.read_text(encoding="utf-8").splitlines() if line]
    listed: dict[str, str] = {}
    for line in lines:
        digest, relative = line.split("  ", 1)
        assert relative not in listed, f"duplicate checksum entry: {relative}"
        listed[relative] = digest

    actual = {
        path.relative_to(EVIDENCE).as_posix()
        for path in EVIDENCE.rglob("*")
        if path.is_file() and path.name != "SHA256SUMS"
    }
    assert set(listed) == actual, (
        f"checksum inventory mismatch; missing={sorted(actual - set(listed))}, "
        f"extra={sorted(set(listed) - actual)}"
    )
    for relative, expected in listed.items():
        assert sha256_file(EVIDENCE / relative) == expected, f"checksum mismatch: {relative}"


def verify_frozen_subject() -> None:
    technical = RESULTS["technical"]
    assert technical["commit"] == MANIFEST["technicalCommit"]
    assert technical["tree"] == MANIFEST["technicalTree"]
    assert technical["parent"] == MANIFEST["technicalParent"]
    assert git("rev-parse", f"{technical['commit']}^{{tree}}") == technical["tree"]
    assert git("rev-parse", f"{technical['commit']}^") == technical["parent"]
    assert git("diff", "--check", f"{technical['parent']}..{technical['commit']}") == ""

    patch = gzip.decompress((EVIDENCE / "TECHNICAL_CORRECTION.patch.gz").read_bytes())
    expected_patch = subprocess.run(
        ["git", "diff", "--binary", f"{technical['parent']}..{technical['commit']}"],
        cwd=ROOT,
        check=True,
        capture_output=True,
    ).stdout
    assert patch == expected_patch, "technical patch does not match the frozen commit delta"


def verify_positive_results() -> None:
    unit = aggregate(sorted((EVIDENCE / "positive/unit-results").glob("*.ctrf")))
    local = aggregate(sorted((EVIDENCE / "positive/local-results").glob("*.ctrf")))
    focused = summary(EVIDENCE / "positive/transaction-results/transactional.json")
    assert len(list((EVIDENCE / "positive/unit-results").glob("*.ctrf"))) == 19
    assert len(list((EVIDENCE / "positive/local-results").glob("*.ctrf"))) == 7
    assert unit == {"tests": 2271, "passed": 2271, "failed": 0, "skipped": 0, "pending": 0, "other": 0}
    assert local == {"tests": 244, "passed": 244, "failed": 0, "skipped": 0, "pending": 0, "other": 0}
    assert {key: focused[key] for key in ("tests", "passed", "failed", "skipped", "pending", "other")} == {
        "tests": 39, "passed": 39, "failed": 0, "skipped": 0, "pending": 0, "other": 0
    }

    for name in ("engineering-restore.binlog", "engineering-build.binlog"):
        assert (EVIDENCE / "positive" / name).stat().st_size > 1024, f"empty binlog: {name}"
    build_log = gzip.decompress((EVIDENCE / "positive/engineering-build.log.gz").read_bytes()).decode(
        "utf-8", errors="replace"
    )
    assert "0 Warnung(en)" in build_log and "0 Fehler" in build_log

    findings = json.loads((EVIDENCE / "positive/local-fixture/fixture-findings.json").read_text(encoding="utf-8"))
    assert findings["findings"] == []
    assert findings["brokers"] == ["postgres", "azurite", "localstack", "activemq", "artemis"]
    for broker, expected in findings["logs"].items():
        raw = gzip.decompress((EVIDENCE / f"positive/local-fixture/{broker}-broker.log.gz").read_bytes())
        assert raw and sha256_bytes(raw) == expected, f"broker log mismatch: {broker}"


def verify_mutations() -> None:
    technical = MANIFEST["technicalCommit"]
    mutation_ids: list[str] = []
    for mutation in MANIFEST["mutations"]:
        mutation_id = mutation["id"]
        mutation_ids.append(mutation_id)
        patch_path = EVIDENCE / mutation["patch"]
        assert sha256_file(patch_path) == mutation["patchSha256"]
        baseline = subprocess.run(
            ["git", "show", f"{technical}:{mutation['targetPath']}"],
            cwd=ROOT,
            check=True,
            capture_output=True,
        ).stdout
        assert sha256_bytes(baseline) == mutation["baselineSha256"] == mutation["postRestoreSha256"]

        with tempfile.TemporaryDirectory(prefix=f"verify-{mutation_id}-") as temporary:
            temporary_root = Path(temporary)
            target = temporary_root / mutation["targetPath"]
            target.parent.mkdir(parents=True)
            target.write_bytes(baseline)
            expanded_patch = temporary_root / f"{mutation_id}.patch"
            expanded_patch.write_bytes(gzip.decompress(patch_path.read_bytes()))
            subprocess.run(
                ["patch", "--batch", "--forward", "-p1", "-i", str(expanded_patch)],
                cwd=temporary_root,
                check=True,
                text=True,
                capture_output=True,
            )
            assert sha256_file(target) == mutation["mutantSha256"], f"mutant mismatch: {mutation_id}"

        assert (EVIDENCE / mutation["buildEvidence"]).stat().st_size > 1024
        build_log = gzip.decompress((EVIDENCE / mutation["buildLog"]).read_bytes()).decode(
            "utf-8", errors="replace"
        )
        assert "0 Warnung(en)" in build_log and "0 Fehler" in build_log

        result_path = EVIDENCE / mutation["testEvidence"]
        report = json.loads(result_path.read_text(encoding="utf-8"))
        current = report["results"]["summary"]
        for key in ("tests", "passed", "failed", "skipped"):
            assert current[key] == mutation[key], f"{mutation_id} {key} mismatch"
        assert current["pending"] == 0 and current["other"] == 0
        failures = [test for test in report["results"]["tests"] if test["status"] == "failed"]
        assert len(failures) == 1
        assert failures[0]["name"].startswith(mutation["owner"])
        assert failures[0].get("message"), f"missing causal failure: {mutation_id}"
        assert gzip.decompress((EVIDENCE / mutation["testLog"]).read_bytes())

    assert mutation_ids == ["M18", "M19", "M20", "M21", "M22", "M23"]


def verify_execution_table() -> None:
    table_path = EVIDENCE / "MUTATION_EXECUTION.tsv"
    with table_path.open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream, delimiter="\t"))

    assert rows and rows[0].keys() == {
        "mutation_id",
        "build_working_directory",
        "build_argv",
        "build_exit",
        "test_working_directory",
        "test_argv",
        "test_exit",
        "tests",
        "passed",
        "failed",
        "skipped",
        "owner",
        "target",
        "patch",
        "raw_result",
    }
    mutations = {mutation["id"]: mutation for mutation in MANIFEST["mutations"]}
    assert [row["mutation_id"] for row in rows] == list(mutations)

    build_environment = [
        f"{name}={value}" for name, value in MANIFEST["buildEnvironment"].items()
    ]
    for row in rows:
        mutation = mutations[row["mutation_id"]]
        mutation_id = mutation["id"]
        assert row["build_working_directory"] == MANIFEST["mutationWorktree"]
        assert row["test_working_directory"] == MANIFEST["mutationWorktree"]
        assert int(row["build_exit"]) == mutation["buildExitCode"]
        assert int(row["test_exit"]) == mutation["testExitCode"]
        for key in ("tests", "passed", "failed", "skipped"):
            assert int(row[key]) == mutation[key], f"{mutation_id} table {key} mismatch"
        assert row["owner"] == mutation["owner"]
        assert row["target"] == mutation["targetPath"]
        assert row["patch"] == mutation["patch"]
        assert row["raw_result"] == mutation["testEvidence"]

        build_log = str(EVIDENCE / mutation["buildLog"])
        assert build_log.endswith(".gz")
        expected_build = [
            "env",
            *build_environment,
            "/usr/local/share/dotnet/dotnet",
            "build",
            "tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj",
            "--configuration",
            "Release",
            "--no-restore",
            "--disable-build-servers",
            "--no-incremental",
            "--maxcpucount:1",
            "-p:BuildInParallel=false",
            "-p:UseSharedCompilation=false",
            f"/bl:{EVIDENCE / mutation['buildEvidence']}",
            f"/flp:LogFile={build_log[:-3]};Verbosity=normal",
        ]
        assert shlex.split(row["build_argv"]) == expected_build, (
            f"{mutation_id} build argv mismatch"
        )

        result_path = EVIDENCE / mutation["testEvidence"]
        expected_test = [
            "env",
            "VICIONE_TESTS__Profile=UnitArchitecture",
            "artifacts/sdk/bin/ViciOne.ServiceBus.Tests.Unit/release/ViciOne.ServiceBus.Tests",
            "--filter-method",
            mutation["owner"],
            "--minimum-expected-tests",
            str(mutation["minimum"]),
            "--fail-skips",
            "on",
            "--parallel",
            "none",
            "--report-xunit-ctrf",
            "--results-directory",
            str(result_path.parent),
            "--report-xunit-ctrf-filename",
            result_path.name,
            "--progress",
            "off",
            "--no-ansi",
            "--output",
            "Normal",
        ]
        assert shlex.split(row["test_argv"]) == expected_test, (
            f"{mutation_id} test argv mismatch"
        )


def main() -> None:
    verify_checksums()
    verify_frozen_subject()
    verify_positive_results()
    verify_mutations()
    verify_execution_table()
    print("PASS: transactional-bus correction evidence is complete and byte-consistent")


if __name__ == "__main__":
    main()
