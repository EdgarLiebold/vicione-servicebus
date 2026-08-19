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
import secrets
import shlex
import signal
import subprocess
import sys
import time
import xml.etree.ElementTree as ElementTree
from collections import Counter
from datetime import datetime
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import run_ownership  # noqa: E402  (repository local, resolved from this file's folder)

TRX_NAMESPACE = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]

RAW_RUN_OUTPUT_DIR = REPOSITORY_ROOT / "artifacts" / "run-output"

# Ownership of the directory this run writes into lives in one place, because the broker runner and the
# canonical entry point have to agree with this one about what a claim is.
RUN_ROOT_VARIABLE = run_ownership.RUN_ROOT_VARIABLE
RUN_TOKEN_VARIABLE = run_ownership.RUN_TOKEN_VARIABLE
RUN_TOKEN_FILE = run_ownership.RUN_TOKEN_FILE

# How long the whole owned process tree is given to end after it was asked to, before the ask becomes
# a kill.
TERMINATION_GRACE_SECONDS = 20

# How long a green run's own process group is given to drain before a process still in it counts as a
# survivor. Measured: on a green core run an MSBuild node was still in the group the instant the child
# returned and was gone a moment later, so a census taken at that instant reports a survivor every
# time.
SURVIVOR_GRACE_SECONDS = 15

# Processes the .NET SDK deliberately keeps alive after a build, which are therefore not evidence that
# a run outlived itself. VBCSCompiler is the shared Roslyn compiler server: it idles for minutes by
# design so the next build reuses it, it is shared with every other build on this machine, and it holds
# nothing of this run - no fixture port, no file under the run root, no broker.
#
# Named rather than waited out. Its idle timeout is longer than any grace period this runner could
# sensibly have, so a longer wait would only turn a false finding into a slow false finding. Measured
# here: this control first fired on a leaked pid with no name, then on a green ActiveMQ category whose
# survivor was exactly this process.
SHARED_BUILD_SERVERS = ("VBCSCompiler",)


class CategoryError(RuntimeError):
    pass


TEST_TIMEZONE = "UTC"
ZERO_COUNTERS = {"total": 0, "executed": 0, "passed": 0, "failed": 0, "notExecuted": 0}

REPO_ROOT = Path(__file__).resolve().parents[2]
VERIFICATION_MODEL = REPO_ROOT / "build/verification/VERIFICATION_MODEL.json"


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


def category_contract(category: str, project: str) -> dict:
    """The one run of the model this invocation is allowed to be, or a refusal naming what is wrong.

    Fail closed in every direction. The three readers this replaced returned None for a missing model,
    an unknown category, an unreadable file or an absent floor, and the runner then skipped the very
    check that None stood for - so a category the model does not know ran without a floor, without an
    inventory and without anyone noticing. A policy gate would have caught most of it, but this runner
    is also the canonical local entry point and may not depend on another gate having run first.
    """
    if not VERIFICATION_MODEL.is_file():
        raise CategoryError(
            f"the verification model {VERIFICATION_MODEL.relative_to(REPO_ROOT)} is missing, so nothing "
            "states what this category is or what it may skip")
    try:
        model = json.loads(VERIFICATION_MODEL.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise CategoryError(f"the verification model is not readable: {error}") from error

    declared = [run for capability in model.get("capabilities", [])
                for run in capability.get("runs", [])
                if run.get("category") == category]
    if not declared:
        raise CategoryError(
            f"the verification model declares no run for category '{category}', so this invocation is "
            "not a required category of this repository")
    if len(declared) > 1:
        raise CategoryError(
            f"the verification model declares {len(declared)} runs for category '{category}', so which "
            "one this invocation is meant to be is undecided")

    run = declared[0]
    if run.get("project") != project:
        raise CategoryError(
            f"category '{category}' is declared against '{run.get('project')}' and was invoked against "
            f"'{project}'")
    if not (REPO_ROOT / project).is_file():
        raise CategoryError(f"category '{category}' names '{project}', which is not a project file")

    floor = run.get("minimumExecutedCases")
    if not isinstance(floor, int) or floor <= 0:
        raise CategoryError(
            f"category '{category}' declares no executed floor, so its case count could fall without a "
            "single failure")

    budget = run.get("budgetSeconds")
    if not isinstance(budget, (int, float)) or budget <= 0:
        raise CategoryError(
            f"category '{category}' declares no budget, so a test process that never returns would hold "
            "this run for as long as the machine stays up")

    return run


def permitted_not_executed(run: dict) -> list[str]:
    """The cases the model permits this category to leave unexecuted, as full identities.

    An entry without a full identity is not a permission: naming a case by its short form would
    authorise every case that happens to share it.
    """
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


def fixture_of(class_name: str) -> str:
    """The fixture identity of a TRX class attribute, without its parameterised arguments.

    A parameterised fixture reports as Namespace.Fixture(argument list). The anchor names the fixture,
    so the arguments come off before the two are compared - and only from the end, because a name is
    never compared as a prefix here.
    """
    head, opened, _ = type_name(class_name).partition("(")

    return head if opened else class_name


def read_executed(trx_path: Path) -> list[tuple[str, str]]:
    """(fixture, identity) of every case this run really executed.

    Read from the result list rather than from the definitions: a case that was defined and not started
    is not evidence of anything, and that difference is the whole point of binding an anchor to a run.
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
        definitions[definition.attrib.get("id")] = (
            fixture_of(method.attrib.get("className", "")), identity_of(method))

    executed = []
    for result in root.findall("t:Results/t:UnitTestResult", TRX_NAMESPACE):
        if result.attrib.get("outcome") == "NotExecuted":
            continue
        entry = definitions.get(result.attrib.get("testId"))
        if entry:
            executed.append(entry)

    return executed


def required_anchors(category: str) -> list[str]:
    """The fixtures this category has to have executed, because another capability leans on them.

    A capability without a test project of its own may say it is verified through the run of another
    one. That link used to be prose plus a name searched for in source text, which a comment satisfied
    just as well as a fixture. Here it becomes a claim about this run: the named fixture executed a case
    in it, or the category is not the proof it was declared to be.
    """
    try:
        model = json.loads(VERIFICATION_MODEL.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return []

    fixtures = []
    for capability in model.get("capabilities", []):
        for anchor in capability.get("testAnchors", []):
            if isinstance(anchor, dict) and anchor.get("category") == category and anchor.get("fixture"):
                fixtures.append(str(anchor["fixture"]))

    return sorted(set(fixtures))


def unproven_anchors(anchors: list[str], executed: list[tuple[str, str]]) -> list[str]:
    """The anchors this run executed no case for, which is the only way one is proven."""
    ran = {fixture for fixture, _ in executed}

    return sorted(anchor for anchor in anchors if anchor not in ran)


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


def claim_run_root() -> Path:
    """The root this run owns, minted here or handed down with the token that proves it.

    The decision itself lives in run_ownership, because the broker runner and the canonical entry point
    make the same one and two implementations of it would drift.
    """
    try:
        return run_ownership.claim_run_root(RAW_RUN_OUTPUT_DIR)
    except run_ownership.OwnershipError as error:
        raise CategoryError(str(error)) from error


def terminate_tree(child: subprocess.Popen) -> bool:
    """Asks the whole owned process tree to end, waits, and kills what is left. True when it killed.

    Signalled as a process group, which is why the child is started in a session of its own: dotnet
    test is a tree of MSBuild nodes, a vstest console and a test host, and signalling only the process
    that was started leaves the rest of them running. Three such trees survived more than thirteen
    hours on this machine because nothing ever asked them to stop.
    """
    try:
        os.killpg(child.pid, signal.SIGTERM)
    except (ProcessLookupError, PermissionError):
        return False

    deadline = time.monotonic() + TERMINATION_GRACE_SECONDS
    while time.monotonic() < deadline:
        if child.poll() is not None:
            return False
        time.sleep(0.2)

    try:
        os.killpg(child.pid, signal.SIGKILL)
    except (ProcessLookupError, PermissionError):
        return False

    return True


def surviving_owned_processes(group: int) -> list[str]:
    """Processes still in this run's own process group, read after a grace period, with what they are.

    The grace period is what makes this a finding rather than noise. Measured on a green core run: an
    MSBuild node was still in the group the instant the child returned and was gone shortly after, so
    a census taken at that moment reports a survivor on every successful run.

    The command line is part of the finding, not decoration. A survivor reported as a number alone is
    a mystery by the time anybody reads it - the process is gone and nothing says what leaked, which is
    exactly what happened the first time this control fired.
    """
    deadline = time.monotonic() + SURVIVOR_GRACE_SECONDS
    while True:
        listing = subprocess.run(["ps", "-Ao", "pid,pgid,command"], capture_output=True, text=True,
                                 check=False)
        survivors = []
        for line in listing.stdout.splitlines()[1:]:
            parts = line.split(None, 2)
            if len(parts) != 3 or not parts[1].isdigit():
                continue
            if int(parts[1]) != group or int(parts[0]) == group:
                continue
            if any(server in parts[2] for server in SHARED_BUILD_SERVERS):
                continue
            survivors.append(f"{parts[0]} {parts[2][:200]}")
        if not survivors or time.monotonic() >= deadline:
            return survivors
        time.sleep(0.5)


def run_child(command: list[str], environment: dict[str, str], budget: float) -> dict[str, object]:
    """Runs the test process under a finite budget, in a session it alone owns.

    The budget is the point. This runner is the canonical local entry point as well as the one CI
    calls, and a job level timeout in a workflow does nothing for a developer machine. Without it a
    test process that never returns holds the run for as long as the machine stays up.
    """
    started = time.monotonic()
    child = subprocess.Popen(command, env=environment, text=True, start_new_session=True,
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    timed_out = False
    killed = False
    try:
        out, err = child.communicate(timeout=budget)
    except subprocess.TimeoutExpired:
        timed_out = True
        killed = terminate_tree(child)
        try:
            out, err = child.communicate(timeout=TERMINATION_GRACE_SECONDS)
        except subprocess.TimeoutExpired:
            child.kill()
            out, err = "", ""

    return {
        "exitCode": child.returncode,
        "stdout": out or "",
        "stderr": err or "",
        "timedOut": timed_out,
        "escalatedToKill": killed,
        "seconds": round(time.monotonic() - started, 3),
        "survivingOwnedProcesses": surviving_owned_processes(child.pid),
    }


def omitted_results(trx_path: Path) -> list[str]:
    """Cases the result file defines and never reports a result for.

    A counter cannot show this: total counts definitions, and a definition whose result entry is
    missing is neither passed, failed nor not-executed. It is simply absent, and every count above it
    still adds up.
    """
    if not trx_path.is_file():
        return []
    try:
        root = ElementTree.parse(trx_path).getroot()
    except ElementTree.ParseError:
        return []

    defined = {}
    for definition in root.findall("t:TestDefinitions/t:UnitTest", TRX_NAMESPACE):
        method = definition.find("t:TestMethod", TRX_NAMESPACE)
        if method is not None:
            defined[definition.attrib.get("id")] = identity_of(method)

    reported = {result.attrib.get("testId") for result in root.findall("t:Results/t:UnitTestResult", TRX_NAMESPACE)}

    return sorted(identity for test_id, identity in defined.items() if test_id not in reported)


def run_category(category: str, project: str, evidence_dir: Path) -> dict[str, object]:
    # What this category is, before anything runs. An unknown category, a project the model does not
    # declare for it, a malformed model, a missing floor or a missing budget each end the run here
    # rather than after a green looking result.
    contract = category_contract(category, project)

    # Everything this run writes lives under one root that belongs to it alone, minted here or handed
    # down with a token this run can check.
    run_root = claim_run_root()

    # An explicit --evidence-dir is a parent, never the file. Two runs of one category pointed at the
    # same directory would otherwise overwrite each other's record, which is exactly what a caller
    # naming a stable path expects not to happen.
    evidence_dir = evidence_dir / run_root.name
    evidence_dir.mkdir(parents=True, exist_ok=True)

    # The TRX is the raw output of one run: large, repetitive and reproducible by rerunning. It is
    # written under artifacts/, which .gitignore covers, so that it cannot accumulate in the tree the
    # way 434 MiB of it once did. What is kept beside the evidence is the record below, which names
    # every counter the TRX carried. tools/ci/policy_validator.py rejects a raw artifact under evidence/.
    trx_path = run_root / f"{category}.trx"
    if trx_path.exists():
        trx_path.unlink()

    # One exact invocation, with nothing forwarded into it. A required proof is the whole category run
    # the same way every time, so there is no caller argument to weigh: the runner used to pass an
    # arbitrary tail through to dotnet test, and a second --logger, a --results-directory, a --diag, a
    # collector or an MSBuild output override each write files outside the root this run owns - while
    # the repository states that one run owns all of its mutable output. --framework and -c would
    # have changed what was measured on top of that.
    command = [
        "dotnet", "test", project,
        "-c", "Release",
        "--logger", f"trx;LogFileName={trx_path.resolve()}",
    ]
    # The suite is not timezone independent. Two cron cases build a template hour in one offset and
    # reuse it across 31 October 2010, the day a European DST transition falls; they pass in UTC and
    # fail in CEST. Pinning the zone is the same idea as pinning an image digest: the local run and
    # the runner must not differ by their environment. An explicit TZ from the caller is respected.
    environment = dict(os.environ)
    environment.setdefault("TZ", TEST_TIMEZONE)

    child = run_child(command, environment, float(contract["budgetSeconds"]))
    process_seconds = child["seconds"]

    counters = read_counters(trx_path)
    skipped = read_not_executed(trx_path)
    permitted = permitted_not_executed(contract)
    unlisted = unauthorised_not_executed(skipped, permitted)
    omitted = omitted_results(trx_path)

    executed = read_executed(trx_path)
    anchors = required_anchors(category)
    unproven = unproven_anchors(anchors, executed)
    # The identities that carry each anchor, so the record says which cases proved it rather than that
    # something did. A capability leaning on this run is only as good as this list.
    proof_of_anchor = {
        anchor: sorted(identity for fixture, identity in executed if fixture == anchor)
        for anchor in anchors
    }

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
        "exitCode": child["exitCode"],
        "budgetSeconds": contract["budgetSeconds"],
        "timedOut": child["timedOut"],
        "escalatedToKill": child["escalatedToKill"],
        "survivingOwnedProcesses": child["survivingOwnedProcesses"],
        "counters": counters,
        "processWallDurationSeconds": process_seconds,
        "trxRunDurationSeconds": read_run_duration(trx_path),
        "trxProduced": trx_path.is_file(),
        "trxPath": trx_path.relative_to(REPOSITORY_ROOT).as_posix(),
        "runRoot": run_root.relative_to(REPOSITORY_ROOT).as_posix(),
        "evidenceDir": evidence_dir.relative_to(REPOSITORY_ROOT).as_posix()
        if evidence_dir.is_relative_to(REPOSITORY_ROOT) else str(evidence_dir),
        "notExecuted": skipped,
        "notExecutedUnlisted": unlisted,
        "resultsOmitted": omitted,
        "verifiedAnchors": proof_of_anchor,
        "unprovenAnchors": unproven,
    }
    # Written before the gate is evaluated: a rejected category must leave evidence too.
    (evidence_dir / f"{category}.json").write_text(
        json.dumps(record, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )

    sys.stdout.write(child["stdout"])
    sys.stderr.write(child["stderr"])

    if child["timedOut"]:
        raise CategoryError(
            f"Required category '{category}' did not finish within its budget of "
            f"{contract['budgetSeconds']} s. The whole owned process tree was taken down"
            + (" and had to be killed" if child["escalatedToKill"] else "")
            + f"; {len(child['survivingOwnedProcesses'])} process(es) of it survived that."
        )
    if child["survivingOwnedProcesses"]:
        raise CategoryError(
            f"Required category '{category}' left {len(child['survivingOwnedProcesses'])} of its own "
            f"process(es) behind: {child['survivingOwnedProcesses']}. A run that outlives itself holds "
            "ports, files and a fixture that the next run then meets."
        )
    if counters["total"] <= 0:
        raise CategoryError(f"Required category '{category}' executed 0 tests; a required category may never be empty.")
    if counters["executed"] <= 0:
        raise CategoryError(f"Required category '{category}' executed no test at all; every test was skipped or filtered.")
    if counters["failed"] > 0:
        raise CategoryError(f"Required category '{category}' reported {counters['failed']} failing test(s).")
    # A result file may define a case and never report a result for it. No counter shows that: total
    # counts definitions, and such a case is neither passed, failed nor not-executed, so every number
    # above it still adds up.
    if omitted:
        raise CategoryError(
            f"Required category '{category}' defines {len(omitted)} case(s) the result file reports no "
            f"result for: {', '.join(omitted[:5])}{' …' if len(omitted) > 5 else ''}. A counter cannot "
            "show an omitted result, so it is compared here."
        )
    floor = contract["minimumExecutedCases"]
    if counters["executed"] < floor:
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
    if unproven:
        raise CategoryError(
            f"Required category '{category}' executed no case of {len(unproven)} fixture(s) that another "
            f"capability is verified through: {', '.join(unproven)}. A capability that leans on this run "
            "is proven by the cases that ran in it, not by a name that appears in a source file."
        )
    if child["exitCode"] != 0:
        raise CategoryError(f"Required category '{category}' exited with {child['exitCode']}.")

    return record


def build_parser() -> argparse.ArgumentParser:
    """The command line of this runner, as an object, and a closed one.

    Closed in both directions: nothing is forwarded to dotnet test, and nothing unknown is accepted.
    It is separate from main so that tools/ci/policy_validator.py can reconstruct the argument vector
    a workflow step really produces and parse it against exactly this parser.
    """
    parser = argparse.ArgumentParser(
        description=__doc__,
        epilog="The invocation is fixed. A required category is a proof of the whole category, run the "
               "same way every time, so this runner takes no further argument and forwards none.")
    parser.add_argument("--category", required=True, help="Stable identifier of the required category.")
    parser.add_argument("--project", required=True, help="Test project or solution to run.")
    parser.add_argument("--evidence-dir", required=True, type=Path,
                        help="Where the counter record is written. The raw TRX goes to artifacts/run-output.")

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    try:
        record = run_category(args.category, args.project, args.evidence_dir)
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
