#!/usr/bin/env python3
"""What a test may ask of the fixture during a run, and what it is told in reply.

A recovery test needs the broker to go away and come back, and only the runner knows the ephemeral
ports and the Compose project of this run - so the test asks, over a control directory under the run
root, and this answers. The request is a file and the reply is a file, and a reply is made of the
observed state of the broker rather than of what the runner believes it did.

Standard library only.
"""

from __future__ import annotations

import json
import subprocess
import threading
import time
from pathlib import Path

from fixtures import compose_fixture

# How long a control thread is given to notice its stop request and end, before the fixture may be
# removed underneath it.
CONTROLLER_JOIN_SECONDS = 10


def serve_outage_requests(broker: str, control: Path, environment: dict[str, str],
                          stop_serving: threading.Event) -> None:
    """Answers one outage request at a time, with what it observed rather than with what it was asked.

    A request whose effect cannot be confirmed is a failed request. 'interrupt' stops the broker and
    waits until Docker reports it stopped; 'restore' starts it and waits for the fixture's own health
    check, because a container that is running is not yet a broker that answers.
    """
    while not stop_serving.is_set():
        for request in sorted(control.glob("*.request")):
            # Checked per request rather than per pass. A directory holding several requests would
            # otherwise be worked to the end after the teardown already asked this thread to stop.
            if stop_serving.is_set():
                break

            result = request.with_suffix(".result")
            if result.exists():
                continue

            # Initialised per request: a value carried over from the previous one would be reported
            # as this request's action in a failure answer.
            answer: dict[str, object]
            request_id = request.stem
            action: str | None = None
            try:
                asked = json.loads(request.read_text(encoding="utf-8"))
                if not isinstance(asked, dict):
                    raise ValueError("the request is not an object")

                version = asked.get("schemaVersion")
                if version != compose_fixture.CONTROL_SCHEMA_VERSION:
                    raise ValueError(
                        f"the request speaks schema version {version!r}, this runner speaks "
                        f"{compose_fixture.CONTROL_SCHEMA_VERSION}")

                if asked.get("requestId") != request_id:
                    raise ValueError(
                        f"the request carries id {asked.get('requestId')!r} in a file named {request_id!r}")

                action = asked.get("action")
                if action not in compose_fixture.OUTAGE_ACTIONS:
                    raise ValueError(f"unknown action {action!r}, expected one of {compose_fixture.OUTAGE_ACTIONS}")

                performed = compose_fixture.compose("stop" if action == "interrupt" else "start", broker,
                                    capture=True, environment=environment)
                if performed.returncode != 0:
                    raise compose_fixture.RunnerError(
                        f"docker compose could not {action} {broker}: "
                        f"{performed.stderr.strip() or performed.stdout.strip()}")

                deadline = time.monotonic() + compose_fixture.OUTAGE_BUDGET_SECONDS
                observed = ""
                while time.monotonic() < deadline:
                    # The readiness wait is the long one, and it is the one the teardown collides
                    # with: a thread that sleeps through its stop request keeps asking Docker about a
                    # service the teardown is already removing, and the two then race over the same
                    # compose project. Asked before every Docker call, so the last thing this thread
                    # does after the request is to answer, not to act.
                    if stop_serving.is_set():
                        raise compose_fixture.RunnerError(
                            f"the fixture is being torn down while the {action} was still being "
                            "confirmed, so this runner issues no further Docker action for it")

                    if action == "interrupt":
                        observed = compose_fixture.broker_state(broker, environment)
                        if observed in ("exited", "stopped", "absent"):
                            break
                    else:
                        if compose_fixture.broker_is_healthy(broker, environment):
                            observed = "healthy"
                            break
                        observed = compose_fixture.broker_state(broker, environment)

                    # Ends the moment the stop is requested; time.sleep did not.
                    stop_serving.wait(0.5)
                else:
                    raise TimeoutError(
                        f"the broker was still '{observed}' {compose_fixture.OUTAGE_BUDGET_SECONDS} s after {action}, so "
                        "the outage was not established")

                answer = {"schemaVersion": compose_fixture.CONTROL_SCHEMA_VERSION, "requestId": request_id,
                          "action": action, "status": "ok", "observed": observed}
            except Exception as error:  # noqa: BLE001 - the answer carries the reason
                answer = {"schemaVersion": compose_fixture.CONTROL_SCHEMA_VERSION, "requestId": request_id,
                          "action": action, "status": "failed",
                          "error": f"{type(error).__name__}: {error}"}

            compose_fixture.publish_json(result, answer)

        stop_serving.wait(0.1)


class OutageController:
    """The runner side of the outage control directory, and the only thing that touches Docker for it.

    It is an object rather than a bare thread because a thread that dies of an exception dies quietly:
    the interpreter prints a traceback into the log and the run keeps whatever exit code it had. The
    failure is kept here instead and reported as a cleanup finding, so an outage service that stopped
    answering cannot leave a green run behind it.
    """

    def __init__(self, broker: str, control: Path, environment: dict[str, str]) -> None:
        self._broker = broker
        self._control = control
        self._environment = environment
        self._stop = threading.Event()
        self._thread = threading.Thread(target=self._serve, name="outage-controller", daemon=True)
        self._started = False
        self.error: BaseException | None = None

    def start(self) -> list[str]:
        """Starts the control thread. Returns findings instead of raising, and is idempotent.

        The teardown has to be able to shut this object down whatever state it reached, including the
        state where the thread was never started at all - a Thread.join() on one of those raises, and
        that exception used to travel out of a teardown which promises not to raise and skip the
        restore, the logs and the fixture removal behind it.
        """
        if self._started:
            return []
        try:
            self._thread.start()
        except RuntimeError as error:
            self._stop.set()
            return [f"outage-controller: the control thread could not be started: {error}"]

        self._started = True

        return []

    def _serve(self) -> None:
        try:
            serve_outage_requests(self._broker, self._control, self._environment, self._stop)
        except BaseException as error:  # noqa: BLE001 - kept so the run can report it as its own
            self.error = error

    def shutdown(self) -> list[str]:
        """Stops the thread and says what it found. Never raises, in any state it can be in.

        Idempotent and safe before a start: a controller that never ran is nothing to wait for, and a
        second shutdown finds a thread that has already ended.
        """
        self._stop.set()
        findings: list[str] = []

        if self._started:
            try:
                self._thread.join(timeout=CONTROLLER_JOIN_SECONDS)
            except RuntimeError as error:
                findings.append(f"outage-controller: the control thread could not be joined: {error}")

            if self._thread.is_alive():
                findings.append(
                    f"outage-controller: the control thread did not end within {CONTROLLER_JOIN_SECONDS} s, "
                    "so it can still issue Docker actions against a fixture that is being removed")

        if self.error is not None:
            findings.append(
                f"outage-controller: the control thread ended with {type(self.error).__name__}: {self.error}")

        return findings

    @property
    def stopped(self) -> bool:
        """Whether this controller has been told to stop. Read by the teardown before it removes."""
        return self._stop.is_set()
