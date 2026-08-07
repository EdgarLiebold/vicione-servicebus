#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Hostile tests for frozen proof-contract mutation enforcement."""

from __future__ import annotations

import unittest

from proof_contract_gate import (
    MUTANT_FAMILIES,
    SCENARIOS,
    SLICE_CONTENT_SHA256,
    SUBJECTS,
    expected_contract_id,
    mutate,
    required_mutants,
    validate_contracts,
)


def canonical_fixture() -> dict[str, object]:
    contracts = []
    bindings = []
    for subject, expected in SUBJECTS.items():
        kind = str(expected["kind"])
        contracts.append(
            {
                "id": expected_contract_id(subject, kind),
                "subjectId": subject,
                "subjectKind": kind,
                "proofProfileId": expected["profile"],
                "requiredEvidence": expected["evidence"],
                "requiredMutants": required_mutants(subject, kind),
                "requiredScenarios": SCENARIOS.copy(),
                "evidenceTarget": f"repositories/vicione-servicebus/evidence/WP-F2-SERVICEBUS-IDENTITY/{subject}/",
            }
        )
        bindings.append(
            {
                "id": subject,
                "owner": expected["owner"],
                "proofProfileId": expected["profile"],
                "disposition": "ARCHITECTURE_CONCERN",
            }
        )
    return {"contentSha256": SLICE_CONTENT_SHA256, "proofContracts": contracts, "bindings": bindings}


class ProofContractGateTests(unittest.TestCase):
    def test_accepts_exact_frozen_contract_shape(self) -> None:
        self.assertEqual([], validate_contracts(canonical_fixture()))

    def test_kills_every_subject_mutant_family(self) -> None:
        fixture = canonical_fixture()
        for subject in SUBJECTS:
            for family in MUTANT_FAMILIES:
                with self.subTest(subject=subject, family=family):
                    findings = validate_contracts(mutate(fixture, subject, family))
                    self.assertTrue(any(item["subject"] == subject for item in findings))


if __name__ == "__main__":
    unittest.main()
