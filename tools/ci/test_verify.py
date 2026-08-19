#!/usr/bin/env python3
"""Tests for the canonical verification entry point and the receipt reader.

The point of this entry point is that a count cannot decide completeness. The case that matters most
below is the one a floor and a counter agree with and a set does not: one expected identity dropped
while another appears, so every number stays the same and the scope silently changed.

Nothing here starts dotnet. What is under test is the decision the entry point makes about a result
file, not the test platform that produced one.

Standard library only.
"""

from __future__ import annotations

import copy
import io
import json
import contextlib
import shutil
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parent))

import run_test_category  # noqa: E402
import validate_receipt  # noqa: E402
import verify  # noqa: E402

TRX_HEADER = ('<?xml version="1.0" encoding="UTF-8"?>\n'
              '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">\n')


def trx_document(cases: list[tuple[str, str, str]]) -> str:
    """A minimal result file. Each case is (class name, case name, outcome)."""
    definitions, results = [], []
    for index, (class_name, name, outcome) in enumerate(cases):
        identifier = f"00000000-0000-0000-0000-{index:012d}"
        definitions.append(
            f'    <UnitTest id="{identifier}" name="{name}">\n'
            f'      <TestMethod className="{class_name}" name="{name}" />\n'
            f"    </UnitTest>\n")
        results.append(f'    <UnitTestResult testId="{identifier}" testName="{name}" outcome="{outcome}" />\n')

    executed = sum(1 for _, _, outcome in cases if outcome != "NotExecuted")
    failed = sum(1 for _, _, outcome in cases if outcome not in ("NotExecuted", "Passed"))

    return (TRX_HEADER
            + "  <TestDefinitions>\n" + "".join(definitions) + "  </TestDefinitions>\n"
            + "  <Results>\n" + "".join(results) + "  </Results>\n"
            + '  <ResultSummary outcome="Completed">\n'
            + f'    <Counters total="{len(cases)}" executed="{executed}" passed="{executed - failed}" '
            + f'failed="{failed}" notExecuted="{len(cases) - executed}" />\n'
            + "  </ResultSummary>\n</TestRun>\n")


PROJECT = "tests/Some.Tests/Some.Tests.csproj"
FIXTURE = "Suite.Delivering_a_message"
THREE_CASES = [(FIXTURE, "Should_arrive", "Passed"),
               (FIXTURE, "Should_acknowledge", "Passed"),
               (FIXTURE, "Should_retry", "Passed")]


class VerifyFixture(unittest.TestCase):
    """A repository of this case alone, with every global the entry point reads bound to it."""

    def setUp(self) -> None:
        # Resolved, like the repository root the code under test derives every path from:
        # /var is a symbolic link to /private/var on this machine, and a fixture that is not
        # canonical makes a run root and the root it is compared with two different strings.
        self.root = Path(tempfile.mkdtemp()).resolve()
        self.addCleanup(shutil.rmtree, self.root, ignore_errors=True)

        for module, name in ((verify, "REPO_ROOT"), (verify, "MODEL_FILE"),
                             (verify, "RAW_RUN_OUTPUT_DIR"),
                             (run_test_category, "REPO_ROOT"), (run_test_category, "REPOSITORY_ROOT"),
                             (run_test_category, "RAW_RUN_OUTPUT_DIR"),
                             (run_test_category, "VERIFICATION_MODEL")):
            self.addCleanup(setattr, module, name, getattr(module, name))
        verify.REPO_ROOT = self.root
        verify.MODEL_FILE = self.root / "build/verification/VERIFICATION_MODEL.json"
        verify.RAW_RUN_OUTPUT_DIR = self.root / "artifacts/run-output"
        run_test_category.REPO_ROOT = self.root
        run_test_category.REPOSITORY_ROOT = self.root
        run_test_category.RAW_RUN_OUTPUT_DIR = verify.RAW_RUN_OUTPUT_DIR
        run_test_category.VERIFICATION_MODEL = verify.MODEL_FILE

        (self.root / PROJECT).parent.mkdir(parents=True, exist_ok=True)
        (self.root / PROJECT).write_text("<Project />\n", encoding="utf-8")

    def write_model(self, selections: dict | None = None, **changes) -> dict:
        run = {"job": "some", "category": "core", "project": PROJECT, "minimumExecutedCases": 1,
               "budgetSeconds": 60, "notExecuted": [], "brokers": [], "allowBrokerOutage": None,
               "oneRefusalPerVhost": None, "expectedIdentities": None,
               "testProjectDirectory": "tests/Some.Tests", "explicitAttributeCount": 0}
        run.update(changes)
        model = {
            "schemaVersion": 1, "kind": "SERVICEBUS_VERIFICATION_MODEL",
            "selections": selections or {"all": {"members": ["core"]}, "core": {"members": ["core"]}},
            "capabilities": [{"id": "capability-core", "class": "LOCAL_REQUIRED_RUN", "runs": [run]}],
        }
        verify.MODEL_FILE.parent.mkdir(parents=True, exist_ok=True)
        verify.MODEL_FILE.write_text(json.dumps(model, indent=2), encoding="utf-8")
        self.model = model

        return model

    def child_writing(self, cases: list[tuple[str, str, str]], **outcome):
        """A child that produces this result file instead of starting a test platform."""
        def child(command, environment, budget):
            trx = Path(environment[verify.run_ownership.RUN_ROOT_VARIABLE]) / "core.trx"
            trx.write_text(trx_document(cases), encoding="utf-8")

            answer = {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                      "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}
            answer.update(outcome)

            return answer

        return child

    def expect(self, identities: list[str]) -> str:
        """Records an expected identity set the way the record mode writes one."""
        path = self.root / "build/verification/expected/core.txt"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("".join(f"{identity}\n" for identity in identities), encoding="utf-8")

        return path.relative_to(self.root).as_posix()

    def run_verify(self, cases, expected=None, **outcome) -> dict:
        model = self.write_model(expectedIdentities=expected)
        with mock.patch.object(run_test_category, "run_child",
                               side_effect=self.child_writing(cases, **outcome)), \
                contextlib.redirect_stdout(io.StringIO()):
            return verify.verify_category(verify.declared_run(model, "core"),
                                          self.root / "artifacts/verification/run")

    def run_verify_writing(self, document: str | None, expected=None, **changes) -> dict:
        """A run whose child leaves exactly this result file, however malformed, or none at all."""
        def child(command, environment, budget):
            if document is not None:
                (Path(environment[verify.run_ownership.RUN_ROOT_VARIABLE]) / "core.trx").write_text(
                    document, encoding="utf-8")

            return {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                    "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}

        model = self.write_model(expectedIdentities=expected, **changes)
        with mock.patch.object(run_test_category, "run_child", side_effect=child), \
                contextlib.redirect_stdout(io.StringIO()):
            return verify.verify_category(verify.declared_run(model, "core"),
                                          self.root / "artifacts/verification/run")


class Resolving_a_selection(VerifyFixture):
    """A scope that quietly shrinks is how a narrow run comes to look like a complete one."""

    def test_resolves_a_selection_of_selections(self) -> None:
        model = self.write_model(selections={
            "all": {"members": ["local", "fixture"]},
            "local": {"members": ["core"]},
            "fixture": {"members": ["core"]},
        })

        self.assertEqual(["core"], verify.resolve_selection(model, "all"))

    def test_a_selection_may_carry_the_name_of_its_only_category(self) -> None:
        model = self.write_model(selections={"core": {"members": ["core"]}})

        self.assertEqual(["core"], verify.resolve_selection(model, "core"))

    def test_refuses_a_selection_the_model_does_not_know(self) -> None:
        model = self.write_model()

        with self.assertRaises(verify.VerificationError) as raised:
            verify.resolve_selection(model, "everything")

        self.assertIn("is not a selection", str(raised.exception))

    def test_refuses_a_member_that_is_neither_a_selection_nor_a_category(self) -> None:
        model = self.write_model(selections={"all": {"members": ["something-else"]}})

        with self.assertRaises(verify.VerificationError) as raised:
            verify.resolve_selection(model, "all")

        self.assertIn("neither a selection nor a category", str(raised.exception))

    def test_refuses_a_circle(self) -> None:
        model = self.write_model(selections={"all": {"members": ["wider"]},
                                             "wider": {"members": ["all"]}})

        with self.assertRaises(verify.VerificationError) as raised:
            verify.resolve_selection(model, "all")

        self.assertIn("in a circle", str(raised.exception))


class Comparing_the_set_a_run_executed(VerifyFixture):
    """The whole reason this entry point exists.

    A floor and a counter both agree with a run that dropped one case and gained another. The set does
    not, and that is the first case below.
    """

    def test_one_case_dropped_and_another_gained_keeps_every_count_and_is_still_red(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge",
                                f"{FIXTURE}.Should_retry"])
        swapped = [(FIXTURE, "Should_arrive", "Passed"),
                   (FIXTURE, "Should_acknowledge", "Passed"),
                   (FIXTURE, "Should_do_something_else", "Passed")]

        result = self.run_verify(swapped, expected=expected)

        self.assertEqual(3, len(result["executed"]), "the count did not change, which is the point")
        self.assertEqual([f"{FIXTURE}.Should_retry"], result["missing"])
        self.assertEqual([f"{FIXTURE}.Should_do_something_else"], result["unexpected"])
        self.assertEqual("FAIL", result["terminal"])

    def test_the_exact_set_is_a_pass(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge",
                                f"{FIXTURE}.Should_retry"])

        result = self.run_verify(THREE_CASES, expected=expected)

        self.assertEqual([], result["findings"])
        self.assertEqual("PASS", result["terminal"])

    def test_a_duplicate_identity_is_red(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge"])
        twice = [(FIXTURE, "Should_arrive", "Passed"), (FIXTURE, "Should_arrive", "Passed"),
                 (FIXTURE, "Should_acknowledge", "Passed")]

        result = self.run_verify(twice, expected=expected)

        self.assertEqual([f"{FIXTURE}.Should_arrive"], result["duplicate"])
        self.assertEqual("FAIL", result["terminal"])

    def test_a_failed_identity_is_red(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive"])

        result = self.run_verify([(FIXTURE, "Should_arrive", "Failed")], expected=expected)

        self.assertEqual([f"{FIXTURE}.Should_arrive"], result["failed"])
        self.assertEqual("FAIL", result["terminal"])

    def test_a_skipped_identity_the_model_did_not_approve_is_red(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive"])
        model = self.write_model(expectedIdentities=expected)
        del model

        result = self.run_verify([(FIXTURE, "Should_arrive", "NotExecuted")], expected=expected)

        self.assertEqual([f"{FIXTURE}.Should_arrive"], result["unapprovedNotExecuted"])
        self.assertEqual("FAIL", result["terminal"])

    def test_a_skipped_identity_the_model_approved_is_not_a_finding(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive"])
        model = self.write_model(
            expectedIdentities=expected,
            notExecuted=[{"identity": f"{FIXTURE}.Should_wait", "fixture": "Delivering_a_message",
                          "test": "Should_wait", "mechanism": "EXPLICIT",
                          "dueness": "NOT_DUE_MANUAL_OBSERVATION", "reason": "watched by hand"}])
        cases = [(FIXTURE, "Should_arrive", "Passed"), (FIXTURE, "Should_wait", "NotExecuted")]

        with mock.patch.object(run_test_category, "run_child", side_effect=self.child_writing(cases)), \
                contextlib.redirect_stdout(io.StringIO()):
            result = verify.verify_category(verify.declared_run(model, "core"),
                                            self.root / "artifacts/verification/run")

        self.assertEqual([], result["unapprovedNotExecuted"])
        self.assertEqual("PASS", result["terminal"])

    def test_a_category_without_a_recorded_expected_set_cannot_pass(self) -> None:
        result = self.run_verify(THREE_CASES)

        self.assertFalse(result["expectedRecorded"])
        self.assertEqual("FAIL", result["terminal"])
        self.assertIn("no expected identity set", " ".join(result["findings"]))

    def test_a_missing_result_file_is_red(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive"])

        def child(command, environment, budget):
            return {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                    "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}

        model = self.write_model(expectedIdentities=expected)
        with mock.patch.object(run_test_category, "run_child", side_effect=child), \
                contextlib.redirect_stdout(io.StringIO()):
            result = verify.verify_category(verify.declared_run(model, "core"),
                                            self.root / "artifacts/verification/run")

        self.assertEqual([f"{FIXTURE}.Should_arrive"], result["missing"])
        self.assertEqual("FAIL", result["terminal"])

    def test_a_malformed_result_file_is_refused(self) -> None:
        """And refused as a result, not as an exception.

        A run that ends on a traceback ends before it writes its receipt, so the one record that says
        what happened is the one thing that is missing. The unreadable file is a finding of this
        category like any other.
        """
        expected = self.expect([f"{FIXTURE}.Should_arrive"])

        result = self.run_verify_writing("<TestRun>", expected=expected)

        self.assertEqual("FAIL", result["terminal"])
        self.assertIn("not readable", " ".join(result["resultFindings"]))
        self.assertIn("result file:", " ".join(result["findings"]),
                      "the unreadable result file never reached the findings of the category")

    def test_a_timed_out_child_is_red_even_with_an_exact_set(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge",
                                f"{FIXTURE}.Should_retry"])

        result = self.run_verify(THREE_CASES, expected=expected, timedOut=True, exitCode=1)

        self.assertEqual("FAIL", result["terminal"])
        self.assertIn("did not finish within", " ".join(result["findings"]))

    def test_a_surviving_owned_process_is_red_even_with_an_exact_set(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge",
                                f"{FIXTURE}.Should_retry"])

        result = self.run_verify(THREE_CASES, expected=expected,
                                 survivingOwnedProcesses=["4711 dotnet test"])

        self.assertEqual("FAIL", result["terminal"])
        self.assertIn("survived it", " ".join(result["findings"]))


class Recording_an_expected_set(VerifyFixture):
    """Recording resolves a difference from the previous expectation. It may not bake in a defect."""

    def record(self, cases, expected=None, **outcome) -> dict:
        model = self.write_model(expectedIdentities=expected)
        with mock.patch.object(run_test_category, "run_child",
                               side_effect=self.child_writing(cases, **outcome)), \
                contextlib.redirect_stdout(io.StringIO()):
            result = verify.verify_category(verify.declared_run(model, "core"),
                                            self.root / "artifacts/verification/run")
            verify.record_expected(model, result)

        return result

    def test_records_a_set_that_differs_from_the_previous_one(self) -> None:
        """A case added or removed on purpose lands exactly here, and that is not a defect of the run."""
        expected = self.expect([f"{FIXTURE}.Should_arrive"])

        self.record(THREE_CASES, expected=expected)

        written = (self.root / "build/verification/expected/core.txt").read_text(encoding="utf-8")

        self.assertIn(f"{FIXTURE}.Should_retry", written)

    def test_refuses_to_record_from_a_run_with_a_failed_case(self) -> None:
        with self.assertRaises(verify.VerificationError) as raised:
            self.record([(FIXTURE, "Should_arrive", "Failed")])

        self.assertIn("cannot record", str(raised.exception))

    def test_refuses_to_record_from_a_run_that_was_taken_down(self) -> None:
        with self.assertRaises(verify.VerificationError):
            self.record(THREE_CASES, timedOut=True, exitCode=1)

    def test_refuses_to_record_from_a_run_that_left_a_process_behind(self) -> None:
        with self.assertRaises(verify.VerificationError):
            self.record(THREE_CASES, survivingOwnedProcesses=[4711])

    def test_refuses_to_record_from_a_run_with_an_unapproved_skip(self) -> None:
        with self.assertRaises(verify.VerificationError):
            self.record([(FIXTURE, "Should_arrive", "NotExecuted")])


class Owning_what_a_run_writes(VerifyFixture):
    """Two runs of one selection share no mutable file, and each child gets its own proven root."""

    def test_two_runs_of_one_category_get_different_roots_and_different_receipts(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge",
                                f"{FIXTURE}.Should_retry"])

        first = self.run_verify(THREE_CASES, expected=expected)
        second = self.run_verify(THREE_CASES, expected=expected)

        self.assertNotEqual(first["runRoot"], second["runRoot"])
        self.assertTrue((self.root / first["runRoot"] / "core.trx").is_file())
        self.assertTrue((self.root / second["runRoot"] / "core.trx").is_file())

    def test_the_child_is_handed_a_root_it_can_prove_is_its_own(self) -> None:
        seen = {}

        def child(command, environment, budget):
            seen["root"] = environment[verify.run_ownership.RUN_ROOT_VARIABLE]
            seen["token"] = environment[verify.run_ownership.RUN_TOKEN_VARIABLE]
            (Path(seen["root"]) / "core.trx").write_text(trx_document(THREE_CASES), encoding="utf-8")

            return {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                    "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}

        model = self.write_model()
        with mock.patch.object(run_test_category, "run_child", side_effect=child), \
                contextlib.redirect_stdout(io.StringIO()):
            verify.verify_category(verify.declared_run(model, "core"),
                                   self.root / "artifacts/verification/run")

        proof = Path(seen["root"]) / verify.run_ownership.RUN_TOKEN_FILE

        self.assertEqual(seen["token"], proof.read_text(encoding="utf-8").strip(),
                         "the child was handed a root it cannot prove belongs to this run")


class Carrying_the_fixture_side_into_the_receipt(VerifyFixture):
    """A category that runs against a fixture is only verified if the fixture side was verified too.

    The broker runner writes its findings and its log digests into the run root, and the entry point
    reads that file. Recognising a cleanup failure by matching prose in stderr would be reading a
    sentence, and a sentence is not a contract - and reading nothing at all, which is what the absence
    of that file used to mean, is not even a sentence.
    """

    IDENTITIES = [f"{FIXTURE}.Should_acknowledge", f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_retry"]

    def child_with_fixture(self, record: dict | None):
        def child(command, environment, budget):
            root = Path(environment[verify.run_ownership.RUN_ROOT_VARIABLE])
            (root / "core.trx").write_text(trx_document(THREE_CASES), encoding="utf-8")
            if record is not None:
                (root / "fixture-findings.json").write_text(json.dumps(record), encoding="utf-8")

            return {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                    "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}

        return child

    def fixture_record(self, **changes) -> dict:
        record = {"schemaVersion": 1, "kind": "SERVICEBUS_FIXTURE_FINDINGS",
                  "brokers": ["activemq"], "allowedBrokerOutage": None, "findings": [],
                  "logs": {"activemq": "a" * 64}}
        record.update(changes)

        return record

    def verify_with(self, record: dict | None, **model_changes) -> dict:
        expected = self.expect(self.IDENTITIES)
        model = self.write_model(expectedIdentities=expected, brokers=["activemq"], **model_changes)
        with mock.patch.object(run_test_category, "run_child",
                               side_effect=self.child_with_fixture(record)), \
                contextlib.redirect_stdout(io.StringIO()):
            return verify.verify_category(verify.declared_run(model, "core"),
                                          self.root / "artifacts/verification/run")

    def test_a_clean_fixture_leaves_the_category_green(self) -> None:
        result = self.verify_with(self.fixture_record())

        self.assertEqual("PASS", result["terminal"], "; ".join(result["findings"]))
        self.assertEqual({"activemq": "a" * 64}, result["brokerLogSha256"])
        self.assertTrue(result["fixtureRecorded"])

    def test_a_cleanup_finding_makes_an_otherwise_exact_category_red(self) -> None:
        result = self.verify_with(
            self.fixture_record(findings=["broker-teardown: the fixture could not be removed"]))

        self.assertEqual("FAIL", result["terminal"],
                         "the set was exact and the fixture was left standing, and the receipt said "
                         "nothing about it")
        self.assertIn("broker-teardown", " ".join(result["findings"]))

    def test_a_broker_backed_run_that_wrote_no_fixture_record_is_red(self) -> None:
        """The counterexample: an exact result file, a clean child, and nothing about the fixture.

        This ran green. The result set was exactly the expected one, so every identity check agreed,
        and the one file that would have said whether the declared brokers were ever started, whether
        the outage protocol was the declared one and whether the fixture came back was simply absent.
        Absence was read as no finding.
        """
        result = self.verify_with(None)

        self.assertEqual("FAIL", result["terminal"],
                         "a category that runs against a fixture passed without a word about that "
                         "fixture, on the strength of its test results alone")
        self.assertFalse(result["fixtureRecorded"])
        self.assertIn("no fixture record was written", " ".join(result["findings"]))

    def test_a_category_without_brokers_needs_no_fixture_record(self) -> None:
        expected = self.expect(self.IDENTITIES)

        result = self.run_verify(THREE_CASES, expected=expected)

        self.assertEqual([], result["fixtureFindings"])
        self.assertFalse(result["fixtureRecorded"])
        self.assertEqual("PASS", result["terminal"], "; ".join(result["findings"]))

    def test_a_record_about_other_brokers_is_not_this_run_s_fixture_proof(self) -> None:
        result = self.verify_with(self.fixture_record(brokers=["rabbitmq"],
                                                      logs={"rabbitmq": "b" * 64}))

        self.assertIn("is about ['rabbitmq'] and this category runs against ['activemq']",
                      " ".join(result["findings"]),
                      "a record of another fixture was accepted as this category's fixture proof")
        self.assertEqual("FAIL", result["terminal"], "; ".join(result["findings"]))

    def test_a_record_with_another_outage_permission_is_refused(self) -> None:
        """The fixture ran a protocol this category never declared, and its results are that run's."""
        result = self.verify_with(self.fixture_record(allowedBrokerOutage="activemq"))

        self.assertIn("outage permission", " ".join(result["findings"]),
                      "the fixture ran an outage protocol this category never declared and the run "
                      "was still counted as proof of it")
        self.assertEqual("FAIL", result["terminal"], "; ".join(result["findings"]))

    def test_a_record_of_another_kind_is_refused(self) -> None:
        result = self.verify_with(self.fixture_record(kind="SOMETHING_ELSE"))

        self.assertIn("not a fixture record", " ".join(result["findings"]),
                      "a document of another kind was read as this run's fixture record")
        self.assertEqual("FAIL", result["terminal"], "; ".join(result["findings"]))

    def test_a_record_without_a_log_digest_binds_no_broker_output_to_this_run(self) -> None:
        result = self.verify_with(self.fixture_record(logs={}))

        self.assertIn("no log digest for 'activemq'", " ".join(result["findings"]),
                      "nothing bound the broker's own output to this run and the fixture still "
                      "counted as proved")
        self.assertEqual("FAIL", result["terminal"], "; ".join(result["findings"]))

    def test_a_record_carrying_a_field_this_reader_does_not_check_is_refused(self) -> None:
        result = self.verify_with(self.fixture_record(outageWasClean=True))

        self.assertIn("unknown field 'outageWasClean'", " ".join(result["findings"]),
                      "the record carried a claim this reader never checks and was accepted anyway")
        self.assertEqual("FAIL", result["terminal"], "; ".join(result["findings"]))

    def test_an_unreadable_record_is_a_finding_and_not_an_exception(self) -> None:
        expected = self.expect(self.IDENTITIES)
        model = self.write_model(expectedIdentities=expected, brokers=["activemq"])

        def child(command, environment, budget):
            root = Path(environment[verify.run_ownership.RUN_ROOT_VARIABLE])
            (root / "core.trx").write_text(trx_document(THREE_CASES), encoding="utf-8")
            (root / "fixture-findings.json").write_text("{not json", encoding="utf-8")

            return {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                    "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}

        with mock.patch.object(run_test_category, "run_child", side_effect=child), \
                contextlib.redirect_stdout(io.StringIO()):
            result = verify.verify_category(verify.declared_run(model, "core"),
                                            self.root / "artifacts/verification/run")

        self.assertIn("not readable", " ".join(result["fixtureFindings"]),
                      "an unreadable fixture record left the run with nothing to say about it")
        self.assertEqual("FAIL", result["terminal"], "; ".join(result["findings"]))

    def test_the_passed_identities_are_named_rather_than_derived(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge"])
        mixed = [(FIXTURE, "Should_arrive", "Passed"), (FIXTURE, "Should_acknowledge", "Failed")]

        result = self.run_verify(mixed, expected=expected)

        self.assertEqual([f"{FIXTURE}.Should_arrive"], result["passed"])
        self.assertEqual([f"{FIXTURE}.Should_acknowledge"], result["failed"])


class Reading_the_result_file_the_platform_wrote(VerifyFixture):
    """The native result is the executed truth, so a file that cannot carry one is refused.

    Every shape below leaves the counters adding up. That is the point: a summary is a summary of
    whatever the writer decided to summarise, and the only thing that says which cases really ran is
    the result list itself.
    """

    def test_a_result_naming_a_case_the_file_never_defines_is_refused(self) -> None:
        document = trx_document(THREE_CASES).replace(
            '<UnitTestResult testId="00000000-0000-0000-0000-000000000002"',
            '<UnitTestResult testId="00000000-0000-0000-0000-000000000099"')

        result = self.run_verify_writing(document, expected=self.expect(
            [f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge"]))

        self.assertIn("defines no case for", " ".join(result["resultFindings"]))
        self.assertEqual("FAIL", result["terminal"])

    def test_a_test_id_defined_twice_leaves_the_case_a_result_belongs_to_undecided(self) -> None:
        document = trx_document(THREE_CASES).replace(
            '<UnitTest id="00000000-0000-0000-0000-000000000001" name="Should_acknowledge">',
            '<UnitTest id="00000000-0000-0000-0000-000000000000" name="Should_acknowledge">')

        result = self.run_verify_writing(document, expected=self.expect([f"{FIXTURE}.Should_arrive"]))

        self.assertIn("is defined more than once", " ".join(result["resultFindings"]))
        self.assertEqual("FAIL", result["terminal"])

    def test_one_case_reported_twice_under_one_test_id_is_refused(self) -> None:
        document = trx_document(THREE_CASES).replace(
            '<UnitTestResult testId="00000000-0000-0000-0000-000000000002" testName="Should_retry" outcome="Passed" />',
            '<UnitTestResult testId="00000000-0000-0000-0000-000000000000" testName="Should_arrive" outcome="Passed" />')

        result = self.run_verify_writing(document, expected=self.expect(
            [f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge"]))

        self.assertIn("is reported 2 times under one test id", " ".join(result["resultFindings"]))
        self.assertEqual("FAIL", result["terminal"])

    def test_a_summary_that_does_not_count_its_own_results_is_refused(self) -> None:
        document = trx_document(THREE_CASES).replace('passed="3"', 'passed="4"')

        result = self.run_verify_writing(document, expected=self.expect(self_identities()))

        self.assertIn("the summary counts 4 passed and the file carries 3",
                      " ".join(result["resultFindings"]))
        self.assertEqual("FAIL", result["terminal"])

    def test_a_file_without_a_summary_is_refused(self) -> None:
        document = trx_document(THREE_CASES)
        document = document[:document.index("  <ResultSummary")] + "</TestRun>\n"

        result = self.run_verify_writing(document, expected=self.expect(self_identities()))

        self.assertIn("no counter summary", " ".join(result["resultFindings"]))
        self.assertEqual("FAIL", result["terminal"])

    def test_counters_that_are_not_numbers_are_refused(self) -> None:
        document = trx_document(THREE_CASES).replace('total="3"', 'total="many"')

        result = self.run_verify_writing(document, expected=self.expect(self_identities()))

        self.assertIn("not numbers", " ".join(result["resultFindings"]))
        self.assertEqual("FAIL", result["terminal"])

    def test_a_clean_file_leaves_no_finding_at_all(self) -> None:
        """The mutation guard of every case above: this file has to stay silent.

        Without it a parser that simply reported something on every file would satisfy all of them.
        """
        result = self.run_verify_writing(trx_document(THREE_CASES),
                                         expected=self.expect(self_identities()))

        self.assertEqual([], result["resultFindings"])
        self.assertEqual("PASS", result["terminal"], "; ".join(result["findings"]))
        self.assertEqual({"total": 3, "executed": 3, "passed": 3, "failed": 0, "notExecuted": 0},
                         result["resultCounters"])

    def test_the_not_executed_counter_is_recorded_and_never_believed(self) -> None:
        """Measured on a real rabbitmq run: twenty NotExecuted results, notExecuted="0".

        The identities are read from the results for that reason, and this case holds the reason in
        place - a reader that started comparing that counter would be red on the real file.
        """
        cases = [(FIXTURE, "Should_arrive", "Passed"), (FIXTURE, "Should_retry", "NotExecuted")]
        document = trx_document(cases).replace('notExecuted="1"', 'notExecuted="0"')

        result = self.run_verify_writing(document, expected=self.expect([f"{FIXTURE}.Should_arrive"]),
                                         notExecuted=[{"identity": f"{FIXTURE}.Should_retry",
                                                       "reason": "NOT_DUE_BENCHMARK"}])

        self.assertEqual([], result["resultFindings"],
                         "a counter the writer is known to get wrong was compared with the results")
        self.assertEqual([f"{FIXTURE}.Should_retry"], result["skipped"])
        self.assertEqual("PASS", result["terminal"], "; ".join(result["findings"]))


def self_identities() -> list[str]:
    return [f"{FIXTURE}.Should_acknowledge", f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_retry"]


class Building_the_child_command(VerifyFixture):
    """The categories, projects, brokers and filters live in the model, not in a workflow."""

    def test_a_category_without_a_broker_goes_to_the_category_runner(self) -> None:
        model = self.write_model()
        command = verify.child_command(verify.declared_run(model, "core"), self.root / "evidence")

        self.assertIn("run_test_category.py", " ".join(command))
        self.assertNotIn("run_broker_category.py", " ".join(command))

    def test_a_category_with_brokers_goes_through_the_broker_runner_with_all_of_them(self) -> None:
        model = self.write_model(brokers=["activemq", "artemis"], allowBrokerOutage="activemq")
        command = verify.child_command(verify.declared_run(model, "core"), self.root / "evidence")

        self.assertIn("run_broker_category.py", " ".join(command))
        self.assertEqual(["activemq", "artemis"],
                         [command[index + 1] for index, token in enumerate(command) if token == "--broker"])
        self.assertIn("--allow-broker-outage", command)

    def test_the_refusal_rule_of_a_category_comes_from_the_model(self) -> None:
        model = self.write_model(brokers=["rabbitmq"], oneRefusalPerVhost="test-exclusive-*")
        command = verify.child_command(verify.declared_run(model, "core"), self.root / "evidence")

        self.assertIn("--one-refusal-per-vhost", command)
        self.assertIn("test-exclusive-*", command)


class Reading_a_receipt(VerifyFixture):
    """A receipt is a claim about a run, and this is where it is decided whether it may be believed.

    The record under test is not written by hand. It is the record a real run of this entry point
    produced through the child double, so producer and reader are held to one contract: whatever this
    reader refuses, that run can never write.

    What a receipt cannot do is prove that a test process ever started - only the exit status of the
    canonical command inside the required check does that. What it can do is be checked against itself
    and against the native result file where that file is still beside it, and neither of those was
    happening: every derived number was read back exactly as the caller wrote it.
    """

    def setUp(self) -> None:
        super().setUp()
        self.entry = self.run_verify(THREE_CASES, expected=self.expect(self_identities()))
        self.assertEqual("PASS", self.entry["terminal"], "; ".join(self.entry["findings"]))

    def receipt(self, entry: dict | None = None, **changes) -> dict:
        base = {
            "schemaVersion": verify.RECEIPT_SCHEMA_VERSION, "kind": verify.RECEIPT_KIND,
            "runId": "vicione-abcdef123456", "commit": "c" * 40, "tree": "t" * 40,
            "worktreeClean": True, "verificationModelSha256": "m" * 64,
            "selection": "core", "resolvedCategories": ["core"],
            "startedUtc": "2026-08-19T00:00:00+00:00", "finishedUtc": "2026-08-19T00:00:01+00:00",
            "categories": [copy.deepcopy(entry if entry is not None else self.entry)],
            "terminal": "PASS",
        }
        base.update(changes)

        return base

    def altered(self, **changes) -> dict:
        """The receipt of the clean run with single fields overwritten, and nothing else touched."""
        entry = copy.deepcopy(self.entry)
        entry.update(changes)

        return self.receipt(entry)

    def findings(self, receipt: dict, selection: str = "core") -> list[str]:
        return verify.receipt_findings(receipt, selection, "c" * 40, "t" * 40, "m" * 64, ["core"],
                                       self.model)

    def test_accepts_the_receipt_of_a_run_that_really_was_exact(self) -> None:
        self.assertEqual([], self.findings(self.receipt()))

    def test_refuses_a_receipt_of_another_commit(self) -> None:
        self.assertIn("commit", " ".join(self.findings(self.receipt(commit="d" * 40))))

    def test_refuses_a_receipt_of_another_model(self) -> None:
        self.assertIn("different verification model",
                      " ".join(self.findings(self.receipt(verificationModelSha256="n" * 64))))

    def test_refuses_a_narrow_receipt_presented_as_a_wider_one(self) -> None:
        narrow = self.receipt(selection="diagnostics", resolvedCategories=["diagnostics"])

        self.assertIn("A narrower run can never satisfy a wider one", " ".join(self.findings(narrow)))

    def test_refuses_a_receipt_from_a_dirty_working_tree(self) -> None:
        self.assertIn("was not clean", " ".join(self.findings(self.receipt(worktreeClean=False))))

    def test_refuses_something_that_is_not_a_receipt(self) -> None:
        self.assertIn("not a verification receipt", " ".join(self.findings({"kind": "SOMETHING_ELSE"})))

    def test_refuses_a_receipt_of_another_schema_version(self) -> None:
        self.assertIn("schema version", " ".join(self.findings(self.receipt(schemaVersion=99))))

    def test_refuses_a_fabricated_receipt_whose_own_numbers_contradict_its_sets(self) -> None:
        """The counterexample, and the one case this whole reader exists for.

        Every binding field is right - this commit, this tree, this model, this selection, this
        project, this command - and the record says it expected an identity, executed one fewer, and
        is missing none. The reader used to read expected, executed, passed, missing and failed back
        exactly as they were written and find nothing wrong.

        It has to fail for the contradiction. A refusal that came from some unrelated field would
        prove nothing about the arithmetic, so no finding here may name one.
        """
        dropped = "Suite.Delivering_a_message.Should_never_have_been_dropped"

        problems = self.findings(self.altered(expected=sorted(self.entry["expected"] + [dropped])))

        self.assertTrue(problems, "the fabricated receipt was believed")
        self.assertIn(dropped, " ".join(problems),
                      "the receipt was refused without naming the identity it contradicts itself over")
        for problem in problems:
            for unrelated in ("commit", "tree ", "verification model", "selection", "project",
                              "broker", "command", "schema version", "unknown field", "working tree"):
                self.assertNotIn(unrelated, problem,
                                 f"the refusal rests on '{unrelated}' and not on the contradiction: "
                                 f"{problem}")

    def test_refuses_a_receipt_whose_passed_set_is_not_what_it_executed_and_failed(self) -> None:
        problems = self.findings(self.altered(failed=[self_identities()[0]]))

        self.assertIn("declares passed=", " ".join(problems))

    def test_refuses_a_receipt_that_calls_a_category_with_findings_a_pass(self) -> None:
        problems = self.findings(self.altered(
            survivingOwnedProcesses=["4711 dotnet test"], findings=[], terminal="PASS"))

        self.assertIn("calls itself 'PASS' and its own facts make it FAIL", " ".join(problems))

    def test_refuses_a_receipt_whose_category_has_no_recorded_expected_set(self) -> None:
        problems = self.findings(self.altered(expected=None, expectedRecorded=False,
                                              missing=[], unexpected=[], duplicate=[]))

        self.assertIn("no expected identity set is recorded", " ".join(problems))

    def test_refuses_a_receipt_that_reports_a_missing_identity(self) -> None:
        entry = copy.deepcopy(self.entry)
        entry["executed"] = entry["executed"][1:]

        self.assertIn("missing", " ".join(self.findings(self.receipt(entry))))

    def test_refuses_a_receipt_whose_fixture_left_a_finding(self) -> None:
        problems = self.findings(self.altered(
            fixtureFindings=["broker-teardown: the fixture could not be removed"]))

        self.assertIn("broker-teardown", " ".join(problems))

    def test_refuses_a_receipt_carrying_a_field_this_reader_does_not_check(self) -> None:
        """An unknown field is either another schema being read as this one or a claim nobody checks."""
        entry = copy.deepcopy(self.entry)
        entry["everythingWasFine"] = True

        self.assertIn("unknown field 'everythingWasFine'", " ".join(self.findings(self.receipt(entry))))

    def test_refuses_a_receipt_that_leaves_a_field_out(self) -> None:
        entry = copy.deepcopy(self.entry)
        del entry["survivingOwnedProcesses"]

        self.assertIn("has no 'survivingOwnedProcesses'", " ".join(self.findings(self.receipt(entry))))

    def test_refuses_a_receipt_whose_category_ran_another_project(self) -> None:
        self.assertIn("names project", " ".join(self.findings(self.altered(project="tests/Other.csproj"))))

    def test_refuses_a_receipt_whose_category_ran_another_command(self) -> None:
        command = list(self.entry["command"])
        command[command.index("core") if "core" in command else -1] = "diagnostics"

        self.assertIn("is declared to run", " ".join(self.findings(self.altered(command=command))))

    def test_refuses_a_receipt_reporting_brokers_the_model_does_not_declare(self) -> None:
        self.assertIn("names brokers", " ".join(self.findings(self.altered(brokers=["activemq"]))))


class Reading_a_receipt_against_the_file_it_was_produced_from(VerifyFixture):
    """Where the native result is still there, the receipt is checked against it and not only itself.

    This is the only part of reading a receipt that is evidence from outside it. Everything else is
    arithmetic on numbers the receipt states, which is worth doing and is not the same claim.
    """

    def setUp(self) -> None:
        super().setUp()
        self.entry = self.run_verify(THREE_CASES, expected=self.expect(self_identities()))
        self.trx = self.root / self.entry["runRoot"] / "core.trx"

    def receipt(self, entry: dict) -> dict:
        return {"schemaVersion": verify.RECEIPT_SCHEMA_VERSION, "kind": verify.RECEIPT_KIND,
                "runId": "vicione-abcdef123456", "commit": "c" * 40, "tree": "t" * 40,
                "worktreeClean": True, "verificationModelSha256": "m" * 64, "selection": "core",
                "resolvedCategories": ["core"], "startedUtc": "2026-08-19T00:00:00+00:00",
                "finishedUtc": "2026-08-19T00:00:01+00:00", "categories": [entry], "terminal": "PASS"}

    def findings(self, entry: dict) -> list[str]:
        return verify.receipt_findings(self.receipt(entry), "core", "c" * 40, "t" * 40, "m" * 64,
                                       ["core"], self.model)

    def test_the_clean_receipt_is_read_against_its_own_result_file(self) -> None:
        self.assertTrue(self.trx.is_file())
        self.assertEqual([], self.findings(copy.deepcopy(self.entry)))
        self.assertEqual({"categories": 1, "withNativeResult": 1},
                         verify.accompanying_evidence(self.receipt(self.entry)))

    def test_refuses_a_receipt_whose_result_file_has_changed_since_it_was_written(self) -> None:
        self.trx.write_text(trx_document(THREE_CASES[:2]), encoding="utf-8")

        self.assertIn("bytes have changed", " ".join(self.findings(copy.deepcopy(self.entry))))

    def test_refuses_a_record_that_disagrees_with_the_file_it_names(self) -> None:
        """The digest is recomputed and so are the identities, because one of them is the claim.

        A record could carry the right digest and still report a set that is not the one in the file,
        which is why the file is parsed again rather than only hashed.
        """
        document = trx_document(THREE_CASES[:2])
        self.trx.write_text(document, encoding="utf-8")
        entry = copy.deepcopy(self.entry)
        entry["rawResultSha256"] = verify.digest(self.trx)

        problems = " ".join(self.findings(entry))

        self.assertIn("its result file carries 2", problems)

    def test_a_receipt_without_its_result_files_is_checked_and_says_so(self) -> None:
        """A handed-over receipt is worth checking and is worth exactly what it is.

        It is refused when it contradicts itself and accepted when it does not, and the standing of
        what was accepted is printed rather than left to a reader's assumption.
        """
        self.trx.unlink()

        self.assertEqual([], self.findings(copy.deepcopy(self.entry)))
        self.assertEqual({"categories": 1, "withNativeResult": 0},
                         verify.accompanying_evidence(self.receipt(self.entry)))

        path = self.root / "receipt.json"
        path.write_text(json.dumps(self.receipt(self.entry)), encoding="utf-8")
        output = io.StringIO()
        with mock.patch.object(verify, "git", side_effect=lambda *arguments: ""), \
                mock.patch.object(verify, "load_model", return_value=(self.model, "m" * 64)), \
                contextlib.redirect_stdout(output):
            code = validate_receipt.main(["--receipt", str(path), "--selection", "core"])

        self.assertEqual(0, code)
        self.assertIn("0/1 native result file(s) were still there",
                      output.getvalue(),
                      "a record checked only against itself was reported as if a result file had been "
                      "read again")


if __name__ == "__main__":
    unittest.main()
