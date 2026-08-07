#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Tests for failure-partition identity normalization."""

from __future__ import annotations

import unittest

from test_failure_partition_gate import normalize_identity
from identity_rules import FORMER_LOWER, FORMER_PASCAL


class TestFailurePartitionGateTests(unittest.TestCase):
    def test_normalizes_target_identity_to_baseline_identity(self) -> None:
        target = "ViciOne.ServiceBus.Tests ViciOneServiceBus vicione-servicebus"
        self.assertEqual(
            f"{FORMER_PASCAL}.Tests {FORMER_PASCAL} {FORMER_LOWER}",
            normalize_identity(target),
        )


if __name__ == "__main__":
    unittest.main()
