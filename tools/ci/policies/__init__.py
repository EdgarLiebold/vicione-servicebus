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
import subprocess
import shlex
import sys
import textwrap
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import run_broker_category  # noqa: E402  (repository local, resolved from this file's folder)
import verify  # noqa: E402
import run_test_category  # noqa: E402
from verification import model as verification_model  # noqa: E402
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

# The one command a required job runs. Everything that decides what it verifies lives in the model.
CANONICAL_ENTRY_POINT = "tools/ci/verify.py"

# The exact shape of that call. A required step is compared against this rather than parsed, which is
# why no shell program has to be understood: echo, a wrapper, chaining, a pipe, a redirection, a
# command substitution and '|| true' all produce something that is not this vector.
CANONICAL_INVOCATION = ("python3", CANONICAL_ENTRY_POINT, "--selection")

# Jobs of the required profile that verify nothing and are therefore not in the model's job map.
NON_VERIFYING_JOBS = ("policy", "build", "pack")

# The only projects that may leave the product's net10.0 target, by exact repository relative path.
# Two are loaded by the compiler; the third compiles nothing and its framework is the consumer surface
# of the analyzer package. Directory.Build.targets grants the exception by the same three paths.
ROSLYN_COMPONENT_PROJECTS = (
    "src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj",
    "src/ViciOne.ServiceBus.Analyzers.CodeFixes/ViciOne.ServiceBus.Analyzers.CodeFixes.csproj",
)
ANALYZER_PACKAGE_PROJECT = "src/ViciOne.ServiceBus.Analyzers.Package/ViciOne.ServiceBus.Analyzers.Package.csproj"

# Retired: a project cannot grant itself a framework exception, so nobody may carry this any more.
RETIRED_SELF_MARKERS = ("vicioneanalyzerpackagesurface", "vicionecompilerhost")

# Where the expected identity sets live. One directory, one file per category.
EXPECTED_IDENTITY_DIRECTORY = "build/verification/expected"
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


# The two files that own the central build contract. Everything else in the repository is bound by it.
CENTRAL_BUILD_FILES = ("Directory.Build.props", "Directory.Build.targets")

# Properties that decide whether the central contract applies at all, with what writing one does.
RESERVED_CENTRAL_PROPERTIES = {
    "importdirectorybuildtargets": "Turning the import off skips the late central build gates entirely.",
    "restorelockedmode": "Setting it in a file resolves past a lock file the project still carries.",
    "restorelockedmodefromcommandline": "It is the flag the lock gate reads to tell the documented "
                                        "update apart from an escape, so a project that sets it hands "
                                        "itself the exception.",
    "directorybuildtargetspath": "It points the late contract at another file.",
    "custombeforemicrosoftcommontargets": "It injects a file ahead of the central contract.",
    "customaftermicrosoftcommontargets": "It injects a file behind the central contract.",
}


def local_name(tag: object) -> str:
    """An MSBuild element name without its namespace and without case.

    Old style project files carry the 2003 MSBuild namespace and SDK style ones carry none, so the two
    spell the same element differently. Comments and processing instructions have no string tag at all.
    """
    if not isinstance(tag, str):
        return ""

    return tag.rpartition("}")[2].lower()


def msbuild_properties(root: ElementTree.Element) -> list[tuple[str, str]]:
    """(name, value) of every property this file declares, wherever it declares it.

    Any element inside a PropertyGroup is a property, and a PropertyGroup stands wherever MSBuild
    allows one: under the project, inside a Choose/When, inside a Target. A reader that only looked at
    the top level would miss exactly the places a property is put to avoid being seen.
    """
    declared = []
    for group in root.iter():
        if local_name(group.tag) != "propertygroup":
            continue
        for element in group:
            name = local_name(element.tag)
            if name:
                declared.append((name, (element.text or "").strip()))

    return declared


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



def joined_lines(script: str) -> list[str]:
    """The logical lines of a step, with its continuations joined the way a shell joins them."""
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

    return joined



# What turns a failure into a success without changing anything else about a step.
OUTCOME_MASKS = (("||", "true"), ("||", ":"), ("||", "exit"))


def masked_outcomes(script: str) -> list[str]:
    """Every place in this step where a failure is turned into a success.

    Read with the operators kept, because the masking is the pair - the operator and what follows it -
    rather than either half on its own. The build and pack jobs chain commands legitimately, so an
    operator by itself says nothing.
    """
    found = []
    for logical in joined_lines(script):
        lexer = shlex.shlex(logical, posix=True, punctuation_chars=True)
        lexer.whitespace_split = True
        try:
            tokens = list(lexer)
        except ValueError:
            continue
        for index, token in enumerate(tokens[:-1]):
            if (token, tokens[index + 1]) in OUTCOME_MASKS:
                tail = " ".join(tokens[index:index + 3])
                found.append(tail if tokens[index + 1] == "exit" else " ".join(tokens[index:index + 2]))

    return found


def effective_commands(script: str) -> list[list[str]]:
    """The commands a shell really builds from one step, with continuations joined and quotes honoured.

    This is the whole point of the rule below. A step is text until the shell has joined its
    continuation lines, and a rule that looks for expected substrings agrees with a step whose
    continuation is missing exactly as readily as with one that works.
    """
    commands: list[list[str]] = []
    for logical in joined_lines(script):
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


class PolicyBase:

    DOTNET_VERBS_THAT_NEED_A_TARGET = ("dotnet restore", "dotnet build", "dotnet pack")


    def __init__(self, root: Path) -> None:
        self.root = root
        self.failures: list[str] = []

    def fail(self, rule: str, detail: str) -> None:
        self.failures.append(f"{rule}: {detail}")

    def read(self, relative: str) -> str | None:
        path = self.root / relative
        return path.read_text(encoding="utf-8") if path.is_file() else None

    def tracked(self, relative: str) -> bool:
        """Whether this path is part of the repository, as git answers it.

        A tree that is not a repository cannot answer the question, and this reports True there rather
        than inventing a failure: the fixtures of this validator's own suite are plain directories, and
        a rule that failed on all of them would be about the fixture and not about the repository. The
        case that proves this control runs against a real repository for that reason.
        """
        if not (self.root / ".git").exists():
            return True
        answer = subprocess.run(["git", "-C", str(self.root), "ls-files", "--error-unmatch", relative],
                                capture_output=True, text=True, check=False)

        return answer.returncode == 0

    @staticmethod
    def job_section(body: str, job: str) -> str:
        """The body of one workflow job, cut at the next job rather than the next indented line."""
        marker = f"\n  {job}:"
        if marker not in body:
            return ""
        rest = body.split(marker, 1)[1]
        following = re.search(r"\n  [A-Za-z][\w-]*:\s*$", rest, re.M)
        return rest[: following.start()] if following else rest

    def workflow_files(self) -> list[str]:
        directory = self.root / ".github/workflows"
        if not directory.is_dir():
            return []
        return [p.relative_to(self.root).as_posix() for p in sorted(directory.glob("*.yml"))]

    def msbuild_properties_of(self, relative: str) -> dict[str, str] | None:
        """The evaluated properties of one project, or None when there is no project to read."""
        path = self.root / relative
        if not path.is_file():
            return None
        try:
            root = ElementTree.fromstring(path.read_text(encoding="utf-8-sig", errors="replace"))
        except ElementTree.ParseError:
            return None

        return dict(msbuild_properties(root))

    def build_files(self) -> list[Path]:
        """Every project and every project-local props or targets file of this repository.

        The two root files are excluded because they are the contract; everything else is bound by it,
        including a Directory.Build.props further down the tree, which is exactly the file somebody
        would add to change the contract for one folder.
        """
        found = []
        for pattern in ("*.csproj", "*.props", "*.targets"):
            for path in self.root.rglob(pattern):
                relative = path.relative_to(self.root).as_posix()
                if relative.startswith("artifacts/") or "/bin/" in relative or "/obj/" in relative:
                    continue
                if relative in CENTRAL_BUILD_FILES:
                    continue
                found.append(path)

        return found
