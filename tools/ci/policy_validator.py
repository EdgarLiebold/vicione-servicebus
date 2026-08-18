#!/usr/bin/env python3
"""Repository-local CI policy validator.

Guards the invariants this work package established, so the known regressions cannot come back
quietly. Standard library only: the validator must run before any restore and must never depend on
the thing it validates.

Every rule exists because something actually went wrong, not because a rule seemed nice:

  broker images      a global rename rewrote two external Docker Hub names into images that do not
                     exist, and every broker job died in "Initialize containers"
  pinning            a moving tag silently changes the fixture under a green run
  host binding       a bare "5672:5672" publishes the test broker on every host interface
  credentials        'guest' outside loopback and loopback_users=none weaken the broker instead of
                     using an account scoped to the run
  required profile   an unsatisfiable repository or branch guard made whole categories vanish while
                     the run still reported success
  pack               packing without the required gates, or without a verifiable run artifact,
                     produces packages nobody has checked
  publication        this slice publishes nothing, to no registry
  analyzer tracking  a provenance comment in the wrong place breaks the Roslyn release parser
  restore sources    without a repository-local NuGet.config a restore inherits whatever the machine
                     has configured; here that was four sources, three of them internal, which made
                     the build depend on host state and raised NU1507
"""

from __future__ import annotations

import argparse
import contextlib
import io
import json
import re
import shlex
import sys
import textwrap
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import run_broker_category  # noqa: E402  (repository local, resolved from this file's folder)
import run_test_category  # noqa: E402
import verification_model  # noqa: E402
from xml.etree import ElementTree

RESTORE_CONFIG = "NuGet.config"
REQUIRED_SOURCE_KEY = "nuget.org"
REQUIRED_SOURCE_VALUE = "https://api.nuget.org/v3/index.json"
CREDENTIAL_ELEMENT = "packageSourceCredentials"
CREDENTIAL_KEYS = ("Username", "Password", "ClearTextPassword")

FORBIDDEN_IMAGES = ("vicione-servicebus/rabbitmq", "vicione-servicebus/activemq")
# Every job the required profile has to contain. It is derived from the capability matrix rather than
# remembered: a retained capability that runs locally belongs in the required path, and a job that
# only exists as a selectable target without an executing job is false green.
REQUIRED_CATEGORIES = ("build", "analyzer", "core-unit", "signalr", "quartz", "activemq",
                       "sql-transport", "benchmarks", "rabbitmq", "entity-framework", "pack")

# The one operating system the required profile runs on, and the exact SDK global.json releases.
REQUIRED_RUNNER = "ubuntu-24.04"
APPROVED_SDK_FILE = "global.json"
DIGEST = re.compile(r"@sha256:[0-9a-f]{64}")
SHA256 = re.compile(r"\b[0-9a-f]{64}\b")
# The fixture must publish ephemeral loopback ports: "127.0.0.1::5672". Docker then allocates a free
# host port atomically, which is what lets the run survive a machine where 5672 is already taken.
EPHEMERAL_LOOPBACK_PORT = re.compile(r'"127\.0\.0\.1::\d+"')
FIXED_LOOPBACK_PORT = re.compile(r'"127\.0\.0\.1:\d+:\d+"')
BARE_PORT = re.compile(r'"\d+:\d+"')
WILDCARD_PORT = re.compile(r'"0\.0\.0\.0:')
CANONICAL_RUNNER = "tools/ci/run_broker_category.py"
BROKER_CATEGORIES = ("rabbitmq", "activemq")
KNOWN_CREDENTIALS = ("guest", "admin")

# The single binding list of everything a required category does not execute. Both halves of the
# exclusion rule read it: this validator before a run, run_test_category.py after one.
VERIFICATION_MODEL = "build/verification/VERIFICATION_MODEL.json"

# The guard that keeps the historic localhost/guest defaults out of the required broker run.
RUN_SCOPED_GUARD_FILE = "tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/RabbitMqTestSetUpFixture.cs"
RUN_SCOPED_GUARD_METHOD = "RequireRunScopedCredentials"
RUN_SCOPED_GUARD_VARIABLES = (
    "UsernameVariable", "PasswordVariable", "HostVariable", "PortVariable", "ManagementPortVariable",
)

# A required job runs on every push and pull request. A job level condition can only ever reduce
# that, and an unsatisfiable one removes the category while the run still reports success. Step
# level conditions are unaffected: they sit deeper and cannot remove the job.
REQUIRED_JOB_CONDITION = re.compile(r"^    if:", re.M)

# Ports a broker fixture listens on. A spec that writes one of these literally cannot be addressing
# the run-scoped fixture, because that one is published on an ephemeral port chosen per run.
# 61618 is included deliberately: it was the fixed Artemis port of the imported baseline.
BROKER_PORTS_NEVER_HARDCODED = frozenset({5672, 8161, 15672, 61613, 61616, 61617, 61618})

# The database fixtures publish ephemeral loopback ports too, so a literal local database endpoint in a
# spec cannot be addressing them. 'Password12!' is the well known secret the imported baseline shipped in
# its workflow and in two spec files; it must never be effective again.
# The port is optional on purpose: an Npgsql string writes it as a separate key, and a connection with
# no port at all is even worse, because it silently takes the default one.
LOCAL_DATABASE_ENDPOINT = re.compile(
    r"(?:Server\s*=\s*tcp:|Data\s+Source\s*=|host\s*=)\s*(?P<host>localhost|127\.0\.0\.1)",
    re.IGNORECASE)
KNOWN_DATABASE_SECRET = "Password12!"

# Only a local host can accidentally reach a real broker on the developer machine or the runner.
# Address parsing specs legitimately carry literals such as "rabbitmq://remote-host:5672/queue" as
# data; those never open a connection and are not what this rule is about.
# The optional userinfo group is the point: without it the rule matched amqp://localhost:5672 and
# missed amqp://guest:guest@localhost:5672, which is the spelling that actually carries the default
# account — exactly the "due broker fixture with a default-credential fallback" REQ-CI-007 names.
LOCAL_BROKER_ENDPOINT = re.compile(
    r"(?:amqp|activemq|rabbitmq|tcp)://(?:[^/@\s]*@)?"
    r"(?P<host>localhost|127\.0\.0\.1|0\.0\.0\.0|\[::1\]):(?P<port>\d{2,5})")


def strip_comments(text: str) -> str:
    """Drop whole-line comments so prose about a forbidden pattern is not mistaken for the pattern."""
    return "\n".join(line for line in text.splitlines() if not line.lstrip().startswith("#"))


class WorkflowScriptError(RuntimeError):
    """The workflow is not in a shape this reader can turn into commands.

    A reader that guesses is worse than no reader: it would agree with a broken step for the wrong
    reason. Everything it was not written for is refused so somebody looks.
    """


# What ends one command and starts the next one in a shell.
SHELL_OPERATORS = frozenset({";", ";;", "&", "&&", "|", "|&", "||", "\n"})

# The repository runners a workflow step may invoke, and the module that owns each command line.
RUNNER_MODULES = {
    "tools/ci/run_test_category.py": run_test_category,
    "tools/ci/run_broker_category.py": run_broker_category,
}


def run_scripts(body: str) -> list[tuple[int, str]]:
    """Every 'run:' script of a workflow, with the line it starts on.

    A small line reader rather than a YAML parser, because this validator has to run before any
    restore and may not depend on a package. It understands the two shapes this repository uses - a
    one line script and a literal block - and refuses every other one.
    """
    scripts: list[tuple[int, str]] = []
    lines = body.splitlines()
    number = 0
    while number < len(lines):
        line = lines[number]
        number += 1
        if line.lstrip().startswith("#"):
            continue
        match = re.match(r"^\s*(?:-\s+)?run:\s*(.*)$", line)
        if match is None:
            continue

        start = number
        # The column 'run:' stands in decides what belongs to its block, so the list item form
        # '- run: |' measures from the same place as the plain 'run: |'.
        indent = " " * line.index("run:")
        first = match.group(1).strip()

        if first.startswith(">"):
            raise WorkflowScriptError(
                f"line {start}: a folded 'run: >' script joins its lines in a way this reader does not "
                "reconstruct, so the command it produces would be a guess")
        if first and first not in ("|", "|-", "|+"):
            scripts.append((start, first))
            continue
        if not first:
            raise WorkflowScriptError(f"line {start}: 'run:' carries neither a script nor a block scalar")

        block: list[str] = []
        while number < len(lines):
            following = lines[number]
            if following.strip() and not following.startswith(indent + " "):
                break
            block.append(following)
            number += 1
        scripts.append((start, textwrap.dedent("\n".join(block))))

    return scripts


def effective_commands(script: str) -> list[list[str]]:
    """The commands a shell really builds from one step, with continuations joined and quotes honoured.

    This is the whole point of the rule below. A step is text until the shell has joined its
    continuation lines, and a rule that looks for expected substrings agrees with a step whose
    continuation is missing exactly as readily as with one that works.
    """
    joined: list[str] = []
    pending = ""
    for line in script.splitlines():
        stripped = line.rstrip()
        trailing = len(stripped) - len(stripped.rstrip("\\"))
        # An odd number of trailing backslashes continues the line. An even number is an escaped
        # backslash and ends it, which is why they are counted rather than tested for one.
        if trailing % 2 == 1:
            pending += stripped[:-1] + " "
            continue
        joined.append(pending + stripped)
        pending = ""
    if pending:
        joined.append(pending)

    commands: list[list[str]] = []
    for logical in joined:
        if not logical.strip() or logical.lstrip().startswith("#"):
            continue

        lexer = shlex.shlex(logical, posix=True, punctuation_chars=True)
        lexer.whitespace_split = True
        try:
            tokens = list(lexer)
        except ValueError as error:
            raise WorkflowScriptError(f"'{logical.strip()}' is not a shell command line: {error}") from error

        current: list[str] = []
        for token in tokens:
            if token in SHELL_OPERATORS:
                if current:
                    commands.append(current)
                current = []
                continue
            current.append(token)
        if current:
            commands.append(current)

    return commands


def refusal(module, vector: list[str]) -> str:
    """Empty when that runner accepts this argument vector, the reason it gives otherwise.

    The vector is handed to the runner's own parser, so what is proved here is that the command the
    shell builds is a command the runner takes - not that the step mentions the right words.
    """
    parser = module.build_parser()
    said = io.StringIO()
    try:
        with contextlib.redirect_stderr(said), contextlib.redirect_stdout(said):
            args = parser.parse_args(vector)
            close = getattr(module, "validate", None)
            if close is not None:
                close(parser, args)
    except SystemExit:
        spoken = said.getvalue().strip().splitlines()
        return spoken[-1].strip() if spoken else "the runner refused the call without saying why"

    return ""


class Policy:
    def __init__(self, root: Path) -> None:
        self.root = root
        self.failures: list[str] = []

    def fail(self, rule: str, detail: str) -> None:
        self.failures.append(f"{rule}: {detail}")

    def read(self, relative: str) -> str | None:
        path = self.root / relative
        return path.read_text(encoding="utf-8") if path.is_file() else None

    @staticmethod
    def job_section(body: str, job: str) -> str:
        """The body of one workflow job, cut at the next job rather than the next indented line."""
        marker = f"\n  {job}:"
        if marker not in body:
            return ""
        rest = body.split(marker, 1)[1]
        following = re.search(r"\n  [A-Za-z][\w-]*:\s*$", rest, re.M)
        return rest[: following.start()] if following else rest

    # -- rules ---------------------------------------------------------------------------------

    def check_no_forbidden_images(self) -> None:
        for path in sorted(self.root.rglob("*.y*ml")):
            if any(part in {"bin", "obj", ".git"} for part in path.parts):
                continue
            body = strip_comments(path.read_text(encoding="utf-8", errors="replace"))
            for image in FORBIDDEN_IMAGES:
                if image in body:
                    self.fail("broker-image", f"{path.relative_to(self.root)} references the nonexistent image '{image}'")

    def check_pinning(self) -> None:
        lock = self.read("build/test-infrastructure/images.lock.json")
        if lock is None:
            self.fail("pinning", "build/test-infrastructure/images.lock.json is missing")
            return
        data = json.loads(lock)
        for name, image in data.get("baseImages", {}).items():
            if not str(image.get("digest", "")).startswith("sha256:") or len(str(image.get("digest"))) != 71:
                self.fail("pinning", f"base image '{name}' has no full sha256 digest")
        for name, plugin in data.get("plugins", {}).items():
            if SHA256.fullmatch(str(plugin.get("sha256", ""))) is None:
                self.fail("pinning", f"plugin '{name}' has no sha256 checksum")

        for dockerfile in sorted(self.root.glob("build/test-infrastructure/*/Dockerfile")):
            body = strip_comments(dockerfile.read_text(encoding="utf-8"))
            name = dockerfile.relative_to(self.root)
            for line in body.splitlines():
                stripped = line.strip()
                if stripped.startswith("FROM ") and DIGEST.search(stripped) is None:
                    self.fail("pinning", f"{name} pins no digest: {stripped}")
                if stripped.startswith("FROM ") and stripped.rstrip().endswith(":latest"):
                    self.fail("pinning", f"{name} uses the moving tag latest")
                if stripped.startswith("ADD ") and "--checksum=sha256:" not in body:
                    self.fail("pinning", f"{name} downloads without a checksum")

    def check_host_binding(self) -> None:
        compose = self.read("build/test-infrastructure/compose.yaml")
        if compose is None:
            self.fail("host-binding", "build/test-infrastructure/compose.yaml is missing")
            return
        body = strip_comments(compose)
        # A published port entry is a list item whose quoted value ends in ':<port>'. This has to
        # match the ephemeral form "127.0.0.1::5672" as well as the fixed and wildcard forms.
        published = [
            line.strip() for line in body.splitlines()
            if re.match(r'^-\s*"[^"]*:\d+"$', line.strip())
        ]
        if not published:
            self.fail("host-binding", "compose.yaml publishes no port; the fixture would be unreachable")
        for entry in published:
            if WILDCARD_PORT.search(entry):
                self.fail("host-binding", f"port is published on every interface: {entry}")
            elif FIXED_LOOPBACK_PORT.search(entry):
                self.fail("host-binding",
                          f"port is pinned to a fixed host port: {entry}. Publish it ephemerally as "
                          '"127.0.0.1::<container port>" so a broker already listening on the developer '
                          "machine cannot collide.")
            elif not EPHEMERAL_LOOPBACK_PORT.search(entry):
                if BARE_PORT.search(entry):
                    self.fail("host-binding", f"port is published without a host address: {entry}")
                else:
                    self.fail("host-binding", f"port is not an ephemeral 127.0.0.1 binding: {entry}")

    def check_credentials(self) -> None:
        for path in sorted(self.root.glob("build/test-infrastructure/**/*")):
            if not path.is_file():
                continue
            body = strip_comments(path.read_text(encoding="utf-8", errors="replace"))
            name = path.relative_to(self.root)
            if "loopback_users" in body and "none" in body:
                self.fail("credentials", f"{name} relaxes loopback_users")
            if re.search(r"RABBITMQ_DEFAULT_USER\s*[:=]\s*guest", body):
                self.fail("credentials", f"{name} uses the guest account")

        compose = self.read("build/test-infrastructure/compose.yaml")
        if compose is None:
            return
        body = strip_comments(compose)
        for variable in ("VICIONE_SERVICEBUS_RMQ_USER", "VICIONE_SERVICEBUS_RMQ_PASS",
                         "VICIONE_SERVICEBUS_AMQ_USER", "VICIONE_SERVICEBUS_AMQ_PASS"):
            if variable in body and f"${{{variable}:?" not in body:
                self.fail("credentials", f"{variable} may fall back silently; it must use the ':?' required form")

    def check_canonical_runner(self) -> None:
        """A broker category must go through the one run path that resolves the ephemeral ports."""
        workflow = self.read(".github/workflows/build.yml")
        if workflow is None:
            return
        body = strip_comments(workflow)
        for category in BROKER_CATEGORIES:
            marker = f"\n  {category}:"
            if marker not in body:
                continue
            # Cut at the next job, which is a two-space indented key at column three, not at the
            # next indented line of any kind.
            section = self.job_section(body, category)
            if CANONICAL_RUNNER not in section:
                self.fail("canonical-runner",
                          f"broker category '{category}' does not go through {CANONICAL_RUNNER}; "
                          "the fixture endpoints would have to be guessed")
            if "docker compose" in section and CANONICAL_RUNNER not in section:
                self.fail("canonical-runner",
                          f"broker category '{category}' starts the fixture on its own instead of using the runner")

    def check_no_effective_known_credentials(self) -> None:
        """A well known account must never be usable against the fixture."""
        for relative in ["build/test-infrastructure/compose.yaml"] + self.workflow_files():
            body = self.read(relative)
            if body is None:
                continue
            for line in strip_comments(body).splitlines():
                lowered = line.lower()
                if "password" not in lowered and "_pass" not in lowered and "_user" not in lowered:
                    continue
                for credential in KNOWN_CREDENTIALS:
                    if re.search(rf"[:=]\s*[\"']?{credential}[\"']?\s*$", line.strip()):
                        self.fail("known-credentials",
                                  f"{relative} makes the well known account '{credential}' effective: {line.strip()}")

    def check_no_required_test_masking(self) -> None:
        """The required path may not filter, skip or ignore its way to green.

        This used to match a quoted category predicate. That was one spelling of one selector: the
        same exclusion written with single quotes, or written against FullyQualifiedName, Name or
        the NUnit selector, walked straight past it. A required category is defined by running
        everything it contains, so no selector is admissible here at all and the rule matches the
        switch rather than the predicate behind it.
        """
        workflow = self.read(".github/workflows/build.yml")
        if workflow is None:
            return
        body = strip_comments(workflow)
        for pattern, detail in (
            (r"--filter\b", "the required path selects a subset of its tests"),
            (r"NUnit\.Where", "the required path applies an NUnit selector"),
            (r"--settings\b", "the required path loads a run settings file, which can carry a filter"),
            (r"TestCaseFilter", "the required path applies a test case filter"),
            (r"--blame-hang", "the required path masks hangs instead of failing"),
            (r"VSTEST_.*SKIP", "the required path sets a skip switch"),
        ):
            if re.search(pattern, body):
                self.fail("test-masking", f"{detail}: pattern '{pattern}'")

    def check_no_silent_test_exclusion(self) -> None:
        """Every test the required categories do not execute is named in one inventory.

        A category that reports a positive count and zero failures says nothing about the tests it
        never started. NUnit offers two ways to leave one out, and they are treated differently on
        purpose:

          [Ignore]    never appears in this repository and is rejected outright. It disables a case
                      without a run time decision, which is exactly the regression this rule exists
                      for.
          [Explicit]  is inherited from the imported suite for cases that genuinely cannot run in a
                      required category. Each of them is listed in the inventory with its reason, and
                      the count per project is bound, so a new one cannot appear unnoticed.

        The run time half of this rule is in run_test_category.py, which rejects any not executed
        case the inventory does not name. This half catches the same mutation before a run.
        """
        inventory = self.read(VERIFICATION_MODEL)
        if inventory is None:
            self.fail("test-exclusion", f"{VERIFICATION_MODEL} is missing; "
                                        "the required categories would have no binding list of what they skip")
            return
        try:
            data = json.loads(inventory)
        except json.JSONDecodeError as error:
            self.fail("test-exclusion", f"{VERIFICATION_MODEL} is not readable: {error}")
            return

        categories = {run["category"]: run for run in verification_model.runs(data) if run.get("category")}
        if not categories:
            self.fail("test-exclusion", f"{VERIFICATION_MODEL} declares no required run")
            return

        for name, category in sorted(categories.items()):
            project = str(category.get("testProjectDirectory", ""))
            directory = self.root / project
            if not project or not directory.is_dir():
                self.fail("test-exclusion", f"category '{name}' names no existing test project: '{project}'")
                continue

            sources = [
                source for source in sorted(directory.rglob("*.cs"))
                if "/bin/" not in source.as_posix() and "/obj/" not in source.as_posix()
            ]
            ignored = 0
            explicit = 0
            for source in sources:
                for number, line in enumerate(source.read_text(encoding="utf-8-sig", errors="replace").splitlines(), 1):
                    if line.lstrip().startswith("//"):
                        continue
                    if "[Ignore" in line:
                        ignored += 1
                        self.fail("test-exclusion",
                                  f"{source.relative_to(self.root).as_posix()}:{number} uses [Ignore]; "
                                  "a required project may not silence a case without a run time decision")
                    explicit += line.count("[Explicit")

            declared = category.get("explicitAttributeCount")
            if not isinstance(declared, int) or declared != explicit:
                self.fail("test-exclusion",
                          f"category '{name}' declares {declared} [Explicit] attributes but {project} carries "
                          f"{explicit}; a new exclusion has to be classified in {VERIFICATION_MODEL}")

            cases = category.get("notExecuted")
            if not isinstance(cases, list):
                self.fail("test-exclusion", f"category '{name}' has no case list")
                continue
            for case in cases:
                if not isinstance(case, dict) or not {"identity", "fixture", "test", "mechanism", "dueness", "reason"} <= set(case):
                    self.fail("test-exclusion", f"category '{name}' has an incomplete case entry: {case}")
                    continue
                # The identity carries the namespace, the class including parameterised fixture
                # arguments and the case name. A short form would let one entry authorise every
                # case that happens to share it, which is how two fixtures of the same name in
                # different namespaces once shared a single permission.
                identity = str(case.get("identity", ""))
                suffix = f".{case.get('fixture')}.{case.get('test')}"
                # Ends exactly on .fixture.test, and something has to stand in front of it. The first
                # version of this rule asked whether the identity merely contained the two names, so
                # the bare short form Fixture.Test passed as a full identity and a name inserted
                # between fixture and test passed as well. Both are the collision this contract
                # exists to prevent, so the check is a suffix and a non empty namespace.
                namespace = identity[:-len(suffix)] if identity.endswith(suffix) else ""
                if not namespace:
                    self.fail("test-exclusion",
                              f"category '{name}' names the case {case.get('fixture')}.{case.get('test')} with the "
                              f"identity '{identity}', which is not a namespaced identity ending on "
                              f"'{suffix}'")
                if str(case.get("dueness", "")).startswith("DUE"):
                    self.fail("test-exclusion",
                              f"category '{name}' lists a due case as not executed: "
                              f"{case.get('fixture')}.{case.get('test')}. A due case is run, never inventoried.")
            _ = ignored

    def check_run_scoped_credential_guard(self) -> None:
        """The required broker suite refuses to start without the run-scoped fixture configuration.

        Both the harness and the spec helper keep the historic localhost/guest defaults so an
        external consumer can still point them at their own broker. That is deliberate, and it is
        also exactly the fallback that once let a foreign broker on the default port carry fourteen
        green tests. The single thing that keeps it out of the required run is this guard, so the
        guard itself is bound here: it must name every variable the fixture provides and it must
        fail the run, not warn.
        """
        guard = self.read(RUN_SCOPED_GUARD_FILE)
        if guard is None:
            self.fail("run-scoped-guard", f"{RUN_SCOPED_GUARD_FILE} is missing; "
                                          "the required broker suite could fall back to localhost and guest")
            return
        body = strip_comments(guard)
        for variable in RUN_SCOPED_GUARD_VARIABLES:
            if variable not in body:
                self.fail("run-scoped-guard",
                          f"{RUN_SCOPED_GUARD_FILE} does not require {variable}; "
                          "an unset value would silently fall back to a default")
        if "Assert.Fail" not in body:
            self.fail("run-scoped-guard", f"{RUN_SCOPED_GUARD_FILE} does not fail the run on a missing value")
        if "[OneTimeSetUp]" not in body or "[SetUpFixture]" not in body:
            self.fail("run-scoped-guard",
                      f"{RUN_SCOPED_GUARD_FILE} does not run the check as a set up fixture, "
                      "so individual specs could start before it")
        # Defining the method is not calling it. The declaration sits below the one time set up, so
        # a plain substring search after that marker finds the definition and reports a guard that
        # no longer runs as present.
        called = any(
            line.strip() == f"{RUN_SCOPED_GUARD_METHOD}();"
            for line in body.splitlines()
        )
        if not called:
            self.fail("run-scoped-guard",
                      f"{RUN_SCOPED_GUARD_FILE} defines {RUN_SCOPED_GUARD_METHOD} but never calls it, "
                      "so the required run would start without the fixture configuration")

    def check_required_profile(self) -> None:
        workflow = self.read(".github/workflows/build.yml")
        if workflow is None:
            self.fail("required-profile", ".github/workflows/build.yml is missing")
            return
        body = strip_comments(workflow)
        for category in REQUIRED_CATEGORIES:
            marker = f"\n  {category}:"
            if marker not in body:
                self.fail("required-profile", f"required category '{category}' is absent from the required profile")
                continue
            # Present is not the same as reachable. The three literals below are the guards that
            # actually came back once, but any job level condition can hide a category just as well,
            # so the shape is rejected rather than the three spellings.
            if REQUIRED_JOB_CONDITION.search(self.job_section(body, category)):
                self.fail("required-profile",
                          f"required category '{category}' carries a job level condition; a required "
                          "category runs unconditionally or it is not required")
        for guard in ("refs/heads/master", "refs/heads/develop", "ViciOne.ServiceBus/ViciOne.ServiceBus"):
            if guard in body:
                self.fail("required-profile", f"an upstream repository or branch guard returned: {guard}")
        if "run_test_category.py" not in body:
            self.fail("required-profile", "required categories do not go through the test count gate")

        # A job level condition was rejected while the trigger above it was never read. A path or
        # branch filter on the workflow itself removes every required job at once and for every change
        # outside the list, which is the same effect with a wider blast radius. The invariant this file
        # states is that a required job runs on every push and pull request, so the trigger is held to
        # it too.
        trigger = body.split("\njobs:", 1)[0]
        for keyword in ("paths:", "paths-ignore:", "branches-ignore:"):
            if re.search(rf"^\s+{re.escape(keyword)}", trigger, re.M):
                self.fail("required-profile",
                          f"the required profile filters its own trigger with '{keyword}'; a change outside that "
                          "list would run no required job at all")

    def check_workflow_steps_are_the_commands_they_look_like(self) -> None:
        """A required step is judged by the command the shell builds from it, not by its text.

        The RabbitMQ step lost the backslash after its --evidence-dir line. Every token the other
        rules look for was still there, so the whole Python suite stayed green - while the shell built
        two commands out of it: the runner without --one-refusal-per-vhost, and a second command whose
        program name was '--one-refusal-per-vhost'. The required RabbitMQ job could not run at all.

        Two things are held here. A command may not begin with an option, which is what a missing
        continuation always produces, and every reconstructed call of a repository runner is parsed
        against that runner's own command line, so a vector it would refuse is refused here first.
        """
        for relative in sorted(self.workflow_files()):
            body = self.read(relative)
            if body is None:
                continue

            try:
                scripts = run_scripts(body)
            except WorkflowScriptError as error:
                self.fail("workflow-command", f"{relative}: {error}")
                continue

            for line, script in scripts:
                try:
                    commands = effective_commands(script)
                except WorkflowScriptError as error:
                    self.fail("workflow-command", f"{relative} line {line}: {error}")
                    continue

                for command in commands:
                    if command[0].startswith("-"):
                        self.fail("workflow-command",
                                  f"{relative} line {line}: the shell builds '{shlex.join(command)}' as a "
                                  "command of its own, so the line before it is missing its continuation "
                                  "and the option never reaches the program it was written for")
                        continue

                    named = [token for token in command if token in RUNNER_MODULES]
                    for script_path in named:
                        vector = command[command.index(script_path) + 1:]
                        problem = refusal(RUNNER_MODULES[script_path], vector)
                        if problem:
                            self.fail("workflow-command",
                                      f"{relative} line {line}: {script_path} would be called as "
                                      f"'{shlex.join(vector)}', which it does not accept: {problem}")

    def check_pack(self) -> None:
        workflow = self.read(".github/workflows/build.yml")
        if workflow is None:
            return
        body = strip_comments(workflow)
        pack = body.split("\n  pack:", 1)
        if len(pack) < 2:
            self.fail("pack", "the required profile has no pack job")
            return
        section = pack[1]
        for gate in REQUIRED_CATEGORIES:
            if gate == "pack":
                continue
            if f"- {gate}" not in section.split("steps:", 1)[0]:
                self.fail("pack", f"pack does not depend on the required gate '{gate}'")
        if "upload-artifact" not in section:
            self.fail("pack", "pack uploads no verifiable run artifact")
        if "sha256sum" not in section:
            self.fail("pack", "pack does not hash its packages")
        # "a package is there" has to mean "this run produced it". A local run proved the difference:
        # pack failed and the hash step was still satisfied by what an earlier run had left behind.
        if "rm -rf artifacts/packages" not in section:
            self.fail("pack", "pack does not empty its output directory, so a stale package satisfies its hash step")

    def check_no_publication(self) -> None:
        for relative in self.workflow_files():
            body = self.read(relative)
            if body is None:
                continue
            stripped = strip_comments(body)
            # Matching the two command spellings was too narrow: a workflow publishes just as
            # effectively through an action, and neither 'nuget push' nor 'docker push' appears in
            # that case. The rule now covers the effect rather than one way of writing it.
            for pattern, detail in (
                (r"nuget\s+push", "pushes a package"),
                (r"api\.nuget\.org", "addresses the nuget.org publish endpoint"),
                (r"nuget\.org/api", "addresses the nuget.org publish endpoint"),
                (r"--api-key", "passes a publish credential"),
                (r"NUGET_API_KEY", "passes a publish credential"),
                (r"docker\s+push", "publishes a container image"),
                (r"ghcr\.io", "addresses a container registry"),
                (r"push-action", "uses a publishing action"),
                (r"^\s*push:\s*true", "enables publishing on an action"),
                (r"login-action", "authenticates against a registry"),
            ):
                if re.search(pattern, stripped, re.M):
                    self.fail("publication", f"{relative} {detail}: pattern '{pattern}'")

    def check_analyzer_release_tracking(self) -> None:
        for name in ("AnalyzerReleases.Shipped.md", "AnalyzerReleases.Unshipped.md"):
            body = self.read(f"src/ViciOne.ServiceBus.Analyzers/{name}")
            if body is None:
                self.fail("analyzer-tracking", f"{name} is missing")
                continue
            for line in body.lstrip("﻿").splitlines():
                if not line.strip():
                    continue
                if line.lstrip().startswith(";"):
                    continue  # the release tracking parser accepts ';' comments
                if not line.startswith("##"):
                    self.fail("analyzer-tracking",
                              f"{name} starts with '{line.strip()[:60]}' instead of a release header; "
                              "this is exactly what raises RS2007")
                break

    def check_no_hardcoded_broker_endpoint_in_tests(self) -> None:
        """A spec may not build its own broker endpoint from a fixed port.

        The fixture publishes an ephemeral loopback port per run, so a literal broker port in a spec
        cannot address it. It silently addresses whatever else listens there instead, and the spec
        then reports a result that says nothing about the fixture the runner started. Four ActiveMQ
        fixtures and one RabbitMQ fixture failed exactly that way before this rule existed.
        """
        for project in ("tests/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests",
                        "tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests",
                        "tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests",
                        "tests/ViciOne.ServiceBus.Tests"):
            directory = self.root / project
            if not directory.is_dir():
                continue
            for source in sorted(directory.rglob("*.cs")):
                if "/bin/" in source.as_posix() or "/obj/" in source.as_posix():
                    continue
                # RunScopedBroker and ArtemisBroker are the two places allowed to name a variable
                # that carries a port; they read it, they do not fix it.
                if source.name in ("RunScopedBroker.cs", "ArtemisBroker.cs", "RunScopedDatabase.cs"):
                    continue
                relative = source.relative_to(self.root).as_posix()
                for number, line in enumerate(source.read_text(encoding="utf-8-sig",
                                                              errors="replace").splitlines(), 1):
                    # A commented out line is prose about the old shape, not the shape itself. This is a
                    # C# file, so the marker is '//', which the shared comment stripper does not know.
                    if line.lstrip().startswith("//"):
                        continue
                    if KNOWN_DATABASE_SECRET in line:
                        self.fail("test-endpoint",
                                  f"{relative}:{number} carries the well known database secret "
                                  f"'{KNOWN_DATABASE_SECRET}'; the fixture provisions a run-scoped one")
                    for match in LOCAL_DATABASE_ENDPOINT.finditer(line):
                        self.fail("test-endpoint",
                                  f"{relative}:{number} hardcodes the local database host "
                                  f"'{match.group('host')}'; a spec must read the run-scoped endpoint")
                    for match in LOCAL_BROKER_ENDPOINT.finditer(line):
                        port = int(match.group("port"))
                        if port in BROKER_PORTS_NEVER_HARDCODED:
                            self.fail("test-endpoint",
                                      f"{relative}:{number} hardcodes broker endpoint "
                                      f"{match.group('host')}:{port}; a spec must read the run-scoped endpoint")

    def check_transport_operations_take_a_lease(self) -> None:
        """Every broker operation of the RabbitMQ contexts must hold its owner's lease.

        The ownership model is only worth as much as its least careful member. A single operation that
        calls the client directly can have the channel or connection disposed underneath it while it is
        still unwinding, and the broker's answer is then replaced by an ObjectDisposedException -- the
        defect the model exists to prevent. Four operations were outside it after the first pass and
        nothing noticed, because the rule lived in a review rather than in a gate. It lives here now, so
        an operation added later without a lease fails the run instead of being found by reading.

        Structural on purpose: what is checked is that each public operation body mentions a lease, not
        what it does with one. That cannot prove correct use, and it is not meant to -- the deterministic
        lifetime specs do that. It proves that no operation silently bypasses the owner.
        """
        contexts = {
            "src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/RabbitMqChannelContext.cs":
                {"NotifyFaulted", "DisposeAsync"},
            "src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/RabbitMqConnectionContext.cs":
                {"DisposeAsync"},
        }

        signature = re.compile(r"^        public (?:async )?(?:Task|ValueTask|void)(?:<[^>]+>)? (\w+)\(", re.M)
        # A call, not the word: 'NoLease()' contains 'Lease' and must not satisfy the rule.
        takes_lease = re.compile(r"\b(?:TryLease|Lease)\s*\(")

        for relative, exempt in contexts.items():
            source = self.read(relative)
            if source is None:
                # Absent in the validator's own fixtures, which build a minimal repository. A file that
                # really disappeared from the product is a build error long before this rule runs.
                continue

            lines = source.splitlines()
            starts = [(match.group(1), source[: match.start()].count("\n")) for match in signature.finditer(source)]
            if not starts:
                self.fail("transport-lease", f"{relative} exposes no operation, so this rule matched nothing")
                continue

            for index, (name, first) in enumerate(starts):
                last = starts[index + 1][1] if index + 1 < len(starts) else len(lines)
                body = "\n".join(lines[first:last])

                if name in exempt or takes_lease.search(body):
                    continue

                self.fail("transport-lease",
                          f"{relative}: {name} calls the broker without taking a lease, so the subject can be "
                          "disposed while it is still running")

    def check_required_runner_and_sdk(self) -> None:
        """The required profile runs on one operating system and on the SDK that was approved.

        A windows-latest leg claimed support this product does not have and does not want. A floating
        '10.0.x' resolves to whatever the runner image ships, which global.json then rejects after the
        fact; the version is pinned to the approved one instead.
        """
        approved = self.read(APPROVED_SDK_FILE)
        version = None
        if approved is None:
            self.fail("required-runner", f"{APPROVED_SDK_FILE} is missing, so no SDK is approved")
        else:
            match = re.search(r'"version"\s*:\s*"([^"]+)"', approved)
            if match is None:
                self.fail("required-runner", f"{APPROVED_SDK_FILE} names no SDK version")
            else:
                version = match.group(1)

        for relative in sorted(self.workflow_files()):
            body = self.read(relative)
            if body is None:
                continue
            stripped = strip_comments(body)

            for runner in re.findall(r"runs-on:\s*(\S+)", stripped):
                if runner != REQUIRED_RUNNER:
                    self.fail("required-runner",
                              f"{relative} runs a job on '{runner}'; this product builds and is supported "
                              f"on {REQUIRED_RUNNER} only")

            if re.search(r"windows|macos", stripped, re.I):
                self.fail("required-runner", f"{relative} still names a Windows or macOS runner")

            for declared in re.findall(r"dotnet-version:\s*'?\"?([^'\"\n]+)'?\"?", stripped):
                declared = declared.strip()
                if declared.startswith("${{"):
                    continue
                if version is not None and declared != version:
                    self.fail("required-runner",
                              f"{relative} asks for SDK '{declared}' while {APPROVED_SDK_FILE} approves "
                              f"'{version}'")

            for declared in re.findall(r"DOTNET_VERSION:\s*'?([^'\n]+)'?", stripped):
                declared = declared.strip().strip("'")
                if version is not None and declared != version:
                    self.fail("required-runner",
                              f"{relative} pins DOTNET_VERSION to '{declared}' while {APPROVED_SDK_FILE} "
                              f"approves '{version}'")

    def check_no_selector_without_a_job(self) -> None:
        """A selectable target has to have a job that runs it.

        A workflow that offers four targets and defines a job for one of them ends green for the other
        three on an inventory print alone. That is not a skipped gate, it is a gate that reports success
        without running.
        """
        for relative in sorted(self.workflow_files()):
            body = self.read(relative)
            if body is None:
                continue
            stripped = strip_comments(body)

            options = re.search(r"options:\s*\n((?:\s+-\s+\S+\n)+)", stripped)
            if options is None:
                continue

            targets = [line.strip().lstrip("- ").strip("'\"")
                       for line in options.group(1).splitlines() if line.strip()]
            for target in targets:
                if not re.search(rf"inputs\.target\s*==\s*['\"]{re.escape(target)}['\"]", stripped):
                    self.fail("selector-binding",
                              f"{relative} offers the target '{target}' but no job runs it, so selecting it "
                              "ends green without executing anything")

    def check_pack_depends_on_every_gate(self) -> None:
        """Pack may only run after every required gate, and may not resolve the graph on its own."""
        body = self.read(".github/workflows/build.yml")
        if body is None:
            return
        stripped = strip_comments(body)
        section = self.job_section(stripped, "pack")

        needs = re.search(r"needs:\s*\n((?:\s+-\s+\S+\n)+)", section)
        declared = ({line.strip().lstrip("- ").strip() for line in needs.group(1).splitlines() if line.strip()}
                    if needs else set())

        for gate in REQUIRED_CATEGORIES:
            if gate == "pack":
                continue
            if gate not in declared:
                self.fail("pack", f"pack does not depend on the required gate '{gate}'")

        if "--locked-mode" not in section:
            self.fail("pack", "the pack job does not restore in locked mode, so it resolves the graph unbound")
        if "--no-restore" not in section:
            self.fail("pack", "the pack job packs without --no-restore, so it restores a second time unbound")

    def check_verification_model(self) -> None:
        """The active capability truth has to hold before anything reads it."""
        for problem in verification_model.findings(self.root):
            self.fail("verification-model", problem)

    def workflow_files(self) -> list[str]:
        directory = self.root / ".github/workflows"
        if not directory.is_dir():
            return []
        return [p.relative_to(self.root).as_posix() for p in sorted(directory.glob("*.yml"))]

    def check_dueness_classes(self) -> None:
        """A case may not claim missing infrastructure that the category's own fixture provides.

        Nine cases were excluded as NOT_DUE_EXTERNAL_INFRASTRUCTURE while the pinned broker they need
        is exactly what the required profile starts. The class was the whole reason nobody looked at
        them again, so a category with a fixture may not use it. Every category in the matrix whose
        capability is verified against a pinned fixture is such a category.
        """
        inventory = self.read(VERIFICATION_MODEL)
        if inventory is None:
            return
        try:
            data = json.loads(inventory)
        except json.JSONDecodeError:
            return

        try:
            matrix = verification_model.load(self.root)
        except verification_model.ModelError:
            return

        with_a_fixture = {
            run["category"]
            for capability in matrix.get("capabilities", [])
            if capability.get("class") == "PINNED_FIXTURE_REQUIRED_RUN"
            for run in capability.get("runs", [])
            if run.get("category")
        }

        for run in sorted(verification_model.runs(data), key=lambda r: r.get("category") or ""):
            name = run.get("category")
            if name not in with_a_fixture:
                continue
            for case in run.get("notExecuted", []):
                if not isinstance(case, dict):
                    continue
                if case.get("dueness") == "NOT_DUE_EXTERNAL_INFRASTRUCTURE":
                    self.fail("dueness-class",
                              f"category '{name}' runs against a pinned fixture, so "
                              f"'{case.get('identity', '<unnamed>')}' may not be excluded as needing external "
                              "infrastructure")

    def check_executed_floor(self) -> None:
        """Every category that a required job runs records the count it must not fall below."""
        inventory = self.read(VERIFICATION_MODEL)
        if inventory is None:
            return
        try:
            data = json.loads(inventory)
        except json.JSONDecodeError:
            return

        for name, category in sorted(data.get("categories", {}).items()):
            floor = category.get("minimumExecutedCases")
            if not isinstance(floor, int) or floor <= 0:
                self.fail("executed-floor",
                          f"category '{name}' records no minimumExecutedCases, so a category that shrinks "
                          "would still report green")

    def check_no_raw_run_artifacts_in_evidence(self) -> None:
        """evidence/ is the durable record. A raw run artifact is not durable record material.

        Measured before this rule existed: 526 MiB of tracked evidence, 434 of it in 139 TRX files and
        23 collected broker logs, with 45 files that were a byte identical repeat of another, and not
        one outbox record binding a TRX. What a record binds and a reader reads is the compact category
        summary the runner writes beside the TRX; the TRX itself is reproducible by rerunning, and the
        digests of the removed ones are in evidence/RAW_RUN_ARTIFACT_MANIFEST.json.

        A run may of course still produce them - it has to, the counters are parsed out of them. They
        belong under artifacts/, which git ignores wholesale, not under the tree that is kept.
        """
        evidence = self.root / "evidence"
        if not evidence.is_dir():
            return

        for artifact in sorted(evidence.rglob("*")):
            if artifact.suffix in (".trx", ".log") and artifact.is_file():
                relative = artifact.relative_to(self.root).as_posix()
                self.fail("raw-artifact",
                          f"{relative} is a raw run artifact under evidence/; keep the category summary "
                          "and the digest instead, and write the run output under artifacts/")

    def check_every_tool_test_module_runs(self) -> None:
        """A test module nobody starts is an inventory entry, not a proof.

        This repository holds fifteen Python test modules under tools/. Exactly one of them was named
        by a required job; the other fourteen, 94 cases across the identity gates and the two CI
        runners, were green and never started by anything but a developer who remembered them. That is
        the same defect the not-executed inventory exists to prevent, one layer down.

        A job satisfies this either by naming the module path or by discovering its directory. The
        check reads the workflow text, so it proves that the command is written, not that it passed -
        the run itself proves that.
        """
        text = "\n".join((self.root / name).read_text(encoding="utf-8") for name in self.workflow_files())

        for module in sorted(self.root.glob("tools/**/test_*.py")):
            relative = module.relative_to(self.root).as_posix()
            directory = module.parent.relative_to(self.root).as_posix()
            if relative in text or f"unittest discover -s {directory}" in text:
                continue
            self.fail("tool-test-not-run",
                      f"{relative} is never started by a required job; name it or discover "
                      f"{directory}, or the module is an inventory entry rather than a proof")

    DOTNET_VERBS_THAT_NEED_A_TARGET = ("dotnet restore", "dotnet build", "dotnet pack")

    def check_every_dotnet_command_names_its_target(self) -> None:
        """A dotnet command in a required job has to say which solution it means.

        This repository holds two solutions at its root: the product one and the benchmark one. An
        unqualified 'dotnet restore --locked-mode' or 'dotnet build -c Release' does not fall back to
        one of them, it exits with MSB1011 and does nothing at all. The build job and the whole pack
        chain stood in the workflow that way, so both would have failed on the runner the first time
        anything triggered them.

        The rule is unconditional rather than conditional on the number of solutions: a job that names
        its target cannot become ambiguous by someone adding a second solution later.
        """
        for relative in self.workflow_files():
            workflow = self.read(relative)
            if workflow is None:
                continue
            for number, line in enumerate(workflow.splitlines(), 1):
                stripped = line.strip()
                if stripped.startswith("#"):
                    continue
                for verb in self.DOTNET_VERBS_THAT_NEED_A_TARGET:
                    if verb not in stripped:
                        continue
                    target = stripped.split(verb, 1)[1]
                    if any(token.endswith((".slnx", ".sln", ".csproj")) for token in target.split()):
                        continue
                    self.fail("dotnet-target",
                              f"{relative}:{number} runs '{verb}' without naming a solution or project; "
                              "this repository holds two solutions, so the command exits with MSB1011")

    def check_every_project_belongs_to_a_solution(self) -> None:
        """A project no solution references is not built, and a reference to nothing is not a project.

        Both directions, and the solution is read as the XML it is. A regular expression over
        Path="..." finds a reference inside a comment and misses one written with single quotes, which
        is not a gate, it is a guess that usually agrees.

        The first direction is not hypothetical: a stray test project reappeared twice, once when a
        disallowed history rewrite resurrected it and once when a routine `git add -A` picked the
        untracked file up again. Nothing referenced it and nothing built it. The second direction
        catches the opposite mistake - a project moved or removed while a solution still names it,
        which fails the build for everyone rather than silently.
        """
        referenced: dict[str, str] = {}

        for solution in sorted(self.root.glob("*.slnx")):
            name = solution.relative_to(self.root).as_posix()
            try:
                tree = ElementTree.parse(solution)
            except ElementTree.ParseError as error:
                self.fail("solution", f"{name} is not parsable as XML: {error}")
                continue

            for element in tree.getroot().iter("Project"):
                path = element.get("Path")
                if not path:
                    self.fail("solution", f"{name} carries a Project element without a Path")
                    continue

                relative = path.replace("\\", "/")
                if not (self.root / relative).is_file():
                    self.fail("solution", f"{name} references '{relative}', which is not a file")
                referenced[relative] = name

        for project in sorted(self.root.rglob("*.csproj")):
            relative = project.relative_to(self.root).as_posix()
            if relative.startswith("artifacts/"):
                continue
            if relative not in referenced:
                self.fail("orphan-project",
                          f"{relative} is referenced by no solution, so nothing builds it and nothing "
                          "verifies it")

    def check_central_build_targets_are_effective(self) -> None:
        """Directory.Build.targets has to assert something, not merely exist.

        A decorative file is worse than no file: it looks like a central contract and enforces
        nothing, so the next reader assumes the promises in it are held. What makes this one effective
        is that every gate raises an Error rather than a Warning - a warning is a note nobody reads in
        a repository that does not fail on them - and that each gate hangs off a real build target, so
        it is evaluated rather than only defined.
        """
        relative = "Directory.Build.targets"
        text = self.read(relative)
        if text is None:
            self.fail("build-targets", f"{relative} is missing, so the repository has no late central "
                                       "build contract at all")
            return

        try:
            root = ElementTree.fromstring(text)
        except ElementTree.ParseError as error:
            self.fail("build-targets", f"{relative} is not parsable as XML: {error}")
            return

        targets = [element for element in root if element.tag == "Target"]
        if not targets:
            self.fail("build-targets", f"{relative} declares no target, so it asserts nothing")
            return

        for target in targets:
            name = target.get("Name", "<unnamed>")
            if not (target.get("BeforeTargets") or target.get("AfterTargets") or target.get("DependsOnTargets")):
                self.fail("build-targets",
                          f"target '{name}' in {relative} hangs off no build target, so it is defined "
                          "and never evaluated")
            if not [child for child in target if child.tag == "Error"]:
                self.fail("build-targets",
                          f"target '{name}' in {relative} raises no Error; a warning in a central "
                          "contract is a note nobody reads")

    def check_no_project_leaves_the_central_contract(self) -> None:
        """A project may not take itself out of the repository's build contract.

        Three ways exist and each was open. ImportDirectoryBuildTargets=false skips the late gates
        entirely; RestoreLockedMode=false inside a project resolves past its own lock file while
        keeping it; and pointing CustomBeforeMicrosoftCommonTargets or DirectoryBuildTargetsPath
        somewhere else replaces the contract with another file. The documented package update passes
        its property on the command line, where the whole run and the diff of the change see it.
        """
        escapes = (
            ("ImportDirectoryBuildTargets", "false", "skips the late central build gates"),
            ("RestoreLockedMode", "false", "resolves past its own lock file"),
        )

        for project in sorted(self.root.rglob("*.csproj")):
            relative = project.relative_to(self.root).as_posix()
            if relative.startswith("artifacts/"):
                continue

            text = project.read_text(encoding="utf-8-sig", errors="replace")
            for name, value, effect in escapes:
                if f"<{name}>{value}</{name}>" in text.replace(" ", ""):
                    self.fail("central-contract",
                              f"{relative} sets {name} to {value}, which {effect}. That belongs on the "
                              "command line of the one documented call, not into a project")

            for redirect in ("DirectoryBuildTargetsPath", "CustomBeforeMicrosoftCommonTargets",
                             "CustomAfterMicrosoftCommonTargets"):
                if f"<{redirect}>" in text:
                    self.fail("central-contract",
                              f"{relative} sets {redirect}, which points the central build path at "
                              "something other than the repository's own contract")

    def check_workflow_inputs_are_pinned(self) -> None:
        """Nothing a required run consumes may be a moving reference.

        A tag and a runner label are both names somebody else can repoint. The SDK is pinned, the
        images are pinned by digest, and an action pinned to v5 would have been the one input left
        where a change outside this repository changes what runs inside it. ubuntu-latest is the same
        thing for the machine underneath: it moves to the next LTS when GitHub decides, and the build
        that meets it is not the build that was verified.
        """
        for relative in self.workflow_files():
            workflow = self.read(relative)
            if workflow is None:
                continue

            for number, line in enumerate(workflow.splitlines(), 1):
                stripped = line.strip()
                if stripped.startswith("#"):
                    continue

                if stripped.startswith("- uses:") or stripped.startswith("uses:"):
                    reference = stripped.split("uses:", 1)[1].strip().split()[0]
                    if "@" not in reference or not re.fullmatch(r"[0-9a-f]{40}", reference.split("@", 1)[1]):
                        self.fail("moving-reference",
                                  f"{relative}:{number} uses '{reference}', which is a tag rather than a "
                                  "commit; whoever can move that tag decides what runs here")

                if stripped.startswith("runs-on:") and stripped.endswith("-latest"):
                    self.fail("moving-reference",
                              f"{relative}:{number} runs on '{stripped.split(':', 1)[1].strip()}', which "
                              "moves when GitHub repoints it; name the image this repository verified against")

    def check_restore_lock_files(self) -> None:
        """Every project resolves against a tracked lock file.

        Locked mode alone does not carry this. Measured: a restore with --locked-mode and no lock file
        present writes one and succeeds, because RestorePackagesWithLockFile is on. A deleted lock file
        would therefore reopen the package graph without anything reporting it, which is precisely what
        the lock files exist to prevent, so the presence of the file is checked here, before any
        restore runs.
        """
        # No vacuity guard here: this rule runs against synthetic trees in its own tests as well, and a
        # tree without projects is not a policy violation. That the real repository has projects at all,
        # and a lock file for every one of them, is asserted in tools/ci/test_locked_restore.py.
        for project in sorted(self.root.rglob("*.csproj")):
            relative = project.relative_to(self.root)
            if relative.parts[0] in {"artifacts", "obj", "bin"}:
                continue

            lock_file = project.parent / "packages.lock.json"
            if not lock_file.is_file():
                self.fail("restore-lock", f"{relative.as_posix()} has no packages.lock.json, so its restore is not bound")

    def check_restore_sources(self) -> None:
        """The repository names its own restore source, so a restore cannot inherit machine state.

        Order matters: <clear /> has to stand before the source it keeps, otherwise it wipes it again.
        The mapping has to claim every pattern, because an unclaimed pattern is exactly the case
        NU1507 warns about and the case where a package could arrive from somewhere unnamed.
        """
        body = self.read(RESTORE_CONFIG)
        if body is None:
            self.fail("restore-sources", f"{RESTORE_CONFIG} is missing, so a restore inherits the machine's sources")
            return

        try:
            root = ElementTree.fromstring(body)
        except ElementTree.ParseError as error:
            self.fail("restore-sources", f"{RESTORE_CONFIG} is not parsable: {error}")
            return

        sources = root.find("packageSources")
        if sources is None:
            self.fail("restore-sources", f"{RESTORE_CONFIG} declares no packageSources")
            return

        children = list(sources)
        if not any(child.tag == "clear" for child in children):
            self.fail("restore-sources", "packageSources does not clear the inherited sources")
        else:
            first_add = next((index for index, child in enumerate(children) if child.tag == "add"), len(children))
            last_clear = max(index for index, child in enumerate(children) if child.tag == "clear")
            if last_clear > first_add:
                self.fail("restore-sources", "a clear stands after the source it should keep, which removes it again")

        added = [child for child in children if child.tag == "add"]
        if len(added) != 1:
            self.fail("restore-sources",
                      f"packageSources declares {len(added)} sources; exactly one is allowed")
        else:
            key, value = added[0].get("key"), added[0].get("value")
            if key != REQUIRED_SOURCE_KEY or value != REQUIRED_SOURCE_VALUE:
                self.fail("restore-sources", f"the only source must be {REQUIRED_SOURCE_KEY} at "
                                             f"{REQUIRED_SOURCE_VALUE}, found '{key}' at '{value}'")

        mapping = root.find("packageSourceMapping")
        if mapping is None:
            self.fail("restore-sources", "no packageSourceMapping, so central package management cannot "
                                         "decide which source a package comes from")
        else:
            mapped = {entry.get("key"): [package.get("pattern") for package in entry.findall("package")]
                      for entry in mapping.findall("packageSource")}
            if set(mapped) != {REQUIRED_SOURCE_KEY}:
                self.fail("restore-sources",
                          f"the mapping must name exactly {REQUIRED_SOURCE_KEY}, found {sorted(mapped)}")
            elif "*" not in mapped[REQUIRED_SOURCE_KEY]:
                self.fail("restore-sources", "the mapping does not claim the pattern *, so some package "
                                             "pattern stays unmapped")

        # Clearing the inherited sources is only half of the host independence. A machine level
        # disabledPackageSources entry can switch off the one source that is left, and the restore then
        # has none at all. The file states that it clears them; the gate has to hold that statement.
        disabled = root.findall("disabledPackageSources")
        if len(disabled) != 1:
            self.fail("restore-sources",
                      f"{RESTORE_CONFIG} declares {len(disabled)} disabledPackageSources sections; "
                      "exactly one is required so a host entry cannot disable the only source")
        else:
            children = list(disabled[0])
            if not any(child.tag == "clear" for child in children):
                self.fail("restore-sources", "disabledPackageSources does not clear what the host disabled")
            else:
                first_clear = next(index for index, child in enumerate(children) if child.tag == "clear")
                if any(child.tag != "clear" for child in children[:first_clear]):
                    self.fail("restore-sources",
                              "an operation stands before the clear in disabledPackageSources, so it "
                              "survives the clear")
            for child in children:
                if child.tag == "add" and child.get("key") == REQUIRED_SOURCE_KEY:
                    self.fail("restore-sources",
                              f"disabledPackageSources disables {REQUIRED_SOURCE_KEY}, which is the only "
                              "source the restore has")

        if root.find(CREDENTIAL_ELEMENT) is not None:
            self.fail("restore-sources", f"{RESTORE_CONFIG} carries {CREDENTIAL_ELEMENT}; the single source "
                                         "is public and no credential belongs in the repository")
        for element in root.iter("add"):
            if element.get("key") in CREDENTIAL_KEYS:
                self.fail("restore-sources", f"{RESTORE_CONFIG} carries a '{element.get('key')}' entry")

    def run(self) -> int:
        for rule in (self.check_no_forbidden_images, self.check_pinning, self.check_host_binding,
                     self.check_credentials, self.check_canonical_runner,
                     self.check_no_effective_known_credentials, self.check_no_required_test_masking,
                     self.check_no_silent_test_exclusion, self.check_run_scoped_credential_guard,
                     self.check_no_hardcoded_broker_endpoint_in_tests,
                     self.check_required_profile, self.check_pack,
                     self.check_no_publication, self.check_analyzer_release_tracking,
                     self.check_transport_operations_take_a_lease,
                     self.check_restore_sources, self.check_restore_lock_files,
                     self.check_required_runner_and_sdk, self.check_no_selector_without_a_job,
                     self.check_pack_depends_on_every_gate, self.check_verification_model,
                     self.check_dueness_classes, self.check_executed_floor,
                     self.check_no_raw_run_artifacts_in_evidence,
                     self.check_every_tool_test_module_runs,
                     self.check_every_dotnet_command_names_its_target,
                     self.check_every_project_belongs_to_a_solution,
                     self.check_central_build_targets_are_effective,
                     self.check_workflow_inputs_are_pinned,
                     self.check_workflow_steps_are_the_commands_they_look_like,
                     self.check_no_project_leaves_the_central_contract):
            rule()

        if self.failures:
            for failure in self.failures:
                print(f"FAIL ci-policy {failure}", file=sys.stderr)
            return 1

        print("PASS ci-policy all invariants hold")
        return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2],
                        help="Repository root to validate.")
    args = parser.parse_args(argv)
    return Policy(args.root).run()


if __name__ == "__main__":
    raise SystemExit(main())
