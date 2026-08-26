#!/usr/bin/env python3
"""Fail-closed tests for the verification model's workflow boundary."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from verification import model  # noqa: E402


REPO_ROOT = Path(__file__).resolve().parents[3]


class VerificationWorkflowBoundaryTests(unittest.TestCase):
    @staticmethod
    def job(*step_lines: str, job_controls: tuple[str, ...] = ()) -> str:
        controls = "".join(f"    {line}\n" for line in job_controls)
        steps = "\n".join(
            f"      {line}" if line.startswith("- ") else f"        {line}"
            for line in step_lines
        )
        return f"    name: Required\n{controls}    steps:\n{steps}\n"

    def assert_rejected(self, text: str, selection: str = "core") -> None:
        self.assertTrue(model.verification_step_findings("core-unit", text, selection))

    def test_accepts_exactly_one_canonical_unconditional_step(self) -> None:
        text = self.job("- name: Verify", "run: python3 tools/ci/verify.py --selection core")
        self.assertEqual([], model.verification_step_findings("core-unit", text, "core"))

    def test_rejects_missing_verification_step(self) -> None:
        self.assert_rejected(self.job("- name: Build", "run: dotnet build"))

    def test_rejects_wrong_selection(self) -> None:
        self.assert_rejected(
            self.job("- name: Verify", "run: python3 tools/ci/verify.py --selection quartz")
        )

    def test_rejects_duplicate_verification_steps(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core",
            "- name: Verify again",
            "run: python3 tools/ci/verify.py --selection core",
        ))

    def test_rejects_shell_composition(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core || true",
        ))

    def test_rejects_conditional_step(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "if: false",
            "run: python3 tools/ci/verify.py --selection core",
        ))

    def test_rejects_continue_on_error(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "continue-on-error: true",
            "run: python3 tools/ci/verify.py --selection core",
        ))

    def test_rejects_job_level_skip(self) -> None:
        self.assert_rejected(
            self.job(
                "- name: Verify",
                "run: python3 tools/ci/verify.py --selection core",
                job_controls=("if: false",),
            )
        )

    def test_repository_model_and_required_workflow_are_consistent(self) -> None:
        self.assertEqual([], model.findings(REPO_ROOT))


if __name__ == "__main__":
    unittest.main()
