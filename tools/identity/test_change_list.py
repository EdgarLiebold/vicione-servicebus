#!/usr/bin/env python3
"""Focused tests for the change list gate.

The generator is licence evidence tooling, not product code, so what has to hold is narrow: the
document it writes must be accepted, and a document that no longer matches it must be refused with a
non zero exit.

The split below is the point of this file. Two cases read the canonical checkout and assert that the
real baseline computation still agrees with the tracked CHANGELIST.md; they only read. Every case
that has to damage a document does so inside a repository it created itself, with the baseline
classification replaced by a small injected model, so the checked-out truth is never the subject of
an experiment. A test that damages the real file and restores it afterwards is unsafe even when the
restore works: two concurrent invocations see each other's damage, and a cancelled or killed process
leaves the repository without its licence evidence.
"""

from __future__ import annotations

import hashlib
import io
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parent))

import change_list  # noqa: E402

REPOSITORY = Path(__file__).resolve().parents[2]


class Reading_the_canonical_change_list(unittest.TestCase):
    """The tracked document still matches what the generator produces. Read only, by contract."""

    def check(self) -> int:
        return change_list.main(["--repository", str(REPOSITORY)])

    def test_accepts_the_generated_document(self) -> None:
        self.assertEqual(0, self.check())

    def test_reports_the_number_of_rows_it_classified(self) -> None:
        """The console count is quoted in reports, so it has to be the number of entries.

        Derived as a line count minus a fixed header offset it is one too high for the header this
        file has, and every count taken from the gate output then exceeds what the document lists.
        """
        captured = io.StringIO()
        with redirect_stdout(captured):
            self.assertEqual(0, self.check())

        reported = int(re.search(r"\((\d+) entries\)", captured.getvalue()).group(1))
        text = (REPOSITORY / change_list.CHANGE_LIST).read_text(encoding="utf-8")
        totals = {status: int(value) for status, value in
                  re.findall(r"^\| (Added|Modified|Deleted|Renamed) \| (\d+) \|$", text, re.M)}

        self.assertEqual(sum(totals.values()), reported,
                         "the reported count has to be the sum of the four status totals")
        self.assertEqual(len(re.findall(r"^\| `", text, re.M)), reported,
                         "the reported count has to be the number of rows in the table")


class Refusing_a_document_that_no_longer_matches(unittest.TestCase):
    """Every case owns its repository, so nothing here can reach the checkout.

    The classification is an injected model rather than the real baseline: what these cases are about
    is the verdict the gate reaches about a document on disk, and computing four thousand rows from
    the upstream archive would make each of them slow without asserting anything more.
    """

    ROWS = [
        ("added/New.cs", "Added", ""),
        ("kept/Renamed.cs", "Renamed", "kept/Original.cs"),
        ("kept/Touched.cs", "Modified", "kept/Touched.cs"),
        ("removed/Gone.cs", "Deleted", "removed/Gone.cs"),
    ]

    def setUp(self) -> None:
        self.root = Path(tempfile.mkdtemp(prefix="change-list-fixture-"))
        self.addCleanup(shutil.rmtree, self.root, ignore_errors=True)

        patcher = mock.patch.object(change_list, "classify", lambda root: list(self.ROWS))
        patcher.start()
        self.addCleanup(patcher.stop)

        self.document = self.root / change_list.CHANGE_LIST
        self.assertEqual(0, self.write(), "the fixture could not be generated")
        self.assertTrue(self.document.is_file(), "--write did not write into the repository it was given")

    def write(self) -> int:
        return change_list.main(["--repository", str(self.root), "--write"])

    def check(self) -> int:
        return change_list.main(["--repository", str(self.root)])

    def test_accepts_the_document_it_just_generated(self) -> None:
        self.assertEqual(0, self.check())

    def test_rejects_a_removed_entry(self) -> None:
        lines = self.document.read_text(encoding="utf-8").splitlines(keepends=True)
        without = [line for line in lines if not line.startswith("| `kept/Touched.cs`")]
        self.assertEqual(len(lines) - 1, len(without), "the fixture row to remove was not found")
        self.document.write_text("".join(without), encoding="utf-8")

        self.assertEqual(1, self.check())

    def test_rejects_a_changed_status(self) -> None:
        text = self.document.read_text(encoding="utf-8")
        self.document.write_text(text.replace("| Modified |", "| Renamed |", 1), encoding="utf-8")

        self.assertEqual(1, self.check())

    def test_rejects_an_added_entry(self) -> None:
        text = self.document.read_text(encoding="utf-8")
        self.document.write_text(text + "| `invented/Extra.cs` | Added |  |\n", encoding="utf-8")

        self.assertEqual(1, self.check())

    def test_rejects_a_missing_document(self) -> None:
        self.document.unlink()

        self.assertEqual(1, self.check())

    def test_writes_only_below_the_repository_it_was_given(self) -> None:
        """--write is a mutating entry point, so the case that exercises it proves where it lands."""
        written = sorted(p.relative_to(self.root).as_posix() for p in self.root.rglob("*"))

        self.assertEqual([change_list.CHANGE_LIST], written)


class Leaving_the_checkout_alone(unittest.TestCase):
    """The repository invariant, measured rather than argued.

    Directive 0087 asks for this explicitly: bind the tracked file and the porcelain status, run the
    cases that damage a document - including one that deliberately fails - and prove both are
    identical afterwards. Cleanup is hygiene; this is the safety statement.
    """

    def measure(self) -> tuple[str, str]:
        document = (REPOSITORY / change_list.CHANGE_LIST).read_bytes()
        status = subprocess.run(["git", "status", "--porcelain"], cwd=REPOSITORY,
                                capture_output=True, text=True, check=True).stdout
        return hashlib.sha256(document).hexdigest(), status

    def test_the_damaging_cases_leave_the_tracked_document_untouched(self) -> None:
        before = self.measure()

        suite = unittest.TestLoader().loadTestsFromTestCase(Refusing_a_document_that_no_longer_matches)
        result = unittest.TextTestRunner(stream=io.StringIO()).run(suite)
        self.assertTrue(result.wasSuccessful(), f"the isolated cases did not pass: {result.errors + result.failures}")

        self.assertEqual(before, self.measure())

    def test_a_failing_isolated_case_leaves_the_tracked_document_untouched(self) -> None:
        before = self.measure()

        class Deliberately_failing(Refusing_a_document_that_no_longer_matches):
            def test_fails_after_damaging_its_own_fixture(inner) -> None:
                inner.document.write_text("damaged\n", encoding="utf-8")
                inner.fail("this case fails on purpose, after it has damaged its own fixture")

        suite = unittest.TestLoader().loadTestsFromName(
            "test_fails_after_damaging_its_own_fixture", Deliberately_failing)
        result = unittest.TextTestRunner(stream=io.StringIO()).run(suite)
        self.assertFalse(result.wasSuccessful(), "the deliberately failing case did not fail")

        self.assertEqual(before, self.measure())


if __name__ == "__main__":
    unittest.main()
