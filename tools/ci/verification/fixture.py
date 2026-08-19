#!/usr/bin/env python3
"""What the fixture side of a run proved, and what is unproven when it wrote nothing.

A category that names brokers is a category whose result only means something if the fixture was
there, was the declared one and came back. A run of it that left no record has shown none of that.
Absence used to be read as "no finding", which is the difference between nothing went wrong and
nothing was looked at.

Read from the file the fixture wrote rather than from a child's output: recognising a cleanup failure
by matching prose in stderr would be reading a sentence, and a sentence is not a contract.

Standard library only.
"""

from __future__ import annotations

import json
from pathlib import Path

from verification import trx

FIXTURE_RECORD_NAME = "fixture-findings.json"
FIXTURE_RECORD_KIND = "SERVICEBUS_FIXTURE_FINDINGS"
FIXTURE_RECORD_SCHEMA_VERSION = 1


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

    sha256 = trx.digest(path)
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
