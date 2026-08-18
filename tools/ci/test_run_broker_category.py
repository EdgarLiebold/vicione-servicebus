#!/usr/bin/env python3
"""Focused tests for the broker category runner.

They cover the two things a runner can get wrong without anybody noticing: accepting a command line
that means two different runs at once, and reporting success after a teardown that did not happen.
Both are checked without Docker, because what is under test is the runner's decision rather than the
container engine's behaviour.
"""

from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parent))

import run_broker_category as runner  # noqa: E402


class Refusing_a_contradictory_command_line(unittest.TestCase):
    """Two modes exist and they do not overlap, so every mixture is refused with a reason."""

    def refuses(self, argv: list[str]) -> str:
        with self.assertRaises(SystemExit) as raised, mock.patch("sys.stderr") as stderr:
            runner.main(argv)

        self.assertEqual(2, raised.exception.code)

        return "".join(str(call.args[0]) for call in stderr.write.call_args_list)

    def test_refuses_a_command_run_that_also_names_a_category(self) -> None:
        self.assertIn("--category", self.refuses(["--broker", "rabbitmq", "--category", "x", "--command", "--", "true"]))

    def test_refuses_a_command_run_that_also_names_a_project(self) -> None:
        self.assertIn("--project", self.refuses(["--broker", "rabbitmq", "--project", "x", "--command", "--", "true"]))

    def test_refuses_a_command_run_without_a_command(self) -> None:
        self.assertIn("--command", self.refuses(["--broker", "rabbitmq", "--command"]))

    def test_refuses_a_category_run_without_a_project(self) -> None:
        self.assertIn("either", self.refuses(["--broker", "rabbitmq", "--category", "x"]))

    def test_refuses_trailing_arguments_without_the_separator(self) -> None:
        self.assertIn("without it",
                      self.refuses(["--broker", "rabbitmq", "--category", "x", "--project", "y", "extra"]))

    def test_refuses_an_outage_for_a_broker_the_run_does_not_start(self) -> None:
        self.assertIn("does not start",
                      self.refuses(["--broker", "rabbitmq", "--category", "x", "--project", "y",
                                    "--allow-broker-outage", "activemq"]))


class Reporting_a_teardown_that_did_not_happen(unittest.TestCase):
    """A fixture that could not be removed is not a green run.

    The next run would meet a database that still holds an earlier secret, or a broker that is up and
    answers nothing, and the failure would surface somewhere else entirely.
    """

    def test_a_failed_down_raises_instead_of_being_ignored(self) -> None:
        refused = subprocess.CompletedProcess(args=[], returncode=1, stdout="", stderr="network is in use")

        with mock.patch.object(runner, "compose", return_value=refused):
            with self.assertRaises(runner.TeardownError) as raised:
                runner.stop({})

        self.assertIn("network is in use", str(raised.exception))

    def test_a_successful_down_says_nothing(self) -> None:
        removed = subprocess.CompletedProcess(args=[], returncode=0, stdout="", stderr="")

        with mock.patch.object(runner, "compose", return_value=removed):
            runner.stop({})


class Naming_the_project_of_this_run(unittest.TestCase):
    """Every docker command carries the identity of its own run, or two runs share one fixture."""

    def test_every_compose_call_names_the_project_from_the_environment(self) -> None:
        with mock.patch.object(runner.subprocess, "run") as run:
            runner.compose("ps", environment={runner.PROJECT_VARIABLE: "vicione-abc123"})

        command = run.call_args.args[0]
        self.assertIn("-p", command)
        self.assertEqual("vicione-abc123", command[command.index("-p") + 1])

    def test_a_call_without_an_identity_does_not_invent_one(self) -> None:
        with mock.patch.object(runner.subprocess, "run") as run:
            runner.compose("ps", environment={})

        self.assertNotIn("-p", run.call_args.args[0])


if __name__ == "__main__":
    unittest.main()
