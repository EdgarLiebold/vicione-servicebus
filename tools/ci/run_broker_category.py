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
import json
import os
import re
import secrets
import shutil
import socket
import subprocess
import sys
import threading
import time
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]

# A collected broker log is raw run output, not repository structure: it is large, it repeats between
# runs and it is gone the moment the compose project is torn down anyway. It goes where the TRX goes,
# under artifacts/, which .gitignore covers; tools/ci/policy_validator.py rejects one under evidence/.
RAW_RUN_OUTPUT_DIR = REPO_ROOT / "artifacts" / "run-output"


def broker_log_path(broker: str) -> Path:
    return RAW_RUN_OUTPUT_DIR / f"{broker}-broker.log"
COMPOSE_FILE = REPO_ROOT / "build/test-infrastructure/compose.yaml"

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


def compose(*args: str, capture: bool = False, environment: dict[str, str] | None = None) -> subprocess.CompletedProcess:
    command = ["docker", "compose", "-f", str(COMPOSE_FILE), *args]
    return subprocess.run(command, text=True, capture_output=capture, check=False, env=environment)


def start(brokers: list[str], environment: dict[str, str]) -> None:
    # Clean slate before starting, not only afterwards. A database image applies its credentials only
    # when it initialises an empty data directory, so a volume left behind by an earlier run keeps the
    # old secret and the run fails authentication against its own fixture.
    compose("down", "-v", capture=True, environment=environment)

    result = compose("up", "-d", "--wait", *brokers, capture=True, environment=environment)
    if result.returncode != 0:
        raise RunnerError(f"the {', '.join(brokers)} fixture did not become ready: {result.stderr.strip()}")


def stop(environment: dict[str, str] | None = None) -> None:
    compose("down", "-v", capture=True, environment=environment)


# The fixture boundary a test asks for an outage through. The test writes a request file and waits for
# a result file; this runner is the only thing that ever touches Docker.
#
# Why pause rather than stop: measured on this fixture, 'compose stop' followed by 'compose start'
# rebinds the published port, because compose publishes an ephemeral loopback port per container start
# (32955 became 32958). A client that reconnects to the address it was configured with would then be
# reconnecting to a port nobody listens on, and the case would be measuring the fixture rather than the
# transport. 'docker pause' freezes the container's processes and keeps the published port, so the
# endpoint the bus holds stays the endpoint the broker comes back on. 'network disconnect' was measured
# too and drops the mapping as well, so it is out for the same reason.
OUTAGE_CONTROL_VARIABLE = "VICIONE_SERVICEBUS_FIXTURE_CONTROL"

OUTAGE_ACTIONS = ("interrupt", "restore")

# The port a client of that broker holds, and therefore the one whose reachability decides
# whether an outage really happened.
OUTAGE_PORT = {"activemq": 61616, "artemis": 61616, "rabbitmq": 5672,
               "mssql": 1433, "postgres": 5432}


def published_port(broker: str, container_port: int, environment: dict[str, str]) -> str | None:
    result = compose("port", broker, str(container_port), capture=True, environment=environment)

    return result.stdout.strip() or None


def port_accepts(endpoint: str, timeout: float = 2.0) -> bool:
    host, _, port = endpoint.rpartition(":")

    try:
        with socket.create_connection((host or "127.0.0.1", int(port)), timeout=timeout):
            return True
    except OSError:
        return False


def serve_outage_requests(broker: str, container_port: int, control: Path, environment: dict[str, str],
                          stop: threading.Event) -> None:
    """Answers one outage request at a time, and answers it with what it observed rather than with what
    it asked for. A request whose effect the runner cannot confirm is a failed request."""
    while not stop.is_set():
        for request in sorted(control.glob("*.request")):
            result = request.with_suffix(".result")
            if result.exists():
                continue

            answer: dict[str, object]
            try:
                asked = json.loads(request.read_text(encoding="utf-8"))
                action = asked.get("action")
                if action not in OUTAGE_ACTIONS:
                    raise ValueError(f"unknown action {action!r}, expected one of {OUTAGE_ACTIONS}")

                before = published_port(broker, container_port, environment)
                compose("pause" if action == "interrupt" else "unpause", broker,
                        capture=True, environment=environment)

                wanted = action == "restore"
                deadline = time.monotonic() + 60
                observed = None
                while time.monotonic() < deadline:
                    observed = port_accepts(published_port(broker, container_port, environment) or "")
                    if observed == wanted:
                        break
                    time.sleep(0.2)

                after = published_port(broker, container_port, environment)
                if observed != wanted:
                    raise TimeoutError(
                        f"the broker port still {'refuses' if wanted else 'accepts'} connections after "
                        f"{action}, so the outage was not established")
                if before != after:
                    raise RuntimeError(
                        f"the published port changed from {before} to {after}, so this is not the same "
                        "endpoint any more and no client could reconnect to it")

                answer = {"ok": True, "action": action, "endpoint": after, "accepts": observed}
            except Exception as error:  # noqa: BLE001 - the answer carries the reason
                answer = {"ok": False, "error": f"{type(error).__name__}: {error}"}

            result.write_text(json.dumps(answer, sort_keys=True) + "\n", encoding="utf-8")

        stop.wait(0.1)


def capture_logs(brokers: list[str], environment: dict[str, str]) -> None:
    """Write each broker's own log next to the test results, before the fixture is torn down.

    A broker states things no test process can observe about itself: that it took a delivery back
    because the acknowledgement timed out, that it refused an exclusive queue, which channel it closed
    and why. Once 'down -v' has run, that record is gone for good, so it is collected here rather than
    reconstructed from assertions afterwards. Failure to collect it does not fail the run -- the log is
    evidence about a run that has already produced its verdict.
    """
    RAW_RUN_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    for broker in brokers:
        result = compose("logs", "--no-color", "--timestamps", broker, capture=True, environment=environment)
        if result.returncode != 0:
            print(f"WARN broker-log {broker}: not collected ({result.stderr.strip()})", file=sys.stderr)
            continue
        target = broker_log_path(broker)
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


def resolve_ports(broker: str, environment: dict[str, str]) -> dict[str, str]:
    """Ask Docker for the ports it actually bound, and refuse anything outside loopback."""
    resolved: dict[str, str] = {}
    for container_port, variable in BROKER_PORTS[broker].items():
        result = compose("port", broker, str(container_port), capture=True, environment=environment)
        binding = result.stdout.strip()
        if result.returncode != 0 or not binding:
            raise RunnerError(f"container port {container_port} of {broker} is not published")

        host, _, port = binding.rpartition(":")
        if host not in ("127.0.0.1", "[::1]"):
            raise RunnerError(
                f"container port {container_port} of {broker} is published on '{host}' instead of loopback"
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


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    # A suite may legitimately span more than one broker: the ActiveMQ specs are parameterized over an
    # 'artemis' flavor that addresses a second, separate broker. Repeating --broker starts each of them,
    # so no spec has to fall back to a fixed port because its fixture was not started.
    parser.add_argument("--broker", required=True, action="append", choices=sorted(BROKER_PORTS))
    parser.add_argument("--category")
    parser.add_argument("--project")
    parser.add_argument("--evidence-dir", type=Path, default=Path("artifacts/run-output"))
    parser.add_argument("--ports-out", type=Path, help="Optional file for the resolved endpoints, secrets excluded.")
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
    args = parser.parse_args(argv)

    if args.command and not args.rest:
        parser.error("--command needs the command itself after --")
    if not args.command and not (args.category and args.project):
        parser.error("either --category with --project, or --command with the command after --")

    brokers = list(dict.fromkeys(args.broker))
    credentials = build_environment()
    if os.environ.get("GITHUB_ACTIONS") == "true":
        for value in credentials.values():
            print(f"::add-mask::{value}")

    environment = dict(os.environ)
    environment.update(credentials)

    captured: set[str] = set()

    try:
        start(brokers, environment)
        endpoints: dict[str, str] = {}
        for broker in brokers:
            endpoints.update(resolve_ports(broker, environment))
            endpoints[BROKER_HOST_VARIABLE[broker]] = "127.0.0.1"
        environment.update(endpoints)

        print(f"fixture {' and '.join(brokers)} ready on loopback: "
              + ", ".join(f"{name}={value}" for name, value in sorted(endpoints.items())))

        outage_stop = threading.Event()
        outage_thread = None
        if args.allow_broker_outage:
            broker = args.allow_broker_outage
            if broker not in brokers:
                raise RunnerError(f"--allow-broker-outage names '{broker}', which this run does not start")

            control = REPO_ROOT / "artifacts" / "run-output" / "fixture-control"
            if control.exists():
                shutil.rmtree(control)
            control.mkdir(parents=True)
            environment[OUTAGE_CONTROL_VARIABLE] = str(control)

            outage_thread = threading.Thread(
                target=serve_outage_requests,
                args=(broker, OUTAGE_PORT[broker], control, dict(environment), outage_stop),
                daemon=True)
            outage_thread.start()
            print(f"outage control ready for {broker} at {control}")

        if args.ports_out:
            args.ports_out.parent.mkdir(parents=True, exist_ok=True)
            args.ports_out.write_text(json.dumps(endpoints, indent=2, sort_keys=True) + "\n", encoding="utf-8")

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
                 "--evidence-dir", str(args.evidence_dir),
                 *(["--", *args.rest] if args.rest else [])],
                env=environment, text=True, check=False,
            )

        if args.one_refusal_per_vhost:
            # Before the teardown, and on the log this run produced. The check runs even when the tests
            # already failed: a retry loop is worth naming either way, and it must never be the reason a
            # red run looks green.
            capture_logs(brokers, environment)
            captured.update(brokers)

            for broker in brokers:
                if not assert_one_refusal_per_vhost(broker_log_path(broker), args.one_refusal_per_vhost):
                    return completed.returncode or 1

        outage_stop.set()
        if outage_thread is not None:
            outage_thread.join(timeout=5)

        return completed.returncode
    except RunnerError as error:
        print(f"FAIL broker-category {args.category}: {error}", file=sys.stderr)
        return 1
    finally:
        # An outage is undone before anything else, and unconditionally. A child that hung or was killed
        # leaves the container paused, and a paused container survives a failed teardown: the next run
        # then meets a broker that is up, has its ports and answers nothing.
        if args.allow_broker_outage:
            compose("unpause", args.allow_broker_outage, capture=True, environment=environment)

        # The log is collected before the teardown, or it does not exist any more. Brokers whose log the
        # refusal check already collected are not fetched a second time: that would overwrite the very
        # file the verdict was read from.
        try:
            remaining = [broker for broker in brokers if broker not in captured]
            if remaining:
                capture_logs(remaining, environment)
        except OSError as error:
            print(f"WARN broker-log: not collected ({error})", file=sys.stderr)

        # Trap equivalent: the fixture is removed on success, on failure and on an exception alike.
        stop(environment)


if __name__ == "__main__":
    raise SystemExit(main())
