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
import base64
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
from fixtures import broker_logs, compose_fixture, outage_protocol  # noqa: E402
from verification import run_scope  # noqa: E402

# A collected broker log is raw run output, not repository structure: it is large, it repeats between
# runs and it is gone the moment the compose project is torn down anyway. It goes where the TRX goes,
# under artifacts/, which .gitignore covers; raw run output does not belong under evidence/.
RAW_RUN_OUTPUT_DIR = REPO_ROOT / "artifacts" / "run-output"


def digest_of(path: Path) -> str | None:
    """The sha256 of a collected broker log, or None when there is none to hash."""
    if not path.is_file():
        return None

    return hashlib.sha256(path.read_bytes()).hexdigest()


def build_environment(brokers: list[str]) -> dict[str, str]:
    """Generate a fresh account for every broker in the compose file, not only the started one.

    Compose interpolates the whole file even when a single service is started, so a variable of the
    idle broker would abort the run. Generating a real secret for it as well avoids introducing a
    placeholder with a known value; the idle service is never started, so the secret is never used.
    """
    environment: dict[str, str] = {}
    for user_variable, pass_variable in broker_logs.BROKER_CREDENTIAL_VARIABLES.values():
        environment[user_variable] = (
            broker_logs.AZURITE_ACCOUNT_NAME
            if user_variable == broker_logs.AZURITE_ACCOUNT_VARIABLE
            else broker_logs.ACCOUNT_NAME
        )
        environment[pass_variable] = (
            base64.b64encode(secrets.token_bytes(32)).decode("ascii")
            if pass_variable == broker_logs.AZURITE_KEY_VARIABLE
            else secrets.token_hex(16)
        )
    environment["VICIONE_SERVICEBUS_PG_DATABASE"] = "postgres"
    environment[broker_logs.MSSQL_USER_VARIABLE] = broker_logs.MSSQL_ACCOUNT_NAME
    environment[broker_logs.MSSQL_PASSWORD_VARIABLE] = secrets.token_hex(16) + "Aa1!"
    if "localstack" in brokers:
        # These are run credentials, not product configuration. Standard AWS SDK variables make the
        # provider chain the one credential owner while the typed test configuration contains only
        # non-secret endpoint coordinates. They are generated only for an actual LocalStack run, so
        # unrelated fixture runs cannot accidentally acquire an AWS identity.
        environment["AWS_ACCESS_KEY_ID"] = "AKIA" + secrets.token_hex(8).upper()
        environment["AWS_SECRET_ACCESS_KEY"] = secrets.token_urlsafe(32)
        environment["AWS_REGION"] = "eu-central-1"
        environment["AWS_DEFAULT_REGION"] = "eu-central-1"
        environment["AWS_EC2_METADATA_DISABLED"] = "true"
        environment["VICIONE_SERVICEBUS_LOCALSTACK_REGION"] = "eu-central-1"
        environment["VICIONE_SERVICEBUS_LOCALSTACK_ACCOUNT_ID"] = "000000000000"
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

    Separate from main so callers can reconstruct the argument vector a
    workflow step really produces and parse it against this parser. Reading the workflow for expected
    substrings proved nothing about the command the shell would build from them.
    """
    parser = argparse.ArgumentParser(description=__doc__)
    # A suite may legitimately span more than one broker: the ActiveMQ specs are parameterized over an
    # 'artemis' flavor that addresses a second, separate broker. Repeating --broker starts each of them,
    # so no spec has to fall back to a fixed port because its fixture was not started.
    parser.add_argument("--broker", required=True, action="append", choices=sorted(compose_fixture.BROKER_PORTS))
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


class RunState:
    """What the teardown has to know about a run that may have failed anywhere inside it."""

    def __init__(self) -> None:
        self.controller: outage_protocol.OutageController | None = None
        self.captured: set[str] = set()


def execute(args: argparse.Namespace, brokers: list[str], environment: dict[str, str], run_root: Path,
            state: RunState) -> int:
    """Starts the fixture, runs the child, and returns the child's exit code.

    Everything in here is the primary outcome of the run. Nothing in here tears anything down: that is
    the teardown's work, and keeping the two apart is what stops a cleanup finding from occupying the
    slot the primary failure is read from.
    """
    proxy = compose_fixture.OUTAGE_PROXY.get(args.allow_broker_outage or "")
    compose_fixture.start(brokers + ([proxy] if proxy else []), environment)

    endpoints: dict[str, str] = {}
    for broker in brokers:
        # In a recovery run the addresses of the fronted broker come from its relay. That is the
        # whole point: the client keeps one address while the broker behind it restarts.
        source = proxy if proxy and broker == args.allow_broker_outage else broker
        endpoints.update(compose_fixture.resolve_ports(broker, environment, service=source))
        endpoints[broker_logs.BROKER_HOST_VARIABLE[broker]] = "127.0.0.1"
    environment.update(endpoints)

    if proxy:
        print(f"{args.allow_broker_outage} is reached through {proxy}, so its address survives a restart")

    print(f"fixture {' and '.join(brokers)} ready on loopback: "
          + ", ".join(f"{name}={value}" for name, value in sorted(endpoints.items())))

    if args.allow_broker_outage:
        # Under this run's own root, so it is never the path another run deletes.
        control = run_root / "fixture-control"
        control.mkdir(parents=True)
        environment[compose_fixture.OUTAGE_CONTROL_VARIABLE] = str(control)

        controller = outage_protocol.OutageController(args.allow_broker_outage, control, dict(environment))
        problems = controller.start()
        # Published to the teardown either way, because a controller that failed to start still has to
        # be shut down and still has findings to report. What it must not do is reach that state as a
        # half-built object, so it is constructed, started and only then handed over.
        state.controller = controller
        if problems:
            raise compose_fixture.RunnerError("; ".join(problems))
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
        broker_logs.capture_logs(brokers, environment)
        state.captured.update(brokers)

        for broker in brokers:
            if not broker_logs.assert_one_refusal_per_vhost(broker_logs.broker_log_path(broker, environment), args.one_refusal_per_vhost):
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
            f"{environment.get(compose_fixture.PROJECT_VARIABLE, '<unnamed>')} are left for a human to remove")

        return findings

    # An outage is undone before anything else, and unconditionally. A child that hung or was killed
    # leaves the broker stopped, and a stopped container survives a failed teardown: the next run
    # then meets a fixture that is up and answers nothing.
    if args.allow_broker_outage:
        def restore() -> None:
            restored = compose_fixture.compose("start", args.allow_broker_outage, capture=True, environment=environment)
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
            broker_logs.capture_logs(remaining, environment)
    except Exception as error:  # noqa: BLE001 - evidence about a verdict that already exists
        print(f"WARN broker-log: not collected ({error})", file=sys.stderr)

    # Trap equivalent: the fixture is removed on success, on failure and on an exception alike, and a
    # teardown that fails says so.
    guarded("broker-teardown", findings, lambda: compose_fixture.stop(environment))

    return findings


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    validate(parser, args)

    brokers = list(dict.fromkeys(args.broker))
    credentials = build_environment(brokers)
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
    environment[compose_fixture.PROJECT_VARIABLE] = identity
    environment[broker_logs.RUN_ROOT_VARIABLE] = str(run_root)
    environment[run_scope.RUN_TOKEN_VARIABLE] = (
        (run_root / run_scope.RUN_TOKEN_FILE).read_text(encoding="utf-8").strip())
    print(f"run identity {identity}, output under {run_root.relative_to(REPO_ROOT)}")

    state = RunState()
    primary = 0
    escaping: BaseException | None = None

    try:
        primary = execute(args, brokers, environment, run_root, state)
    except compose_fixture.RunnerError as error:
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
    compose_fixture.publish_json(run_root / compose_fixture.FIXTURE_FINDINGS_FILE, {
        "schemaVersion": 1,
        "kind": "SERVICEBUS_FIXTURE_FINDINGS",
        "brokers": brokers,
        "allowedBrokerOutage": args.allow_broker_outage,
        "findings": findings,
        "logs": {broker: digest_of(broker_logs.broker_log_path(broker, environment)) for broker in brokers},
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
