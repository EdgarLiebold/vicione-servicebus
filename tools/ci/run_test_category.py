#!/usr/bin/env python3
"""Run one required test category and prove it actually executed tests.

ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-08.

A required CI category that silently runs zero tests looks exactly like a green one. This runner
executes the category, reads the counters straight out of the TRX result file rather than from
console prose, and fails when the category executed nothing or when anything failed. The counters
are written to the evidence path so the gate is auditable after the fact.

Standard library only, so it needs no package restore of its own.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import xml.etree.ElementTree as ElementTree
from pathlib import Path

TRX_NAMESPACE = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


class CategoryError(RuntimeError):
    pass


TEST_TIMEZONE = "UTC"
ZERO_COUNTERS = {"total": 0, "executed": 0, "passed": 0, "failed": 0, "notExecuted": 0}


def read_counters(trx_path: Path) -> dict[str, int]:
    """Read the authoritative counters from a TRX result file.

    A missing or malformed result file is reported as all-zero rather than as an exception, so the
    evidence record is still written. A category that produced no result executed no test, and that
    is exactly what the gate must reject.
    """
    if not trx_path.is_file():
        return dict(ZERO_COUNTERS)

    try:
        counters = ElementTree.parse(trx_path).getroot().find("t:ResultSummary/t:Counters", TRX_NAMESPACE)
    except ElementTree.ParseError:
        return dict(ZERO_COUNTERS)

    if counters is None:
        return dict(ZERO_COUNTERS)

    def value(name: str) -> int:
        return int(counters.attrib.get(name, "0"))

    return {
        "total": value("total"),
        "executed": value("executed"),
        "passed": value("passed"),
        "failed": value("failed"),
        "notExecuted": value("notExecuted"),
    }


def run_category(category: str, project: str, evidence_dir: Path, extra: list[str]) -> dict[str, object]:
    evidence_dir.mkdir(parents=True, exist_ok=True)
    trx_path = evidence_dir / f"{category}.trx"
    if trx_path.exists():
        trx_path.unlink()

    command = [
        "dotnet", "test", project,
        "-c", "Release",
        "--logger", f"trx;LogFileName={trx_path.resolve()}",
        *extra,
    ]
    # The suite is not timezone independent. Two cron cases build a template hour in one offset and
    # reuse it across 31 October 2010, the day a European DST transition falls; they pass in UTC and
    # fail in CEST. Pinning the zone is the same idea as pinning an image digest: the local run and
    # the runner must not differ by their environment. An explicit TZ from the caller is respected.
    environment = dict(os.environ)
    environment.setdefault("TZ", TEST_TIMEZONE)

    completed = subprocess.run(command, text=True, capture_output=True, env=environment)
    counters = read_counters(trx_path)

    record = {
        "schemaVersion": 1,
        "kind": "REQUIRED_CATEGORY_RESULT",
        "category": category,
        "project": project,
        "command": " ".join(command),
        "timezone": environment["TZ"],
        "exitCode": completed.returncode,
        "counters": counters,
        "trxProduced": trx_path.is_file(),
    }
    # Written before the gate is evaluated: a rejected category must leave evidence too.
    (evidence_dir / f"{category}.json").write_text(
        json.dumps(record, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )

    sys.stdout.write(completed.stdout)
    sys.stderr.write(completed.stderr)

    if counters["total"] <= 0:
        raise CategoryError(f"Required category '{category}' executed 0 tests; a required category may never be empty.")
    if counters["executed"] <= 0:
        raise CategoryError(f"Required category '{category}' executed no test at all; every test was skipped or filtered.")
    if counters["failed"] > 0:
        raise CategoryError(f"Required category '{category}' reported {counters['failed']} failing test(s).")
    if completed.returncode != 0:
        raise CategoryError(f"Required category '{category}' exited with {completed.returncode}.")

    return record


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--category", required=True, help="Stable identifier of the required category.")
    parser.add_argument("--project", required=True, help="Test project or solution to run.")
    parser.add_argument("--evidence-dir", required=True, type=Path, help="Where the TRX and counter record are written.")
    parser.add_argument("rest", nargs="*", help="Additional arguments forwarded to dotnet test.")
    args = parser.parse_args(argv)

    try:
        record = run_category(args.category, args.project, args.evidence_dir, list(args.rest))
    except CategoryError as error:
        print(f"FAIL required-category {args.category}: {error}", file=sys.stderr)
        return 1

    counters = record["counters"]
    print(
        f"PASS required-category {args.category} "
        f"total={counters['total']} passed={counters['passed']} failed={counters['failed']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
