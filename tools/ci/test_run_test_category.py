#!/usr/bin/env python3
"""Focused tests for the identity contract of the required category runner.

The runner used to key a case as the last segment of its class name plus the method name and then
removed duplicates. That made two different tests share one permission to stay unexecuted: a fixture
in one namespace authorised a fixture of the same name in another, and seven parameterised cases
collapsed into one. These tests hold the corrected contract in place.

Standard library only, like the runner itself.
"""

from __future__ import annotations

import contextlib
import io
import json
import tempfile
import shutil
import subprocess
from unittest import mock
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
        path = self.root / "model.json"
        path.write_text(json.dumps({
            "schemaVersion": 1,
            "kind": "SERVICEBUS_VERIFICATION_MODEL",
            "capabilities": [{
                "id": f"capability-{category}",
                "class": "LOCAL_REQUIRED_RUN",
                "runs": [{
                    "job": category,
                    "category": category,
                    "project": "tests/Some.Tests/Some.Tests.csproj",
                    "minimumExecutedCases": 1,
                    "notExecuted": [{"identity": identity} for identity in identities],
                }],
            }],
        }, indent=2), encoding="utf-8")
        runner.VERIFICATION_MODEL = path

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


class Keeping_the_invocation_closed(unittest.TestCase):
    """A required category is the whole category, run the same way every time.

    The runner used to forward an arbitrary tail to dotnet test and refuse only the options that
    select tests. Everything else passed, including every way of writing output somewhere the run does
    not own: a second --logger, a --results-directory, a --diag, a collector and an MSBuild output
    override. The repository states that one run owns all of its mutable output, and that statement was
    false for any caller who added one of them. There is no allowlist now, because a required proof has
    no argument left to weigh.
    """

    CANONICAL = ["--category", "core", "--project", "tests/Core/Core.csproj", "--evidence-dir", "artifacts/x"]

    # One per class of damage, named by what it would have written or changed.
    REFUSED = {
        "a second result log": ["--logger", "trx;LogFileName=elsewhere.trx"],
        "a shared results directory": ["--results-directory", "artifacts"],
        "a diagnostic log": ["--diag", "artifacts/diag.log"],
        "a data collector": ["--collect", "XPlat Code Coverage"],
        "an MSBuild output redirect": ["-p:OutputPath=artifacts/elsewhere"],
        "an MSBuild artifacts redirect": ["-p:ArtifactsPath=artifacts/elsewhere"],
        "a different framework": ["--framework", "net9.0"],
        "a different configuration": ["-c", "Debug"],
        "a test selector": ["--filter", "Category!=Slow"],
        "a run settings file": ["--settings", "run.runsettings"],
    }

    def test_the_canonical_invocation_is_accepted(self):
        parsed = runner.build_parser().parse_args(self.CANONICAL)

        self.assertEqual("core", parsed.category)

    def test_every_further_argument_is_refused(self):
        for damage, extra in sorted(self.REFUSED.items()):
            with self.subTest(damage=damage):
                with self.assertRaises(SystemExit, msg=f"{damage} reached dotnet test"), \
                        contextlib.redirect_stderr(io.StringIO()):
                    runner.build_parser().parse_args(self.CANONICAL + extra)

    def test_the_command_dotnet_test_receives_is_exactly_the_canonical_one(self):
        """Not only that nothing is accepted, but that nothing is passed on either."""
        seen = {}

        def record(command, **kwargs):
            seen["command"] = command
            return subprocess.CompletedProcess(args=command, returncode=0, stdout="", stderr="")

        root = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, root, ignore_errors=True)
        self.addCleanup(setattr, runner, "RAW_RUN_OUTPUT_DIR", runner.RAW_RUN_OUTPUT_DIR)
        self.addCleanup(setattr, runner, "REPOSITORY_ROOT", runner.REPOSITORY_ROOT)
        runner.RAW_RUN_OUTPUT_DIR = root / "run-output"
        runner.REPOSITORY_ROOT = root

        with mock.patch.object(runner.subprocess, "run", side_effect=record), \
                mock.patch.dict(runner.os.environ, {runner.RUN_ROOT_VARIABLE: ""}, clear=False), \
                contextlib.redirect_stdout(io.StringIO()):
            with self.assertRaises(runner.CategoryError):
                runner.run_category("core", "tests/Core/Core.csproj", root / "evidence")

        options = [token for token in seen["command"] if token.startswith("-")]
        self.assertEqual(["-c", "--logger"], options,
                         "dotnet test received an option this runner does not own")


class Owning_the_output_of_one_run(unittest.TestCase):
    """Two invocations of the same category must not meet in any file.

    This is the case the earlier parallel proof did not cover: it started two command children against
    different brokers, so it never reached the category runner where the TRX and the record were
    written to one fixed name each.
    """

    def setUp(self) -> None:
        self.root = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.root, ignore_errors=True)
        self.addCleanup(setattr, runner, "RAW_RUN_OUTPUT_DIR", runner.RAW_RUN_OUTPUT_DIR)
        self.addCleanup(setattr, runner, "REPOSITORY_ROOT", runner.REPOSITORY_ROOT)
        runner.RAW_RUN_OUTPUT_DIR = self.root / "run-output"
        runner.REPOSITORY_ROOT = self.root

    def run_category(self, evidence: Path, run_root: str | None = None) -> dict:
        """Runs one category with the process boundary replaced, so no test starts dotnet."""
        environment = {runner.RUN_ROOT_VARIABLE: run_root} if run_root else {}

        def fake_run(command, **kwargs):
            trx = Path([part for part in command if part.startswith("trx;LogFileName=")][0].split("=", 1)[1])
            trx.parent.mkdir(parents=True, exist_ok=True)
            trx.write_text(TRX_WITH_ONE_PASSING_CASE, encoding="utf-8")

            return subprocess.CompletedProcess(args=command, returncode=0, stdout="", stderr="")

        with mock.patch.dict(runner.os.environ, environment, clear=False), \
                mock.patch.object(runner.subprocess, "run", side_effect=fake_run), \
                mock.patch.object(runner, "minimum_executed", return_value=None), \
                mock.patch.object(runner, "inventoried_cases", return_value=[]):
            return runner.run_category("core", "some.csproj", evidence)

    def test_two_runs_of_one_category_write_different_files(self) -> None:
        evidence = self.root / "artifacts/required/core"

        first = self.run_category(evidence)
        second = self.run_category(evidence)

        self.assertNotEqual(first["trxPath"], second["trxPath"], "both runs wrote the same TRX")
        self.assertNotEqual(first["evidenceDir"], second["evidenceDir"], "both runs wrote the same record")
        self.assertTrue((self.root / first["trxPath"]).is_file(), "the first run's TRX was removed")
        self.assertTrue((self.root / second["trxPath"]).is_file())

    def test_a_caller_path_is_a_parent_and_not_a_file(self) -> None:
        evidence = self.root / "artifacts/required/core"

        record = self.run_category(evidence)

        self.assertTrue(record["evidenceDir"].startswith("artifacts/required/core/"),
                        f"the caller's directory has to stay a parent: {record['evidenceDir']}")

    def test_a_run_root_handed_down_is_used_rather_than_a_new_one(self) -> None:
        handed = self.root / "run-output" / "vicione-fromtherunner"

        record = self.run_category(self.root / "artifacts/required/core", run_root=str(handed))

        self.assertTrue(record["trxPath"].endswith("vicione-fromtherunner/core.trx"), record["trxPath"])

    def test_an_interrupted_run_leaves_the_other_run_untouched(self) -> None:
        evidence = self.root / "artifacts/required/core"

        survivor = self.run_category(evidence)

        # A second run that dies before it writes anything: its root exists, the first one's files do not
        # move, and nothing of the first run is deleted.
        abandoned = runner.RAW_RUN_OUTPUT_DIR / "vicione-killedmidway"
        abandoned.mkdir(parents=True)
        (abandoned / "core.trx").write_text("half written", encoding="utf-8")

        self.assertTrue((self.root / survivor["trxPath"]).is_file(),
                        "an abandoned run removed the file of a run that finished")
        self.assertEqual(TRX_WITH_ONE_PASSING_CASE, (self.root / survivor["trxPath"]).read_text(encoding="utf-8"))


TRX_WITH_ONE_PASSING_CASE = """<?xml version="1.0" encoding="UTF-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Times start="2026-08-18T10:00:00.0000000+00:00" finish="2026-08-18T10:00:01.0000000+00:00" />
  <Results>
    <UnitTestResult testName="Suite.Behaviour.Should_pass" outcome="Passed" />
  </Results>
  <TestDefinitions>
    <UnitTest name="Suite.Behaviour.Should_pass">
      <TestMethod className="Suite.Behaviour" name="Should_pass" />
    </UnitTest>
  </TestDefinitions>
  <ResultSummary outcome="Completed">
    <Counters total="1" executed="1" passed="1" failed="0" />
  </ResultSummary>
</TestRun>
"""


if __name__ == "__main__":
    unittest.main()
