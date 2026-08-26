#!/usr/bin/env python3
"""Fail-closed tests for the verification model's workflow boundary."""

from __future__ import annotations

import hashlib
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from verification import model  # noqa: E402


REPO_ROOT = Path(__file__).resolve().parents[3]

EXPECTED_SUPPORT_JOB_SHA256 = {
    "legacy-tooling": "0726813a612a984604aac4753cdc3322c2e71fd11ff98477e001d9f5a07770a6",
    "build": "d6e5bda334b170811d4d1049c6913a48d58651e149adf1e710dade8f5d8ab2d6",
    "pack": "3fee9502cfe7a263ef39792da193e70a006bad835d682007d99c31affab1a315",
}


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
        return (
            "    name: Required\n"
            "    runs-on: ubuntu-24.04\n"
            "    timeout-minutes: 40\n"
            f"{controls}"
            "    steps:\n"
            f"{steps}\n"
        )

    def assert_rejected(self, text: str, selection: str = "core") -> None:
        self.assertTrue(model.verification_step_findings("core-unit", text, selection))

    def assert_workflow_rejected(self, contents: str, message: str) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            workflow = root / ".github/workflows/build.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text(contents, encoding="utf-8")
            (root / "global.json").write_text(
                '{"sdk":{"version":"10.0.302"}}\n',
                encoding="utf-8",
            )
            with self.assertRaisesRegex(model.WorkflowShapeError, message):
                model.workflow_jobs(root)

    @staticmethod
    def canonical_header() -> str:
        return (
            "name: Required CI\n"
            "on:\n"
            "  push:\n"
            "    branches:\n"
            "      - '**'\n"
            "  pull_request:\n"
            "  workflow_dispatch:\n"
            "permissions:\n"
            "  contents: read\n"
            "env:\n"
            "  DOTNET_VERSION: '10.0.302'\n"
            "  DOTNET_CLI_TELEMETRY_OPTOUT: 1\n"
            "  DOTNET_NOLOGO: 1\n"
        )

    def canonical_workflow(self, prefix: str | None = None, suffix: str = "") -> str:
        return (
            (self.canonical_header() if prefix is None else prefix)
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

    def test_accepts_the_single_supported_job_environment(self) -> None:
        text = self.job(
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core",
            job_controls=("env:", "  DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: false"),
        )
        self.assertEqual([], model.verification_step_findings("core-unit", text, "core"))

    def test_rejects_quoted_job_level_defaults(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core",
            job_controls=('"defaults":', "  run:", "    shell: bash {0} || true"),
        ))

    def test_rejects_escaped_job_level_defaults(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core",
            job_controls=('"def\\u0061ults":', "  run:", "    shell: bash {0} || true"),
        ))

    def test_rejects_quoted_job_level_condition(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core",
            job_controls=('"if": false',),
        ))

    def test_rejects_hidden_job_controls_after_steps(self) -> None:
        text = self.job("- name: Verify", "run: python3 tools/ci/verify.py --selection core")
        text += '    "def\\u0061ults":\n      run:\n        shell: bash {0} || true\n'
        self.assert_rejected(text)

    def test_rejects_duplicate_job_keys(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core",
            job_controls=("name: Duplicate",),
        ))

    def test_rejects_reordered_job_keys(self) -> None:
        text = self.job("- name: Verify", "run: python3 tools/ci/verify.py --selection core")
        text = text.replace(
            "    name: Required\n    runs-on: ubuntu-24.04\n",
            "    runs-on: ubuntu-24.04\n    name: Required\n",
        )
        self.assert_rejected(text)

    def test_rejects_an_uncontrolled_job_environment(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core",
            job_controls=("env:", "  PATH: /tmp/attacker"),
        ))

    def test_rejects_complex_job_keys(self) -> None:
        self.assert_rejected(self.job(
            "- name: Verify",
            "run: python3 tools/ci/verify.py --selection core",
            job_controls=("? defaults", ":", "  run:", "    shell: bash {0} || true"),
        ))

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

    def test_accepts_the_canonical_workflow_header(self) -> None:
        contents = self.canonical_workflow()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            workflow = root / ".github/workflows/build.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text(contents, encoding="utf-8")
            (root / "global.json").write_text(
                '{"sdk":{"version":"10.0.302"}}\n',
                encoding="utf-8",
            )
            self.assertEqual(["core-unit"], sorted(model.workflow_jobs(root)))

    def test_rejects_a_workflow_path_override(self) -> None:
        contents = self.canonical_workflow().replace(
            "  DOTNET_NOLOGO: 1\n",
            "  DOTNET_NOLOGO: 1\n  PATH: ${{ github.workspace }}/.ci-shim:/usr/bin:/bin\n",
        )
        self.assert_workflow_rejected(contents, "canonical required header")

    def test_rejects_a_workflow_pythonpath_override(self) -> None:
        contents = self.canonical_workflow().replace(
            "  DOTNET_NOLOGO: 1\n",
            "  DOTNET_NOLOGO: 1\n  PYTHONPATH: .ci-shim\n",
        )
        self.assert_workflow_rejected(contents, "canonical required header")

    def test_rejects_a_missing_push_trigger(self) -> None:
        contents = self.canonical_workflow().replace(
            "  push:\n    branches:\n      - '**'\n",
            "",
        )
        self.assert_workflow_rejected(contents, "canonical required header")

    def test_rejects_a_missing_pull_request_trigger(self) -> None:
        contents = self.canonical_workflow().replace("  pull_request:\n", "")
        self.assert_workflow_rejected(contents, "canonical required header")

    def test_rejects_a_filtered_push_trigger(self) -> None:
        contents = self.canonical_workflow().replace("      - '**'\n", "      - main\n")
        self.assert_workflow_rejected(contents, "canonical required header")

    def test_rejects_workflow_concurrency_controls(self) -> None:
        contents = self.canonical_workflow().replace(
            "jobs:\n",
            "concurrency:\n  group: required-ci\njobs:\n",
        )
        self.assert_workflow_rejected(contents, "unsupported top-level workflow key")

    def test_rejects_elevated_workflow_permissions(self) -> None:
        contents = self.canonical_workflow().replace("  contents: read\n", "  contents: write\n")
        self.assert_workflow_rejected(contents, "canonical required header")

    def test_rejects_an_sdk_version_that_differs_from_global_json(self) -> None:
        contents = self.canonical_workflow().replace("10.0.302", "10.0.999")
        self.assert_workflow_rejected(contents, "canonical required header")

    def test_sdk_binding_rejects_a_global_json_only_change(self) -> None:
        contents = self.canonical_workflow()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            workflow = root / ".github/workflows/build.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text(contents, encoding="utf-8")
            (root / "global.json").write_text(
                '{"sdk":{"version":"10.0.303"}}\n',
                encoding="utf-8",
            )
            with self.assertRaisesRegex(model.WorkflowShapeError, "canonical required header"):
                model.workflow_jobs(root)

    def test_sdk_binding_accepts_another_matching_stable_version(self) -> None:
        contents = self.canonical_workflow().replace("10.0.302", "10.0.303")
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            workflow = root / ".github/workflows/build.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text(contents, encoding="utf-8")
            (root / "global.json").write_text(
                '{"sdk":{"version":"10.0.303"}}\n',
                encoding="utf-8",
            )
            self.assertEqual(["core-unit"], sorted(model.workflow_jobs(root)))

    def test_required_support_jobs_match_the_repository_contract(self) -> None:
        jobs = model.workflow_jobs(REPO_ROOT)
        for job, expected_hash in EXPECTED_SUPPORT_JOB_SHA256.items():
            with self.subTest(job=job):
                self.assertEqual([], model.support_job_findings(job, jobs[job]))
                actual_lines = tuple(
                    line for line in jobs[job].splitlines()
                    if line.strip() and not line.lstrip().startswith("#")
                )
                actual_hash = hashlib.sha256("\n".join(actual_lines).encode()).hexdigest()
                contract_hash = hashlib.sha256(
                    "\n".join(model.REQUIRED_SUPPORT_JOB_CONTRACTS[job]).encode()
                ).hexdigest()
                self.assertEqual(expected_hash, actual_hash)
                self.assertEqual(expected_hash, contract_hash)

    def test_required_support_jobs_reject_noop_bodies(self) -> None:
        noop = (
            "    name: Required\n"
            "    runs-on: ubuntu-24.04\n"
            "    timeout-minutes: 10\n"
            "    steps:\n"
            "      - run: true\n"
        )
        for job in model.REQUIRED_SUPPORT_JOB_CONTRACTS:
            with self.subTest(job=job):
                self.assertTrue(model.support_job_findings(job, noop))

    def test_legacy_tooling_requires_every_tool_check(self) -> None:
        job = model.workflow_jobs(REPO_ROOT)["legacy-tooling"]
        commands = (
            "        run: python3 tools/ci/verification/model.py\n",
            "        run: python3 -m unittest discover -s tools/ci -p 'test_*.py'\n",
            "        run: python3 -m unittest discover -s tools/identity -p 'test_*.py'\n",
        )
        for command in commands:
            with self.subTest(command=command):
                self.assertTrue(model.support_job_findings("legacy-tooling", job.replace(command, "")))

    def test_build_requires_every_restore_and_build_in_order(self) -> None:
        job = model.workflow_jobs(REPO_ROOT)["build"]
        commands = (
            "        run: dotnet restore ViciOne.ServiceBus.slnx --locked-mode\n",
            "        run: dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore\n",
            "        run: dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode\n",
            "        run: dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore\n",
        )
        for command in commands:
            with self.subTest(command=command):
                self.assertTrue(model.support_job_findings("build", job.replace(command, "")))
        for index in range(len(commands) - 1):
            first = commands[index]
            second = commands[index + 1]
            reordered = job.replace(first, "__FIRST__\n", 1).replace(second, first, 1).replace(
                "__FIRST__\n", second, 1)
            with self.subTest(reordered=(index, index + 1)):
                self.assertTrue(model.support_job_findings("build", reordered))

    def test_pack_requires_dependencies_commands_hash_and_upload(self) -> None:
        job = model.workflow_jobs(REPO_ROOT)["pack"]
        job_map = model.load(REPO_ROOT)["jobs"]
        required_needs = (
            "legacy-tooling",
            "build",
            "core-unit",
            "activemq",
            "sql-transport",
            "benchmarks",
            "rabbitmq",
        )
        for need in required_needs:
            hostile = job.replace(f"      - {need}\n", "", 1)
            with self.subTest(need=need):
                self.assertTrue(model.support_job_findings("pack", hostile))
                self.assertTrue(model.pack_needs_findings(hostile, job_map))

        obligations = (
            f"      {model.CHECKOUT_ACTION_STEP}\n",
            f"      {model.SETUP_DOTNET_ACTION_STEP}\n",
            "          dotnet-version: ${{ env.DOTNET_VERSION }}\n",
            "        run: dotnet restore ViciOne.ServiceBus.slnx --locked-mode\n",
            "        run: dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore\n",
            "          rm -rf artifacts/packages\n",
            "          dotnet pack ViciOne.ServiceBus.slnx -c Release --no-build --no-restore -o artifacts/packages\n",
            "          set -euo pipefail\n",
            '          test -n "$(ls -A artifacts/packages)" || { echo "pack produced no package"; exit 1; }\n',
            "          sha256sum artifacts/packages/*.nupkg | tee artifacts/packages/SHA256SUMS\n",
            f"      {model.UPLOAD_ARTIFACT_ACTION_STEP}\n",
            "          name: required-packages\n",
            "          path: artifacts/packages\n",
            "          if-no-files-found: error\n",
        )
        for obligation in obligations:
            with self.subTest(obligation=obligation):
                self.assertTrue(model.support_job_findings("pack", job.replace(obligation, "")))

    def test_required_support_jobs_reject_quoted_controls(self) -> None:
        jobs = model.workflow_jobs(REPO_ROOT)
        for job in model.REQUIRED_SUPPORT_JOB_CONTRACTS:
            hostile = jobs[job].replace(
                "    steps:\n",
                '    "if": false\n    steps:\n',
            )
            with self.subTest(job=job):
                self.assertTrue(model.support_job_findings(job, hostile))

    def test_repository_model_and_required_workflow_are_consistent(self) -> None:
        self.assertEqual([], model.findings(REPO_ROOT))


if __name__ == "__main__":
    unittest.main()
