#!/usr/bin/env python3
"""The pinned container fixture a broker backed category runs against, from start to removal.

One Compose project per run, named after the run root, so two runs on one machine address different
containers and share nothing at all. A fixture that could not be cleaned before starting is a startup
failure rather than something to write into, and a fixture that could not be removed afterwards is a
finding of the run rather than a line in somebody's output.

Standard library only.
"""

from __future__ import annotations

import json
import os
import subprocess
import time
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
COMPOSE_FILE = REPO_ROOT / "build/test-infrastructure/compose.yaml"

# One Compose project per run, so a command of this run addresses the containers of this run.
PROJECT_VARIABLE = "VICIONE_SERVICEBUS_COMPOSE_PROJECT"

# Everything a run writes lives under one root of its own.
RAW_RUN_OUTPUT_DIR = REPO_ROOT / "artifacts" / "run-output"
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
    # Artemis is a separate broker behind the required 'artemis' ActiveMQ LocalIntegration variants.
    # It is listed here so those cases receive a run-scoped endpoint instead of a fixed port.
    "artemis": {
        # Artemis exposes one multi-protocol acceptor here. Calling it OpenWire hid the AMQP owner
        # and encouraged callers to infer a second endpoint that does not exist.
        61616: "VICIONE_SERVICEBUS_ARTEMIS_PORT",
        8161: "VICIONE_SERVICEBUS_ARTEMIS_JOLOKIA_PORT",
    },
    # Not brokers, but the Entity Framework specs need them and they obey the same rules.
    "mssql": {1433: "VICIONE_SERVICEBUS_MSSQL_PORT"},
    "postgres": {5432: "VICIONE_SERVICEBUS_PG_PORT"},
    "azurite": {
        10000: "VICIONE_SERVICEBUS_AZURITE_BLOB_PORT",
        10002: "VICIONE_SERVICEBUS_AZURITE_TABLE_PORT",
    },
    "eventhubs": {5672: "VICIONE_SERVICEBUS_EVENTHUB_PORT"},
    "localstack": {4566: "VICIONE_SERVICEBUS_LOCALSTACK_PORT"},
}


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

    # Compose starts every named service concurrently. That is needlessly hostile to the bounded
    # developer/CI runners this repository supports: SQL Server, both ActiveMQ brokers, LocalStack
    # and the Event Hubs emulator all perform their most expensive initialization at once, and SQL
    # Server can fail with EAGAIN while the same pinned image is healthy in isolation. Keep one
    # run-scoped project and one final simultaneous fixture, but bring each requested service to its
    # declared readiness before starting the next one.
    for broker in dict.fromkeys(brokers):
        result = compose("up", "-d", "--wait", broker, capture=True, environment=environment)
        if result.returncode != 0:
            raise RunnerError(f"the {broker} fixture did not become ready: {result.stderr.strip()}")


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
