#!/usr/bin/env python3
"""The broker's own output, captured into the run root and bound to that run by digest.

A log nobody hashes is a file that was there. The digest is what puts a broker's output and one run
into a single statement, which is what the fixture record carries and what the receipt reader checks.

Standard library only.
"""

from __future__ import annotations

import fnmatch
import re
import sys
from pathlib import Path

from fixtures import compose_fixture
from verification import run_scope


def broker_log_path(broker: str, environment: dict[str, str]) -> Path:
    """Under this run's own root. A shared file name means a second run overwrites the log the first
    one's verdict was read from."""
    root = environment.get(RUN_ROOT_VARIABLE)

    return (Path(root) if root else compose_fixture.RAW_RUN_OUTPUT_DIR) / f"{broker}-broker.log"


COMPOSE_FILE = compose_fixture.REPO_ROOT / "build/test-infrastructure/compose.yaml"

# What separates one run from another. The compose project name decides which containers, networks and
# volumes a command addresses, and the run root decides where output and control files land. Both are
# derived from one identity so that nothing this run writes can collide with, or be removed by, another
# run on the same machine.

RUN_ROOT_VARIABLE = run_scope.RUN_ROOT_VARIABLE

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


def capture_logs(brokers: list[str], environment: dict[str, str]) -> None:
    """Write each broker's own log next to the test results, before the fixture is torn down.

    A broker states things no test process can observe about itself: that it took a delivery back
    because the acknowledgement timed out, that it refused an exclusive queue, which channel it closed
    and why. Once 'down -v' has run, that record is gone for good, so it is collected here rather than
    reconstructed from assertions afterwards. Failure to collect it does not fail the run -- the log is
    evidence about a run that has already produced its verdict.
    """
    for broker in brokers:
        result = compose_fixture.compose("logs", "--no-color", "--timestamps", broker, capture=True, environment=environment)
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
