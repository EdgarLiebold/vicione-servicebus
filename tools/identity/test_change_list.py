#!/usr/bin/env python3
"""Focused tests for the change list gate.

Deliberately small. The generator is licence evidence tooling, not product code, so what has to hold
is narrow: the document it writes must be accepted, and a document that no longer matches it must be
refused with a non zero exit. The three ways it can stop matching are covered once each.
"""

from __future__ import annotations

import io
import re
import shutil
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import change_list  # noqa: E402

REPOSITORY = Path(__file__).resolve().parents[2]


class ChangeListGateTestCase(unittest.TestCase):
    def setUp(self) -> None:
        self.document = REPOSITORY / change_list.CHANGE_LIST
        self.backup = Path(tempfile.mkdtemp()) / "CHANGELIST.md"
        shutil.copy2(self.document, self.backup)
        self.addCleanup(shutil.copy2, self.backup, self.document)

    def check(self) -> int:
        return change_list.main(["--repository", str(REPOSITORY)])

    def test_accepts_the_generated_document(self) -> None:
        self.assertEqual(0, self.check())

    def test_rejects_a_removed_entry(self) -> None:
        lines = self.document.read_text(encoding="utf-8").splitlines(keepends=True)
        self.document.write_text("".join(lines[:30] + lines[31:]), encoding="utf-8")

        self.assertEqual(1, self.check())

    def test_rejects_a_changed_status(self) -> None:
        text = self.document.read_text(encoding="utf-8")
        self.document.write_text(text.replace("| Modified |", "| Renamed |", 1), encoding="utf-8")

        self.assertEqual(1, self.check())

    def test_reports_the_number_of_rows_it_classified(self) -> None:
        """The console count is quoted in reports, so it has to be the number of entries.

        It used to be derived as the line count minus a fixed header offset, and the offset was one
        too high for the header this file actually has. Every count taken from the gate output was
        therefore one more than the document listed, which is exactly the kind of number that reaches
        a report and is believed.
        """
        captured = io.StringIO()
        with redirect_stdout(captured):
            self.assertEqual(0, self.check())

        reported = int(re.search(r"\((\d+) entries\)", captured.getvalue()).group(1))
        text = self.document.read_text(encoding="utf-8")
        totals = {status: int(value) for status, value in
                  re.findall(r"^\| (Added|Modified|Deleted|Renamed) \| (\d+) \|$", text, re.M)}

        self.assertEqual(sum(totals.values()), reported,
                         "the reported count has to be the sum of the four status totals")
        self.assertEqual(len(re.findall(r"^\| `", text, re.M)), reported,
                         "the reported count has to be the number of rows in the table")

    def test_rejects_a_missing_document(self) -> None:
        self.document.unlink()

        self.assertEqual(1, self.check())


if __name__ == "__main__":
    unittest.main()
