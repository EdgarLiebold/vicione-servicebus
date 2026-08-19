#!/usr/bin/env python3
"""The pinned fixture, its images, its ports and its credentials.

A digest rather than a tag, a loopback binding rather than every interface, a run
scoped account rather than a well known one, and no endpoint written into a test.

Standard library only.
"""

from __future__ import annotations

import json
import re

from policies import (BARE_PORT, BROKER_PORTS_NEVER_HARDCODED, DIGEST, EPHEMERAL_LOOPBACK_PORT,
                      FIXED_LOOPBACK_PORT, FORBIDDEN_IMAGES, KNOWN_CREDENTIALS,
                      KNOWN_DATABASE_SECRET, LOCAL_BROKER_ENDPOINT, LOCAL_DATABASE_ENDPOINT,
                      PolicyBase, RUN_SCOPED_GUARD_FILE, RUN_SCOPED_GUARD_METHOD,
                      RUN_SCOPED_GUARD_VARIABLES, SHA256, WILDCARD_PORT, strip_comments)


class FixturesPolicy(PolicyBase):
    """The pinned fixture, its images, its ports and its credentials."""

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
