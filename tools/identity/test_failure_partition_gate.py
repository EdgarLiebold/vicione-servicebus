#!/usr/bin/env python3
"""Partition every failed full-suite result and verify targeted/baseline evidence."""

from __future__ import annotations

import argparse
import json
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

from identity_rules import FORMER_LOWER, FORMER_PASCAL


TRX_NAMESPACE = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
ENDPOINT_METHODS = {
    "Should_include_the_namespace_and_prefix",
    "Should_include_the_namespace_and_prefix_with_generic_consumer",
    "Should_include_the_namespace_and_prefix_with_message_name",
    "Should_include_the_namespace",
}


def normalize_identity(value: str) -> str:
    return (
        value.replace("ViciOne.ServiceBus", FORMER_PASCAL)
        .replace("ViciOneServiceBus", FORMER_PASCAL)
        .replace("vicione-servicebus", FORMER_LOWER)
    )


def trx_records(path: Path) -> dict[str, object]:
    root = ET.parse(path).getroot()
    definitions: dict[str, tuple[str, str]] = {}
    for definition in root.findall(".//t:UnitTest", TRX_NAMESPACE):
        method = definition.find("t:TestMethod", TRX_NAMESPACE)
        definitions[definition.attrib.get("id", "")] = (
            method.attrib.get("className", "") if method is not None else "",
            method.attrib.get("name", "") if method is not None else "",
        )
    records: list[dict[str, str | None]] = []
    for result in root.findall(".//t:UnitTestResult", TRX_NAMESPACE):
        class_name, method_name = definitions.get(result.attrib.get("testId", ""), ("", ""))
        message = result.find(".//t:Message", TRX_NAMESPACE)
        message_text = message.text or "" if message is not None else ""
        records.append(
            {
                "className": class_name,
                "methodName": method_name,
                "outcome": result.attrib.get("outcome", "Unknown"),
                "messageFirstLine": message_text.splitlines()[0] if message_text else None,
            }
        )
    return {
        "path": path.name,
        "counts": dict(sorted(Counter(str(record["outcome"]) for record in records).items())),
        "records": records,
        "text": "\n".join(root.itertext()),
    }


def normalized_signature(run: dict[str, object]) -> Counter[tuple[str, str, str, str]]:
    return Counter(
        (
            normalize_identity(str(record["className"])),
            str(record["methodName"]),
            str(record["outcome"]),
            normalize_identity(str(record["messageFirstLine"] or "")),
        )
        for record in run["records"]
    )


def project(record: dict[str, object]) -> str:
    assembly = str(record.get("assembly", "")).replace("\\", "/")
    parts = assembly.casefold().split("/tests/", 1)
    return parts[1].split("/", 1)[0] if len(parts) == 2 else ""


def classify(record: dict[str, object]) -> tuple[str, str]:
    class_name = str(record.get("className", ""))
    method_name = str(record.get("methodName", ""))
    assembly_project = project(record)
    existing = str(record.get("classification", ""))

    if assembly_project == "vicione.servicebus.analyzers.tests":
        return (
            "PRE_EXISTING_BASELINE_FAILURE",
            "analyzer target/baseline comparison has identical 36 pass / 72 fail outcome",
        )
    if class_name == "ViciOne.ServiceBus.Tests.EndpointName_Specs" and method_name in ENDPOINT_METHODS:
        return (
            "IDENTITY_REGRESSION_FIXED_TARGET_RERUN_PASS",
            "corrected endpoint formatter covered by 18/18 passing EndpointName rerun",
        )
    if class_name == "ViciOne.ServiceBus.Tests.CronExpressionTest":
        return (
            "PRE_EXISTING_BASELINE_FAILURE",
            "target and unchanged baseline have identical filtered outcome",
        )
    if assembly_project in {
        "vicione.servicebus.hangfireintegration.tests",
        "vicione.servicebus.quartzintegration.tests",
    }:
        return (
            "PRE_EXISTING_BASELINE_FAILURE",
            "target and unchanged baseline have identical filtered outcome",
        )
    if class_name == "ViciOne.ServiceBus.Tests.ContainerTests.KillSwitch_Specs":
        return (
            "NON_REPRODUCIBLE_FULL_RUN_FAILURE_TARGET_RERUN_PASS",
            "isolated target rerun passed",
        )
    if existing == "BLOCKED_EXTERNAL_INFRASTRUCTURE" or assembly_project in {
        "vicione.servicebus.dynamodbintegration.tests",
        "vicione.servicebus.eventhubintegration.tests",
        "vicione.servicebus.martenintegration.tests",
    }:
        return (
            "BLOCKED_EXTERNAL_INFRASTRUCTURE",
            "provider integration requires unavailable or incompatible external service",
        )
    return ("UNCLASSIFIED_FAILURE", "no exact classification rule")


def build_result(args: argparse.Namespace) -> dict[str, object]:
    full = json.loads(args.full_report.read_text(encoding="utf-8"))
    failures = [record for record in full.get("results", []) if record.get("outcome") == "Failed"]
    endpoint = json.loads(args.endpoint_report.read_text(encoding="utf-8"))
    analyzer = json.loads(args.analyzer_comparison.read_text(encoding="utf-8"))
    runs = {
        "targetCore": trx_records(args.target_core),
        "baselineCore": trx_records(args.baseline_core),
        "targetHangfire": trx_records(args.target_hangfire),
        "baselineHangfire": trx_records(args.baseline_hangfire),
        "targetQuartz": trx_records(args.target_quartz),
        "baselineQuartz": trx_records(args.baseline_quartz),
        "targetMarten": trx_records(args.target_marten),
        "targetEventHubHealth": trx_records(args.target_eventhub_health),
    }

    partition: list[dict[str, object]] = []
    for record in failures:
        category, reason = classify(record)
        partition.append(
            {
                "assemblyProject": project(record),
                "className": record.get("className"),
                "methodName": record.get("methodName"),
                "messageFirstLine": record.get("messageFirstLine"),
                "category": category,
                "reason": reason,
            }
        )

    categories = Counter(str(record["category"]) for record in partition)
    endpoint_passes = {
        str(record.get("methodName"))
        for record in endpoint.get("results", [])
        if record.get("outcome") == "Passed"
    }
    core_target_passes = {
        str(record["methodName"])
        for record in runs["targetCore"]["records"]
        if record["outcome"] == "Passed"
    }
    checks = {
        "fullFailureCountIs413": len(failures) == 413,
        "partitionHasNoResidue": categories.get("UNCLASSIFIED_FAILURE", 0) == 0,
        "partitionSumsTo413": sum(categories.values()) == 413,
        "partitionCountsExact": categories
        == Counter(
            {
                "BLOCKED_EXTERNAL_INFRASTRUCTURE": 330,
                "PRE_EXISTING_BASELINE_FAILURE": 78,
                "IDENTITY_REGRESSION_FIXED_TARGET_RERUN_PASS": 4,
                "NON_REPRODUCIBLE_FULL_RUN_FAILURE_TARGET_RERUN_PASS": 1,
            }
        ),
        "endpointFourCasesCoveredBy18PassingRerun": endpoint.get("status") == "PASS"
        and endpoint.get("counts", {}).get("outcomes") == {"Passed": 18}
        and ENDPOINT_METHODS <= endpoint_passes,
        "analyzerBaselineComparisonPass": analyzer.get("status") == "PASS"
        and analyzer.get("classification") == "PRE_EXISTING_BASELINE_TEST_HARNESS_DEFECT",
        "coreTargetMatchesBaseline": normalized_signature(runs["targetCore"])
        == normalized_signature(runs["baselineCore"]),
        "coreTargetOutcome": runs["targetCore"]["counts"]
        == {"Failed": 2, "NotExecuted": 1, "Passed": 114},
        "killSwitchTargetRerunPassed": "Should_be_degraded_after_too_many_exceptions"
        in core_target_passes,
        "hangfireTargetMatchesBaseline": normalized_signature(runs["targetHangfire"])
        == normalized_signature(runs["baselineHangfire"]),
        "hangfireTargetOutcome": runs["targetHangfire"]["counts"] == {"Failed": 1},
        "quartzTargetMatchesBaseline": normalized_signature(runs["targetQuartz"])
        == normalized_signature(runs["baselineQuartz"]),
        "quartzTargetOutcome": runs["targetQuartz"]["counts"] == {"Failed": 3, "Passed": 2},
        "martenExternalCauseProven": runs["targetMarten"]["counts"] == {"Failed": 1}
        and 'password authentication failed for user "postgres"'
        in str(runs["targetMarten"]["text"]),
        "eventHubExternalCauseProven": runs["targetEventHubHealth"]["counts"] == {"Failed": 1}
        and "Unhealthy - not ready" in str(runs["targetEventHubHealth"]["text"]),
    }
    findings = [name for name, passed in checks.items() if not passed]
    run_evidence = {
        name: {"path": run["path"], "counts": run["counts"]}
        for name, run in runs.items()
    }
    return {
        "status": "PASS" if not findings else "FAIL",
        "overallTestStatus": "FAIL_INCOMPLETE",
        "counts": {
            "fullFailures": len(failures),
            "categories": dict(sorted(categories.items())),
        },
        "checks": checks,
        "findings": findings,
        "targetedRunEvidence": run_evidence,
        "partition": partition,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--full-report", required=True, type=Path)
    parser.add_argument("--endpoint-report", required=True, type=Path)
    parser.add_argument("--analyzer-comparison", required=True, type=Path)
    parser.add_argument("--target-core", required=True, type=Path)
    parser.add_argument("--baseline-core", required=True, type=Path)
    parser.add_argument("--target-hangfire", required=True, type=Path)
    parser.add_argument("--baseline-hangfire", required=True, type=Path)
    parser.add_argument("--target-quartz", required=True, type=Path)
    parser.add_argument("--baseline-quartz", required=True, type=Path)
    parser.add_argument("--target-marten", required=True, type=Path)
    parser.add_argument("--target-eventhub-health", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    result = build_result(args)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "counts": result["counts"]}, indent=2))
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
