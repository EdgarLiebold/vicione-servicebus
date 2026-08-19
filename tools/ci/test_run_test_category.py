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
import os
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



MODEL_PROJECT = "tests/Some.Tests/Some.Tests.csproj"


class RunnerFixture(unittest.TestCase):
    """A repository of this case alone, with every global and every variable restored afterwards.

    The suite used to swap runner.VERIFICATION_MODEL without a guaranteed restoration, so the order of
    the cases decided what a later one read, and it patched the environment while leaving every
    unrelated ambient variable in place. Both are how a case can pass on real global state rather than
    on the fixture it claims to test. Here every global the runner reads is bound to this case's own
    temporary repository and put back in a cleanup, and the environment starts cleared.
    """

    def setUp(self) -> None:
        self.root = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.root, ignore_errors=True)

        for name in ("RAW_RUN_OUTPUT_DIR", "REPOSITORY_ROOT", "REPO_ROOT", "VERIFICATION_MODEL"):
            self.addCleanup(setattr, runner, name, getattr(runner, name))
        runner.RAW_RUN_OUTPUT_DIR = self.root / "artifacts/run-output"
        runner.REPOSITORY_ROOT = self.root
        runner.REPO_ROOT = self.root
        runner.VERIFICATION_MODEL = self.root / "build/verification/VERIFICATION_MODEL.json"

        # Cleared, not merged: an ambient VICIONE_SERVICEBUS_RUN_ROOT from the developer's shell would
        # otherwise decide where a run under test writes.
        cleared = mock.patch.dict(os.environ, {}, clear=True)
        cleared.start()
        self.addCleanup(cleared.stop)

        (self.root / MODEL_PROJECT).parent.mkdir(parents=True, exist_ok=True)
        (self.root / MODEL_PROJECT).write_text("<Project />\n", encoding="utf-8")

    def write_model(self, **changes) -> None:
        """A model this fixture's category is declared in, with one field changed per case."""
        run = {
            "job": "some", "category": "core", "project": MODEL_PROJECT,
            "minimumExecutedCases": 1, "budgetSeconds": 60, "notExecuted": [],
            "testProjectDirectory": "tests/Some.Tests", "explicitAttributeCount": 0,
        }
        run.update({k: v for k, v in changes.items() if v is not runner})
        for key, value in changes.items():
            if value is runner:
                run.pop(key, None)

        path = runner.VERIFICATION_MODEL
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps({
            "schemaVersion": 1, "kind": "SERVICEBUS_VERIFICATION_MODEL",
            "capabilities": [{"id": "capability-core", "class": "LOCAL_REQUIRED_RUN", "runs": [run]}],
        }, indent=2), encoding="utf-8")


class Refusing_a_category_the_model_does_not_close(RunnerFixture):
    """The canonical runner is also the local entry point, so it may not lean on a policy gate.

    Every reader this replaced answered None for a missing model, an unknown category, an unreadable
    file or an absent floor, and the runner then skipped the check that None stood for. A category the
    model does not know ran with no floor and no inventory, and nothing said so.
    """

    ABSENT = runner  # a sentinel that means "leave this field out"

    def refuses(self, what: str, **changes) -> None:
        self.write_model(**changes)
        with self.assertRaises(runner.CategoryError) as raised:
            runner.category_contract("core", MODEL_PROJECT)

        self.assertIn(what, str(raised.exception))

    def test_accepts_the_category_the_model_declares(self) -> None:
        self.write_model()

        self.assertEqual(60, runner.category_contract("core", MODEL_PROJECT)["budgetSeconds"])

    def test_refuses_a_missing_model(self) -> None:
        with self.assertRaises(runner.CategoryError) as raised:
            runner.category_contract("core", MODEL_PROJECT)

        self.assertIn("missing", str(raised.exception))

    def test_refuses_a_malformed_model(self) -> None:
        runner.VERIFICATION_MODEL.parent.mkdir(parents=True, exist_ok=True)
        runner.VERIFICATION_MODEL.write_text("{ not json", encoding="utf-8")

        with self.assertRaises(runner.CategoryError) as raised:
            runner.category_contract("core", MODEL_PROJECT)

        self.assertIn("not readable", str(raised.exception))

    def test_refuses_a_category_the_model_does_not_declare(self) -> None:
        self.write_model(category="something-else")

        with self.assertRaises(runner.CategoryError) as raised:
            runner.category_contract("core", MODEL_PROJECT)

        self.assertIn("declares no run", str(raised.exception))

    def test_refuses_a_project_the_category_is_not_declared_against(self) -> None:
        self.write_model()

        with self.assertRaises(runner.CategoryError) as raised:
            runner.category_contract("core", "tests/Another.Tests/Another.Tests.csproj")

        self.assertIn("was invoked against", str(raised.exception))

    def test_refuses_a_project_that_is_not_there(self) -> None:
        (self.root / MODEL_PROJECT).unlink()

        self.refuses("not a project file")

    def test_refuses_an_absent_floor(self) -> None:
        self.refuses("no executed floor", minimumExecutedCases=self.ABSENT)

    def test_refuses_a_floor_that_is_not_a_positive_number(self) -> None:
        self.refuses("no executed floor", minimumExecutedCases=0)

    def test_refuses_an_absent_budget(self) -> None:
        self.refuses("no budget", budgetSeconds=self.ABSENT)

    def test_refuses_a_budget_that_is_not_a_positive_number(self) -> None:
        self.refuses("no budget", budgetSeconds=-1)


class Claiming_a_run_root(RunnerFixture):
    """A directory name is not a claim, and an environment variable is a name.

    Any process on this machine can set VICIONE_SERVICEBUS_RUN_ROOT. A run that adopted it would write
    into, and later clean up, a directory belonging to somebody else, so a handed-down root is
    accepted only against the token its owner wrote into it.
    """

    def test_mints_its_own_root_when_none_was_handed_down(self) -> None:
        root = runner.claim_run_root()

        self.assertTrue(root.is_dir())
        self.assertTrue((root / runner.RUN_TOKEN_FILE).is_file())
        self.assertTrue(root.name.startswith("vicione-"))

    def test_two_runs_mint_different_roots_and_different_tokens(self) -> None:
        first, second = runner.claim_run_root(), runner.claim_run_root()

        self.assertNotEqual(first, second)
        self.assertNotEqual((first / runner.RUN_TOKEN_FILE).read_text(encoding="utf-8"),
                            (second / runner.RUN_TOKEN_FILE).read_text(encoding="utf-8"))

    def test_accepts_a_handed_down_root_with_its_token(self) -> None:
        owned = runner.claim_run_root()
        token = (owned / runner.RUN_TOKEN_FILE).read_text(encoding="utf-8").strip()

        with mock.patch.dict(os.environ, {runner.RUN_ROOT_VARIABLE: str(owned),
                                          runner.RUN_TOKEN_VARIABLE: token}):
            self.assertEqual(owned, runner.claim_run_root())

    def test_refuses_a_handed_down_root_without_a_token(self) -> None:
        stranger = self.root / "somebody-elses-run"
        stranger.mkdir()

        with mock.patch.dict(os.environ, {runner.RUN_ROOT_VARIABLE: str(stranger)}):
            with self.assertRaises(runner.CategoryError) as raised:
                runner.claim_run_root()

        self.assertIn("without a valid ownership token", str(raised.exception))

    def test_refuses_another_runs_root_even_with_a_token_variable(self) -> None:
        """The variable is the attacker's; the file is the owner's. They have to agree."""
        theirs = runner.claim_run_root()

        with mock.patch.dict(os.environ, {runner.RUN_ROOT_VARIABLE: str(theirs),
                                          runner.RUN_TOKEN_VARIABLE: "a token I made up"}):
            with self.assertRaises(runner.CategoryError):
                runner.claim_run_root()

    def test_neither_of_two_concurrent_runs_can_claim_the_other(self) -> None:
        first, second = runner.claim_run_root(), runner.claim_run_root()
        first_token = (first / runner.RUN_TOKEN_FILE).read_text(encoding="utf-8").strip()

        with mock.patch.dict(os.environ, {runner.RUN_ROOT_VARIABLE: str(second),
                                          runner.RUN_TOKEN_VARIABLE: first_token}):
            with self.assertRaises(runner.CategoryError):
                runner.claim_run_root()


class Bounding_the_test_process(RunnerFixture):
    """A child that never returns may not hold the run for as long as the machine stays up.

    Three process trees of this repository survived more than thirteen hours on this machine because
    nothing ever asked them to stop. The child runs in a session of its own so the whole tree can be
    signalled, and the budget is what ends it.
    """

    def test_a_child_that_finishes_inside_its_budget_is_reported_as_it_is(self) -> None:
        child = runner.run_child(["sh", "-c", "echo done; exit 3"], dict(os.environ), 30)

        self.assertEqual(3, child["exitCode"])
        self.assertFalse(child["timedOut"])
        self.assertIn("done", child["stdout"])
        self.assertEqual([], child["survivingOwnedProcesses"])

    def test_a_child_that_never_ends_is_taken_down_with_its_whole_tree(self) -> None:
        # A shell that spawns a sleeping grandchild and then waits forever: signalling only the
        # process that was started would leave the grandchild running, which is exactly what happened
        # to the real runs.
        with mock.patch.object(runner, "TERMINATION_GRACE_SECONDS", 3), \
                mock.patch.object(runner, "SURVIVOR_GRACE_SECONDS", 6):
            # 60 s rather than something endless: with the budget in place the child is taken down
            # after two seconds, and a probe that removes the budget then ends in a minute instead of
            # holding the suite. A mutation probe may not need the very defect it is proving.
            child = runner.run_child(["sh", "-c", "sleep 60 & sleep 60"], dict(os.environ), 2)

        self.assertTrue(child["timedOut"], "the budget did not end the child")
        self.assertNotEqual(0, child["exitCode"], "a run that had to be taken down is not a success")
        self.assertEqual([], child["survivingOwnedProcesses"],
                         "a process of this run's own tree outlived the run that started it")

    def test_a_survivor_is_reported_with_what_it_is(self) -> None:
        """A number alone is a mystery by the time anybody reads the finding: the process is gone and
        nothing says what leaked."""
        # The leaked child closes the output pipes it inherited. One that keeps them open is waited
        # for by communicate() anyway - the run then takes as long as the leak, which is a different
        # shape and is covered by the budget.
        with mock.patch.object(runner, "SURVIVOR_GRACE_SECONDS", 1):
            child = runner.run_child(["sh", "-c", "sleep 30 >/dev/null 2>&1 & exit 0"],
                                     dict(os.environ), 30)

        self.assertEqual(1, len(child["survivingOwnedProcesses"]), child["survivingOwnedProcesses"])
        self.assertIn("sleep 30", child["survivingOwnedProcesses"][0],
                      "the finding names a pid and nothing about what it is")

    def test_the_shared_compiler_server_is_not_a_survivor(self) -> None:
        """The SDK keeps VBCSCompiler alive on purpose so the next build reuses it.

        It holds nothing of this run - no fixture port, no file under the run root, no broker - and its
        idle timeout is longer than any grace period this runner could sensibly have, so waiting it out
        would only turn a false finding into a slow one. Measured: this control fired on a green
        ActiveMQ category whose only survivor was exactly this process.
        """
        with mock.patch.object(runner, "SURVIVOR_GRACE_SECONDS", 1):
            child = runner.run_child(
                ["sh", "-c", "sleep 30 >/dev/null 2>&1 & exec -a "
                             "/usr/local/share/dotnet/sdk/10.0.302/Roslyn/bincore/VBCSCompiler "
                             "sleep 30 >/dev/null 2>&1 & exit 0"],
                dict(os.environ), 30)

        self.assertEqual(1, len(child["survivingOwnedProcesses"]),
                         f"the compiler server was counted as a leak: {child['survivingOwnedProcesses']}")
        self.assertIn("sleep 30", child["survivingOwnedProcesses"][0])

    def test_the_child_gets_a_session_of_its_own(self) -> None:
        child = runner.run_child(["sh", "-c", "ps -o pgid= -p $$"], dict(os.environ), 30)

        self.assertNotEqual(str(os.getpgid(0)), child["stdout"].strip(),
                            "the child shares this process's group, so signalling its tree would "
                            "signal the runner as well")


class Reading_every_result_the_run_defined(RunnerFixture):
    """A counter cannot show a case whose result entry is simply absent."""

    def test_a_definition_without_a_result_is_reported(self) -> None:
        trx = self.root / "gap.trx"
        trx.write_text(
            TRX_HEADER
            + "  <TestDefinitions>\n"
            + '    <UnitTest id="00000000-0000-0000-0000-000000000000" name="Should_run">\n'
            + '      <TestMethod className="Suite.Fixture" name="Should_run" />\n'
            + "    </UnitTest>\n"
            + '    <UnitTest id="00000000-0000-0000-0000-000000000001" name="Should_also_run">\n'
            + '      <TestMethod className="Suite.Fixture" name="Should_also_run" />\n'
            + "    </UnitTest>\n"
            + "  </TestDefinitions>\n"
            + "  <Results>\n"
            + '    <UnitTestResult testId="00000000-0000-0000-0000-000000000000" testName="Should_run" outcome="Passed" />\n'
            + "  </Results>\n</TestRun>\n", encoding="utf-8")

        self.assertEqual(["Suite.Fixture.Should_also_run"], runner.omitted_results(trx))

    def test_a_complete_result_file_reports_nothing(self) -> None:
        trx = write_trx(self.root / "whole.trx", [("Suite.Fixture", "Should_run", "Passed"),
                                                  ("Suite.Fixture", "Should_skip", "NotExecuted")])

        self.assertEqual([], runner.omitted_results(trx))


class IdentityContractTestCase(RunnerFixture):
    """The identity of one case, and what a permission to skip it does and does not cover.

    It writes the model through the fixture, which restores the global afterwards. The version before
    this assigned runner.VERIFICATION_MODEL and never put it back, so the order of the suite decided
    what a later case read, and one of its cases assigned a runner.NOT_EXECUTED_INVENTORY attribute
    that production does not read at all - it could only ever have passed on real global state.
    """

    def permit(self, identities: list[str]) -> list[str]:
        """The model permits these identities to stay unexecuted, and returns what that authorises."""
        self.write_model(notExecuted=[{"identity": identity} for identity in identities])

        return runner.permitted_not_executed(runner.category_contract("core", MODEL_PROJECT))

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
        permitted = self.permit(["Suite.Middleware.Specifying_a_rate_limit.Should_only_do_n_messages_per_interval"])

        unlisted = runner.unauthorised_not_executed(runner.read_not_executed(trx), permitted)

        self.assertEqual(unlisted, ["Suite.Pipeline.Specifying_a_rate_limit.Should_only_do_n_messages_per_interval"])

    def test_an_additional_not_executed_identity_is_reported(self):
        trx = write_trx(self.root / "d.trx", [
            ("Suite.Benchmarks.Throughput", "Just_how_fast_are_you", "NotExecuted"),
            ("Suite.Behaviour.Delivery", "Should_deliver_the_message", "NotExecuted"),
        ])
        permitted = self.permit(["Suite.Benchmarks.Throughput.Just_how_fast_are_you"])

        unlisted = runner.unauthorised_not_executed(runner.read_not_executed(trx), permitted)

        self.assertEqual(unlisted, ["Suite.Behaviour.Delivery.Should_deliver_the_message"])

    def test_a_repeated_identity_is_not_authorised_twice_by_one_entry(self):
        trx = write_trx(self.root / "e.trx", [
            ("Suite.Behaviour.Delivery", "Should_deliver_the_message", "NotExecuted"),
            ("Suite.Behaviour.Delivery", "Should_deliver_the_message", "NotExecuted"),
        ])
        permitted = self.permit(["Suite.Behaviour.Delivery.Should_deliver_the_message"])

        unlisted = runner.unauthorised_not_executed(runner.read_not_executed(trx), permitted)

        self.assertEqual(unlisted, ["Suite.Behaviour.Delivery.Should_deliver_the_message"],
                         "One permission covers one case, so the second occurrence stays unauthorised")

    def test_a_short_form_entry_authorises_nothing(self):
        """A permission without a full identity permits nothing.

        Written against the model the runner really reads. The version before this wrote an obsolete
        shape into a file it then assigned to an attribute production does not have, so whatever it
        proved, it was not this.
        """
        trx = write_trx(self.root / "f.trx", [
            ("Suite.Behaviour.Delivery", "Should_deliver_the_message", "NotExecuted"),
        ])
        self.write_model(notExecuted=[
            {"fixture": "Delivery", "test": "Should_deliver_the_message", "mechanism": "EXPLICIT",
             "dueness": "NOT_DUE_BENCHMARK", "reason": "no identity given"}])
        permitted = runner.permitted_not_executed(runner.category_contract("core", MODEL_PROJECT))

        self.assertEqual([], permitted, "an entry without an identity is not a permission")

        unlisted = runner.unauthorised_not_executed(runner.read_not_executed(trx), permitted)

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



class Passing_nothing_on_to_dotnet_test(RunnerFixture):
    """Not only that nothing is accepted on the command line, but that nothing is passed on either."""

    def test_the_command_dotnet_test_receives_is_exactly_the_canonical_one(self) -> None:
        self.write_model()
        seen = {}

        def child(command, environment, budget):
            seen["command"] = command

            return {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                    "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}

        with mock.patch.object(runner, "run_child", side_effect=child), \
                contextlib.redirect_stdout(io.StringIO()):
            with self.assertRaises(runner.CategoryError):
                runner.run_category("core", MODEL_PROJECT, self.root / "evidence")

        options = [token for token in seen["command"] if token.startswith("-")]
        self.assertEqual(["-c", "--logger"], options,
                         "dotnet test received an option this runner does not own")


class Owning_the_output_of_one_run(RunnerFixture):
    """Two invocations of the same category must not meet in any file.

    This is the case the earlier parallel proof did not cover: it started two command children against
    different brokers, so it never reached the category runner where the TRX and the record were
    written to one fixed name each.
    """

    def run_category(self, evidence: Path, run_root: Path | None = None) -> dict:
        """Runs one category with the process boundary replaced, so no test starts dotnet."""
        self.write_model()

        environment = {}
        if run_root is not None:
            environment = {runner.RUN_ROOT_VARIABLE: str(run_root),
                           runner.RUN_TOKEN_VARIABLE:
                               (run_root / runner.RUN_TOKEN_FILE).read_text(encoding="utf-8").strip()}

        def child(command, environment, budget):
            trx = Path([part for part in command if part.startswith("trx;LogFileName=")][0].split("=", 1)[1])
            trx.parent.mkdir(parents=True, exist_ok=True)
            trx.write_text(TRX_WITH_ONE_PASSING_CASE, encoding="utf-8")

            return {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                    "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}

        with mock.patch.dict(os.environ, environment, clear=False), \
                mock.patch.object(runner, "run_child", side_effect=child):
            return runner.run_category("core", MODEL_PROJECT, evidence)

    def test_two_runs_of_one_category_write_different_files(self) -> None:
        evidence = self.root / "artifacts/required/core"

        first = self.run_category(evidence)
        second = self.run_category(evidence)

        self.assertNotEqual(first["trxPath"], second["trxPath"], "both runs wrote the same TRX")
        self.assertNotEqual(first["evidenceDir"], second["evidenceDir"], "both runs wrote the same record")
        self.assertTrue((self.root / first["trxPath"]).is_file(), "the first run's TRX was removed")
        self.assertTrue((self.root / second["trxPath"]).is_file())

    def test_a_caller_path_is_a_parent_and_not_a_file(self) -> None:
        record = self.run_category(self.root / "artifacts/required/core")

        self.assertTrue(record["evidenceDir"].startswith("artifacts/required/core/"),
                        f"the caller's directory has to stay a parent: {record['evidenceDir']}")

    def test_a_run_root_handed_down_with_its_token_is_used_rather_than_a_new_one(self) -> None:
        handed = runner.claim_run_root()

        record = self.run_category(self.root / "artifacts/required/core", run_root=handed)

        self.assertTrue(record["trxPath"].endswith(f"{handed.name}/core.trx"), record["trxPath"])

    def test_a_run_root_handed_down_without_a_token_is_refused(self) -> None:
        """The ambient variable alone is not a claim, and a run that believed it would write into and
        clean up a directory belonging to somebody else."""
        self.write_model()
        stranger = self.root / "somebody-elses-run"
        stranger.mkdir()

        with mock.patch.dict(os.environ, {runner.RUN_ROOT_VARIABLE: str(stranger)}, clear=False):
            with self.assertRaises(runner.CategoryError) as raised:
                runner.run_category("core", MODEL_PROJECT, self.root / "artifacts/required/core")

        self.assertIn("without a valid ownership token", str(raised.exception))

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


class Binding_an_indirect_anchor_to_a_case_that_ran(unittest.TestCase):
    """A capability that leans on another capability's run is proven by what that run executed.

    The old link was a name searched for in raw source text, so a comment, an ordinary class or a whole
    namespace satisfied it equally well. What replaces it is a claim about this run: the named fixture
    executed a case in it.
    """

    def setUp(self) -> None:
        self._directory = tempfile.TemporaryDirectory()
        self.root = Path(self._directory.name)
        self.addCleanup(self._directory.cleanup)
        self.addCleanup(setattr, runner, "VERIFICATION_MODEL", runner.VERIFICATION_MODEL)

    ANCHOR = "Suite.Visualizing.When_visualizing_a_state_machine"

    def model(self, anchors: list[dict], category: str = "core") -> None:
        path = self.root / "model.json"
        path.write_text(json.dumps({
            "schemaVersion": 1,
            "kind": "SERVICEBUS_VERIFICATION_MODEL",
            "capabilities": [
                {
                    "id": "capability-core",
                    "class": "LOCAL_REQUIRED_RUN",
                    "runs": [{"job": category, "category": category,
                              "project": "tests/Some.Tests/Some.Tests.csproj",
                              "minimumExecutedCases": 1, "notExecuted": []}],
                },
                {
                    "id": "capability-visualizer",
                    "class": "LOCAL_REQUIRED_RUN",
                    "verifiedThroughCapability": "capability-core",
                    "testAnchors": anchors,
                },
            ],
        }, indent=2), encoding="utf-8")
        runner.VERIFICATION_MODEL = path

    def unproven(self, cases: list[tuple[str, str, str]], category: str = "core") -> list[str]:
        trx = write_trx(self.root / "run.trx", cases)

        return runner.unproven_anchors(runner.required_anchors(category), runner.read_executed(trx))

    def test_an_anchor_whose_fixture_executed_a_case_is_proven(self) -> None:
        self.model([{"category": "core", "fixture": self.ANCHOR}])

        self.assertEqual([], self.unproven([(self.ANCHOR, "Should_render_the_graph", "Passed")]))

    def test_an_anchor_whose_fixture_was_not_executed_is_not_proven(self) -> None:
        """The counterexample that matters most: the fixture exists, it is even in the result file, and
        the run never started it. A source search cannot tell that apart from a case that ran."""
        self.model([{"category": "core", "fixture": self.ANCHOR}])

        self.assertEqual([self.ANCHOR],
                         self.unproven([(self.ANCHOR, "Should_render_the_graph", "NotExecuted")]))

    def test_an_anchor_no_case_of_this_run_belongs_to_is_not_proven(self) -> None:
        self.model([{"category": "core", "fixture": self.ANCHOR}])

        self.assertEqual([self.ANCHOR], self.unproven([("Suite.Other.Delivering", "Should_arrive", "Passed")]))

    def test_a_failing_case_still_belongs_to_its_anchor(self) -> None:
        """Executed is the question here. Whether the category is green is decided elsewhere, and a
        fixture that ran and failed is not an unproven anchor on top of it."""
        self.model([{"category": "core", "fixture": self.ANCHOR}])

        self.assertEqual([], self.unproven([(self.ANCHOR, "Should_render_the_graph", "Failed")]))

    def test_a_parameterised_fixture_carries_its_own_anchor(self) -> None:
        self.model([{"category": "core", "fixture": self.ANCHOR}])

        self.assertEqual([], self.unproven([(f"{self.ANCHOR}(&quot;dot&quot;)", "Should_render", "Passed")]))

    def test_a_fixture_whose_name_only_starts_with_the_anchor_does_not_carry_it(self) -> None:
        """A prefix is not an identity. Suffixing the name would otherwise let a neighbouring fixture
        stand in for the one that was named."""
        self.model([{"category": "core", "fixture": self.ANCHOR}])

        self.assertEqual([self.ANCHOR],
                         self.unproven([(f"{self.ANCHOR}_again", "Should_render_the_graph", "Passed")]))

    def test_only_the_anchors_of_the_category_being_run_are_required(self) -> None:
        self.model([{"category": "quartz", "fixture": self.ANCHOR}])

        self.assertEqual([], runner.required_anchors("core"))

    def test_the_record_names_the_cases_that_carried_each_anchor(self) -> None:
        trx = write_trx(self.root / "run.trx", [
            (self.ANCHOR, "Should_render_the_graph", "Passed"),
            (self.ANCHOR, "Should_render_the_composite", "Passed"),
            ("Suite.Other.Delivering", "Should_arrive", "Passed"),
        ])
        executed = runner.read_executed(trx)

        carried = sorted(identity for fixture, identity in executed if fixture == self.ANCHOR)

        self.assertEqual([f"{self.ANCHOR}.Should_render_the_composite",
                          f"{self.ANCHOR}.Should_render_the_graph"], carried)


if __name__ == "__main__":
    unittest.main()
