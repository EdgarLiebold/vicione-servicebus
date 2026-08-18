#!/usr/bin/env python3
"""Focused tests for the broker category runner.

They cover what a runner can get wrong without anybody noticing: accepting a command line that means
two different runs at once, and reporting success after a cleanup that did not happen. Both are
checked without Docker, because what is under test is the runner's decision rather than the container
engine's behaviour.

The verdict cases below drive main() rather than the single steps. Testing stop() alone was the gap
the Lead's counterexamples went through: stop() was correct and the decision in main() that reads its
result was not, so every step passed while the run still returned 0 after a failed cleanup.
"""

from __future__ import annotations

import contextlib
import io
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

    def test_refuses_an_outage_for_an_unstarted_broker_in_command_mode_too(self) -> None:
        """The same invariant, on the mode that used to return before reading it.

        The command branch returned first, so this call was accepted: it started the ActiveMQ relay
        with no ActiveMQ behind it and returned 0 for a fixture nothing could ever interrupt.
        """
        self.assertIn("does not start",
                      self.refuses(["--broker", "rabbitmq", "--allow-broker-outage", "activemq",
                                    "--command", "--", "true"]))


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


class Separating_the_primary_outcome_from_the_cleanup(unittest.TestCase):
    """A cleanup finding may never occupy the slot the primary failure is read from.

    One variable held both, so a cleanup that failed on an otherwise green run wrote itself into it,
    and the verdict at the end - "report this only if nothing failed before" - then found it already
    occupied and returned 0. Two deterministic false greens came out of that single confusion, and
    neither was visible from a test of the step that failed.

    Every row here drives main() with the process and the Docker boundary replaced. The child is a
    real process, so what the runner reads back is an exit code it did not invent.
    """

    def setUp(self) -> None:
        self.root = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.root, ignore_errors=True)

        # Both, and together: the runner prints its run root relative to the repository, so a run
        # root outside it is not a shape production ever has.
        for name, value in (("RAW_RUN_OUTPUT_DIR", self.root / "run-output"), ("REPO_ROOT", self.root)):
            patched = mock.patch.object(runner, name, value)
            patched.start()
            self.addCleanup(patched.stop)

        self.compose_calls: list[tuple[str, ...]] = []

    def compose(self, restore: int):
        """Stands in for Docker. Only the restore is ever asked of it here; start and stop are replaced."""
        def answer(*args: str, capture: bool = False, environment: dict | None = None):
            self.compose_calls.append(args)
            code = restore if args[:1] == ("start",) else 0
            return subprocess.CompletedProcess(args=list(args), returncode=code, stdout="", stderr="no such container")

        return answer

    def run_main(self, argv: list[str], *, restore: int = 0, stop_raises: bool = False) -> tuple[int, str]:
        def stop(environment=None):
            if stop_raises:
                raise runner.TeardownError("the fixture could not be removed: network is in use")

        errors = io.StringIO()
        with mock.patch.object(runner, "start"), \
                mock.patch.object(runner, "resolve_ports", return_value={}), \
                mock.patch.object(runner, "capture_logs"), \
                mock.patch.object(runner, "compose", side_effect=self.compose(restore)), \
                mock.patch.object(runner, "stop", side_effect=stop), \
                contextlib.redirect_stdout(io.StringIO()), \
                contextlib.redirect_stderr(errors):
            code = runner.main(argv)

        return code, errors.getvalue()

    OUTAGE = ["--broker", "activemq", "--allow-broker-outage", "activemq", "--command", "--"]

    def test_a_green_run_whose_cleanup_holds_returns_zero(self) -> None:
        code, errors = self.run_main([*self.OUTAGE, "true"])

        self.assertEqual(0, code)
        self.assertNotIn("FAIL", errors)

    def test_a_green_child_with_a_failed_restore_is_red(self) -> None:
        code, errors = self.run_main([*self.OUTAGE, "true"], restore=1)

        self.assertNotEqual(0, code, "the broker was left stopped and the run still reported success")
        self.assertIn("broker-restore", errors)

    def test_a_green_child_with_a_control_thread_that_does_not_end_is_red(self) -> None:
        released = threading.Event()
        self.addCleanup(released.set)

        def deaf(broker, control, environment, stop_serving) -> None:
            released.wait(30)

        with mock.patch.object(runner, "serve_outage_requests", deaf), \
                mock.patch.object(runner, "CONTROLLER_JOIN_SECONDS", 0.2):
            code, errors = self.run_main([*self.OUTAGE, "true"])

        self.assertNotEqual(0, code, "a control thread that never ended left the run green")
        self.assertIn("outage-controller", errors)
        self.assertIn("did not end", errors)

    def test_a_green_child_with_a_failed_down_is_red(self) -> None:
        code, errors = self.run_main([*self.OUTAGE, "true"], stop_raises=True)

        self.assertNotEqual(0, code, "the fixture was left standing and the run still reported success")
        self.assertIn("broker-teardown", errors)

    def test_an_exception_escaping_the_control_thread_is_red(self) -> None:
        """A thread that dies of an exception dies quietly: the interpreter prints it and the run
        keeps its exit code. The failure is carried out of the thread so the run can be red for it."""
        def explode(broker, control, environment, stop_serving) -> None:
            raise RuntimeError("the control directory vanished under the thread")

        with mock.patch.object(runner, "serve_outage_requests", explode):
            code, errors = self.run_main([*self.OUTAGE, "true"])

        self.assertNotEqual(0, code, "the control thread died of an exception and the run stayed green")
        self.assertIn("the control directory vanished under the thread", errors)

    def test_a_failing_child_keeps_its_own_exit_code_and_the_cleanup_is_still_reported(self) -> None:
        """The primary diagnostic stays primary. The cleanup finding is printed all the same, because
        a fixture left in a bad state is what the next run meets."""
        code, errors = self.run_main([*self.OUTAGE, "sh", "-c", "exit 3"], restore=1, stop_raises=True)

        self.assertEqual(3, code, "the child's own exit code is the verdict a reader has to see")
        self.assertIn("broker-restore", errors)
        self.assertIn("broker-teardown", errors)

    def test_a_failing_child_alone_keeps_its_exit_code(self) -> None:
        code, errors = self.run_main([*self.OUTAGE, "sh", "-c", "exit 3"])

        self.assertEqual(3, code)
        self.assertNotIn("FAIL", errors)

    def test_the_broker_is_restored_before_the_fixture_is_removed(self) -> None:
        """Order, not only presence: a restore issued after 'down -v' addresses a container that is
        already gone."""
        self.run_main([*self.OUTAGE, "true"])

        self.assertIn(("start", "activemq"), [tuple(call[:2]) for call in self.compose_calls])


class Ending_the_control_thread_while_it_waits(unittest.TestCase):
    """The readiness wait is the long one, and it is the one the teardown collides with."""

    def test_a_stop_request_ends_the_readiness_wait_instead_of_being_slept_through(self) -> None:
        control = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, control, ignore_errors=True)

        (control / "restore-0.request").write_text(
            json.dumps({"schemaVersion": 1, "requestId": "restore-0", "action": "restore"}), encoding="utf-8")

        started = threading.Event()

        def never_healthy(*args: str, capture: bool = False, environment: dict | None = None):
            started.set()
            return subprocess.CompletedProcess(args=list(args), returncode=0, stdout="", stderr="")

        stop = threading.Event()
        with mock.patch.object(runner, "compose", side_effect=never_healthy), \
                mock.patch.object(runner, "broker_is_healthy", return_value=False), \
                mock.patch.object(runner, "broker_state", return_value="starting"):
            thread = threading.Thread(
                target=runner.serve_outage_requests, args=("activemq", control, {}, stop), daemon=True)
            thread.start()

            self.assertTrue(started.wait(5), "the controller never started working on the request")
            stop.set()
            thread.join(timeout=5)

        self.assertFalse(thread.is_alive(),
                         "the controller slept through its stop request and kept asking Docker about a "
                         "service the teardown was already removing")

        answer = json.loads((control / "restore-0.result").read_text(encoding="utf-8"))
        self.assertEqual("failed", answer["status"])
        self.assertIn("torn down", answer["error"])


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
