#!/usr/bin/env python3
"""Tests for fail-closed TRX failure classification."""

from __future__ import annotations

import unittest

from test_result_gate import classify


class TestResultGateTests(unittest.TestCase):
    def test_classifies_success_and_suite_skip(self) -> None:
        self.assertEqual("PASS", classify("Passed", ""))
        self.assertEqual("SUITE_DECLARED_NOT_EXECUTED", classify("NotExecuted", ""))

    def test_classifies_direct_external_infrastructure_failure(self) -> None:
        self.assertEqual(
            "BLOCKED_EXTERNAL_INFRASTRUCTURE",
            classify("Failed", "Connection refused (127.0.0.1:10002)"),
        )

    def test_classifies_exact_sql_and_event_hub_infrastructure_signatures(self) -> None:
        messages = (
            "Netzwerkbezogener oder instanzspezifischer Fehler beim Herstellen einer Verbindung mit SQL Server",
            "ReceiveTransport faulted: db://localhost/",
            "None of the server sasl-mechanisms ([PLAIN]) are supported by the client",
        )
        for message in messages:
            with self.subTest(message=message):
                self.assertEqual("BLOCKED_EXTERNAL_INFRASTRUCTURE", classify("Failed", message))

    def test_does_not_hide_unknown_failure_as_infrastructure(self) -> None:
        self.assertEqual(
            "PRODUCT_OR_BASELINE_FAILURE_REQUIRES_RERUN",
            classify("Failed", "Expected true but was false"),
        )

    def test_marks_known_corrected_identity_failure_for_rerun(self) -> None:
        self.assertEqual(
            "IDENTITY_REGRESSION_FIXED_REQUIRES_RERUN",
            classify("Failed", 'Assert.That(name, Is.EqualTo("vicione-servicebus-tests-endpoint-name-specs"))'),
        )
        self.assertEqual(
            "IDENTITY_REGRESSION_FIXED_REQUIRES_RERUN",
            classify(
                "Failed",
                'Assert.That(name, Is.EqualTo("dev-vicione-servicebus-tests-endpoint-name-specs-some-really-cool"))',
            ),
        )

    def test_does_not_reclassify_known_baseline_harness_defect_as_identity(self) -> None:
        self.assertEqual(
            "PRE_EXISTING_BASELINE_TEST_HARNESS_DEFECT",
            classify(
                "Failed",
                'Mismatch between number of diagnostics returned, expected "1" actual "0"',
            ),
        )


if __name__ == "__main__":
    unittest.main()
