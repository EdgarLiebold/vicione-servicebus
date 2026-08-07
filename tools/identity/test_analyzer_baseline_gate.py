#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Tests for analyzer baseline-result normalization and fail-closed comparison."""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from analyzer_baseline_gate import normalize_class, trx_result
from identity_rules import FORMER_PASCAL


TRX = """<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <TestDefinitions><UnitTest id="1"><TestMethod className="{class_name}" name="Case" /></UnitTest></TestDefinitions>
  <Results><UnitTestResult testId="1" testName="Case" outcome="{outcome}">{output}</UnitTestResult></Results>
</TestRun>
"""


class AnalyzerBaselineGateTests(unittest.TestCase):
    def test_normalizes_only_the_two_product_test_prefixes(self) -> None:
        self.assertEqual(
            "<PRODUCT>.Analyzers.Tests.Suite",
            normalize_class(FORMER_PASCAL + ".Analyzers.Tests.Suite"),
        )
        self.assertEqual(
            "<PRODUCT>.Analyzers.Tests.Suite",
            normalize_class("ViciOne.ServiceBus.Analyzers.Tests.Suite"),
        )
        self.assertEqual("ThirdParty.Suite", normalize_class("ThirdParty.Suite"))

    def test_parses_failure_and_missing_diagnostic_without_hiding_it(self) -> None:
        output = (
            "<Output><ErrorInfo><Message>Mismatch between number of diagnostics returned, "
            "expected &quot;1&quot; actual &quot;0&quot;</Message></ErrorInfo></Output>"
        )
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "result.trx"
            path.write_text(
                TRX.format(
                    class_name="ViciOne.ServiceBus.Analyzers.Tests.Suite",
                    outcome="Failed",
                    output=output,
                ),
                encoding="utf-8",
            )
            result = trx_result(path)
        self.assertEqual({"Failed": 1}, result["outcomes"])
        self.assertEqual(1, sum(result["missingDiagnosticMessages"].values()))


if __name__ == "__main__":
    unittest.main()
