#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Tests for NotExecuted source attribution helpers."""

from __future__ import annotations

import unittest

from not_executed_source_gate import project_name


class NotExecutedSourceGateTests(unittest.TestCase):
    def test_extracts_project_from_test_assembly(self) -> None:
        assembly = "/repo/tests/ViciOne.ServiceBus.Tests/bin/Release/net9.0/tests.dll"
        self.assertEqual("ViciOne.ServiceBus.Tests", project_name(assembly))

    def test_rejects_non_test_assembly_shape(self) -> None:
        self.assertEqual("", project_name("/repo/src/component/bin/component.dll"))


if __name__ == "__main__":
    unittest.main()
