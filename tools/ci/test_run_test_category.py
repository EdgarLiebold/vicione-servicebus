#!/usr/bin/env python3
"""Focused tests for the identity contract of the required category runner.

ViciOne modification: WP-F2-SERVICEBUS-A-PLUS-RECOVERY-03, 2026-08-16.

The runner used to key a case as the last segment of its class name plus the method name and then
removed duplicates. That made two different tests share one permission to stay unexecuted: a fixture
in one namespace authorised a fixture of the same name in another, and seven parameterised cases
collapsed into one. These tests hold the corrected contract in place.

Standard library only, like the runner itself.
"""

from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

import run_test_category as runner

TRX_HEADER = '<?xml version="1.0" encoding="UTF-8"?>\n<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">\n'


def write_trx(path: Path, cases: list[tuple[str, str, str]]) -> Path:
    """A minimal result file. Each case is (class name, case name, outcome)."""
    definitions, results = [], []
    for index, (class_name, name, outcome) in enumerate(cases):
        identifier = f"00000000-0000-0000-0000-{index:012d}"
        definitions.append(
            f'    <UnitTest id="{identifier}" name="{name}">\n'
            f'      <TestMethod className="{class_name}" name="{name}" />\n'
            f"    </UnitTest>\n"
        )
        results.append(f'    <UnitTestResult testId="{identifier}" testName="{name}" outcome="{outcome}" />\n')

    path.write_text(
        TRX_HEADER
        + "  <TestDefinitions>\n" + "".join(definitions) + "  </TestDefinitions>\n"
        + "  <Results>\n" + "".join(results) + "  </Results>\n"
        + "</TestRun>\n",
        encoding="utf-8",
    )
    return path


class IdentityContractTestCase(unittest.TestCase):
    def setUp(self) -> None:
        self._directory = tempfile.TemporaryDirectory()
        self.root = Path(self._directory.name)
        self.addCleanup(self._directory.cleanup)

    def inventory(self, identities: list[str], category: str = "core") -> None:
        path = self.root / "inventory.json"
        path.write_text(json.dumps({
            "categories": {
                category: {
                    "cases": [
                        {"identity": identity, "fixture": identity.split(".")[-2], "test": identity.split(".")[-1],
                         "mechanism": "EXPLICIT", "dueness": "NOT_DUE_BENCHMARK", "reason": "measured, not behaviour"}
                        for identity in identities
                    ]
                }
            }
        }), encoding="utf-8")
        runner.NOT_EXECUTED_INVENTORY = path

    def test_the_same_fixture_and_case_name_in_two_namespaces_are_two_identities(self):
        trx = write_trx(self.root / "a.trx", [
            ("Suite.Middleware.Specifying_a_rate_limit", "Should_only_do_n_messages_per_interval", "NotExecuted"),
            ("Suite.Pipeline.Specifying_a_rate_limit", "Should_only_do_n_messages_per_interval", "NotExecuted"),
        ])

        self.assertEqual(runner.read_not_executed(trx), [
            "Suite.Middleware.Specifying_a_rate_limit.Should_only_do_n_messages_per_interval",
            "Suite.Pipeline.Specifying_a_rate_limit.Should_only_do_n_messages_per_interval",
        ])

    def test_parameterised_fixture_arguments_stay_distinct(self):
        trx = write_trx(self.root / "b.trx", [
            ("Suite.Serializer_performance(Suite.BsonSerializer)", "Just_how_fast_are_you", "NotExecuted"),
            ("Suite.Serializer_performance(Suite.JsonSerializer)", "Just_how_fast_are_you", "NotExecuted"),
        ])

        skipped = runner.read_not_executed(trx)

        self.assertEqual(len(skipped), 2, "Two parameterised cases are two identities, not one")
        self.assertEqual(skipped, [
            "Suite.Serializer_performance(Suite.BsonSerializer).Just_how_fast_are_you",
            "Suite.Serializer_performance(Suite.JsonSerializer).Just_how_fast_are_you",
        ])

    def test_inventorying_one_colliding_identity_does_not_authorise_the_other(self):
        trx = write_trx(self.root / "c.trx", [
            ("Suite.Middleware.Specifying_a_rate_limit", "Should_only_do_n_messages_per_interval", "NotExecuted"),
            ("Suite.Pipeline.Specifying_a_rate_limit", "Should_only_do_n_messages_per_interval", "NotExecuted"),
        ])
        self.inventory(["Suite.Middleware.Specifying_a_rate_limit.Should_only_do_n_messages_per_interval"])

        unlisted = runner.unauthorised_not_executed(runner.read_not_executed(trx), runner.inventoried_cases("core"))

        self.assertEqual(unlisted, ["Suite.Pipeline.Specifying_a_rate_limit.Should_only_do_n_messages_per_interval"])

    def test_an_additional_not_executed_identity_is_reported(self):
        trx = write_trx(self.root / "d.trx", [
            ("Suite.Benchmarks.Throughput", "Just_how_fast_are_you", "NotExecuted"),
            ("Suite.Behaviour.Delivery", "Should_deliver_the_message", "NotExecuted"),
        ])
        self.inventory(["Suite.Benchmarks.Throughput.Just_how_fast_are_you"])

        unlisted = runner.unauthorised_not_executed(runner.read_not_executed(trx), runner.inventoried_cases("core"))

        self.assertEqual(unlisted, ["Suite.Behaviour.Delivery.Should_deliver_the_message"])

    def test_a_repeated_identity_is_not_authorised_twice_by_one_entry(self):
        trx = write_trx(self.root / "e.trx", [
            ("Suite.Behaviour.Delivery", "Should_deliver_the_message", "NotExecuted"),
            ("Suite.Behaviour.Delivery", "Should_deliver_the_message", "NotExecuted"),
        ])
        self.inventory(["Suite.Behaviour.Delivery.Should_deliver_the_message"])

        unlisted = runner.unauthorised_not_executed(runner.read_not_executed(trx), runner.inventoried_cases("core"))

        self.assertEqual(unlisted, ["Suite.Behaviour.Delivery.Should_deliver_the_message"],
                         "One permission covers one case, so the second occurrence stays unauthorised")

    def test_a_short_form_entry_authorises_nothing(self):
        trx = write_trx(self.root / "f.trx", [
            ("Suite.Behaviour.Delivery", "Should_deliver_the_message", "NotExecuted"),
        ])
        path = self.root / "short.json"
        path.write_text(json.dumps({"categories": {"core": {"cases": [
            {"fixture": "Delivery", "test": "Should_deliver_the_message", "mechanism": "EXPLICIT",
             "dueness": "NOT_DUE_BENCHMARK", "reason": "no identity given"}
        ]}}}), encoding="utf-8")
        runner.NOT_EXECUTED_INVENTORY = path

        unlisted = runner.unauthorised_not_executed(runner.read_not_executed(trx), runner.inventoried_cases("core"))

        self.assertEqual(unlisted, ["Suite.Behaviour.Delivery.Should_deliver_the_message"])

    def test_passed_and_failed_cases_are_not_reported_as_not_executed(self):
        trx = write_trx(self.root / "g.trx", [
            ("Suite.Behaviour.Delivery", "Should_pass", "Passed"),
            ("Suite.Behaviour.Delivery", "Should_fail", "Failed"),
            ("Suite.Behaviour.Delivery", "Should_skip", "NotExecuted"),
        ])

        self.assertEqual(runner.read_not_executed(trx), ["Suite.Behaviour.Delivery.Should_skip"])

    def test_an_assembly_qualified_class_name_keeps_its_argument_list(self):
        self.assertEqual(runner.type_name("Suite.Fixture(1, 2), Suite.Tests"), "Suite.Fixture(1, 2)")
        self.assertEqual(runner.type_name("Suite.Fixture"), "Suite.Fixture")
        self.assertEqual(runner.type_name("Suite.Fixture(a, b)"), "Suite.Fixture(a, b)")


class RunDurationTestCase(unittest.TestCase):
    def test_seven_fractional_digits_are_parsed(self):
        # The writer emits seven digits, which fromisoformat rejects. Returning None there silently
        # recorded the run duration as null on every single run.
        start = runner.parse_trx_time("2026-08-16T06:31:08.9712660+00:00")
        finish = runner.parse_trx_time("2026-08-16T06:38:43.6651310+00:00")

        self.assertIsNotNone(start)
        self.assertIsNotNone(finish)
        self.assertAlmostEqual((finish - start).total_seconds(), 454.694, places=3)

    def test_an_unparsable_stamp_is_reported_as_absent(self):
        self.assertIsNone(runner.parse_trx_time("not a timestamp"))
        self.assertIsNone(runner.parse_trx_time(None))


class SelectorRejectionTestCase(unittest.TestCase):
    REFUSED = (
        "--filter", "--filter=Category!=Slow", "-filter", "/filter:Name~x",
        "--TestCaseFilter:Name~x", "/TestCaseFilter:Name~x", "-testcasefilter",
        "--tests", "--tests=One", "/Tests:One", "-tests",
        # A settings file carries a TestCaseFilter of its own, so it narrows the run without ever
        # naming a filter on the command line.
        "--settings", "--settings=run.runsettings", "/Settings:run.runsettings", "-s", "-s=run.runsettings",
    )

    def test_every_selector_spelling_is_refused(self):
        for selector in self.REFUSED:
            with self.subTest(selector=selector):
                with self.assertRaises(runner.CategoryError):
                    runner.reject_selectors([selector])

    def test_a_selector_is_refused_wherever_it_stands(self):
        with self.assertRaises(runner.CategoryError):
            runner.reject_selectors(["--no-restore", "--settings", "run.runsettings"])

    def test_ordinary_build_and_restore_options_pass(self):
        # A blanket rejection would stop the category from running at all, which is the opposite of
        # keeping it complete.
        runner.reject_selectors([
            "--no-restore", "--no-build", "-v", "minimal", "--verbosity", "detailed",
            "-c", "Release", "--framework", "net9.0", "--nologo", "--results-directory", "artifacts",
            "--logger", "trx;LogFileName=core.trx", "-p:ContinuousIntegrationBuild=true",
        ])

    def test_the_option_token_ignores_value_and_case(self):
        self.assertEqual(runner.option_token("--Filter=Category!=Slow"), "--filter")
        self.assertEqual(runner.option_token("/Tests:One"), "/tests")
        self.assertEqual(runner.option_token("--no-restore"), "--no-restore")


if __name__ == "__main__":
    unittest.main()
