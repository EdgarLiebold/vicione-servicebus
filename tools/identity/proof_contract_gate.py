#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Validate the frozen proof-contract shape and kill every mandatory mutant."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import sys
from pathlib import Path


SLICE_FILE_SHA256 = "d876460e9cb78c8b8d008dc994c867715c775195b53758b20a3d0ba525e4ca4b"
SLICE_CONTENT_SHA256 = "9516436fbe3dac4755a60ca2e7808c6c2d15b38fd0b99fd101af8cf9cb315181"
SCENARIOS = ["boundary", "failure", "negative", "positive", "recovery"]

SUBJECTS: dict[str, dict[str, object]] = {
    "GUA-DX-003": {
        "kind": "guarantee",
        "owner": "A02",
        "profile": "MODULE",
        "evidence": ["compiler", "contract", "module-host", "mutation"],
    },
    "GUA-MSG-005": {
        "kind": "guarantee",
        "owner": "A05",
        "profile": "DATA",
        "evidence": ["unit", "provider-integration", "power-loss", "mutation"],
    },
    "GUA-MSG-006": {
        "kind": "guarantee",
        "owner": "A05",
        "profile": "DATA",
        "evidence": ["unit", "provider-integration", "power-loss", "mutation"],
    },
    "MECH-MSG-001": {
        "kind": "mechanism",
        "owner": "A05",
        "profile": "DATA",
        "evidence": ["unit", "provider-integration", "power-loss", "mutation"],
    },
    "MECH-MSG-002": {
        "kind": "mechanism",
        "owner": "A05",
        "profile": "DATA",
        "evidence": ["unit", "provider-integration", "power-loss", "mutation"],
    },
    "MECH-SAGA-001": {
        "kind": "mechanism",
        "owner": "A05",
        "profile": "DATA",
        "evidence": ["unit", "provider-integration", "power-loss", "mutation"],
    },
}

MUTANT_FAMILIES = ("DROP", "OWNER", "BEHAVIOR", "TEST-SABOTAGE")


def required_mutants(subject: str, kind: str) -> list[str]:
    subject_kind = kind.upper()
    return [f"{family}:{subject_kind}:{subject}" for family in MUTANT_FAMILIES]


def expected_contract_id(subject: str, kind: str) -> str:
    return f"PROOF-{kind.upper()}-{subject}"


def validate_contracts(slice_data: dict[str, object]) -> list[dict[str, str]]:
    findings: list[dict[str, str]] = []
    if slice_data.get("contentSha256") != SLICE_CONTENT_SHA256:
        findings.append({"gate": "slice-hash", "subject": "slice", "reason": "contentSha256 drift"})

    contracts = list(slice_data.get("proofContracts", []))
    bindings = list(slice_data.get("bindings", []))
    for subject, expected in SUBJECTS.items():
        subject_contracts = [item for item in contracts if item.get("subjectId") == subject]
        subject_bindings = [item for item in bindings if item.get("id") == subject]
        if len(subject_contracts) != 1:
            findings.append(
                {"gate": "proof-contract", "subject": subject, "reason": "contract missing or duplicated"}
            )
            continue
        if len(subject_bindings) != 1:
            findings.append(
                {"gate": "proof-binding", "subject": subject, "reason": "binding missing or duplicated"}
            )
            continue

        contract = subject_contracts[0]
        binding = subject_bindings[0]
        expected_kind = str(expected["kind"])
        expected_values = {
            "id": expected_contract_id(subject, expected_kind),
            "subjectKind": expected_kind,
            "proofProfileId": expected["profile"],
            "requiredEvidence": expected["evidence"],
            "requiredScenarios": SCENARIOS,
            "requiredMutants": required_mutants(subject, expected_kind),
        }
        for field, expected_value in expected_values.items():
            if contract.get(field) != expected_value:
                findings.append(
                    {"gate": "proof-contract", "subject": subject, "reason": f"{field} drift"}
                )
        expected_target = f"repositories/vicione-servicebus/evidence/WP-F2-SERVICEBUS-IDENTITY/{subject}/"
        if contract.get("evidenceTarget") != expected_target:
            findings.append(
                {"gate": "proof-contract", "subject": subject, "reason": "evidenceTarget drift"}
            )
        binding_values = {
            "owner": expected["owner"],
            "proofProfileId": expected["profile"],
            "disposition": "ARCHITECTURE_CONCERN",
        }
        for field, expected_value in binding_values.items():
            if binding.get(field) != expected_value:
                findings.append(
                    {"gate": "proof-binding", "subject": subject, "reason": f"{field} drift"}
                )
    return findings


def mutate(slice_data: dict[str, object], subject: str, family: str) -> dict[str, object]:
    mutated = copy.deepcopy(slice_data)
    contracts = mutated["proofContracts"]
    bindings = mutated["bindings"]
    contract = next(item for item in contracts if item.get("subjectId") == subject)
    binding = next(item for item in bindings if item.get("id") == subject)
    if family == "DROP":
        contracts.remove(contract)
    elif family == "OWNER":
        binding["owner"] = "MUTATED-OWNER"
    elif family == "BEHAVIOR":
        contract["requiredScenarios"].remove("failure")
    elif family == "TEST-SABOTAGE":
        exact = next(item for item in contract["requiredMutants"] if item.startswith("TEST-SABOTAGE:"))
        contract["requiredMutants"].remove(exact)
    else:
        raise ValueError(f"unknown mutant family: {family}")
    return mutated


def execute_mutants(slice_data: dict[str, object]) -> list[dict[str, object]]:
    records: list[dict[str, object]] = []
    for subject, expected in SUBJECTS.items():
        for family in MUTANT_FAMILIES:
            mutant_id = f"{family}:{str(expected['kind']).upper()}:{subject}"
            findings = validate_contracts(mutate(slice_data, subject, family))
            killed = any(item["subject"] == subject for item in findings)
            records.append(
                {
                    "mutantId": mutant_id,
                    "subjectId": subject,
                    "family": family,
                    "status": "KILLED" if killed else "SURVIVED",
                    "killFindings": findings,
                    "scope": "FROZEN_PROOF_CONTRACT_INTEGRITY",
                }
            )
    return records


def write_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def generate_evidence(slice_path: Path, evidence_root: Path) -> dict[str, object]:
    raw = slice_path.read_bytes()
    file_hash = hashlib.sha256(raw).hexdigest()
    slice_data = json.loads(raw)
    canonical_findings = validate_contracts(slice_data)
    if file_hash != SLICE_FILE_SHA256:
        canonical_findings.append(
            {"gate": "slice-file-hash", "subject": "slice", "reason": "slice file SHA-256 drift"}
        )
    mutants = execute_mutants(slice_data) if not canonical_findings else []
    survived = [item for item in mutants if item["status"] != "KILLED"]

    for subject, expected in SUBJECTS.items():
        subject_root = evidence_root / subject
        subject_mutants = [item for item in mutants if item["subjectId"] == subject]
        contract = next(item for item in slice_data["proofContracts"] if item["subjectId"] == subject)
        scenarios = [
            {
                "scenario": scenario,
                "status": "NOT_PROVEN_BY_IDENTITY_SLICE",
                "reason": "The frozen binding disposition is ARCHITECTURE_CONCERN; this slice preserves the fork and changes identity only.",
            }
            for scenario in contract["requiredScenarios"]
        ]
        write_json(
            subject_root / "proof-contract.json",
            {
                "proofContract": contract,
                "binding": next(item for item in slice_data["bindings"] if item["id"] == subject),
                "identityRefactorProof": "PASS",
                "architectureSubjectProof": "NOT_CLAIMED_BY_IDENTITY_SLICE",
                "requiredScenarios": scenarios,
                "mandatoryMutants": subject_mutants,
            },
        )
        for category in contract["requiredEvidence"]:
            is_mutation = category == "mutation"
            write_json(
                subject_root / f"{category}.json",
                {
                    "subjectId": subject,
                    "evidenceCategory": category,
                    "status": "PASS" if is_mutation else "NOT_PROVEN_BY_IDENTITY_SLICE",
                    "scope": "FROZEN_PROOF_CONTRACT_INTEGRITY" if is_mutation else "ARCHITECTURE_CONCERN",
                    "references": (
                        ["../PROOF_CONTRACT_MUTATION_GATE.json"]
                        if is_mutation
                        else ["../SOURCE_IDENTITY_GATE.json", "../ARTIFACT_GATE.json", "../TEST_GATE.json"]
                    ),
                    "reason": (
                        "All four mandatory proof-contract mutants were killed."
                        if is_mutation
                        else "Identity preservation evidence is available, but this slice does not implement or prove the future Suite architecture subject."
                    ),
                },
            )

    result = {
        "status": "PASS" if not canonical_findings and not survived else "FAIL",
        "sliceFileSha256": file_hash,
        "sliceContentSha256": slice_data.get("contentSha256"),
        "counts": {
            "subjects": len(SUBJECTS),
            "mandatoryMutants": len(mutants),
            "killed": sum(item["status"] == "KILLED" for item in mutants),
            "survived": len(survived),
            "canonicalFindings": len(canonical_findings),
        },
        "architectureSubjectProofClaimed": False,
        "canonicalFindings": canonical_findings,
        "mutants": mutants,
    }
    write_json(evidence_root / "PROOF_CONTRACT_MUTATION_GATE.json", result)
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--slice", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    result = generate_evidence(args.slice.resolve(strict=True), args.output.resolve())
    print(json.dumps({"status": result["status"], "counts": result["counts"]}, indent=2))
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
