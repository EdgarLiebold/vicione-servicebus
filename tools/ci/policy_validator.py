#!/usr/bin/env python3
"""Repository-local CI policy validator.

ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-08.

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
LOCAL_BROKER_ENDPOINT = re.compile(
    r"(?:amqp|activemq|rabbitmq|tcp)://(?P<host>localhost|127\.0\.0\.1|0\.0\.0\.0|\[::1\]):(?P<port>\d{2,5})")


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
            rest = body.split(marker, 1)[1]
            following = re.search(r"\n  [A-Za-z][\w-]*:\s*$", rest, re.M)
            section = rest[: following.start()] if following else rest
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
        """The required path may not filter, skip or ignore its way to green."""
        workflow = self.read(".github/workflows/build.yml")
        if workflow is None:
            return
        body = strip_comments(workflow)
        for pattern, detail in (
            (r"--filter\s+\"?Category\s*!=", "the required path filters a test category"),
            (r"--filter\s+\"?TestCategory\s*!=", "the required path filters a test category"),
            (r"--blame-hang", "the required path masks hangs instead of failing"),
            (r"VSTEST_.*SKIP", "the required path sets a skip switch"),
        ):
            if re.search(pattern, body):
                self.fail("test-masking", f"{detail}: pattern '{pattern}'")

    def check_required_profile(self) -> None:
        workflow = self.read(".github/workflows/build.yml")
        if workflow is None:
            self.fail("required-profile", ".github/workflows/build.yml is missing")
            return
        body = strip_comments(workflow)
        for category in REQUIRED_CATEGORIES:
            if f"\n  {category}:" not in body:
                self.fail("required-profile", f"required category '{category}' is absent from the required profile")
        for guard in ("refs/heads/master", "refs/heads/develop", "ViciOne.ServiceBus/ViciOne.ServiceBus"):
            if guard in body:
                self.fail("required-profile", f"an upstream repository or branch guard returned: {guard}")
        if "run_test_category.py" not in body:
            self.fail("required-profile", "required categories do not go through the test count gate")

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
            if "nuget push" in stripped or "api.nuget.org" in stripped:
                self.fail("publication", f"{relative} pushes to nuget.org")
            if "docker push" in stripped or "ghcr.io" in stripped:
                self.fail("publication", f"{relative} publishes a container image")

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

    def run(self) -> int:
        for rule in (self.check_no_forbidden_images, self.check_pinning, self.check_host_binding,
                     self.check_credentials, self.check_canonical_runner,
                     self.check_no_effective_known_credentials, self.check_no_required_test_masking,
                     self.check_no_hardcoded_broker_endpoint_in_tests,
                     self.check_required_profile, self.check_pack,
                     self.check_no_publication, self.check_analyzer_release_tracking):
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
