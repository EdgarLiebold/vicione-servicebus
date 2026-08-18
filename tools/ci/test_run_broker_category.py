#!/usr/bin/env python3
"""Focused tests for the broker category runner.

They cover the two things a runner can get wrong without anybody noticing: accepting a command line
that means two different runs at once, and reporting success after a teardown that did not happen.
Both are checked without Docker, because what is under test is the runner's decision rather than the
container engine's behaviour.
"""

from __future__ import annotations

import json
import shutil
import subprocess
import sys
import tempfile
import threading
import time
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


class Deciding_whether_a_broker_is_ready(unittest.TestCase):
    """No answer is not health.

    compose ps returning nothing means it knows of no such service. Reading that as ready reported a
    container that does not exist as a broker that answers.
    """

    def health(self, stdout: str, returncode: int = 0) -> bool:
        answer = subprocess.CompletedProcess(args=[], returncode=returncode, stdout=stdout, stderr="")

        with mock.patch.object(runner, "compose", return_value=answer):
            return runner.broker_is_healthy("activemq", {})

    def test_no_record_is_not_healthy(self) -> None:
        self.assertFalse(self.health(""))

    def test_a_running_and_healthy_container_is_healthy(self) -> None:
        self.assertTrue(self.health('{"State": "running", "Health": "healthy"}'))

    def test_a_running_container_without_a_health_check_is_healthy(self) -> None:
        self.assertTrue(self.health('{"State": "running", "Health": ""}'))

    def test_a_starting_container_is_not_healthy(self) -> None:
        self.assertFalse(self.health('{"State": "running", "Health": "starting"}'))

    def test_a_stopped_container_is_not_healthy(self) -> None:
        self.assertFalse(self.health('{"State": "exited", "Health": ""}'))

    def test_a_failed_query_is_not_healthy(self) -> None:
        self.assertFalse(self.health('{"State": "running", "Health": "healthy"}', returncode=1))


class Answering_an_outage_request(unittest.TestCase):
    """A control directory is a shared surface, so every request is checked before it is acted on."""

    def setUp(self) -> None:
        self.control = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.control, ignore_errors=True)

    def ask(self, payload: dict, name: str | None = None) -> dict:
        request_id = name or f"interrupt-{len(list(self.control.glob('*.request')))}"
        (self.control / f"{request_id}.request").write_text(json.dumps(payload), encoding="utf-8")

        stop = threading.Event()
        performed = subprocess.CompletedProcess(args=[], returncode=0, stdout="", stderr="")
        healthy = subprocess.CompletedProcess(
            args=[], returncode=0, stdout='{"State": "exited", "Health": ""}', stderr="")

        with mock.patch.object(runner, "compose", side_effect=[performed, healthy] * 40):
            thread = threading.Thread(
                target=runner.serve_outage_requests, args=("activemq", self.control, {}, stop), daemon=True)
            thread.start()

            result = self.control / f"{request_id}.result"
            for _ in range(100):
                if result.exists():
                    break
                time.sleep(0.05)

            stop.set()
            thread.join(timeout=5)

        self.assertTrue(result.exists(), "the runner never answered")

        return json.loads(result.read_text(encoding="utf-8"))

    def test_accepts_a_well_formed_request(self) -> None:
        answer = self.ask({"schemaVersion": 1, "requestId": "interrupt-0", "action": "interrupt"},
                          name="interrupt-0")

        self.assertEqual("ok", answer["status"])

    def test_refuses_a_request_of_another_schema_version(self) -> None:
        answer = self.ask({"schemaVersion": 99, "requestId": "interrupt-0", "action": "interrupt"},
                          name="interrupt-0")

        self.assertEqual("failed", answer["status"])
        self.assertIn("schema version", answer["error"])

    def test_refuses_a_request_whose_id_does_not_match_its_file(self) -> None:
        answer = self.ask({"schemaVersion": 1, "requestId": "somebody-else", "action": "interrupt"},
                          name="interrupt-0")

        self.assertEqual("failed", answer["status"])
        self.assertIn("id", answer["error"])

    def test_refuses_an_unknown_action(self) -> None:
        answer = self.ask({"schemaVersion": 1, "requestId": "interrupt-0", "action": "explode"},
                          name="interrupt-0")

        self.assertEqual("failed", answer["status"])
        self.assertEqual("explode", answer["action"], "the answer names the action this request asked for")

    def test_a_refused_request_reports_nothing_from_the_one_before_it(self) -> None:
        """The parse starts empty for every request.

        A value left over from the previous one would be reported as this request's action, which is
        how a refusal about a malformed message ends up naming an action nobody asked for.
        """
        first = self.ask({"schemaVersion": 1, "requestId": "interrupt-0", "action": "interrupt"},
                         name="interrupt-0")
        self.assertEqual("ok", first["status"])

        second = self.ask({"schemaVersion": 99, "requestId": "interrupt-1", "action": "restore"},
                          name="interrupt-1")

        self.assertEqual("failed", second["status"])
        self.assertIsNone(second["action"],
                          "the version was refused before the action was read, so none is reported")

    def test_reports_a_failed_compose_command(self) -> None:
        refused = subprocess.CompletedProcess(args=[], returncode=1, stdout="", stderr="no such service")
        (self.control / "interrupt-0.request").write_text(
            json.dumps({"schemaVersion": 1, "requestId": "interrupt-0", "action": "interrupt"}), encoding="utf-8")

        stop = threading.Event()
        with mock.patch.object(runner, "compose", return_value=refused):
            thread = threading.Thread(
                target=runner.serve_outage_requests, args=("activemq", self.control, {}, stop), daemon=True)
            thread.start()

            result = self.control / "interrupt-0.result"
            for _ in range(100):
                if result.exists():
                    break
                time.sleep(0.05)

            stop.set()
            thread.join(timeout=5)

        answer = json.loads(result.read_text(encoding="utf-8"))
        self.assertEqual("failed", answer["status"])
        self.assertIn("no such service", answer["error"])


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
