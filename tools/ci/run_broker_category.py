#!/usr/bin/env python3
"""Canonical entry point for a broker-backed test category.

One run path for a developer machine and for GitHub Actions. It starts exactly the broker the
category needs from the single pinned compose definition, asks Docker which loopback ports were
actually bound, hands endpoints and the run-scoped account to the test process alone, and tears the
fixture down through a trap so a failure never leaves a container behind.

Why ports are not chosen up front: probing for a free port and binding it later is a race. Compose
publishes "127.0.0.1::5672", Docker allocates atomically, and this runner reads the result. That is
also what makes the run survive a developer machine where 5672 is already taken by something else.

Secrets are generated per run, passed only through the child environment, and never printed.

Standard library only.
"""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import os
import re
import secrets
import subprocess
import sys
import threading
import time
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]

sys.path.insert(0, str(Path(__file__).resolve().parent))
from verification import run_scope  # noqa: E402  (repository local, resolved from this file's folder)

# A collected broker log is raw run output, not repository structure: it is large, it repeats between
# runs and it is gone the moment the compose project is torn down anyway. It goes where the TRX goes,
# under artifacts/, which .gitignore covers; tools/ci/policy_validator.py rejects one under evidence/.
RAW_RUN_OUTPUT_DIR = REPO_ROOT / "artifacts" / "run-output"


def broker_log_path(broker: str, environment: dict[str, str]) -> Path:
    """Under this run's own root. A shared file name means a second run overwrites the log the first
    one's verdict was read from."""
    root = environment.get(RUN_ROOT_VARIABLE)

    return (Path(root) if root else RAW_RUN_OUTPUT_DIR) / f"{broker}-broker.log"


COMPOSE_FILE = REPO_ROOT / "build/test-infrastructure/compose.yaml"

# What separates one run from another. The compose project name decides which containers, networks and
# volumes a command addresses, and the run root decides where output and control files land. Both are
# derived from one identity so that nothing this run writes can collide with, or be removed by, another
# run on the same machine.
PROJECT_VARIABLE = "VICIONE_SERVICEBUS_COMPOSE_PROJECT"

RUN_ROOT_VARIABLE = run_scope.RUN_ROOT_VARIABLE

# Container ports each broker exposes, mapped onto the environment variable the tests read.
BROKER_PORTS = {
    "rabbitmq": {
        5672: "VICIONE_SERVICEBUS_RMQ_PORT",
        15672: "VICIONE_SERVICEBUS_RMQ_MGMT_PORT",
    },
    "activemq": {
        61616: "VICIONE_SERVICEBUS_AMQ_OPENWIRE_PORT",
        5672: "VICIONE_SERVICEBUS_AMQ_AMQP_PORT",
        8161: "VICIONE_SERVICEBUS_AMQ_JOLOKIA_PORT",
    },
    # Artemis is a separate broker behind the 'artemis' flavor of a few ActiveMQ specs. It is not a
    # required gate; it is listed here so the branch has a run-scoped endpoint instead of a fixed port.
    "artemis": {
        61616: "VICIONE_SERVICEBUS_ARTEMIS_OPENWIRE_PORT",
        8161: "VICIONE_SERVICEBUS_ARTEMIS_JOLOKIA_PORT",
    },
    # Not brokers, but the Entity Framework specs need them and they obey the same rules.
    "mssql": {1433: "VICIONE_SERVICEBUS_MSSQL_PORT"},
    "postgres": {5432: "VICIONE_SERVICEBUS_PG_PORT"},
}

BROKER_HOST_VARIABLE = {
    "rabbitmq": "VICIONE_SERVICEBUS_RMQ_HOST",
    "activemq": "VICIONE_SERVICEBUS_AMQ_HOST",
    "artemis": "VICIONE_SERVICEBUS_ARTEMIS_HOST",
    "mssql": "VICIONE_SERVICEBUS_MSSQL_HOST",
    "postgres": "VICIONE_SERVICEBUS_PG_HOST",
}

BROKER_CREDENTIAL_VARIABLES = {
    "rabbitmq": ("VICIONE_SERVICEBUS_RMQ_USER", "VICIONE_SERVICEBUS_RMQ_PASS"),
    "activemq": ("VICIONE_SERVICEBUS_AMQ_USER", "VICIONE_SERVICEBUS_AMQ_PASS"),
    "artemis": ("VICIONE_SERVICEBUS_ARTEMIS_USER", "VICIONE_SERVICEBUS_ARTEMIS_PASS"),
    "postgres": ("VICIONE_SERVICEBUS_PG_USER", "VICIONE_SERVICEBUS_PG_PASS"),
}

# SQL Server cannot rename 'sa', so only the secret is run-scoped. It must also satisfy the engine's
# complexity rules, which a plain hex secret does not. The account name is published all the same: the
# fixtures read every part of an endpoint from the runner contract and hold no default of their own.
MSSQL_USER_VARIABLE = "VICIONE_SERVICEBUS_MSSQL_USER"
MSSQL_PASSWORD_VARIABLE = "VICIONE_SERVICEBUS_MSSQL_PASS"
MSSQL_ACCOUNT_NAME = "sa"

# The account name is fixed because ActiveMQ authorises its web console by role and that binding
# lives in a config file. A name alone grants nothing; the secret below is new on every run.
ACCOUNT_NAME = "vicione_ci"


class RunnerError(RuntimeError):
    pass


class TeardownError(RuntimeError):
    """Raised when the fixture could not be removed. Never replaces the failure that came first."""


def compose(*args: str, capture: bool = False, environment: dict[str, str] | None = None) -> subprocess.CompletedProcess:
    """Every call names the compose project of this run.

    Without it two runs share one project: the second one's 'down -v' removes the first one's
    containers, and both write the same volumes. The name comes from the environment so that the
    teardown in the finally block addresses exactly the project the start created, even when the
    start itself failed.
    """
    project = (environment or os.environ).get(PROJECT_VARIABLE)
    identity = ["-p", project] if project else []

    command = ["docker", "compose", "-f", str(COMPOSE_FILE), *identity, *args]
    return subprocess.run(command, text=True, capture_output=capture, check=False, env=environment)


def start(brokers: list[str], environment: dict[str, str]) -> None:
    """Brings the fixture up, and refuses to run against one it could not clean first.

    Clean slate before starting, not only afterwards. A database image applies its credentials only
    when it initialises an empty data directory, so a volume left behind by an earlier run keeps the
    old secret and the run fails authentication against its own fixture.

    The result of that pre-clean used to be discarded. A failed one followed by a successful 'up'
    then read as a healthy start, and the tests ran against a fixture still holding another run's
    state - a database with an earlier secret, or a broker with an earlier queue. It is a startup
    failure now, and no test command executes after it.
    """
    cleaned = compose("down", "-v", "--remove-orphans", capture=True, environment=environment)
    if cleaned.returncode != 0:
        raise RunnerError(
            "the fixture of an earlier run could not be removed before this one started: "
            f"{cleaned.stderr.strip() or cleaned.stdout.strip()}")

    result = compose("up", "-d", "--wait", *brokers, capture=True, environment=environment)
    if result.returncode != 0:
        raise RunnerError(f"the {', '.join(brokers)} fixture did not become ready: {result.stderr.strip()}")


def stop(environment: dict[str, str] | None = None) -> None:
    """Removes this run's fixture, and says so when it could not.

    A teardown that ignores the exit code leaves containers and volumes behind while the run reports
    success. The next run then meets a database that still holds an earlier secret, or a broker that
    is up and answers nothing, and the failure surfaces somewhere else entirely.
    """
    result = compose("down", "-v", "--remove-orphans", capture=True, environment=environment)
    if result.returncode != 0:
        raise TeardownError(f"the fixture could not be removed: {result.stderr.strip() or result.stdout.strip()}")


# The fixture boundary a test asks for an outage through. The test writes a request file and waits for
# a result file; this runner is the only thing that ever touches Docker.
#
# The broker is really stopped and really started again. What keeps the client's address stable across
# that is the proxy in front of it, not the broker: compose publishes an ephemeral loopback port per
# container start, so a restarted broker comes back on a different port every time - measured twice,
# 32955 to 32958 here and 33002 to 33005 by the Lead. The proxy is never restarted, so the three
# addresses the test process was given stay valid, and what changes behind them is a broker that was
# genuinely gone.
OUTAGE_CONTROL_VARIABLE = "VICIONE_SERVICEBUS_FIXTURE_CONTROL"

OUTAGE_ACTIONS = ("interrupt", "restore")

# The relay in front of a broker, for runs that take that broker away. Only a broker listed here can be
# interrupted, because only it has an address that survives its own restart.
OUTAGE_PROXY = {"activemq": "activemq-proxy"}

# How long the runner waits for a broker to reach the state it was asked for.
OUTAGE_BUDGET_SECONDS = 120

# The exchange between the child and this runner. Both sides write into a temporary file beside the
# target and publish it with an atomic rename on the same filesystem, so no reader can ever meet a
# half written JSON document.
CONTROL_SCHEMA_VERSION = 1


# What the fixture side of a run leaves behind for its caller, beside the raw output it already writes.
FIXTURE_FINDINGS_FILE = "fixture-findings.json"


def digest_of(path: Path) -> str | None:
    """The sha256 of a collected broker log, or None when there is none to hash."""
    if not path.is_file():
        return None

    return hashlib.sha256(path.read_bytes()).hexdigest()


def publish_json(target: Path, payload: dict[str, object]) -> None:
    temporary = target.with_name(target.name + ".partial")
    temporary.write_text(json.dumps(payload, sort_keys=True) + "\n", encoding="utf-8")
    temporary.replace(target)


def broker_state(broker: str, environment: dict[str, str]) -> str:
    """What Docker says about the container, which is the only honest answer about the broker.

    Not a TCP handshake: the proxy in front of the broker accepts connections whether or not anything
    is behind it, so a successful connect would report the proxy's health and call it the broker's.
    """
    result = compose("ps", "--format", "json", broker, capture=True, environment=environment)
    if result.returncode != 0:
        raise RunnerError(f"docker compose ps failed for {broker}: {result.stderr.strip()}")

    states = []
    for line in result.stdout.splitlines():
        line = line.strip()
        if not line:
            continue
        entry = json.loads(line)
        for record in entry if isinstance(entry, list) else [entry]:
            states.append(str(record.get("State", "")).casefold())

    if not states:
        return "absent"

    return states[0]


def broker_is_healthy(broker: str, environment: dict[str, str]) -> bool:
    """Running is not ready, and no answer is not health.

    A broker that has just been started accepts nothing for a while, and the compose health check is
    the fixture's own definition of when it does. Zero records means compose knows nothing about that
    service, which is the opposite of healthy - answering true there would have reported a container
    that does not exist as ready.
    """
    result = compose("ps", "--format", "json", broker, capture=True, environment=environment)
    if result.returncode != 0:
        return False

    records = []
    for line in result.stdout.splitlines():
        line = line.strip()
        if not line:
            continue
        entry = json.loads(line)
        records.extend(entry if isinstance(entry, list) else [entry])

    if not records:
        return False

    for record in records:
        health = str(record.get("Health", "")).casefold()
        state = str(record.get("State", "")).casefold()
        if state != "running":
            return False
        # A service without a health check reports an empty string; running is then all there is.
        if health not in ("", "healthy"):
            return False

    return True


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
                if version != CONTROL_SCHEMA_VERSION:
                    raise ValueError(
                        f"the request speaks schema version {version!r}, this runner speaks "
                        f"{CONTROL_SCHEMA_VERSION}")

                if asked.get("requestId") != request_id:
                    raise ValueError(
                        f"the request carries id {asked.get('requestId')!r} in a file named {request_id!r}")

                action = asked.get("action")
                if action not in OUTAGE_ACTIONS:
                    raise ValueError(f"unknown action {action!r}, expected one of {OUTAGE_ACTIONS}")

                performed = compose("stop" if action == "interrupt" else "start", broker,
                                    capture=True, environment=environment)
                if performed.returncode != 0:
                    raise RunnerError(
                        f"docker compose could not {action} {broker}: "
                        f"{performed.stderr.strip() or performed.stdout.strip()}")

                deadline = time.monotonic() + OUTAGE_BUDGET_SECONDS
                observed = ""
                while time.monotonic() < deadline:
                    # The readiness wait is the long one, and it is the one the teardown collides
                    # with: a thread that sleeps through its stop request keeps asking Docker about a
                    # service the teardown is already removing, and the two then race over the same
                    # compose project. Asked before every Docker call, so the last thing this thread
                    # does after the request is to answer, not to act.
                    if stop_serving.is_set():
                        raise RunnerError(
                            f"the fixture is being torn down while the {action} was still being "
                            "confirmed, so this runner issues no further Docker action for it")

                    if action == "interrupt":
                        observed = broker_state(broker, environment)
                        if observed in ("exited", "stopped", "absent"):
                            break
                    else:
                        if broker_is_healthy(broker, environment):
                            observed = "healthy"
                            break
                        observed = broker_state(broker, environment)

                    # Ends the moment the stop is requested; time.sleep did not.
                    stop_serving.wait(0.5)
                else:
                    raise TimeoutError(
                        f"the broker was still '{observed}' {OUTAGE_BUDGET_SECONDS} s after {action}, so "
                        "the outage was not established")

                answer = {"schemaVersion": CONTROL_SCHEMA_VERSION, "requestId": request_id,
                          "action": action, "status": "ok", "observed": observed}
            except Exception as error:  # noqa: BLE001 - the answer carries the reason
                answer = {"schemaVersion": CONTROL_SCHEMA_VERSION, "requestId": request_id,
                          "action": action, "status": "failed",
                          "error": f"{type(error).__name__}: {error}"}

            publish_json(result, answer)

        stop_serving.wait(0.1)


def capture_logs(brokers: list[str], environment: dict[str, str]) -> None:
    """Write each broker's own log next to the test results, before the fixture is torn down.

    A broker states things no test process can observe about itself: that it took a delivery back
    because the acknowledgement timed out, that it refused an exclusive queue, which channel it closed
    and why. Once 'down -v' has run, that record is gone for good, so it is collected here rather than
    reconstructed from assertions afterwards. Failure to collect it does not fail the run -- the log is
    evidence about a run that has already produced its verdict.
    """
    for broker in brokers:
        result = compose("logs", "--no-color", "--timestamps", broker, capture=True, environment=environment)
        if result.returncode != 0:
            print(f"WARN broker-log {broker}: not collected ({result.stderr.strip()})", file=sys.stderr)
            continue
        target = broker_log_path(broker, environment)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(result.stdout, encoding="utf-8")
        print(f"broker log {broker}: {target} ({len(result.stdout.splitlines())} lines)")


# A vhost the broker created, and a channel exception it answered with, as the broker itself writes
# them. Both are matched loosely on purpose: the surrounding wording differs between RabbitMQ versions,
# the quoted vhost name does not.
VHOST_CREATED = re.compile(r"Adding vhost '([^']+)'")
RESOURCE_LOCKED = re.compile(r"resource_locked.*?vhost '([^']+)'")


def assert_one_refusal_per_vhost(log_path: Path, pattern: str) -> bool:
    """Fail the run when a virtual host saw anything other than exactly one exclusivity refusal.

    Reply code 405 is permanent: asking a second time cannot change the answer, so a second refusal in
    the same virtual host is a retry loop that should not exist, and no refusal at all is a spec whose
    precondition never came about. Both were previously invisible to the suite -- the specs counted
    endpoint faults and watched a quiet window, which says nothing about how often the broker was
    actually asked. Counted here from the broker's own log, freshly collected and never reused, so the
    rule fails the run by itself instead of depending on someone reading the log afterwards.
    """
    if not log_path.exists():
        print(f"FAIL one-refusal: {log_path} was not collected, so the rule could not be checked", file=sys.stderr)
        return False

    log = log_path.read_text(encoding="utf-8", errors="replace")

    expected = {name for name in VHOST_CREATED.findall(log) if fnmatch.fnmatch(name, pattern)}

    counted: dict[str, int] = {name: 0 for name in expected}
    for name in RESOURCE_LOCKED.findall(log):
        if fnmatch.fnmatch(name, pattern):
            counted[name] = counted.get(name, 0) + 1

    if not counted:
        print(f"FAIL one-refusal: no virtual host matching '{pattern}' appears in {log_path.name}, "
              "so the rule matched nothing and would pass vacuously", file=sys.stderr)
        return False

    if not expected:
        print(f"WARN one-refusal: no vhost creation matching '{pattern}' was found in {log_path.name}; "
              "a virtual host that saw no refusal at all cannot be detected in this run", file=sys.stderr)

    wrong = {name: count for name, count in sorted(counted.items()) if count != 1}
    if wrong:
        for name, count in wrong.items():
            reason = "no refusal, so the conflict never happened" if count == 0 else f"{count} refusals, so it was retried"
            print(f"FAIL one-refusal {name}: {reason}", file=sys.stderr)
        return False

    print(f"one-refusal: {len(counted)} virtual host(s) matching '{pattern}', exactly one refusal each")
    return True


def resolve_ports(broker: str, environment: dict[str, str], service: str | None = None) -> dict[str, str]:
    """Ask Docker for the ports it actually bound, and refuse anything outside loopback.

    The variables always belong to the broker; the service they are read from may be its relay. In a
    recovery run that is the difference between an address the client can keep and one that changes
    every time the broker restarts.
    """
    source = service or broker
    resolved: dict[str, str] = {}
    for container_port, variable in BROKER_PORTS[broker].items():
        result = compose("port", source, str(container_port), capture=True, environment=environment)
        binding = result.stdout.strip()
        if result.returncode != 0 or not binding:
            raise RunnerError(f"container port {container_port} of {source} is not published")

        host, _, port = binding.rpartition(":")
        if host not in ("127.0.0.1", "[::1]"):
            raise RunnerError(
                f"container port {container_port} of {source} is published on '{host}' instead of loopback"
            )
        resolved[variable] = port
    return resolved


def build_environment() -> dict[str, str]:
    """Generate a fresh account for every broker in the compose file, not only the started one.

    Compose interpolates the whole file even when a single service is started, so a variable of the
    idle broker would abort the run. Generating a real secret for it as well avoids introducing a
    placeholder with a known value; the idle service is never started, so the secret is never used.
    """
    environment: dict[str, str] = {}
    for user_variable, pass_variable in BROKER_CREDENTIAL_VARIABLES.values():
        environment[user_variable] = ACCOUNT_NAME
        # 32 hex characters from the OS CSPRNG, new on every run and never written to disk.
        environment[pass_variable] = secrets.token_hex(16)
    environment[MSSQL_USER_VARIABLE] = MSSQL_ACCOUNT_NAME
    environment[MSSQL_PASSWORD_VARIABLE] = secrets.token_hex(16) + "Aa1!"
    return environment


def validate(parser: argparse.ArgumentParser, args: argparse.Namespace) -> None:
    """Closes the command line instead of checking two of its combinations.

    Two modes exist and they do not overlap. A category run names a category and a project; a command
    run names the command after --. Anything that mixes them was written by somebody who expected one
    of the two to happen, and guessing which one is worse than refusing.
    """
    # Before the modes are separated, because it is true of both of them. Behind the mode split this
    # rule was unreachable for a command run: --broker rabbitmq --allow-broker-outage activemq
    # --command -- true was accepted, started the ActiveMQ relay without ActiveMQ behind it, and
    # returned 0 for a fixture nobody could interrupt.
    if args.allow_broker_outage and args.allow_broker_outage not in args.broker:
        parser.error(f"--allow-broker-outage names '{args.allow_broker_outage}', which this run does not start")

    if args.command:
        if not args.rest:
            parser.error("--command needs the command itself after --")
        for option, value in (("--category", args.category), ("--project", args.project),
                              ("--one-refusal-per-vhost", args.one_refusal_per_vhost)):
            if value:
                parser.error(f"--command runs a command, so {option} has no meaning in the same call")
        return

    if not args.category or not args.project:
        parser.error("either --category with --project, or --command with the command after --")
    if args.rest:
        parser.error("a category run forwards extra arguments after --, and these arrived without it: "
                     + " ".join(args.rest))


def build_parser() -> argparse.ArgumentParser:
    """The command line of this runner, as an object.

    Separate from main so that tools/ci/policy_validator.py can reconstruct the argument vector a
    workflow step really produces and parse it against this parser. Reading the workflow for expected
    substrings proved nothing about the command the shell would build from them.
    """
    parser = argparse.ArgumentParser(description=__doc__)
    # A suite may legitimately span more than one broker: the ActiveMQ specs are parameterized over an
    # 'artemis' flavor that addresses a second, separate broker. Repeating --broker starts each of them,
    # so no spec has to fall back to a fixed port because its fixture was not started.
    parser.add_argument("--broker", required=True, action="append", choices=sorted(BROKER_PORTS))
    parser.add_argument("--category")
    parser.add_argument("--project")
    parser.add_argument("--evidence-dir", type=Path, default=Path("artifacts/run-output"))
    # --ports-out is gone. The projection is written under the run root, where it cannot be the file
    # another invocation overwrites, and its path is printed so a caller can find it.
    parser.add_argument(
        "--one-refusal-per-vhost",
        metavar="GLOB",
        help="Fail the run unless every virtual host matching GLOB saw exactly one exclusivity refusal, "
             "counted from the broker's own freshly collected log.",
    )
    parser.add_argument(
        "--allow-broker-outage",
        metavar="BROKER",
        help="Let the child ask this runner to interrupt and restore that broker, through a control "
             "directory handed to it in the environment. The child never touches Docker itself.",
    )
    parser.add_argument(
        "--command",
        action="store_true",
        help="Treat everything after -- as a command to run inside the fixture environment instead of a "
             "test category. The fixture is started, its endpoints and run-scoped account are handed to "
             "the child alone, and it is torn down afterwards exactly as for a category.",
    )
    parser.add_argument("rest", nargs="*")

    return parser


# How long the control thread is given to notice its stop request and end.
CONTROLLER_JOIN_SECONDS = 10


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


class RunState:
    """What the teardown has to know about a run that may have failed anywhere inside it."""

    def __init__(self) -> None:
        self.controller: OutageController | None = None
        self.captured: set[str] = set()


def execute(args: argparse.Namespace, brokers: list[str], environment: dict[str, str], run_root: Path,
            state: RunState) -> int:
    """Starts the fixture, runs the child, and returns the child's exit code.

    Everything in here is the primary outcome of the run. Nothing in here tears anything down: that is
    the teardown's work, and keeping the two apart is what stops a cleanup finding from occupying the
    slot the primary failure is read from.
    """
    proxy = OUTAGE_PROXY.get(args.allow_broker_outage or "")
    start(brokers + ([proxy] if proxy else []), environment)

    endpoints: dict[str, str] = {}
    for broker in brokers:
        # In a recovery run the addresses of the fronted broker come from its relay. That is the
        # whole point: the client keeps one address while the broker behind it restarts.
        source = proxy if proxy and broker == args.allow_broker_outage else broker
        endpoints.update(resolve_ports(broker, environment, service=source))
        endpoints[BROKER_HOST_VARIABLE[broker]] = "127.0.0.1"
    environment.update(endpoints)

    if proxy:
        print(f"{args.allow_broker_outage} is reached through {proxy}, so its address survives a restart")

    print(f"fixture {' and '.join(brokers)} ready on loopback: "
          + ", ".join(f"{name}={value}" for name, value in sorted(endpoints.items())))

    if args.allow_broker_outage:
        # Under this run's own root, so it is never the path another run deletes.
        control = run_root / "fixture-control"
        control.mkdir(parents=True)
        environment[OUTAGE_CONTROL_VARIABLE] = str(control)

        controller = OutageController(args.allow_broker_outage, control, dict(environment))
        problems = controller.start()
        # Published to the teardown either way, because a controller that failed to start still has to
        # be shut down and still has findings to report. What it must not do is reach that state as a
        # half-built object, so it is constructed, started and only then handed over.
        state.controller = controller
        if problems:
            raise RunnerError("; ".join(problems))
        print(f"outage control ready for {args.allow_broker_outage} at {control}")

    # Always under this run's own root. A caller supplied path was a file two runs of one
    # category wrote in turn, and the second one's endpoints were read as the first one's.
    projection = run_root / "endpoints.json"
    projection.write_text(json.dumps(endpoints, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"endpoints projected to {projection.relative_to(REPO_ROOT)}")

    if args.command:
        # A deliberately started scenario rather than a category: the fixture boundary is the same,
        # the child gets the same endpoints and the same run-scoped account, and it still cannot
        # reach a broker this runner did not start.
        completed = subprocess.run(args.rest, env=environment, text=True, check=False)
    else:
        runner = REPO_ROOT / "tools/ci/run_test_category.py"
        completed = subprocess.run(
            [sys.executable, str(runner),
             "--category", args.category,
             "--project", args.project,
             "--evidence-dir", str(args.evidence_dir)],
            env=environment, text=True, check=False,
        )

    if args.one_refusal_per_vhost:
        # Before the teardown, and on the log this run produced. The check runs even when the tests
        # already failed: a retry loop is worth naming either way, and it must never be the reason a
        # red run looks green.
        capture_logs(brokers, environment)
        state.captured.update(brokers)

        for broker in brokers:
            if not assert_one_refusal_per_vhost(broker_log_path(broker, environment), args.one_refusal_per_vhost):
                return completed.returncode or 1

    return completed.returncode


def guarded(stage: str, findings: list[str], action) -> None:
    """Runs one cleanup stage and turns anything it raises into a finding of that stage.

    Every stage is guarded on its own, so one that fails cannot take the ones behind it with it. That
    is what happened when the controller shutdown raised out of a teardown that promised not to: the
    restore, the broker logs and the Compose removal after it were all skipped, and the run reported
    the exception rather than the fixture it had left standing.
    """
    try:
        action()
    except Exception as error:  # noqa: BLE001 - the stage is named with it
        findings.append(f"{stage}: {type(error).__name__}: {error}")


def teardown(args: argparse.Namespace, brokers: list[str], environment: dict[str, str],
             state: RunState) -> list[str]:
    """Removes the fixture and returns every cleanup finding. Never raises.

    A finding is not the primary outcome and may not be written into the same variable. That is
    exactly what made two deterministic false greens: a failed restore and a control thread that was
    still alive each printed FAIL, each filled the slot the primary failure was read from, and the
    verdict at the end then found that slot occupied and returned 0.

    Every stage is attempted, whatever the ones before it did, and each is guarded on its own.
    """
    findings: list[str] = []

    # The control thread first, and its own shutdown decides whether anything after this may touch
    # Docker at all. A thread still answering outage requests while the fixture is being removed acts
    # on containers that are on their way out.
    controller_alive = False
    if state.controller is not None:
        guarded("outage-controller", findings, lambda: findings.extend(state.controller.shutdown()))
        controller_alive = any(finding.startswith("outage-controller") and "did not end" in finding
                               for finding in findings)

    if controller_alive:
        # A hard failure, and the reason the two stages below are skipped rather than raced: removing
        # the fixture while a live thread still issues 'compose stop' and 'compose start' against it
        # is two writers on one project, and whichever wins, the next run meets the loser's state.
        findings.append(
            "broker-fixture: the outage controller is still alive, so this run does not remove the "
            "fixture underneath it. The containers of compose project "
            f"{environment.get(PROJECT_VARIABLE, '<unnamed>')} are left for a human to remove")

        return findings

    # An outage is undone before anything else, and unconditionally. A child that hung or was killed
    # leaves the broker stopped, and a stopped container survives a failed teardown: the next run
    # then meets a fixture that is up and answers nothing.
    if args.allow_broker_outage:
        def restore() -> None:
            restored = compose("start", args.allow_broker_outage, capture=True, environment=environment)
            if restored.returncode != 0:
                findings.append(
                    f"broker-restore: {args.allow_broker_outage} could not be started again: "
                    f"{restored.stderr.strip() or restored.stdout.strip()}")

        guarded("broker-restore", findings, restore)

    # The log is collected before the teardown, or it does not exist any more. Brokers whose log the
    # refusal check already collected are not fetched a second time: that would overwrite the very
    # file the verdict was read from. A log is evidence about a run that already has its verdict, so
    # failing to collect it is a warning rather than a finding.
    try:
        remaining = [broker for broker in brokers if broker not in state.captured]
        if remaining:
            capture_logs(remaining, environment)
    except Exception as error:  # noqa: BLE001 - evidence about a verdict that already exists
        print(f"WARN broker-log: not collected ({error})", file=sys.stderr)

    # Trap equivalent: the fixture is removed on success, on failure and on an exception alike, and a
    # teardown that fails says so.
    guarded("broker-teardown", findings, lambda: stop(environment))

    return findings


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    validate(parser, args)

    brokers = list(dict.fromkeys(args.broker))
    credentials = build_environment()
    if os.environ.get("GITHUB_ACTIONS") == "true":
        for value in credentials.values():
            print(f"::add-mask::{value}")

    environment = dict(os.environ)
    environment.update(credentials)

    # One identity for this run, and everything that can collide is derived from it: the compose
    # project that decides which containers a command addresses, and the root every output and control
    # file of this run lives under. Two runs on one machine share nothing at all.
    #
    # The root is claimed rather than adopted. The canonical entry point hands one down together with
    # the token that proves it minted it; a direct call mints its own. A bare path in the environment
    # is refused, because a run that believed one would write into, and later clean up, a directory
    # belonging to somebody else.
    try:
        run_root = run_scope.claim_run_root(RAW_RUN_OUTPUT_DIR)
    except run_scope.OwnershipError as error:
        print(f"FAIL broker-category {args.category or 'command'}: {error}", file=sys.stderr)
        return 1

    identity = run_root.name
    environment[PROJECT_VARIABLE] = identity
    environment[RUN_ROOT_VARIABLE] = str(run_root)
    environment[run_scope.RUN_TOKEN_VARIABLE] = (
        (run_root / run_scope.RUN_TOKEN_FILE).read_text(encoding="utf-8").strip())
    print(f"run identity {identity}, output under {run_root.relative_to(REPO_ROOT)}")

    state = RunState()
    primary = 0
    escaping: BaseException | None = None

    try:
        primary = execute(args, brokers, environment, run_root, state)
    except RunnerError as error:
        print(f"FAIL broker-category {args.category or 'command'}: {error}", file=sys.stderr)
        primary = 1
    except BaseException as error:  # noqa: BLE001 - re-raised once the fixture is cleaned up
        escaping = error

    findings = teardown(args, brokers, environment, state)
    for finding in findings:
        print(f"FAIL {finding}", file=sys.stderr)

    # Written where the caller can read it rather than left in this process's output. A caller that had
    # to recognise a cleanup failure by matching prose in stderr would be reading a sentence, and a
    # sentence is not a contract: the canonical entry point puts these into its receipt.
    publish_json(run_root / FIXTURE_FINDINGS_FILE, {
        "schemaVersion": 1,
        "kind": "SERVICEBUS_FIXTURE_FINDINGS",
        "brokers": brokers,
        "allowedBrokerOutage": args.allow_broker_outage,
        "findings": findings,
        "logs": {broker: digest_of(broker_log_path(broker, environment)) for broker in brokers},
    })

    # The exception a reader has to see is the one that came first, and the cleanup findings are
    # already printed above it, so nothing is lost by letting it out here.
    if escaping is not None:
        raise escaping

    if findings:
        print("FAIL broker-fixture: the fixture could not be returned to a usable state", file=sys.stderr)
        # A primary failure stays the primary diagnostic and keeps its own exit code. An otherwise
        # green run is red because of the cleanup alone.
        return primary if primary != 0 else 1

    return primary


if __name__ == "__main__":
    raise SystemExit(main())
