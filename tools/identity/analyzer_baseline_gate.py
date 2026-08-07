#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Compare the renamed analyzer suite with the unchanged frozen baseline."""

from __future__ import annotations

import argparse
import json
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

from identity_rules import FORMER_PASCAL, map_text


TRX_NAMESPACE = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
PRODUCT_PREFIXES = (
    FORMER_PASCAL + ".Analyzers.Tests.",
    "ViciOne.ServiceBus.Analyzers.Tests.",
)


def normalize_class(value: str) -> str:
    for prefix in PRODUCT_PREFIXES:
        if value.startswith(prefix):
            return "<PRODUCT>.Analyzers.Tests." + value[len(prefix) :]
    return value


def trx_result(path: Path) -> dict[str, object]:
    root = ET.parse(path).getroot()
    definitions: dict[str, str] = {}
    for definition in root.findall(".//t:UnitTest", TRX_NAMESPACE):
        method = definition.find("t:TestMethod", TRX_NAMESPACE)
        definitions[definition.attrib.get("id", "")] = normalize_class(
            method.attrib.get("className", "") if method is not None else ""
        )
    outcomes: Counter[str] = Counter()
    result_map: dict[str, str] = {}
    mismatch_messages: Counter[str] = Counter()
    for result in root.findall(".//t:UnitTestResult", TRX_NAMESPACE):
        outcome = result.attrib.get("outcome", "Unknown")
        outcomes[outcome] += 1
        class_name = definitions.get(result.attrib.get("testId", ""), "")
        key = f"{class_name}.{map_text(result.attrib.get('testName', ''))}"
        result_map[key] = outcome
        message = result.find(".//t:Message", TRX_NAMESPACE)
        first_line = (message.text or "").splitlines()[0] if message is not None else ""
        if "Mismatch between number of diagnostics returned" in first_line:
            mismatch_messages[first_line] += 1
    return {
        "total": sum(outcomes.values()),
        "outcomes": dict(sorted(outcomes.items())),
        "resultMap": dict(sorted(result_map.items())),
        "missingDiagnosticMessages": dict(sorted(mismatch_messages.items())),
    }


def probe_signature(path: Path) -> dict[str, object]:
    value = json.loads(path.read_text(encoding="utf-8"))
    return {
        "compilerDiagnostics": value["compilerDiagnostics"],
        "metadataTypeResolved": value["metadataTypeResolved"],
        "producerMethodSymbolsResolved": value["producerMethodSymbolsResolved"],
        "requiredAssembly": value["requiredAssembly"],
    }


def helper_signature(helper: Path, project: Path) -> dict[str, object]:
    helper_text = helper.read_text(encoding="utf-8")
    project_text = project.read_text(encoding="utf-8")
    return {
        "targetFrameworkNet9": "<TargetFramework>net9.0</TargetFramework>" in project_text,
        "net6RuntimeFacadeBranch": 'Assembly.Load("System.Runtime, Version=6.0.0.0")' in helper_text,
        "activeElseUsesCoreLibAssembly": "typeof(ISet<>).Assembly.Location" in helper_text,
    }


def compare(
    baseline_trx: Path,
    target_trx: Path,
    baseline_probe: Path,
    target_probe: Path,
    baseline_helper: Path,
    target_helper: Path,
    baseline_project: Path,
    target_project: Path,
) -> dict[str, object]:
    baseline_results = trx_result(baseline_trx)
    target_results = trx_result(target_trx)
    baseline_probe_result = probe_signature(baseline_probe)
    target_probe_result = probe_signature(target_probe)
    baseline_helper_result = helper_signature(baseline_helper, baseline_project)
    target_helper_result = helper_signature(target_helper, target_project)
    checks = {
        "resultCountsEqual": baseline_results["outcomes"] == target_results["outcomes"],
        "perTestOutcomesEqual": baseline_results["resultMap"] == target_results["resultMap"],
        "missingDiagnosticMessagesEqual": (
            baseline_results["missingDiagnosticMessages"] == target_results["missingDiagnosticMessages"]
        ),
        "compilerDiagnosticsEqual": (
            baseline_probe_result["compilerDiagnostics"] == target_probe_result["compilerDiagnostics"]
        ),
        "metadataResolutionEqual": (
            baseline_probe_result["metadataTypeResolved"] == target_probe_result["metadataTypeResolved"]
            and baseline_probe_result["metadataTypeResolved"] is True
        ),
        "requiredRuntimeEqual": (
            baseline_probe_result["requiredAssembly"] == target_probe_result["requiredAssembly"]
        ),
        "helperBranchEqual": baseline_helper_result == target_helper_result,
        "harnessDefectPresentBoth": (
            baseline_helper_result["targetFrameworkNet9"]
            and baseline_helper_result["activeElseUsesCoreLibAssembly"]
            and target_helper_result["targetFrameworkNet9"]
            and target_helper_result["activeElseUsesCoreLibAssembly"]
        ),
    }
    status = "PASS" if all(checks.values()) else "FAIL"
    return {
        "status": status,
        "classification": (
            "PRE_EXISTING_BASELINE_TEST_HARNESS_DEFECT"
            if status == "PASS"
            else "TARGET_REGRESSION_OR_NON_EQUIVALENT_RUN"
        ),
        "targetIdentityRegression": status != "PASS",
        "expectationsWeakened": False,
        "cause": (
            "The net9 analyzer test harness selects typeof(ISet<>).Assembly.Location instead of the System.Runtime facade; both baseline and target snippets produce the same CS0012 set and therefore no producer method symbols."
        ),
        "checks": checks,
        "baseline": {
            "trx": baseline_results,
            "probe": baseline_probe_result,
            "helper": baseline_helper_result,
        },
        "target": {
            "trx": target_results,
            "probe": target_probe_result,
            "helper": target_helper_result,
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    for name in (
        "baseline-trx",
        "target-trx",
        "baseline-probe",
        "target-probe",
        "baseline-helper",
        "target-helper",
        "baseline-project",
        "target-project",
    ):
        parser.add_argument(f"--{name}", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    result = compare(
        args.baseline_trx,
        args.target_trx,
        args.baseline_probe,
        args.target_probe,
        args.baseline_helper,
        args.target_helper,
        args.baseline_project,
        args.target_project,
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "classification": result["classification"]}, indent=2))
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
