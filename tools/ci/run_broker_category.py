#!/usr/bin/env python3
"""Canonical entry point for a broker-backed test category.

ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-08.

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
import json
import os
import secrets
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
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
# complexity rules, which a plain hex secret does not.
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
    environment[MSSQL_PASSWORD_VARIABLE] = secrets.token_hex(16) + "Aa1!"
    return environment


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    # A suite may legitimately span more than one broker: the ActiveMQ specs are parameterized over an
    # 'artemis' flavor that addresses a second, separate broker. Repeating --broker starts each of them,
    # so no spec has to fall back to a fixed port because its fixture was not started.
    parser.add_argument("--broker", required=True, action="append", choices=sorted(BROKER_PORTS))
    parser.add_argument("--category", required=True)
    parser.add_argument("--project", required=True)
    parser.add_argument("--evidence-dir", required=True, type=Path)
    parser.add_argument("--ports-out", type=Path, help="Optional file for the resolved endpoints, secrets excluded.")
    parser.add_argument("rest", nargs="*")
    args = parser.parse_args(argv)

    brokers = list(dict.fromkeys(args.broker))
    credentials = build_environment()
    if os.environ.get("GITHUB_ACTIONS") == "true":
        for value in credentials.values():
            print(f"::add-mask::{value}")

    environment = dict(os.environ)
    environment.update(credentials)

    try:
        start(brokers, environment)
        endpoints: dict[str, str] = {}
        for broker in brokers:
            endpoints.update(resolve_ports(broker, environment))
            endpoints[BROKER_HOST_VARIABLE[broker]] = "127.0.0.1"
        environment.update(endpoints)

        print(f"fixture {' and '.join(brokers)} ready on loopback: "
              + ", ".join(f"{name}={value}" for name, value in sorted(endpoints.items())))

        if args.ports_out:
            args.ports_out.parent.mkdir(parents=True, exist_ok=True)
            args.ports_out.write_text(json.dumps(endpoints, indent=2, sort_keys=True) + "\n", encoding="utf-8")

        runner = REPO_ROOT / "tools/ci/run_test_category.py"
        completed = subprocess.run(
            [sys.executable, str(runner),
             "--category", args.category,
             "--project", args.project,
             "--evidence-dir", str(args.evidence_dir),
             *(["--", *args.rest] if args.rest else [])],
            env=environment, text=True, check=False,
        )
        return completed.returncode
    except RunnerError as error:
        print(f"FAIL broker-category {args.category}: {error}", file=sys.stderr)
        return 1
    finally:
        # Trap equivalent: the fixture is removed on success, on failure and on an exception alike.
        stop(environment)


if __name__ == "__main__":
    raise SystemExit(main())
