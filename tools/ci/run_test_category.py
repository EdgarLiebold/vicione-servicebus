#!/usr/bin/env python3
"""Run one required test category and prove it actually executed tests.

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
import re
import shlex
import subprocess
import sys
import time
import xml.etree.ElementTree as ElementTree
from collections import Counter
from datetime import datetime
from pathlib import Path

TRX_NAMESPACE = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]

RAW_RUN_OUTPUT_DIR = REPOSITORY_ROOT / "artifacts" / "run-output"


class CategoryError(RuntimeError):
    pass


TEST_TIMEZONE = "UTC"
ZERO_COUNTERS = {"total": 0, "executed": 0, "passed": 0, "failed": 0, "notExecuted": 0}

REPO_ROOT = Path(__file__).resolve().parents[2]
VERIFICATION_MODEL = REPO_ROOT / "build/verification/VERIFICATION_MODEL.json"

# Options that narrow the run. A required category is only a proof when it is complete, so the
# controller refuses them itself instead of trusting the workflow text that calls it. A settings file
# belongs here as well: it carries a TestCaseFilter of its own, so it narrows the run without ever
# naming a filter on the command line.
SELECTOR_OPTIONS = frozenset({
    "--filter", "-filter", "/filter",
    "--testcasefilter", "-testcasefilter", "/testcasefilter",
    "--tests", "-tests", "/tests",
    "--settings", "-settings", "/settings", "-s",
})


def option_token(argument: str) -> str:
    """The option part of an argument, without its value and without case.

    A selector reaches the command line as '--filter X', '--filter=X' or '/Tests:X', so the value has
    to be cut off before the option can be recognised at all.
    """
    return argument.split("=", 1)[0].split(":", 1)[0].lower()


def type_name(class_name: str) -> str:
    """The full type name of a TRX class attribute, with a trailing assembly qualification removed.

    The NUnit adapter writes the plain type name including parameterised fixture arguments, but an
    adapter that appends ', Assembly' must not corrupt an argument list that itself contains a comma.
    The tail is only dropped when it cannot be part of an argument list.
    """
    if ", " not in class_name:
        return class_name

    head, _, tail = class_name.rpartition(", ")
    if "(" in tail or ")" in tail:
        return class_name
    if head.count("(") != head.count(")"):
        return class_name

    return head


def identity_of(method: ElementTree.Element) -> str:
    """The stable identity of one case.

    Full namespace and class name including parameterised fixture arguments, plus the complete case
    name the adapter reported. Two different tests must never share one identity: shortening this to
    the last class segment made a fixture in one namespace authorise a fixture of the same name in
    another, and collapsed parameterised cases into a single permission.
    """
    return f"{type_name(method.attrib.get('className', ''))}.{method.attrib.get('name', '')}"


def read_not_executed(trx_path: Path) -> list[str]:
    """Every case the run did not execute, as a full identity, with duplicates preserved.

    The TRX summary is no help here: its notExecuted counter reads 0 even when total and executed
    differ by twenty-five, so the only reliable source is the result list itself. The list is not
    deduplicated either, because two results are two cases even when they look alike.
    """
    if not trx_path.is_file():
        return []
    try:
        root = ElementTree.parse(trx_path).getroot()
    except ElementTree.ParseError:
        return []

    definitions = {}
    for definition in root.findall("t:TestDefinitions/t:UnitTest", TRX_NAMESPACE):
        method = definition.find("t:TestMethod", TRX_NAMESPACE)
        if method is None:
            continue
        definitions[definition.attrib.get("id")] = identity_of(method)

    names = []
    for result in root.findall("t:Results/t:UnitTestResult", TRX_NAMESPACE):
        # Only cases that really did not run. Reading this as "anything but Passed" also swept up
        # failures, so a failing test was reported as an unclassified skip as well and the two
        # rejections said different things about the same case.
        if result.attrib.get("outcome") != "NotExecuted":
            continue
        name = definitions.get(result.attrib.get("testId"))
        if name:
            names.append(name)
    return sorted(names)


def minimum_executed(category: str) -> int | None:
    """The number of cases this category executed when it was last recorded, as a floor.

    Coverage can fall without a single failure: a new exclusion, a renamed fixture, a filter that
    matches less than it used to. The counters would still read green, so the floor is what turns a
    shrinking category red.
    """
    try:
        data = json.loads(VERIFICATION_MODEL.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return None

    run = declared_run(data, category)
    if run is None:
        return None

    floor = run.get("minimumExecutedCases")

    return floor if isinstance(floor, int) and floor > 0 else None


def declared_run(model: dict, category: str) -> dict | None:
    """The one run of the model that starts this category, or None if it declares none."""
    for capability in model.get("capabilities", []):
        for run in capability.get("runs", []):
            if run.get("category") == category:
                return run

    return None


def inventoried_cases(category: str) -> list[str] | None:
    """The cases the model permits this category to leave unexecuted, as full identities.

    None means the model could not be read at all, which is itself a failure: without it there
    is no statement about what the category skips, and a required category may not skip silently.
    An entry without a full identity is not a permission either; naming a case by its short form
    would authorise every case that happens to share that form.
    """
    if not VERIFICATION_MODEL.is_file():
        return None
    try:
        data = json.loads(VERIFICATION_MODEL.read_text(encoding="utf-8"))
    except json.JSONDecodeError:
        return None
    run = declared_run(data, category)
    if run is None:
        # A category the model does not declare is a category nothing authorises to skip.
        return []
    permitted = []
    for case in run.get("notExecuted", []):
        identity = case.get("identity") if isinstance(case, dict) else None
        if isinstance(identity, str) and identity:
            permitted.append(identity)
    return permitted


def unauthorised_not_executed(skipped: list[str], permitted: list[str]) -> list[str]:
    """What the run skipped beyond its permissions, compared as a multiset.

    Two cases that share an inventory entry are not both authorised by it. Subtracting counts rather
    than sets keeps the second one visible.
    """
    remaining = Counter(skipped) - Counter(permitted)
    return sorted(remaining.elements())


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


def read_run_duration(trx_path: Path) -> float | None:
    """The duration the run itself reported, which is not the wall time of the process.

    The process also restores, builds and writes evidence. Keeping the two apart stops a report from
    presenting one as the other.
    """
    if not trx_path.is_file():
        return None
    try:
        times = ElementTree.parse(trx_path).getroot().find("t:Times", TRX_NAMESPACE)
    except ElementTree.ParseError:
        return None
    if times is None:
        return None

    start, finish = parse_trx_time(times.attrib.get("start")), parse_trx_time(times.attrib.get("finish"))
    if start is None or finish is None:
        return None
    return round((finish - start).total_seconds(), 3)


def parse_trx_time(value: str | None) -> datetime | None:
    """A TRX timestamp, whose fractional part carries more digits than fromisoformat accepts.

    The writer emits seven fractional digits. Feeding that straight into fromisoformat returns None on
    every run, which silently turned the recorded run duration into null instead of a number.
    """
    if not value:
        return None

    match = re.match(r"^(?P<head>.*?\.\d{1,9})(?P<tail>.*)$", value)
    if match:
        head, tail = match.group("head"), match.group("tail")
        whole, _, fraction = head.rpartition(".")
        value = f"{whole}.{fraction[:6].ljust(6, '0')}{tail}"

    try:
        return datetime.fromisoformat(value)
    except ValueError:
        return None


def reject_selectors(extra: list[str]) -> None:
    """A required category runs unfiltered, and the controller enforces that itself.

    Only the selector options are refused. Ordinary build and restore options pass, because a blanket
    rejection would stop the category from running at all instead of keeping it complete.
    """
    for argument in extra:
        if option_token(argument) in SELECTOR_OPTIONS:
            raise CategoryError(
                f"Required category refuses the selector '{argument}'. A required category runs unfiltered; "
                "a narrowed run is not a proof of the category."
            )


def run_category(category: str, project: str, evidence_dir: Path, extra: list[str]) -> dict[str, object]:
    reject_selectors(extra)

    evidence_dir.mkdir(parents=True, exist_ok=True)

    # The TRX is the raw output of one run: large, repetitive and reproducible by rerunning. It is
    # written under artifacts/, which .gitignore covers, so that it cannot accumulate in the tree the
    # way 434 MiB of it once did. What is kept beside the evidence is the record below, which names
    # every counter the TRX carried. tools/ci/policy_validator.py rejects a raw artifact under evidence/.
    run_output = RAW_RUN_OUTPUT_DIR
    run_output.mkdir(parents=True, exist_ok=True)
    trx_path = run_output / f"{category}.trx"
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

    started = time.monotonic()
    completed = subprocess.run(command, text=True, capture_output=True, env=environment)
    process_seconds = round(time.monotonic() - started, 3)

    counters = read_counters(trx_path)
    skipped = read_not_executed(trx_path)
    permitted = inventoried_cases(category)
    unlisted = unauthorised_not_executed(skipped, permitted) if permitted is not None else skipped

    record = {
        "schemaVersion": 2,
        "kind": "REQUIRED_CATEGORY_RESULT",
        "category": category,
        "project": project,
        # The argument array is the truth. The joined line is for humans and is quoted so that the
        # semicolon of the logger argument and the spaces of an absolute path survive a shell.
        "commandArguments": command,
        "command": shlex.join(command),
        "environment": {"TZ": environment["TZ"]},
        "timezone": environment["TZ"],
        "exitCode": completed.returncode,
        "counters": counters,
        "processWallDurationSeconds": process_seconds,
        "trxRunDurationSeconds": read_run_duration(trx_path),
        "trxProduced": trx_path.is_file(),
        "trxPath": trx_path.relative_to(REPOSITORY_ROOT).as_posix(),
        "notExecuted": skipped,
        "notExecutedUnlisted": unlisted,
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
    # A green category says nothing about the cases it never started. Every one of those has to be
    # named in the inventory, with a reason, before the category may be read as a proof.
    if permitted is None:
        raise CategoryError(
            f"Required category '{category}' has no readable not-executed inventory at "
            f"{VERIFICATION_MODEL.relative_to(REPO_ROOT)}; a required category may not skip silently."
        )
    floor = minimum_executed(category)
    if floor is not None and counters["executed"] < floor:
        raise CategoryError(
            f"Required category '{category}' executed {counters['executed']} case(s) but the inventory "
            f"records a floor of {floor}. A category that executes fewer cases than it did before has "
            "lost coverage, whether by a new exclusion, a renamed fixture or a filter, and a green "
            "result would hide exactly that."
        )
    if unlisted:
        raise CategoryError(
            f"Required category '{category}' did not execute {len(unlisted)} case(s) that the "
            f"inventory does not name: {', '.join(unlisted)}."
        )
    if completed.returncode != 0:
        raise CategoryError(f"Required category '{category}' exited with {completed.returncode}.")

    return record


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--category", required=True, help="Stable identifier of the required category.")
    parser.add_argument("--project", required=True, help="Test project or solution to run.")
    parser.add_argument("--evidence-dir", required=True, type=Path,
                        help="Where the counter record is written. The raw TRX goes to artifacts/run-output.")
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
