#!/usr/bin/env python3
"""The one command that verifies this repository.

    python3 tools/ci/verify.py --selection all
    python3 tools/ci/verify.py --selection activemq

One entry point, one responsibility, and no orchestration duplicated into YAML. What a selection
contains, which categories need the pinned fixture, how long each of them may take and which cases it
is expected to execute all live in build/verification/VERIFICATION_MODEL.json, which is the
independent expected truth. The workflow only names a selection.

The terminal answer is a set comparison, not a count. A category passes when the identities it
executed are exactly the identities the model expects of it - nothing missing, nothing unexpected,
nothing twice, nothing failed, and nothing skipped that the model did not approve. A count can stay
constant while one case is dropped and another is added, which is precisely the shape a floor cannot
see.

What authorises a pass is the exit status of this command inside the required check. The receipt every
run writes is derived audit evidence: it records what happened, bound to the commit, the tree and the
model it was produced against, and tools/ci/validate_receipt.py checks that it is internally
consistent and that it agrees with the native result files where those are still beside it. A receipt
presented without them is a consistency checked record and nothing more - it cannot show by itself
that a test process ever started, and it does not claim to.

Standard library only.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import subprocess
import sys
import xml.etree.ElementTree as ElementTree
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import run_ownership  # noqa: E402  (repository local, resolved from this file's folder)
import run_test_category  # noqa: E402
import verification_model  # noqa: E402

REPO_ROOT = Path(__file__).resolve().parents[2]
MODEL_FILE = REPO_ROOT / "build/verification/VERIFICATION_MODEL.json"
RAW_RUN_OUTPUT_DIR = REPO_ROOT / "artifacts" / "run-output"
RECEIPT_NAME = "verification-receipt.json"
RECEIPT_KIND = "SERVICEBUS_VERIFICATION_RECEIPT"
RECEIPT_SCHEMA_VERSION = 2

FIXTURE_RECORD_NAME = "fixture-findings.json"
FIXTURE_RECORD_KIND = "SERVICEBUS_FIXTURE_FINDINGS"
FIXTURE_RECORD_SCHEMA_VERSION = 1


class VerificationError(RuntimeError):
    """Raised when the run cannot be carried out at all, as opposed to failing on its result."""


# -- the model -----------------------------------------------------------------------------------

def load_model() -> tuple[dict, str]:
    """The model and the hash of the exact bytes that were read.

    The hash goes into the receipt, so a receipt cannot be presented against a model that has changed
    since it was produced.
    """
    if not MODEL_FILE.is_file():
        raise VerificationError(f"{MODEL_FILE.relative_to(REPO_ROOT)} is missing, so nothing states "
                                "what this repository verifies")
    raw = MODEL_FILE.read_bytes()
    try:
        model = json.loads(raw.decode("utf-8"))
    except json.JSONDecodeError as error:
        raise VerificationError(f"the verification model is not readable: {error}") from error

    return model, hashlib.sha256(raw).hexdigest()


def resolve_selection(model: dict, name: str) -> list[str]:
    """The categories a selection names, transitively.

    The resolution itself lives in verification_model, because the policy validator has to reach the
    same answer this entry point does and two implementations of one rule are two rules.
    """
    try:
        resolved = verification_model.resolve_selection(model, name)
    except verification_model.SelectionError as error:
        raise VerificationError(str(error)) from error

    if not resolved:
        raise VerificationError(f"selection '{name}' resolves to no category at all")

    return resolved


def declared_run(model: dict, category: str) -> dict:
    runs = [run for capability in model.get("capabilities", [])
            for run in capability.get("runs", []) if run.get("category") == category]
    if len(runs) != 1:
        raise VerificationError(
            f"the model declares {len(runs)} runs for category '{category}', so which one is meant is "
            "undecided")

    return runs[0]


# -- expected identities -------------------------------------------------------------------------

def read_identity_file(path: Path) -> list[str]:
    """One identity per line, comments and blank lines ignored."""
    identities = []
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped and not stripped.startswith("#"):
            identities.append(stripped)

    return identities


def expected_identities(run: dict) -> list[str] | None:
    """What this category is expected to execute, or None when nothing has recorded it yet.

    None is a state, not an omission. A category whose expected set nobody has recorded from a
    complete clean run cannot be part of a passing receipt, and the receipt says which ones those are.
    """
    declared = run.get("expectedIdentities")
    if not declared:
        return None

    path = REPO_ROOT / declared
    if not path.is_file():
        raise VerificationError(
            f"category '{run.get('category')}' names the expected set '{declared}', which is not there")

    return read_identity_file(path)


# -- the native result file ------------------------------------------------------------------------

RESULT_COUNTER_NAMES = ("total", "executed", "passed", "failed", "notExecuted")


def unread_result(findings: list[str], present: bool, sha256: str | None) -> dict[str, object]:
    return {"present": present, "sha256": sha256, "executed": [], "failed": [], "skipped": [],
            "omitted": [], "counters": {}, "findings": findings}


def parse_result_file(trx_path: Path) -> dict[str, object]:
    """Everything the test platform's own result file says, and everything wrong with it as a file.

    Read fail closed, because every shape rejected here is a way for a set to look complete while it
    is not. A result naming a test id the file defines no case for belongs to nothing and used to be
    passed over in silence. A definition id that appears twice leaves the case a result belongs to
    undecided. A summary whose counters disagree with the results underneath it is either not about
    this run or not about these cases, and either way its numbers may not be repeated.

    notExecuted is carried into the record and deliberately not compared with it: measured on a real
    run of the rabbitmq category, the writer reported notExecuted="0" for a file holding twenty
    NotExecuted results. That counter carries no information, which is why every identity here is read
    from the results themselves.
    """
    if not trx_path.is_file():
        return unread_result(
            [f"'{trx_path.name}' was never written, so this run left no native result to read"],
            present=False, sha256=None)

    raw = trx_path.read_bytes()
    sha256 = hashlib.sha256(raw).hexdigest()
    try:
        root = ElementTree.fromstring(raw)
    except ElementTree.ParseError as error:
        return unread_result([f"the native result file is not readable: {error}"], True, sha256)

    namespace = run_test_category.TRX_NAMESPACE
    findings: list[str] = []

    definitions: dict[str, str] = {}
    for definition in root.findall("t:TestDefinitions/t:UnitTest", namespace):
        test_id = definition.attrib.get("id")
        method = definition.find("t:TestMethod", namespace)
        if not test_id or method is None:
            findings.append("a case is defined without an id or without a method, so no result can be "
                            "attributed to it")
            continue
        if test_id in definitions:
            findings.append(f"test id {test_id} is defined more than once, so which case a result "
                            "naming it belongs to is undecided")
            continue
        definitions[test_id] = run_test_category.identity_of(method)

    executed: list[str] = []
    failed: list[str] = []
    skipped: list[str] = []
    reported: Counter = Counter()
    outcomes: Counter = Counter()
    results = root.findall("t:Results/t:UnitTestResult", namespace)
    for result in results:
        test_id = result.attrib.get("testId") or ""
        outcome = result.attrib.get("outcome")
        outcomes[outcome] += 1
        identity = definitions.get(test_id)
        if identity is None:
            findings.append(f"a result names test id '{test_id}', which this file defines no case for")
            continue
        reported[test_id] += 1
        if outcome == "NotExecuted":
            skipped.append(identity)
            continue
        executed.append(identity)
        if outcome != "Passed":
            failed.append(identity)

    for test_id, count in sorted(reported.items()):
        if count > 1:
            findings.append(f"'{definitions[test_id]}' is reported {count} times under one test id")

    summary = root.find("t:ResultSummary/t:Counters", namespace)
    counters: dict[str, int] = {}
    if summary is None:
        findings.append("the result file carries no counter summary at all")
    else:
        try:
            counters = {name: int(summary.attrib.get(name, "0")) for name in RESULT_COUNTER_NAMES}
        except ValueError:
            findings.append("the result file's counters are not numbers")
        else:
            measured = {"total": len(results),
                        "executed": len(results) - outcomes["NotExecuted"],
                        "passed": outcomes["Passed"],
                        "failed": outcomes["Failed"]}
            for name, value in sorted(measured.items()):
                if counters[name] != value:
                    findings.append(f"the summary counts {counters[name]} {name} and the file carries "
                                    f"{value}")

    omitted = sorted(identity for test_id, identity in definitions.items() if test_id not in reported)

    return {"present": True, "sha256": sha256, "executed": executed, "failed": failed,
            "skipped": skipped, "omitted": omitted, "counters": counters, "findings": findings}


def compare_identities(expected: list[str], executed: list[str]) -> dict[str, list[str]]:
    """The exact set comparison the terminal answer is read from.

    Counted rather than set-subtracted, so a case that ran twice is visible. A plain set difference
    would report nothing for a run that executed one identity twice and another not at all while the
    total stayed the same.
    """
    wanted, ran = Counter(expected), Counter(executed)

    return {
        "missing": sorted((wanted - ran).elements()),
        "unexpected": sorted((ran - wanted).elements()),
        "duplicate": sorted(identity for identity, count in ran.items() if count > 1),
    }


# -- the fixture side of a run ---------------------------------------------------------------------

FIXTURE_RECORD_FIELDS = ("schemaVersion", "kind", "brokers", "allowedBrokerOutage", "findings", "logs")


def fixture_record_findings(record: dict, run: dict) -> list[str]:
    """Whether this fixture record really is the record of this category's fixture.

    A record that names other brokers, another outage permission or another kind of document is
    evidence about something else. It is refused here rather than counted as fixture proof, because a
    record nobody compares with the declaration is a file, not a binding.
    """
    findings = []
    declared = sorted(run.get("brokers") or [])

    if record.get("kind") != FIXTURE_RECORD_KIND:
        findings.append(f"the record is of kind {record.get('kind')!r} and not a fixture record")
    if record.get("schemaVersion") != FIXTURE_RECORD_SCHEMA_VERSION:
        findings.append(f"the record speaks schema version {record.get('schemaVersion')!r}, this "
                        f"reader speaks {FIXTURE_RECORD_SCHEMA_VERSION}")
    for name in sorted(record):
        if name not in FIXTURE_RECORD_FIELDS:
            findings.append(f"the record carries an unknown field '{name}'")

    brokers = record.get("brokers")
    if not isinstance(brokers, list) or not all(isinstance(entry, str) for entry in brokers):
        findings.append("the record names no broker list")
    elif sorted(brokers) != declared:
        findings.append(f"the record is about {sorted(brokers)} and this category runs against "
                        f"{declared}")

    permitted = run.get("allowBrokerOutage")
    if record.get("allowedBrokerOutage") != permitted:
        findings.append(f"the record was produced with outage permission "
                        f"{record.get('allowedBrokerOutage')!r} and this category declares "
                        f"{permitted!r}")

    reported = record.get("findings")
    if not isinstance(reported, list) or not all(isinstance(entry, str) for entry in reported):
        findings.append("the record's own findings are not a list of sentences")
    else:
        findings.extend(reported)

    logs = record.get("logs")
    if not isinstance(logs, dict):
        findings.append("the record carries no broker log digests")
    else:
        for broker in declared:
            if not isinstance(logs.get(broker), str) or not logs.get(broker):
                findings.append(f"the record holds no log digest for '{broker}', so nothing binds that "
                                "broker's output to this run")

    return findings


def fixture_evidence(run_root: Path, run: dict) -> dict[str, object]:
    """What the fixture side of this run proved, and what is unproven when it wrote nothing.

    A category that names brokers is a category whose result only means something if the fixture was
    there, was the declared one and came back. A run of it that left no record has shown none of that.
    Absence used to be read as "no finding", which is the difference between nothing went wrong and
    nothing was looked at.

    Read from the file the fixture wrote rather than from a child's output: recognising a cleanup
    failure by matching prose in stderr would be reading a sentence, and a sentence is not a contract.
    """
    declared = sorted(run.get("brokers") or [])
    path = run_root / FIXTURE_RECORD_NAME

    if not path.is_file():
        if declared:
            return {"recorded": False, "sha256": None, "logs": {}, "findings": [
                f"this category runs against {', '.join(declared)} and no fixture record was written, "
                "so nothing shows that the fixture was started, was the declared one, or was returned"]}

        return {"recorded": False, "sha256": None, "logs": {}, "findings": []}

    sha256 = digest(path)
    try:
        record = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        return {"recorded": True, "sha256": sha256, "logs": {},
                "findings": [f"the fixture record of this run is not readable: {error}"]}
    if not isinstance(record, dict):
        return {"recorded": True, "sha256": sha256, "logs": {},
                "findings": ["the fixture record of this run is not a record"]}

    logs = record.get("logs") if isinstance(record.get("logs"), dict) else {}

    return {"recorded": True, "sha256": sha256, "logs": logs,
            "findings": fixture_record_findings(record, run)}


# -- running one category ------------------------------------------------------------------------

EVIDENCE_DIR_OPTION = "--evidence-dir"


def child_command(run: dict, evidence_dir: Path) -> list[str]:
    """The exact command for this category, built from the model rather than from a workflow.

    A category with no broker goes straight to the category runner. One with brokers goes through the
    broker runner, which owns the fixture and hands the same run root down again.
    """
    category, project = run["category"], run["project"]
    if not run.get("brokers"):
        return [sys.executable, str(REPO_ROOT / "tools/ci/run_test_category.py"),
                "--category", category, "--project", project, EVIDENCE_DIR_OPTION, str(evidence_dir)]

    command = [sys.executable, str(REPO_ROOT / "tools/ci/run_broker_category.py")]
    for broker in run["brokers"]:
        command += ["--broker", broker]
    if run.get("allowBrokerOutage"):
        command += ["--allow-broker-outage", run["allowBrokerOutage"]]
    command += ["--category", category, "--project", project, EVIDENCE_DIR_OPTION, str(evidence_dir)]
    if run.get("oneRefusalPerVhost"):
        command += ["--one-refusal-per-vhost", run["oneRefusalPerVhost"]]

    return command


def without_evidence_dir(command: list[str]) -> list[str]:
    """The command without the one value a caller chooses, so two of them can be compared."""
    remaining = list(command)
    if EVIDENCE_DIR_OPTION in remaining:
        index = remaining.index(EVIDENCE_DIR_OPTION)
        del remaining[index:index + 2]

    return remaining


def command_findings(command: list[str], run: dict) -> list[str]:
    """Whether the command a receipt reports is the command this category is declared to run.

    Compared with the model rather than believed. Everything that decides what ran - the runner, the
    category, the project, the brokers, the outage permission - comes from the model and has to match
    it, so a record that says one category and ran another is refused instead of read.
    """
    reported = without_evidence_dir(command)[1:]
    canonical = without_evidence_dir(child_command(run, Path("<evidence>")))[1:]
    if reported != canonical:
        return [f"the reported command is {reported} and this category is declared to run {canonical}"]

    return []


def category_findings(entry: dict) -> list[str]:
    """Everything wrong with one category, read from what that category itself reports.

    The single place a category's verdict is decided. The run that produces a receipt and the reader
    that is handed one both come through here, so a receipt can never assert a conclusion its own
    numbers do not carry: the reader recomputes this from the entry's primary facts and compares it
    with what the entry claims.
    """
    findings: list[str] = []

    if not entry["expectedRecorded"]:
        findings.append("no expected identity set is recorded for this category, so its completeness "
                        "is not proven by this run")
    if entry["timedOut"]:
        findings.append(f"the child did not finish within {entry['budgetSeconds']} s and its tree was "
                        "taken down")
    if entry["survivingOwnedProcesses"]:
        findings.append(f"{len(entry['survivingOwnedProcesses'])} process(es) of this run survived it")
    if entry["childExitCode"] != 0:
        findings.append(f"the child exited with {entry['childExitCode']}")
    for name in ("missing", "unexpected", "duplicate"):
        if entry[name]:
            findings.append(f"{len(entry[name])} {name} identity/identities")
    if entry["failed"]:
        findings.append(f"{len(entry['failed'])} failed identity/identities")
    if entry["unapprovedNotExecuted"]:
        findings.append(f"{len(entry['unapprovedNotExecuted'])} unapproved not-executed "
                        "identity/identities")
    if entry["resultsOmitted"]:
        findings.append(f"{len(entry['resultsOmitted'])} defined case(s) the result file reports no "
                        "result for")
    for problem in entry["resultFindings"]:
        findings.append(f"result file: {problem}")
    for problem in entry["fixtureFindings"]:
        findings.append(f"fixture: {problem}")

    return findings


def derived_fields(entry: dict) -> dict[str, object]:
    """The fields of a category record that are functions of its primary facts, recomputed.

    passed, missing, unexpected, duplicate and unapprovedNotExecuted are named in the receipt so that a
    reader does not have to subtract one list from another. Naming them also makes them assertable, and
    an assertion nobody recomputes is where a fabricated receipt lives: expected three identities,
    executed one, missing none.
    """
    expected, executed = entry["expected"], entry["executed"]
    comparison = ({"missing": [], "unexpected": [], "duplicate": []} if expected is None
                  else compare_identities(expected, executed))

    return {
        "passed": sorted((Counter(executed) - Counter(entry["failed"])).elements()),
        "unapprovedNotExecuted": sorted(
            (Counter(entry["skipped"]) - Counter(entry["approvedNotExecuted"])).elements()),
        "expectedRecorded": expected is not None,
        "missing": comparison["missing"],
        "unexpected": comparison["unexpected"],
        "duplicate": comparison["duplicate"],
    }


def verify_category(run: dict, evidence_parent: Path) -> dict[str, object]:
    """Runs one category under its own root and budget, and compares its result set with the model."""
    category = run["category"]
    run_root, token = run_ownership.mint_run_root(RAW_RUN_OUTPUT_DIR)
    environment = run_ownership.handover(run_root, token, dict(os.environ))
    command = child_command(run, evidence_parent / category)

    print(f"verify {category}: {run_root.name}, budget {run['budgetSeconds']} s", flush=True)
    child = run_test_category.run_child(command, environment, float(run["budgetSeconds"]))
    sys.stdout.write(child["stdout"])
    sys.stderr.write(child["stderr"])

    result = parse_result_file(run_root / f"{category}.trx")
    fixture = fixture_evidence(run_root, run)
    expected = expected_identities(run)

    entry = {
        "category": category,
        "project": run["project"],
        "runRoot": run_root.relative_to(REPO_ROOT).as_posix(),
        "command": command,
        "budgetSeconds": run["budgetSeconds"],
        "brokers": list(run.get("brokers") or []),
        "allowBrokerOutage": run.get("allowBrokerOutage"),
        "childExitCode": child["exitCode"],
        "timedOut": child["timedOut"],
        "escalatedToKill": child["escalatedToKill"],
        "survivingOwnedProcesses": child["survivingOwnedProcesses"],
        "expected": sorted(expected) if expected is not None else None,
        "executed": sorted(result["executed"]),
        "failed": sorted(result["failed"]),
        "skipped": sorted(result["skipped"]),
        "approvedNotExecuted": sorted(run_test_category.permitted_not_executed(run)),
        "resultsOmitted": result["omitted"],
        "resultCounters": result["counters"],
        "resultFindings": result["findings"],
        "rawResultSha256": result["sha256"],
        "fixtureRecorded": fixture["recorded"],
        "fixtureFindings": fixture["findings"],
        "fixtureRecordSha256": fixture["sha256"],
        "brokerLogSha256": fixture["logs"],
    }
    entry.update(derived_fields(entry))
    entry["findings"] = category_findings(entry)
    entry["terminal"] = "PASS" if not entry["findings"] else "FAIL"

    return entry


EXPECTED_DIR = "build/verification/expected"


def record_expected(model: dict, result: dict) -> str:
    """Writes the expected identity set of one category from the run that just produced it.

    Only from a run that was otherwise clean. A set recorded from a run with a failure, an unapproved
    skip or a taken-down child would bake that state in as the expectation, and every later run would
    then agree with it.

    The set comes from the test platform's own result file. Nothing in a test declares that it ran.
    """
    category = result["category"]
    # A difference from the previous expectation is not a defect of this run - it is exactly what
    # recording resolves, and a test that was legitimately added or removed lands here. A defect of the
    # run is something else: a case that failed, a skip nobody approved, a result the file never
    # reported, a child that was taken down or a process that outlived it. Recording from one of those
    # would bake that state in as the expectation, and every later run would then agree with it.
    difference = ("no expected identity set", "missing identity", "unexpected identity",
                  "duplicate identity")
    blocking = [finding for finding in result["findings"]
                if not any(finding.startswith(kind) or kind in finding for kind in difference)]
    if blocking:
        raise VerificationError(
            f"category '{category}' cannot record an expected set from this run: {'; '.join(blocking)}")

    target = REPO_ROOT / EXPECTED_DIR / f"{category}.txt"
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(
        f"# The identities category '{category}' is expected to execute.\n"
        "# Generated by tools/ci/verify.py --record-expected from a clean run of that category.\n"
        "# Never edited by hand: it is the result evidence of the test platform, not a wish list.\n"
        + "".join(f"{identity}\n" for identity in sorted(result["executed"])),
        encoding="utf-8")

    relative = target.relative_to(REPO_ROOT).as_posix()
    for capability in model.get("capabilities", []):
        for run in capability.get("runs", []):
            if run.get("category") == category:
                run["expectedIdentities"] = relative
    MODEL_FILE.write_text(json.dumps(model, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    return relative


def digest(path: Path) -> str | None:
    if not path.is_file():
        return None

    return hashlib.sha256(path.read_bytes()).hexdigest()


# -- the shape of a receipt --------------------------------------------------------------------

def identity_list(value: object) -> bool:
    return isinstance(value, list) and all(isinstance(entry, str) for entry in value)


def optional_identity_list(value: object) -> bool:
    return value is None or identity_list(value)


def text(value: object) -> bool:
    return isinstance(value, str)


def optional_text(value: object) -> bool:
    return value is None or isinstance(value, str)


def flag(value: object) -> bool:
    return isinstance(value, bool)


def number(value: object) -> bool:
    return isinstance(value, (int, float)) and not isinstance(value, bool)


def optional_number(value: object) -> bool:
    return value is None or number(value)


def counter_map(value: object) -> bool:
    return isinstance(value, dict) and all(
        isinstance(name, str) and isinstance(count, int) and not isinstance(count, bool)
        for name, count in value.items())


def digest_map(value: object) -> bool:
    return isinstance(value, dict) and all(
        isinstance(name, str) and optional_text(entry) for name, entry in value.items())


def record_list(value: object) -> bool:
    return isinstance(value, list) and all(isinstance(entry, dict) for entry in value)


RECEIPT_FIELDS = {
    "schemaVersion": (number, "a number"),
    "kind": (text, "a string"),
    "runId": (text, "a string"),
    "commit": (text, "a string"),
    "tree": (text, "a string"),
    "worktreeClean": (flag, "true or false"),
    "verificationModelSha256": (text, "a string"),
    "selection": (text, "a string"),
    "resolvedCategories": (identity_list, "a list of category names"),
    "startedUtc": (text, "a string"),
    "finishedUtc": (text, "a string"),
    "categories": (record_list, "a list of category records"),
    "terminal": (text, "a string"),
}

CATEGORY_FIELDS = {
    "category": (text, "a string"),
    "project": (text, "a string"),
    "runRoot": (text, "a string"),
    "command": (identity_list, "a list of command arguments"),
    "budgetSeconds": (number, "a number"),
    "brokers": (identity_list, "a list of broker names"),
    "allowBrokerOutage": (optional_text, "a broker name or null"),
    "childExitCode": (optional_number, "a number or null"),
    "timedOut": (flag, "true or false"),
    "escalatedToKill": (flag, "true or false"),
    "survivingOwnedProcesses": (identity_list, "a list of survivors"),
    "expectedRecorded": (flag, "true or false"),
    "expected": (optional_identity_list, "a list of identities or null"),
    "executed": (identity_list, "a list of identities"),
    "passed": (identity_list, "a list of identities"),
    "failed": (identity_list, "a list of identities"),
    "skipped": (identity_list, "a list of identities"),
    "approvedNotExecuted": (identity_list, "a list of identities"),
    "unapprovedNotExecuted": (identity_list, "a list of identities"),
    "missing": (identity_list, "a list of identities"),
    "unexpected": (identity_list, "a list of identities"),
    "duplicate": (identity_list, "a list of identities"),
    "resultsOmitted": (identity_list, "a list of identities"),
    "resultCounters": (counter_map, "a map of counter names to whole numbers"),
    "resultFindings": (identity_list, "a list of sentences"),
    "rawResultSha256": (optional_text, "a digest or null"),
    "fixtureRecorded": (flag, "true or false"),
    "fixtureFindings": (identity_list, "a list of sentences"),
    "fixtureRecordSha256": (optional_text, "a digest or null"),
    "brokerLogSha256": (digest_map, "a map of broker names to digests"),
    "findings": (identity_list, "a list of sentences"),
    "terminal": (text, "a string"),
}

DERIVED_FIELDS = ("duplicate", "expectedRecorded", "missing", "passed", "unapprovedNotExecuted",
                  "unexpected")


def shape_findings(mapping: dict, fields: dict, where: str) -> list[str]:
    """A closed shape: every declared field present and of its kind, and no field beyond them.

    Closed in both directions on purpose. A missing field cannot be checked at all, and an unknown one
    is either a document of another schema being read as this one or a field somebody added to carry a
    claim this reader does not check. Neither may pass quietly.
    """
    problems = []
    for name, (accepts, description) in sorted(fields.items()):
        if name not in mapping:
            problems.append(f"{where} has no '{name}'")
        elif not accepts(mapping[name]):
            problems.append(f"{where} field '{name}' is not {description}")
    for name in sorted(mapping):
        if name not in fields:
            problems.append(f"{where} carries an unknown field '{name}'")

    return problems


def summarise(value: object) -> str:
    if isinstance(value, list) and len(value) > 3:
        return f"{len(value)} entries"

    return str(value)


def derivation_findings(entry: dict) -> list[str]:
    """Where a category record disagrees with itself.

    This is the whole reason a receipt is worth reading. Every field a reader would otherwise take on
    trust is a function of the primary ones, so it is recomputed and compared: a record claiming three
    expected identities, one executed identity and nothing missing is refused here by arithmetic rather
    than believed by shape.
    """
    category = entry["category"]
    problems = []

    recomputed = derived_fields(entry)
    for name in DERIVED_FIELDS:
        if entry[name] != recomputed[name]:
            problems.append(f"category '{category}' declares {name}={summarise(entry[name])} and its "
                            f"own expected, executed, failed and skipped sets give "
                            f"{summarise(recomputed[name])}")

    derived = category_findings({**entry, **recomputed})
    if entry["findings"] != derived:
        problems.append(f"category '{category}' declares {len(entry['findings'])} finding(s) and its "
                        f"own facts give {len(derived)}")
    terminal = "PASS" if not derived else "FAIL"
    if entry["terminal"] != terminal:
        problems.append(f"category '{category}' calls itself {entry['terminal']!r} and its own facts "
                        f"make it {terminal}")
    for finding in derived:
        problems.append(f"category '{category}': {finding}")

    return problems


def declaration_findings(entry: dict, run: dict) -> list[str]:
    """Where a category record disagrees with the model it claims to have run."""
    category = entry["category"]
    problems = []

    if entry["project"] != run.get("project"):
        problems.append(f"category '{category}' names project '{entry['project']}' and the model "
                        f"declares '{run.get('project')}'")
    if sorted(entry["brokers"]) != sorted(run.get("brokers") or []):
        problems.append(f"category '{category}' names brokers {sorted(entry['brokers'])} and the model "
                        f"declares {sorted(run.get('brokers') or [])}")
    if entry["allowBrokerOutage"] != run.get("allowBrokerOutage"):
        problems.append(f"category '{category}' names outage permission "
                        f"{entry['allowBrokerOutage']!r} and the model declares "
                        f"{run.get('allowBrokerOutage')!r}")
    if entry["brokers"] and not entry["fixtureRecorded"]:
        problems.append(f"category '{category}' runs against a fixture and reports no fixture record, "
                        "so it carries no fixture proof")
    problems.extend(f"category '{category}': {problem}"
                    for problem in command_findings(entry["command"], run))

    return problems


def reparse_findings(entry: dict) -> tuple[list[str], bool]:
    """The record checked against the native evidence, where that evidence is still beside the receipt.

    This is the only part of reading a receipt that is not a consistency check. Where the result file
    is still there it is hashed and parsed again and the record has to agree with it. Where it is not,
    this returns no finding and says so, because a receipt on its own records what a run reported and
    cannot show that the run happened.
    """
    category = entry["category"]
    trx_path = REPO_ROOT / entry["runRoot"] / f"{category}.trx"
    if not trx_path.is_file():
        return [], False

    if digest(trx_path) != entry["rawResultSha256"]:
        return [f"category '{category}' names a result file whose bytes have changed since the receipt "
                "was written"], True

    problems = []
    parsed = parse_result_file(trx_path)
    for name in ("executed", "failed", "skipped"):
        if sorted(parsed[name]) != sorted(entry[name]):
            problems.append(f"category '{category}' reports {len(entry[name])} {name} "
                            f"identity/identities and its result file carries {len(parsed[name])}")
    if sorted(parsed["omitted"]) != sorted(entry["resultsOmitted"]):
        problems.append(f"category '{category}' reports {len(entry['resultsOmitted'])} omitted "
                        f"result(s) and its result file carries {len(parsed['omitted'])}")
    if sorted(parsed["findings"]) != sorted(entry["resultFindings"]):
        problems.append(f"category '{category}' reports {len(entry['resultFindings'])} finding(s) "
                        f"about its result file and reading it again gives {len(parsed['findings'])}")

    return problems, True


def receipt_findings(receipt: dict, selection: str, commit: str, tree: str, model_hash: str,
                     expected_categories: list[str], model: dict | None = None) -> list[str]:
    """Everything wrong with this receipt, as sentences. Empty means it is internally sound.

    Internally sound is what this can decide. Every derived number is recomputed from the primary facts
    the receipt states, every field is checked against a closed shape, and every claim about what ran
    is checked against the model. Where the native result files are still beside the receipt they are
    hashed and parsed again as well; that part, and only that part, is evidence from outside the
    receipt.
    """
    problems: list[str] = []

    if receipt.get("kind") != RECEIPT_KIND:
        problems.append(f"this is not a verification receipt: kind is {receipt.get('kind')!r}")
        return problems
    if receipt.get("schemaVersion") != RECEIPT_SCHEMA_VERSION:
        problems.append(f"the receipt speaks schema version {receipt.get('schemaVersion')!r}, this "
                        f"reader speaks {RECEIPT_SCHEMA_VERSION}")
        return problems

    problems.extend(shape_findings(receipt, RECEIPT_FIELDS, "the receipt"))
    if problems:
        return problems

    if receipt["selection"] != selection:
        problems.append(
            f"the receipt is about selection '{receipt['selection']}' and was presented for "
            f"'{selection}'. A narrower run can never satisfy a wider one")
    if commit and receipt["commit"] != commit:
        problems.append(f"the receipt was produced against commit {receipt['commit']}, and this is "
                        f"{commit}")
    if tree and receipt["tree"] != tree:
        problems.append(f"the receipt was produced against tree {receipt['tree']}, and this is {tree}")
    if receipt["worktreeClean"] is not True:
        problems.append("the receipt was produced from a working tree that was not clean, so it is not "
                        "about the committed state it names")
    if model_hash and receipt["verificationModelSha256"] != model_hash:
        problems.append("the receipt was produced against a different verification model than the one "
                        "in this working tree")

    if receipt["resolvedCategories"] != expected_categories:
        problems.append(f"the receipt resolves '{selection}' to {receipt['resolvedCategories']}, and "
                        f"this model resolves it to {expected_categories}")

    reported = [entry.get("category") for entry in receipt["categories"]]
    if sorted(str(name) for name in reported) != sorted(expected_categories):
        problems.append(f"the receipt reports on {sorted(str(name) for name in reported)} while the "
                        f"selection is {sorted(expected_categories)}")

    for entry in receipt["categories"]:
        where = f"category '{entry.get('category', '<unnamed>')}'"
        shape = shape_findings(entry, CATEGORY_FIELDS, where)
        if shape:
            problems.extend(shape)
            continue
        problems.extend(derivation_findings(entry))
        if model is not None:
            try:
                problems.extend(declaration_findings(entry, declared_run(model, entry["category"])))
            except VerificationError as error:
                problems.append(str(error))
        reparsed, _ = reparse_findings(entry)
        problems.extend(reparsed)

    if receipt["terminal"] != "PASS":
        problems.append(f"the receipt's own terminal result is {receipt['terminal']!r}")

    return problems


def accompanying_evidence(receipt: dict) -> dict[str, int]:
    """How much of this receipt could be read against the files it was produced from.

    A receipt whose result files are gone is still worth checking, and is worth exactly what it is: a
    consistency checked record of what a run reported. This number is what tells a reader which of the
    two they are holding, so it is printed rather than left to be assumed.
    """
    present = 0
    for entry in receipt.get("categories", []):
        category, run_root = entry.get("category"), entry.get("runRoot")
        if isinstance(category, str) and isinstance(run_root, str):
            if (REPO_ROOT / run_root / f"{category}.trx").is_file():
                present += 1

    return {"categories": len(receipt.get("categories", [])), "withNativeResult": present}


def git(*arguments: str) -> str:
    result = subprocess.run(["git", *arguments], cwd=REPO_ROOT, capture_output=True, text=True,
                            check=False)

    return result.stdout.strip() if result.returncode == 0 else ""


def write_receipt(receipt: dict, run_root: Path, evidence_parent: Path) -> list[Path]:
    """The receipt goes under the run's own root and beside the caller's evidence."""
    body = json.dumps(receipt, indent=2, ensure_ascii=False, sort_keys=False) + "\n"
    written = []
    for target in (run_root / RECEIPT_NAME, evidence_parent / RECEIPT_NAME):
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(body, encoding="utf-8")
        written.append(target)

    return written


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    try:
        model, model_hash = load_model()
        categories = resolve_selection(model, args.selection)
    except VerificationError as error:
        print(f"FAIL verify {args.selection}: {error}", file=sys.stderr)
        return 2

    session_root, _ = run_ownership.mint_run_root(RAW_RUN_OUTPUT_DIR)
    # A caller directory is a parent, never the file: two runs pointed at the same one would otherwise
    # overwrite each other's receipt, which is the one artefact that must not be shared.
    evidence_parent = (args.evidence_dir if args.evidence_dir.is_absolute()
                       else REPO_ROOT / args.evidence_dir) / session_root.name
    started = datetime.now(timezone.utc)

    print(f"verify --selection {args.selection} -> {', '.join(categories)}", flush=True)

    results = []
    aborted = None
    try:
        for category in categories:
            result = verify_category(declared_run(model, category), evidence_parent)
            if args.record_expected:
                path = record_expected(model, result)
                print(f"recorded {len(result['executed'])} expected identities in {path}")
                result["expected"] = sorted(result["executed"])
                result.update(derived_fields(result))
                result["findings"] = category_findings(result)
                result["terminal"] = "PASS" if not result["findings"] else "FAIL"
            results.append(result)
    except VerificationError as error:
        print(f"FAIL verify {args.selection}: {error}", file=sys.stderr)
        aborted = str(error)

    receipt = {
        "schemaVersion": RECEIPT_SCHEMA_VERSION,
        "kind": RECEIPT_KIND,
        "runId": session_root.name,
        "commit": git("rev-parse", "HEAD"),
        "tree": git("rev-parse", "HEAD^{tree}"),
        "worktreeClean": git("status", "--porcelain") == "",
        "verificationModelSha256": model_hash,
        "selection": args.selection,
        "resolvedCategories": categories,
        "startedUtc": started.isoformat(),
        "finishedUtc": datetime.now(timezone.utc).isoformat(),
        "categories": results,
        "terminal": ("PASS" if aborted is None
                     and all(entry["terminal"] == "PASS" for entry in results) else "FAIL"),
    }
    written = write_receipt(receipt, session_root, evidence_parent)

    # The producer holds itself to the same rules the standalone reader applies, so a receipt this
    # command wrote can never be one that command would refuse. The dirty-worktree rule is left to the
    # reader: a developer verifying while editing is normal, and the reader is what CI and a handover
    # go through.
    self_check = [problem for problem in receipt_findings(
        receipt, args.selection, receipt["commit"], receipt["tree"], model_hash, categories, model)
        if "working tree" not in problem]

    for entry in results:
        state = entry["terminal"]
        detail = "; ".join(entry["findings"]) if entry["findings"] else "exact"
        print(f"{state} {entry['category']}: {detail}")
    receipt_path = written[-1]
    print("receipt " + (receipt_path.relative_to(REPO_ROOT).as_posix()
                        if receipt_path.is_relative_to(REPO_ROOT) else str(receipt_path)))

    if receipt["terminal"] != "PASS" or self_check:
        for problem in self_check:
            print(f"FAIL receipt {problem}", file=sys.stderr)
        print(f"FAIL verify {args.selection}", file=sys.stderr)
        return 1

    print(f"PASS verify {args.selection} categories={len(categories)}")

    return 0


def build_parser() -> argparse.ArgumentParser:
    """The command line of the canonical entry point, and a closed one.

    Exactly one option with a value. Everything else that decides what runs - the categories, the
    projects, the brokers, the budgets, the expected identities - lives in the model, so a workflow
    step can be compared against one allowed shape instead of being parsed as a shell program.
    """
    parser = argparse.ArgumentParser(
        description=__doc__,
        epilog="What a selection contains is declared in build/verification/VERIFICATION_MODEL.json.")
    parser.add_argument("--selection", required=True,
                        help="The named scope to verify, as the verification model declares it.")
    parser.add_argument("--evidence-dir", type=Path, default=Path("artifacts/verification"),
                        help="Parent directory for the receipt. Each run gets its own child of it.")
    parser.add_argument("--record-expected", action="store_true",
                        help="Write the expected identity set of every category in the selection from "
                             "this run, which has to be clean. Not part of a verifying run: the "
                             "workflow never passes it.")

    return parser


if __name__ == "__main__":
    raise SystemExit(main())
