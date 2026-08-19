#!/usr/bin/env python3
"""What the test platform's own result file says, and what it means as a set of identities.

The native result is the executed truth of a run: nothing in a test declares that it ran, and a test
that attested to its own execution would be attesting to the thing under question. Everything that
reads one lives here, because two readers of one format are two answers about one run - the entry
point and the category runner each had their own, and they did not agree about what a duplicate was.

Read fail closed. Every shape refused here is a way for a set to look complete while it is not: a
result naming a case the file never defines, a definition id that appears twice, one case reported
twice under one id, a summary whose counters do not count the results underneath it.

Standard library only.
"""

from __future__ import annotations

import hashlib
import re
import xml.etree.ElementTree as ElementTree
from collections import Counter
from datetime import datetime
from pathlib import Path

TRX_NAMESPACE = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

ZERO_COUNTERS = {"total": 0, "executed": 0, "passed": 0, "failed": 0, "notExecuted": 0}


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

    namespace = TRX_NAMESPACE
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
        definitions[test_id] = identity_of(method)

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


def digest(path: Path) -> str | None:
    if not path.is_file():
        return None

    return hashlib.sha256(path.read_bytes()).hexdigest()
