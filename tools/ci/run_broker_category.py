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

RUN_ROOT_VARIABLE = "VICIONE_SERVICEBUS_RUN_ROOT"

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
    # Clean slate before starting, not only afterwards. A database image applies its credentials only
    # when it initialises an empty data directory, so a volume left behind by an earlier run keeps the
    # old secret and the run fails authentication against its own fixture.
    compose("down", "-v", capture=True, environment=environment)

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
    """Running is not ready. A broker that has just been started accepts nothing for a while, and the
    compose health check is the fixture's own definition of when it does."""
    result = compose("ps", "--format", "json", broker, capture=True, environment=environment)
    if result.returncode != 0:
        return False

    for line in result.stdout.splitlines():
        line = line.strip()
        if not line:
            continue
        entry = json.loads(line)
        for record in entry if isinstance(entry, list) else [entry]:
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
            result = request.with_suffix(".result")
            if result.exists():
                continue

            answer: dict[str, object]
            request_id = request.stem
            try:
                asked = json.loads(request.read_text(encoding="utf-8"))
                action = asked.get("action")
                if action not in OUTAGE_ACTIONS:
                    raise ValueError(f"unknown action {action!r}, expected one of {OUTAGE_ACTIONS}")

                compose("stop" if action == "interrupt" else "start", broker,
                        capture=True, environment=environment)

                deadline = time.monotonic() + OUTAGE_BUDGET_SECONDS
                observed = ""
                while time.monotonic() < deadline:
                    if action == "interrupt":
                        observed = broker_state(broker, environment)
                        if observed in ("exited", "stopped", "absent"):
                            break
                    else:
                        if broker_is_healthy(broker, environment):
                            observed = "healthy"
                            break
                        observed = broker_state(broker, environment)
                    time.sleep(0.5)
                else:
                    raise TimeoutError(
                        f"the broker was still '{observed}' {OUTAGE_BUDGET_SECONDS} s after {action}, so "
                        "the outage was not established")

                answer = {"schemaVersion": CONTROL_SCHEMA_VERSION, "requestId": request_id,
                          "action": action, "status": "ok", "observed": observed}
            except Exception as error:  # noqa: BLE001 - the answer carries the reason
                answer = {"schemaVersion": CONTROL_SCHEMA_VERSION, "requestId": request_id,
                          "action": asked.get("action") if isinstance(locals().get("asked"), dict) else None,
                          "status": "failed", "error": f"{type(error).__name__}: {error}"}

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
    if args.allow_broker_outage and args.allow_broker_outage not in args.broker:
        parser.error(f"--allow-broker-outage names '{args.allow_broker_outage}', which this run does not start")


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
    # file of this run lives under. Two runs on one machine now share nothing at all.
    identity = f"vicione-{secrets.token_hex(6)}"
    run_root = RAW_RUN_OUTPUT_DIR / identity
    run_root.mkdir(parents=True, exist_ok=True)
    environment[PROJECT_VARIABLE] = identity
    environment[RUN_ROOT_VARIABLE] = str(run_root)
    print(f"run identity {identity}, output under {run_root.relative_to(REPO_ROOT)}")

    captured: set[str] = set()
    outage_stop = threading.Event()
    outage_thread: threading.Thread | None = None
    failure: BaseException | None = None

    try:
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
            broker = args.allow_broker_outage
            # Under this run's own root, so it is never the path another run deletes.
            control = run_root / "fixture-control"
            control.mkdir(parents=True)
            environment[OUTAGE_CONTROL_VARIABLE] = str(control)

            outage_thread = threading.Thread(
                target=serve_outage_requests,
                args=(broker, control, dict(environment), outage_stop),
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
                if not assert_one_refusal_per_vhost(broker_log_path(broker, environment), args.one_refusal_per_vhost):
                    return completed.returncode or 1

        return completed.returncode
    except RunnerError as error:
        failure = error
        print(f"FAIL broker-category {args.category or 'command'}: {error}", file=sys.stderr)
        return 1
    except BaseException as error:  # noqa: BLE001 - remembered so the teardown cannot replace it
        failure = error
        raise
    finally:
        # The control thread is stopped and joined in every exit path, including the ones that raise.
        # A thread still answering outage requests while the fixture is being removed would act on
        # containers that are on their way out.
        outage_stop.set()
        if outage_thread is not None:
            outage_thread.join(timeout=10)

        # An outage is undone before anything else, and unconditionally. A child that hung or was killed
        # leaves the broker stopped, and a stopped container survives a failed teardown: the next run
        # then meets a fixture that is up and answers nothing.
        if args.allow_broker_outage:
            compose("start", args.allow_broker_outage, capture=True, environment=environment)

        # The log is collected before the teardown, or it does not exist any more. Brokers whose log the
        # refusal check already collected are not fetched a second time: that would overwrite the very
        # file the verdict was read from.
        try:
            remaining = [broker for broker in brokers if broker not in captured]
            if remaining:
                capture_logs(remaining, environment)
        except OSError as error:
            print(f"WARN broker-log: not collected ({error})", file=sys.stderr)

        # Trap equivalent: the fixture is removed on success, on failure and on an exception alike, and
        # a teardown that fails says so. It never replaces the failure that came first - that one is
        # already on its way out - but on an otherwise successful run it is the verdict.
        try:
            stop(environment)
        except TeardownError as error:
            print(f"FAIL broker-teardown: {error}", file=sys.stderr)
            if failure is None:
                # Raised rather than returned: a return inside finally would discard the value the try
                # block produced, including a non zero one.
                raise


if __name__ == "__main__":
    raise SystemExit(main())
