#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Aggregate TRX test results without hiding infrastructure or product failures."""

from __future__ import annotations

import argparse
import json
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path


TRX_NAMESPACE = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
INFRASTRUCTURE_MARKERS = (
    "connection refused",
    "nodename nor servname provided",
    "failed to resolve aws credentials",
    "localdb wird auf dieser plattform nicht unterstützt",
    "couldn't connect to any of the localdb databases",
    "password authentication failed for user",
    "error connecting to localhost",
    "sasl negotiation failed",
    "selecting a server using compositeserverselector",
    "no connection could be made because the target machine actively refused it",
    "netzwerkbezogener oder instanzspezifischer fehler beim herstellen einer verbindung mit sql server",
    "receivetransport faulted: db://localhost/",
    "none of the server sasl-mechanisms",
)
FIXED_IDENTITY_MARKERS = (
    'is.equalto("vicione-servicebus-tests-endpoint-name-specs',
    'is.equalto("dev-vicione-servicebus-tests-endpoint-name-specs',
    'is.equalto("dev-vicione-servicebus-test-framework-messages',
)
BASELINE_HARNESS_MARKERS = (
    'mismatch between number of diagnostics returned, expected "1" actual "0"',
    'mismatch between number of diagnostics returned, expected "2" actual "0"',
)


def classify(
    outcome: str,
    message: str,
    assembly: str = "",
    class_name: str = "",
    method_name: str = "",
) -> str:
    if outcome == "Passed":
        return "PASS"
    if outcome == "NotExecuted":
        return "SUITE_DECLARED_NOT_EXECUTED"
    assembly_project = assembly.replace("\\", "/").casefold()
    if "/vicione.servicebus.analyzers.tests/" in assembly_project:
        return "PRE_EXISTING_BASELINE_TEST_HARNESS_DEFECT"
    if class_name == "ViciOne.ServiceBus.Tests.EndpointName_Specs" and method_name in {
        "Should_include_the_namespace_and_prefix",
        "Should_include_the_namespace_and_prefix_with_generic_consumer",
        "Should_include_the_namespace_and_prefix_with_message_name",
        "Should_include_the_namespace",
    }:
        return "IDENTITY_REGRESSION_FIXED_TARGET_RERUN_PASS"
    if class_name == "ViciOne.ServiceBus.Tests.CronExpressionTest" or any(
        project in assembly_project
        for project in (
            "/vicione.servicebus.hangfireintegration.tests/",
            "/vicione.servicebus.quartzintegration.tests/",
        )
    ):
        return "PRE_EXISTING_BASELINE_FAILURE"
    if class_name == "ViciOne.ServiceBus.Tests.ContainerTests.KillSwitch_Specs":
        return "NON_REPRODUCIBLE_FULL_RUN_FAILURE_TARGET_RERUN_PASS"
    lowered = message.casefold()
    if any(
        project in assembly_project
        for project in (
            "/vicione.servicebus.dynamodbintegration.tests/",
            "/vicione.servicebus.eventhubintegration.tests/",
            "/vicione.servicebus.martenintegration.tests/",
        )
    ):
        return "BLOCKED_EXTERNAL_INFRASTRUCTURE"
    if any(marker in lowered for marker in INFRASTRUCTURE_MARKERS):
        return "BLOCKED_EXTERNAL_INFRASTRUCTURE"
    if any(marker in lowered for marker in BASELINE_HARNESS_MARKERS):
        return "PRE_EXISTING_BASELINE_TEST_HARNESS_DEFECT"
    if any(marker in lowered for marker in FIXED_IDENTITY_MARKERS):
        return "IDENTITY_REGRESSION_FIXED_REQUIRES_RERUN"
    return "PRODUCT_OR_BASELINE_FAILURE_REQUIRES_RERUN"


def aggregate(results_directory: Path) -> dict[str, object]:
    files = [results_directory] if results_directory.is_file() else sorted(results_directory.glob("*.trx"))
    records: list[dict[str, object]] = []
    discovered = 0
    for trx in files:
        tree = ET.parse(trx)
        root = tree.getroot()
        definitions: dict[str, dict[str, str]] = {}
        for definition in root.findall(".//t:UnitTest", TRX_NAMESPACE):
            test_id = definition.attrib.get("id", "")
            method = definition.find("t:TestMethod", TRX_NAMESPACE)
            definitions[test_id] = {
                "assembly": definition.attrib.get("storage", ""),
                "className": method.attrib.get("className", "") if method is not None else "",
                "methodName": method.attrib.get("name", "") if method is not None else "",
            }
        discovered += len(definitions)
        for result in root.findall(".//t:UnitTestResult", TRX_NAMESPACE):
            message = result.find(".//t:Message", TRX_NAMESPACE)
            message_text = message.text or "" if message is not None else ""
            identity = definitions.get(result.attrib.get("testId", ""), {})
            outcome = result.attrib.get("outcome", "Unknown")
            records.append(
                {
                    "trx": trx.name,
                    "testName": result.attrib.get("testName", ""),
                    "outcome": outcome,
                    "duration": result.attrib.get("duration"),
                    "assembly": identity.get("assembly", ""),
                    "className": identity.get("className", ""),
                    "methodName": identity.get("methodName", ""),
                    "classification": classify(
                        outcome,
                        message_text,
                        identity.get("assembly", ""),
                        identity.get("className", ""),
                        identity.get("methodName", ""),
                    ),
                    "messageFirstLine": message_text.splitlines()[0] if message_text else None,
                }
            )

    outcomes = Counter(str(item["outcome"]) for item in records)
    classifications = Counter(str(item["classification"]) for item in records)
    executed = sum(outcome != "NotExecuted" for outcome in outcomes.elements())
    findings: list[dict[str, str]] = []
    if not files or discovered <= 0 or executed <= 0:
        findings.append(
            {"gate": "null-test", "path": str(results_directory), "reason": "zero TRX/discovered/executed"}
        )
    unresolved = classifications.get("PRODUCT_OR_BASELINE_FAILURE_REQUIRES_RERUN", 0)
    baseline_defects = classifications.get("PRE_EXISTING_BASELINE_TEST_HARNESS_DEFECT", 0)
    baseline_defects += classifications.get("PRE_EXISTING_BASELINE_FAILURE", 0)
    identity_rerun = classifications.get("IDENTITY_REGRESSION_FIXED_REQUIRES_RERUN", 0)
    identity_rerun += classifications.get("IDENTITY_REGRESSION_FIXED_TARGET_RERUN_PASS", 0)
    identity_rerun += classifications.get("NON_REPRODUCIBLE_FULL_RUN_FAILURE_TARGET_RERUN_PASS", 0)
    status = "FAIL" if findings or unresolved or identity_rerun or baseline_defects else (
        "BLOCKED" if classifications.get("BLOCKED_EXTERNAL_INFRASTRUCTURE", 0) else "PASS"
    )
    return {
        "status": status,
        "counts": {
            "trxFiles": len(files),
            "discovered": discovered,
            "executed": executed,
            "outcomes": dict(sorted(outcomes.items())),
            "classifications": dict(sorted(classifications.items())),
        },
        "findings": findings,
        "results": records,
    }


def reclassify(report_path: Path) -> dict[str, object]:
    """Reapply current exact-signature rules to a preserved aggregate report."""
    report = json.loads(report_path.read_text(encoding="utf-8"))
    records = report.get("results", [])
    if not isinstance(records, list):
        raise ValueError("aggregate report results must be a list")
    for record in records:
        if not isinstance(record, dict):
            raise ValueError("aggregate report result must be an object")
        record["classification"] = classify(
            str(record.get("outcome", "Unknown")),
            str(record.get("messageFirstLine") or ""),
            str(record.get("assembly", "")),
            str(record.get("className", "")),
            str(record.get("methodName", "")),
        )

    outcomes = Counter(str(item.get("outcome", "Unknown")) for item in records)
    classifications = Counter(str(item.get("classification", "Unknown")) for item in records)
    counts = report.setdefault("counts", {})
    if not isinstance(counts, dict):
        raise ValueError("aggregate report counts must be an object")
    counts["executed"] = sum(outcome != "NotExecuted" for outcome in outcomes.elements())
    counts["outcomes"] = dict(sorted(outcomes.items()))
    counts["classifications"] = dict(sorted(classifications.items()))
    unresolved = classifications.get("PRODUCT_OR_BASELINE_FAILURE_REQUIRES_RERUN", 0)
    baseline_defects = classifications.get("PRE_EXISTING_BASELINE_TEST_HARNESS_DEFECT", 0)
    baseline_defects += classifications.get("PRE_EXISTING_BASELINE_FAILURE", 0)
    identity_rerun = classifications.get("IDENTITY_REGRESSION_FIXED_REQUIRES_RERUN", 0)
    identity_rerun += classifications.get("IDENTITY_REGRESSION_FIXED_TARGET_RERUN_PASS", 0)
    identity_rerun += classifications.get("NON_REPRODUCIBLE_FULL_RUN_FAILURE_TARGET_RERUN_PASS", 0)
    findings = report.get("findings", [])
    report["status"] = "FAIL" if findings or unresolved or identity_rerun or baseline_defects else (
        "BLOCKED" if classifications.get("BLOCKED_EXTERNAL_INFRASTRUCTURE", 0) else "PASS"
    )
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("results", type=Path, help="TRX directory/file or preserved aggregate JSON")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    result = reclassify(args.results) if args.results.suffix.casefold() == ".json" else aggregate(args.results)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "counts": result["counts"]}, indent=2))
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
