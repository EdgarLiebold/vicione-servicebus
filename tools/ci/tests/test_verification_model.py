#!/usr/bin/env python3
"""Fail-closed tests for the verification model's workflow boundary."""

from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from verification import model  # noqa: E402


REPO_ROOT = Path(__file__).resolve().parents[3]


class VerificationWorkflowBoundaryTests(unittest.TestCase):
    CHECKOUT = "- uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1"
    SETUP = "- uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0"
    UPLOAD = "- uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1"

    @staticmethod
    def job(*step_lines: str, job_controls: tuple[str, ...] = ()) -> str:
        controls = "".join(f"    {line}\n" for line in job_controls)
        all_steps = (
            VerificationWorkflowBoundaryTests.CHECKOUT,
            VerificationWorkflowBoundaryTests.SETUP,
            "with:",
            "dotnet-version: ${{ env.DOTNET_VERSION }}",
            *step_lines,
            VerificationWorkflowBoundaryTests.UPLOAD,
            "if: always()",
            "with:",
            "name: required-core-unit",
            "path: |",
            "artifacts/verification",
            "artifacts/run-output",
        )
        steps = "\n".join(
            f"      {line}" if line.startswith("- ") else f"        {line}"
            for line in all_steps
        )
        return f"    name: Required\n{controls}    steps:\n{steps}\n"

    def assert_rejected(self, text: str, selection: str = "core") -> None:
        self.assertTrue(model.verification_step_findings("core-unit", text, selection))

    def assert_workflow_rejected(self, contents: str, message: str) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            workflow = root / ".github/workflows/build.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text(contents, encoding="utf-8")
            with self.assertRaisesRegex(model.WorkflowShapeError, message):
                model.workflow_jobs(root)

    def canonical_workflow(self, prefix: str = "", suffix: str = "") -> str:
        return (
            prefix
            + "jobs:\n"
            + "  core-unit:\n"
            + self.job("- name: Verify", "run: python3 tools/ci/verify.py --selection core")
            + suffix
        )

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

    def test_rejects_job_level_default_shell_that_swallows_failure(self) -> None:
        text = self.job("- name: Verify", "run: python3 tools/ci/verify.py --selection core")
        text = text.replace(
            "    steps:\n",
            "    defaults:\n      run:\n        shell: bash {0} || true\n    steps:\n",
        )
        self.assert_rejected(text)

    def test_rejects_an_additional_shell_step_that_can_replace_the_verifier(self) -> None:
        self.assert_rejected(self.job(
            "- name: Replace verifier",
            "run: cd tools/ci && printf 'raise SystemExit(0)\\n' > verify.py",
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core",
        ))

    def test_rejects_workflow_level_default_shell_that_swallows_every_failure(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow(
                "defaults:\n  run:\n    shell: bash {0} || true\n",
            ),
            "top-level defaults",
        )

    def test_rejects_workflow_level_defaults_after_the_jobs_section(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow(
                suffix="defaults:\n  run:\n    shell: bash {0} || true\n",
            ),
            "top-level defaults",
        )

    def test_rejects_single_quoted_top_level_keys(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow("'defaults':\n  run:\n    shell: bash {0} || true\n"),
            "supported unquoted plain form",
        )

    def test_rejects_double_quoted_top_level_keys(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow('"defaults":\n  run:\n    shell: bash {0} || true\n'),
            "supported unquoted plain form",
        )

    def test_rejects_escaped_double_quoted_top_level_keys(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow('"def\\u0061ults":\n  run:\n    shell: bash {0} || true\n'),
            "supported unquoted plain form",
        )

    def test_rejects_tagged_top_level_keys(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow("!<tag:yaml.org,2002:str> defaults:\n  run:\n    shell: bash {0} || true\n"),
            "supported unquoted plain form",
        )

    def test_rejects_complex_top_level_keys(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow("? defaults\n:\n  run:\n    shell: bash {0} || true\n"),
            "supported unquoted plain form",
        )

    def test_rejects_top_level_anchors_and_merge_keys(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow(
                "x-defaults: &workflow-defaults\n  run:\n    shell: bash {0} || true\n"
                "<<: *workflow-defaults\n",
            ),
            "unsupported top-level workflow key",
        )

    def test_rejects_a_utf8_byte_order_mark(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow("\ufeff"),
            "byte-order mark",
        )

    def test_rejects_duplicate_top_level_keys(self) -> None:
        self.assert_workflow_rejected(
            self.canonical_workflow("name: First\nname: Second\n"),
            "declared twice",
        )

    def test_accepts_every_supported_plain_top_level_key(self) -> None:
        contents = self.canonical_workflow(
            "name: Required CI\n"
            "run-name: Required CI run\n"
            "on:\n  push:\n"
            "permissions:\n  contents: read\n"
            "env:\n  DOTNET_NOLOGO: 1\n"
            "concurrency:\n  group: required-ci\n",
        )
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            workflow = root / ".github/workflows/build.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text(contents, encoding="utf-8")
            self.assertEqual(["core-unit"], sorted(model.workflow_jobs(root)))

    def test_repository_model_and_required_workflow_are_consistent(self) -> None:
        self.assertEqual([], model.findings(REPO_ROOT))


if __name__ == "__main__":
    unittest.main()
