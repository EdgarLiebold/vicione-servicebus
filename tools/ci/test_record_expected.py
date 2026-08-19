#!/usr/bin/env python3
"""Tests for the command that writes an expected identity set.

The expected set is what a verifying run is measured against, so the only interesting question here is
what this command refuses: an unclean tree, a run that was not clean, and a removal nobody approved.
An addition is a test somebody wrote. A removal is coverage this repository had and no longer has.

Nothing here starts dotnet. The child is a double that writes a result file.

Standard library only.
"""

from __future__ import annotations

import contextlib
import io
import json
import shutil
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parent))

import record_expected  # noqa: E402
import run_test_category  # noqa: E402
from verification import process_tree  # noqa: E402
import verify  # noqa: E402
from test_verify import FIXTURE, PROJECT, trx_document  # noqa: E402

KEPT = [(FIXTURE, "Should_acknowledge", "Passed"), (FIXTURE, "Should_arrive", "Passed")]
DROPPED = f"{FIXTURE}.Should_retry"
ALL_THREE = [f"{FIXTURE}.Should_acknowledge", f"{FIXTURE}.Should_arrive", DROPPED]


class RecordingFixture(unittest.TestCase):
    """A repository of this case alone, with a clean tree unless the case says otherwise."""

    def setUp(self) -> None:
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
        run_test_category.REPO_ROOT = run_test_category.REPOSITORY_ROOT = self.root
        run_test_category.RAW_RUN_OUTPUT_DIR = verify.RAW_RUN_OUTPUT_DIR
        run_test_category.VERIFICATION_MODEL = verify.MODEL_FILE

        (self.root / PROJECT).parent.mkdir(parents=True, exist_ok=True)
        (self.root / PROJECT).write_text("<Project />\n", encoding="utf-8")

        self.tree = ""                       # clean, unless a case dirties it
        self.addCleanup(mock.patch.object(
            verify, "git",
            side_effect=lambda *arguments: self.tree if arguments[0] == "status" else "x").stop)
        mock.patch.object(
            verify, "git",
            side_effect=lambda *arguments: self.tree if arguments[0] == "status" else "x").start()

    def write_model(self, expected: list[str] | None = ALL_THREE, floor: int = 3) -> None:
        manifest = None
        if expected is not None:
            path = self.root / "build/verification/expected/core.txt"
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("".join(f"{identity}\n" for identity in sorted(expected)),
                            encoding="utf-8")
            manifest = "build/verification/expected/core.txt"
        verify.MODEL_FILE.parent.mkdir(parents=True, exist_ok=True)
        verify.MODEL_FILE.write_text(json.dumps({
            "schemaVersion": 1, "kind": "SERVICEBUS_VERIFICATION_MODEL",
            "selections": {"core": {"members": ["core"]}},
            "capabilities": [{"id": "c", "class": "LOCAL_REQUIRED_RUN", "runs": [{
                "job": "core-unit", "category": "core", "project": PROJECT,
                "minimumExecutedCases": floor, "budgetSeconds": 60, "notExecuted": [], "brokers": [],
                "expectedIdentities": manifest}]}]}, indent=2), encoding="utf-8")

    def child_writing(self, cases, **outcome):
        def child(command, environment, budget):
            trx = Path(environment[verify.run_scope.RUN_ROOT_VARIABLE]) / "core.trx"
            trx.write_text(trx_document(cases), encoding="utf-8")
            answer = {"exitCode": 0, "stdout": "", "stderr": "", "timedOut": False,
                      "escalatedToKill": False, "seconds": 0.1, "survivingOwnedProcesses": []}
            answer.update(outcome)

            return answer

        return child

    def record(self, cases, *arguments, **outcome) -> tuple[int, str, str]:
        out, err = io.StringIO(), io.StringIO()
        with mock.patch.object(process_tree, "run_child",
                               side_effect=self.child_writing(cases, **outcome)), \
                contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
            code = record_expected.main(["--category", "core", "--evidence-dir",
                                         str(self.root / "artifacts/record"), *arguments])

        return code, out.getvalue(), err.getvalue()

    def manifest(self) -> list[str]:
        return verify.read_identity_file(self.root / "build/verification/expected/core.txt")


class Recording_a_set_that_grew(RecordingFixture):
    """An identity that appears is a test somebody wrote, and needs nobody's approval."""

    def test_records_the_added_identity_and_names_it(self) -> None:
        self.write_model(expected=[f"{FIXTURE}.Should_arrive"], floor=1)

        code, output, _ = self.record(KEPT)

        self.assertEqual(0, code, output)
        self.assertIn(f"+ {FIXTURE}.Should_acknowledge", output)
        self.assertEqual(sorted(f"{FIXTURE}.{name}" for _, name, _ in KEPT), self.manifest())

    def test_never_prints_a_verification_result(self) -> None:
        """Maintenance is not verification, and a reader of the output must not have to know which."""
        self.write_model(expected=[f"{FIXTURE}.Should_arrive"], floor=1)

        code, output, _ = self.record(KEPT)

        self.assertEqual(0, code)
        self.assertIn("RECORDED core", output)
        self.assertNotIn("PASS", output,
                         "a maintenance command printed a verification result")

    def test_writes_the_floor_with_the_manifest_it_belongs_to(self) -> None:
        self.write_model(expected=[f"{FIXTURE}.Should_arrive"], floor=1)

        self.record(KEPT)

        model = json.loads(verify.MODEL_FILE.read_text(encoding="utf-8"))
        run = model["capabilities"][0]["runs"][0]
        self.assertEqual(len(self.manifest()), run["minimumExecutedCases"],
                         "the floor and the manifest are two claims about one category and they "
                         "disagree")


class Recording_a_set_that_shrank(RecordingFixture):
    """The counterexample: a run in which a test disappeared used to record the smaller set and pass."""

    def approval(self, removed: list[str], **changes) -> Path:
        document = {
            "schemaVersion": 1, "kind": record_expected.APPROVAL_KIND, "category": "core",
            "reference": "LEAD_DIRECTIVE 0098", "referenceSha256": "0" * 64,
            "removedIdentities": sorted(removed),
            "removedIdentitiesSha256": record_expected.removal_digest(removed),
        }
        document.update(changes)
        path = self.root / "approval.json"
        path.write_text(json.dumps(document), encoding="utf-8")

        return path

    def test_refuses_a_removal_nobody_approved(self) -> None:
        self.write_model()

        code, _, error = self.record(KEPT)

        self.assertEqual(1, code, "coverage disappeared and was written in as the new expectation")
        self.assertIn(DROPPED, error, "the refusal never names the identity that would be lost")
        self.assertIn(record_expected.removal_digest([DROPPED]), error,
                      "the refusal does not say what an approval of this removal would be bound to")
        self.assertEqual(sorted(ALL_THREE), self.manifest(), "the manifest was written anyway")

    def test_records_a_removal_the_approval_is_bound_to(self) -> None:
        self.write_model()

        code, output, error = self.record(KEPT, "--approval", str(self.approval([DROPPED])))

        self.assertEqual(0, code, error)
        self.assertIn(f"- {DROPPED}", output)
        self.assertEqual(sorted(f"{FIXTURE}.{name}" for _, name, _ in KEPT), self.manifest())

    def test_refuses_an_approval_bound_to_a_different_removal(self) -> None:
        """An approval for one removal may not authorise another, which is what binding it means."""
        self.write_model()
        elsewhere = self.approval([f"{FIXTURE}.Should_acknowledge"])

        code, _, error = self.record(KEPT, "--approval", str(elsewhere))

        self.assertIn("approves a different removal", error,
                      "an approval of another identity authorised this removal")
        self.assertEqual(1, code, error)

    def test_refuses_an_approval_whose_digest_is_not_of_the_set_it_names(self) -> None:
        self.write_model()
        loose = self.approval([DROPPED], removedIdentitiesSha256="f" * 64)

        code, _, error = self.record(KEPT, "--approval", str(loose))

        self.assertIn("disagrees with itself about what was approved", error,
                      "an approval whose digest is not the digest of its own list was accepted, and "
                      "nobody can tell which of the two sides was edited")
        self.assertEqual(1, code, error)

    def test_refuses_an_approval_that_names_no_decision(self) -> None:
        self.write_model()

        code, _, error = self.record(KEPT, "--approval", str(self.approval([DROPPED], reference="  ")))

        self.assertIn("names no decision", error,
                      "an approval that comes from nowhere authorised a removal")
        self.assertEqual(1, code, error)

    def test_refuses_an_approval_whose_reference_is_not_bound_by_a_digest(self) -> None:
        self.write_model()
        loose = self.approval([DROPPED], referenceSha256="approved by the lead")

        code, _, error = self.record(KEPT, "--approval", str(loose))

        self.assertIn("not bound by a digest", error,
                      "nothing said which document was approved and the removal went through")
        self.assertEqual(1, code, error)

    def test_refuses_a_document_of_another_kind(self) -> None:
        self.write_model()
        other = self.approval([DROPPED], kind="SOMETHING_ELSE")

        code, _, error = self.record(KEPT, "--approval", str(other))

        self.assertIn("not a removal approval", error,
                      "a document of another kind approved a removal")
        self.assertEqual(1, code, error)

    def test_refuses_an_approval_carrying_a_field_this_reader_does_not_check(self) -> None:
        self.write_model()
        extra = self.approval([DROPPED], alsoApproveEverythingElse=True)

        code, _, error = self.record(KEPT, "--approval", str(extra))

        self.assertIn("unknown field 'alsoApproveEverythingElse'", error,
                      "the approval carried a claim this reader never checks and was accepted")
        self.assertEqual(1, code, error)

    def test_names_a_removal_and_an_addition_in_one_fixture_as_a_rename(self) -> None:
        """A pairing a reviewer reads differently, and still a removal that needs approving."""
        self.write_model()
        renamed = [(FIXTURE, "Should_acknowledge", "Passed"), (FIXTURE, "Should_arrive", "Passed"),
                   (FIXTURE, "Should_retry_with_a_backoff", "Passed")]

        code, output, error = self.record(renamed, "--approval", str(self.approval([DROPPED])))

        self.assertEqual(0, code, error)
        self.assertIn(f"~ {DROPPED}", output)
        self.assertIn(f"{FIXTURE}.Should_retry_with_a_backoff", output)


class Refusing_to_record_at_all(RecordingFixture):
    """A set recorded from a defective run writes the defect in, and every later run agrees with it."""

    def test_refuses_an_unclean_working_tree(self) -> None:
        self.write_model(expected=[f"{FIXTURE}.Should_arrive"], floor=1)
        self.tree = " M src/ViciOne.ServiceBus/Something.cs\n M tests/Other.cs"

        code, _, error = self.record(KEPT)

        self.assertEqual(1, code, "an expected set was recorded from a tree nobody can name")
        self.assertIn("2 uncommitted change(s)", error)
        self.assertEqual([f"{FIXTURE}.Should_arrive"], self.manifest())

    def test_refuses_a_run_with_a_failed_case(self) -> None:
        self.write_model(expected=None)

        code, _, error = self.record([(FIXTURE, "Should_arrive", "Failed")])

        self.assertIn("cannot be recorded from", error,
                      "a run with a failed case was written in as the expectation, and every later "
                      "run would agree with it")
        self.assertIn("failed identity", error)
        self.assertEqual(1, code, error)

    def test_refuses_a_run_that_was_taken_down(self) -> None:
        self.write_model(expected=None)

        code, _, error = self.record(KEPT, timedOut=True, exitCode=1)

        self.assertIn("taken down", error,
                      "a run whose child was taken down decided what this category is expected to be")
        self.assertEqual(1, code, error)

    def test_refuses_a_run_that_left_a_process_behind(self) -> None:
        self.write_model(expected=None)

        code, _, error = self.record(KEPT, survivingOwnedProcesses=["4711 dotnet test"])

        self.assertIn("survived it", error,
                      "a run that outlived itself decided what this category is expected to be")
        self.assertEqual(1, code, error)

    def test_refuses_a_run_with_an_unapproved_skip(self) -> None:
        self.write_model(expected=None)

        code, _, error = self.record([(FIXTURE, "Should_arrive", "NotExecuted")])

        self.assertIn("unapproved not-executed", error,
                      "a skip nobody approved became part of what this category is expected to be")
        self.assertEqual(1, code, error)

    def test_records_from_a_first_run_that_has_no_expectation_yet(self) -> None:
        """The mutation guard: a command that refused every run would satisfy all of the above."""
        self.write_model(expected=None)

        code, output, error = self.record(KEPT)

        self.assertEqual(0, code, error)
        self.assertIn("RECORDED core: 2 identities", output)


if __name__ == "__main__":
    unittest.main()
