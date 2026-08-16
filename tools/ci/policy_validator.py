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
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

FORBIDDEN_IMAGES = ("vicione-servicebus/rabbitmq", "vicione-servicebus/activemq")
REQUIRED_CATEGORIES = ("build", "analyzer", "core-unit", "rabbitmq", "entity-framework", "pack")
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
NOT_EXECUTED_INVENTORY = "build/test-infrastructure/not-executed-inventory.json"

# The guard that keeps the historic localhost/guest defaults out of the required broker run.
RUN_SCOPED_GUARD_FILE = "tests/ViciOne.ServiceBus.RabbitMqTransport.Tests/RabbitMqTestSetUpFixture.cs"
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
        for relative in ("build/test-infrastructure/compose.yaml",
                         ".github/workflows/build.yml",
                         ".github/workflows/extended-transports.yml"):
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
        inventory = self.read(NOT_EXECUTED_INVENTORY)
        if inventory is None:
            self.fail("test-exclusion", f"{NOT_EXECUTED_INVENTORY} is missing; "
                                        "the required categories would have no binding list of what they skip")
            return
        try:
            data = json.loads(inventory)
        except json.JSONDecodeError as error:
            self.fail("test-exclusion", f"{NOT_EXECUTED_INVENTORY} is not readable: {error}")
            return

        categories = data.get("categories")
        if not isinstance(categories, dict) or not categories:
            self.fail("test-exclusion", f"{NOT_EXECUTED_INVENTORY} names no category")
            return

        for name, category in sorted(categories.items()):
            project = str(category.get("project", ""))
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
                          f"{explicit}; a new exclusion has to be classified in {NOT_EXECUTED_INVENTORY}")

            cases = category.get("cases")
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

    def check_no_publication(self) -> None:
        for relative in (".github/workflows/build.yml", ".github/workflows/extended-transports.yml",
                         ".github/workflows/nightly-transports.yml"):
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
        for project in ("tests/ViciOne.ServiceBus.ActiveMqTransport.Tests",
                        "tests/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests",
                        "tests/ViciOne.ServiceBus.RabbitMqTransport.Tests",
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

    def run(self) -> int:
        for rule in (self.check_no_forbidden_images, self.check_pinning, self.check_host_binding,
                     self.check_credentials, self.check_canonical_runner,
                     self.check_no_effective_known_credentials, self.check_no_required_test_masking,
                     self.check_no_silent_test_exclusion, self.check_run_scoped_credential_guard,
                     self.check_no_hardcoded_broker_endpoint_in_tests,
                     self.check_required_profile, self.check_pack,
                     self.check_no_publication, self.check_analyzer_release_tracking,
                     self.check_transport_operations_take_a_lease):
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
