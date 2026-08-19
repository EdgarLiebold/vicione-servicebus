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
        self.root = Path(tempfile.mkdtemp())
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
        expected = self.expect([f"{FIXTURE}.Should_arrive"])

        def child(command, environment, budget):
            (Path(environment[verify.run_ownership.RUN_ROOT_VARIABLE]) / "core.trx").write_text(
                "<TestRun>", encoding="utf-8")

            return {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                    "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}

        model = self.write_model(expectedIdentities=expected)
        with mock.patch.object(run_test_category, "run_child", side_effect=child), \
                contextlib.redirect_stdout(io.StringIO()):
            with self.assertRaises(verify.VerificationError) as raised:
                verify.verify_category(verify.declared_run(model, "core"),
                                       self.root / "artifacts/verification/run")

        self.assertIn("not a readable result file", str(raised.exception))

    def test_a_timed_out_child_is_red_even_with_an_exact_set(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge",
                                f"{FIXTURE}.Should_retry"])

        result = self.run_verify(THREE_CASES, expected=expected, timedOut=True, exitCode=1)

        self.assertEqual("FAIL", result["terminal"])
        self.assertIn("did not finish within", " ".join(result["findings"]))

    def test_a_surviving_owned_process_is_red_even_with_an_exact_set(self) -> None:
        expected = self.expect([f"{FIXTURE}.Should_arrive", f"{FIXTURE}.Should_acknowledge",
                                f"{FIXTURE}.Should_retry"])

        result = self.run_verify(THREE_CASES, expected=expected, survivingOwnedProcesses=[4711])

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


class Building_the_child_command(VerifyFixture):
    """The categories, projects, brokers and filters live in the model, not in a workflow."""

    def test_a_category_without_a_broker_goes_to_the_category_runner(self) -> None:
        model = self.write_model()
        command = verify.child_command(verify.declared_run(model, "core"), self.root / "run",
                                       self.root / "evidence")

        self.assertIn("run_test_category.py", " ".join(command))
        self.assertNotIn("run_broker_category.py", " ".join(command))

    def test_a_category_with_brokers_goes_through_the_broker_runner_with_all_of_them(self) -> None:
        model = self.write_model(brokers=["activemq", "artemis"], allowBrokerOutage="activemq")
        command = verify.child_command(verify.declared_run(model, "core"), self.root / "run",
                                       self.root / "evidence")

        self.assertIn("run_broker_category.py", " ".join(command))
        self.assertEqual(["activemq", "artemis"],
                         [command[index + 1] for index, token in enumerate(command) if token == "--broker"])
        self.assertIn("--allow-broker-outage", command)

    def test_the_refusal_rule_of_a_category_comes_from_the_model(self) -> None:
        model = self.write_model(brokers=["rabbitmq"], oneRefusalPerVhost="test-exclusive-*")
        command = verify.child_command(verify.declared_run(model, "core"), self.root / "run",
                                       self.root / "evidence")

        self.assertIn("--one-refusal-per-vhost", command)
        self.assertIn("test-exclusive-*", command)


class Reading_a_receipt(VerifyFixture):
    """A receipt is a claim, and this is where it is decided whether the claim may be believed."""

    def receipt(self, **changes) -> dict:
        base = {
            "schemaVersion": verify.RECEIPT_SCHEMA_VERSION, "kind": verify.RECEIPT_KIND,
            "runId": "vicione-abcdef123456", "commit": "c" * 40, "tree": "t" * 40,
            "worktreeClean": True, "verificationModelSha256": "m" * 64,
            "selection": "core", "resolvedCategories": ["core"],
            "categories": [{"category": "core", "expectedRecorded": True, "missing": [],
                            "unexpected": [], "duplicate": [], "failed": [],
                            "unapprovedNotExecuted": [], "resultsOmitted": [],
                            "survivingOwnedProcesses": [], "timedOut": False, "childExitCode": 0,
                            "terminal": "PASS"}],
            "terminal": "PASS",
        }
        base.update(changes)

        return base

    def findings(self, receipt: dict, selection: str = "core") -> list[str]:
        return verify.receipt_findings(receipt, selection, "c" * 40, "t" * 40, "m" * 64, ["core"])

    def test_accepts_a_receipt_about_this_commit_model_and_selection(self) -> None:
        self.assertEqual([], self.findings(self.receipt()))

    def test_refuses_a_receipt_of_another_commit(self) -> None:
        self.assertIn("commit", " ".join(self.findings(self.receipt(commit="d" * 40))))

    def test_refuses_a_receipt_of_another_model(self) -> None:
        self.assertIn("different verification model",
                      " ".join(self.findings(self.receipt(verificationModelSha256="n" * 64))))

    def test_refuses_a_narrow_receipt_presented_as_a_wider_one(self) -> None:
        narrow = self.receipt(selection="diagnostics", resolvedCategories=["diagnostics"])

        problems = " ".join(self.findings(narrow))

        self.assertIn("A narrower run can never satisfy a wider one", problems)

    def test_refuses_a_receipt_from_a_dirty_working_tree(self) -> None:
        self.assertIn("was not clean", " ".join(self.findings(self.receipt(worktreeClean=False))))

    def test_refuses_a_receipt_whose_category_has_no_recorded_expected_set(self) -> None:
        loose = self.receipt()
        loose["categories"][0]["expectedRecorded"] = False

        self.assertIn("no recorded expected identity set", " ".join(self.findings(loose)))

    def test_refuses_a_receipt_that_reports_a_missing_identity(self) -> None:
        incomplete = self.receipt()
        incomplete["categories"][0]["missing"] = ["Suite.Fixture.Should_have_run"]

        self.assertIn("missing", " ".join(self.findings(incomplete)))

    def test_refuses_something_that_is_not_a_receipt(self) -> None:
        self.assertIn("not a verification receipt", " ".join(self.findings({"kind": "SOMETHING_ELSE"})))

    def test_refuses_a_receipt_of_another_schema_version(self) -> None:
        self.assertIn("schema version", " ".join(self.findings(self.receipt(schemaVersion=99))))


if __name__ == "__main__":
    unittest.main()
